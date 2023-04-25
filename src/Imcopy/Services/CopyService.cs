using Imcopy.Configuration;
using System.CommandLine;
using System.Diagnostics;
using System.Collections.Concurrent;
using DotNet.Globbing;

namespace Imcopy.Services;

public record FileItem(
    string FileName,
    string SourceDirectory,
    IEnumerable<string> DestinationDirectories,
    OverwriteBehavior? OverwriteBehavior
);

public class CopyService
{
    public const int DefaultParallelism = 8;
    public const OverwriteBehavior DefaultOverwriteBehavior = OverwriteBehavior.IfNewer;

    private readonly IConsole console;

    public CopyService(IConsole console)
    {
        this.console = console;
    }

    public async Task CopyAsync(ImcopyConfiguration configuration)
    {
        if (configuration.Parallelism.HasValue && (configuration.Parallelism.Value == 0 || configuration.Parallelism.Value == 1))
        {
            CopyDirectories(configuration.Directories, configuration.IgnorePatterns);
            return;
        }

        await CopyDirectoriesAsync(configuration);
    }

    private async Task CopyDirectoriesAsync(ImcopyConfiguration configuration)
    {
        var foundFiles = GetFiles(configuration.Directories, configuration.IgnorePatterns);
        var files = new ConcurrentStack<FileItem>(foundFiles);

        var filesCount = files.Count;
        var filesCopied = 0;

        var parallelism = configuration.Parallelism.HasValue ? configuration.Parallelism.Value : DefaultParallelism;
        using var concurrencySemaphore = new SemaphoreSlim(parallelism);

        var lockObject = new object();
        var tasks = new List<Task>(filesCount);

        var stopwatch = Stopwatch.StartNew();
        while (!files.IsEmpty)
        {
            await concurrencySemaphore.WaitAsync();
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    if (files.TryPop(out var file))
                    {
                        CopyFile(file);
                        var newFilesCopied = Interlocked.Increment(ref filesCopied);
                        lock (lockObject)
                        {
                            DisplayProgressBar(newFilesCopied, filesCount);
                        }
                    }
                }
                finally
                {
                    concurrencySemaphore.Release();
                }
            }));
        }
        await Task.WhenAll(tasks);
        stopwatch.Stop();

        DisplaySummary(filesCount, stopwatch);
    }

    private void CopyDirectories(IEnumerable<DirectoryConfiguration> directories, IEnumerable<IgnorePatternConfiguration>? ignorePatterns)
    {
        var foundFiles = GetFiles(directories, ignorePatterns);
        var files = new Stack<FileItem>(foundFiles);

        var filesCount = files.Count;
        var filesCopied = 0;

        var stopwatch = Stopwatch.StartNew();
        while (files.Count > 0)
        {
            var file = files.Pop();
            CopyFile(file);
            filesCopied++;
            DisplayProgressBar(filesCopied, filesCount);
        }
        stopwatch.Stop();

        DisplaySummary(filesCount, stopwatch);
    }

    private static void CopyFile(FileItem file)
    {
        var sourceFile = Path.Combine(file.SourceDirectory, file.FileName);
        foreach (var destinationDirectory in file.DestinationDirectories)
        {
            if (!Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }
            var destinationFile = Path.Combine(destinationDirectory, file.FileName);
            var overwriteBehavior = file.OverwriteBehavior ?? DefaultOverwriteBehavior;
            if ((overwriteBehavior == OverwriteBehavior.IfNewer || overwriteBehavior == OverwriteBehavior.Never) && File.Exists(destinationFile))
            {
                if (overwriteBehavior == OverwriteBehavior.Never)
                {
                    continue;
                }

                var sourceFileInfo = new FileInfo(sourceFile);
                var destinationFileInfo = new FileInfo(destinationFile);
                if (sourceFileInfo.LastWriteTimeUtc <= destinationFileInfo.LastWriteTimeUtc)
                {
                    continue;
                }
            }
            File.Copy(sourceFile, destinationFile, overwrite: true);
        }
    }

    private IEnumerable<FileItem> GetFiles(IEnumerable<DirectoryConfiguration> directories, IEnumerable<IgnorePatternConfiguration>? ignorePatterns)
    {
        var files = new List<FileItem>();
        foreach (var directory in directories)
        {
            var excludeGlobs = new List<Glob>();
            if (!string.IsNullOrEmpty(directory.IgnorePattern) && ignorePatterns is not null)
            {
                var ignorePattern = ignorePatterns.First(p => p.Name.Equals(directory.IgnorePattern, StringComparison.OrdinalIgnoreCase));
                foreach (var pattern in ignorePattern.Patterns)
                {
                    var modifiedPattern = pattern;
                    if (!pattern.StartsWith(Path.DirectorySeparatorChar) && !pattern.StartsWith(Path.AltDirectorySeparatorChar) && !pattern.StartsWith("**"))
                    {
                        modifiedPattern = $"**/{pattern}";
                    }
                    excludeGlobs.Add(Glob.Parse(modifiedPattern));
                }
            }

            var dirInfo = new DirectoryInfo(directory.Source);
            var fileInfos = dirInfo.EnumerateFiles("*", SearchOption.AllDirectories);

            var directoryFiles = fileInfos
                .Where(f =>
                {
                    var sourcePath = directory.Source;
                    if (sourcePath.EndsWith(Path.DirectorySeparatorChar) || sourcePath.EndsWith(Path.AltDirectorySeparatorChar))
                    {
                        sourcePath = sourcePath[..^1];
                    }
                    var relativeFilePath = f.FullName[sourcePath.Length..];
                    return !excludeGlobs.Any(g => g.IsMatch(relativeFilePath));
                })
                .Select(f =>
                {
                    var relativePath = GetRelativePath(directory.Source, f.DirectoryName);
                    var sourceDirectory = Path.Combine(directory.Source, relativePath);
                    var destinationDirectories = directory.Destinations.Select(d => Path.Combine(d, relativePath)).ToArray();
                    var fileName = f.Name;
                    return new FileItem(fileName, sourceDirectory, destinationDirectories, directory.OverwriteBehavior);
                });

            files.AddRange(directoryFiles);
        }

        return files;
    }

    private static string GetRelativePath(string rootDirectory, string? fullPath)
    {
        var relativeDirectory = string.IsNullOrEmpty(fullPath) ? "" : fullPath[rootDirectory.Length..];
        if (relativeDirectory.StartsWith(Path.DirectorySeparatorChar) || relativeDirectory.StartsWith(Path.AltDirectorySeparatorChar))
        {
            relativeDirectory = relativeDirectory[1..];
        }
        return relativeDirectory;
    }

    private void DisplayProgressBar(int currentValue, int maxValue, int barSize = 50)
    {
        var progressFraction = (double)currentValue / maxValue;
        var filledBars = (int)(progressFraction * barSize);
        var emptyBars = barSize - filledBars;

        console.Write("\r[");
        console.Write(new string('#', filledBars));
        console.Write(new string(' ', emptyBars));
        console.Write($"] {progressFraction:P0}");
    }
    private void DisplaySummary(int filesCount, Stopwatch stopwatch)
    {
        console.WriteLine($"{Environment.NewLine}{filesCount} files copied in {stopwatch.Elapsed}");
    }
}
