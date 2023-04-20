using Micopy.Configuration;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Microsoft.Extensions.FileSystemGlobbing;
using System.CommandLine;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace Micopy.Services;

public record FileItem(
    string FileName,
    string SourceFolder,
    string DestinationFolder
);

public class CopyService
{
    public const int DefaultParallelism = 8;

    private readonly IConsole console;

    public CopyService(IConsole console)
    {
        this.console = console;
    }

    public async Task CopyAsync(MicopyConfiguration configuration)
    {
        if (configuration.Parallelism.HasValue && (configuration.Parallelism.Value == 0 || configuration.Parallelism.Value == 1))
        {
            CopyDirectories(configuration.Folders, configuration.IgnorePatterns);
            return;
        }

        await CopyDirectoriesAsync(configuration);
    }

    private async Task CopyDirectoriesAsync(MicopyConfiguration configuration)
    {
        var foundFiles = GetFiles(configuration.Folders, configuration.IgnorePatterns);
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

    private void CopyDirectories(IEnumerable<FolderConfiguration> folders, IEnumerable<IgnorePatternConfiguration>? ignorePatterns)
    {
        var foundFiles = GetFiles(folders, ignorePatterns);
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
        if (!Directory.Exists(file.DestinationFolder))
        {
            Directory.CreateDirectory(file.DestinationFolder);
        }

        var sourceFile = Path.Combine(file.SourceFolder, file.FileName);
        var destinationFile = Path.Combine(file.DestinationFolder, file.FileName);
        File.Copy(sourceFile, destinationFile, overwrite: true);
    }

    private IEnumerable<FileItem> GetFiles(IEnumerable<FolderConfiguration> folders, IEnumerable<IgnorePatternConfiguration>? ignorePatterns)
    {
        var files = new List<FileItem>();
        foreach (var folder in folders)
        {
            var matcher = new Matcher();
            matcher.AddInclude("**/*");  //**
            if (!string.IsNullOrEmpty(folder.IgnorePatternName) && ignorePatterns is not null)
            {
                var ignorePattern = ignorePatterns.First(p => p.Name.Equals(folder.IgnorePatternName, StringComparison.OrdinalIgnoreCase));
                foreach (var pattern in ignorePattern.Patterns)
                {
                    matcher.AddExclude(pattern);
                }
            }

            var dirInfo = new DirectoryInfo(folder.Source);
            var directoryWrapper = new DirectoryInfoWrapper(dirInfo);
            var result = matcher.Execute(directoryWrapper);

            var directoryFiles = result.Files.Select(file => {
                var relativeFolder = Path.GetDirectoryName(file.Path) ?? "";
                var sourceFolder = Path.Combine(folder.Source, relativeFolder);
                var destinationFolder = Path.Combine(folder.Destination, relativeFolder);
                var fileName = Path.GetFileName(file.Path);
                return new FileItem(fileName, sourceFolder, destinationFolder);
            });

            files.AddRange(directoryFiles);
        }

        return files;
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
