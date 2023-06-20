using Imcopy.Configuration;
using System.CommandLine;
using System.Diagnostics;
using System.Collections.Concurrent;
using DotNet.Globbing;
using Imcopy.Reporters;

namespace Imcopy.Services;

public record FileItem(
    string FileName,
    string SourceFileFullName,
    IEnumerable<string> DestinationDirectories,
    OverwriteBehavior? OverwriteBehavior
);

public record DestinationFileItem(
    string SourceFileFullPath,
    string DestinationFileFullName,
    string DestinationDirectoryName
);


public class CopyService
{
    public const int DefaultParallelism = 8;
    public const OverwriteBehavior DefaultOverwriteBehavior = OverwriteBehavior.IfNewer;
    public const RemoveBehavior DefaultRemoveBehavior = RemoveBehavior.Remove;

    private readonly IConsole console;

    public CopyService(IConsole console)
    {
        this.console = console;
    }

    public async Task CopyAsync(ImcopyConfiguration configuration)
    {
        DisplayScanningForCopying();

        var copyReporter = new CopyReporter();
        if (configuration.Parallelism.HasValue && (configuration.Parallelism.Value == 0 || configuration.Parallelism.Value == 1))
        {
            CopyDirectories(configuration.Directories, configuration.IgnorePatterns, configuration.Verbose, copyReporter);
            return;
        }

        await CopyDirectoriesAsync(configuration, copyReporter);
    }

    private async Task CopyDirectoriesAsync(ImcopyConfiguration configuration, CopyReporter reporter)
    {
        var stopwatch = Stopwatch.StartNew();

        var foundFiles = GetFiles(configuration.Directories, configuration.IgnorePatterns);
        var files = new ConcurrentStack<FileItem>(foundFiles);

        var filesCount = files.Count;
        var filesProcessed = 0;
        var filesCopied = 0;

        var parallelism = configuration.Parallelism.HasValue ? configuration.Parallelism.Value : DefaultParallelism;
        using var concurrencySemaphore = new SemaphoreSlim(parallelism);

        var lockObject = new object();
        var tasks = new List<Task>(filesCount);

        while (!files.IsEmpty)
        {
            await concurrencySemaphore.WaitAsync();
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    if (files.TryPop(out var file))
                    {
                        var sourceFileCopiedCount = CopyFile(file, reporter);
                        Interlocked.Add(ref filesCopied, sourceFileCopiedCount);
                        var newFilesProcessed = Interlocked.Increment(ref filesProcessed);
                        lock (lockObject)
                        {
                            DisplayProgressBar(newFilesProcessed, filesCount);
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

        var deleteStopwatch = Stopwatch.StartNew();
        int filesToDeleteCount = DeleteFilesInDestinationDirectories(configuration.Directories, configuration.IgnorePatterns, foundFiles, reporter);
        deleteStopwatch.Stop();

        DisplaySummary(filesCount, filesCopied, stopwatch, filesToDeleteCount, deleteStopwatch, configuration.Verbose, reporter);
    }

    private int DeleteFilesInDestinationDirectories(IEnumerable<DirectoryConfiguration> directories,
        IEnumerable<IgnorePatternConfiguration>? ignorePatterns, IEnumerable<FileItem> foundFiles, CopyReporter reporter)
    {
        DisplayScanningForDeletion();

        var filesInDestinations = GetAllFilesInDestinationDirectories(directories.Where(d => (d.RemoveBehavior ?? DefaultRemoveBehavior) == RemoveBehavior.Remove), ignorePatterns);
        var filesToDelete = filesInDestinations.Where(df => !foundFiles.Any(f => f.SourceFileFullName == df.SourceFileFullPath)).ToArray();

        var filesToDeleteCount = filesToDelete.Length;

        var filesDeleted = 0;
        foreach (var file in filesToDelete)
        {
            DeleteFile(file, reporter);
            DisplayProgressBar(filesDeleted, filesToDeleteCount, deletionStep: true);
            filesDeleted++;
        }

        return filesToDeleteCount;
    }

    private static void DeleteFile(DestinationFileItem file, CopyReporter reporter)
    {
        File.Delete(file.DestinationFileFullName);
        reporter.ReportFileProcessed(file.DestinationFileFullName, file.DestinationFileFullName, CopyState.Deleted);
        var dirInfo = new DirectoryInfo(file.DestinationDirectoryName);
        if (dirInfo.GetFiles().Length == 0 && dirInfo.GetDirectories().Length == 0)
        {
            Directory.Delete(file.DestinationDirectoryName);
            reporter.ReportFileProcessed(file.DestinationDirectoryName, file.DestinationDirectoryName, CopyState.Deleted);
        }
    }

    private void CopyDirectories(IEnumerable<DirectoryConfiguration> directories, IEnumerable<IgnorePatternConfiguration>? ignorePatterns,
        bool? verbose, CopyReporter reporter)
    {
        var stopwatch = Stopwatch.StartNew();

        var foundFiles = GetFiles(directories, ignorePatterns);

        var filesCount = foundFiles.Count();
        var filesProcessed = 0;
        var filesCopied = 0;

        foreach (var file in foundFiles)
        {
            filesCopied += CopyFile(file, reporter);
            filesProcessed++;
            DisplayProgressBar(filesProcessed, filesCount);
        }
        stopwatch.Stop();

        var deleteStopwatch = Stopwatch.StartNew();
        int filesToDeleteCount = DeleteFilesInDestinationDirectories(directories, ignorePatterns, foundFiles, reporter);
        deleteStopwatch.Stop();

        DisplaySummary(filesCount, filesCopied, stopwatch, filesToDeleteCount, deleteStopwatch, verbose, reporter);
    }

    private static int CopyFile(FileItem file, CopyReporter reporter)
    {
        var copiedFiles = 0;
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
                    reporter.ReportFileProcessed(file.SourceFileFullName, destinationFile, CopyState.Ignored);
                    continue;
                }

                var sourceFileInfo = new FileInfo(file.SourceFileFullName);
                var destinationFileInfo = new FileInfo(destinationFile);
                if (sourceFileInfo.LastWriteTimeUtc <= destinationFileInfo.LastWriteTimeUtc)
                {
                    reporter.ReportFileProcessed(file.SourceFileFullName, destinationFile, CopyState.Ignored);
                    continue;
                }
            }
            File.Copy(file.SourceFileFullName, destinationFile, overwrite: true);
            reporter.ReportFileProcessed(file.SourceFileFullName, destinationFile, CopyState.Copied);
            copiedFiles++;
        }
        return copiedFiles;
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
                    var destinationDirectories = directory.Destinations.Select(d => Path.Combine(d, relativePath)).ToArray();
                    var fileName = f.Name;
                    return new FileItem(fileName, f.FullName, destinationDirectories, directory.OverwriteBehavior);
                });

            files.AddRange(directoryFiles);
        }

        return files;
    }

    private IEnumerable<DestinationFileItem> GetAllFilesInDestinationDirectories(IEnumerable<DirectoryConfiguration> directories, IEnumerable<IgnorePatternConfiguration>? ignorePatterns)
    {
        var files = new List<DestinationFileItem>();
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

            foreach (var destination in directory.Destinations)
            {
                var dirInfo = new DirectoryInfo(destination);
                var fileInfos = dirInfo.EnumerateFiles("*", SearchOption.AllDirectories);

                var directoryFiles = fileInfos
                    .Where(f => {
                        var destinationPath = destination;
                        if (destinationPath.EndsWith(Path.DirectorySeparatorChar) || destinationPath.EndsWith(Path.AltDirectorySeparatorChar))
                        {
                            destinationPath = destinationPath[..^1];
                        }
                        var relativeFilePath = f.FullName[destinationPath.Length..];
                        return !excludeGlobs.Any(g => g.IsMatch(relativeFilePath));
                    })
                    .Select(f => {
                        var relativePath = GetRelativePath(destination, f.DirectoryName);
                        var sourceFileFullName = Path.Combine(directory.Source, relativePath, f.Name);
                        return new DestinationFileItem(sourceFileFullName, f.FullName, f.DirectoryName!);
                    });

                files.AddRange(directoryFiles);
            }
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

    private void DisplayProgressBar(int currentValue, int maxValue, bool deletionStep = false, int barSize = 50)
    {
        var progressFraction = (double)currentValue / maxValue;
        var filledBars = (int)(progressFraction * barSize);
        var emptyBars = barSize - filledBars;

        console.Write($"\r{(deletionStep ? "Deleting" : "Copying")} [");
        console.Write(new string('#', filledBars));
        console.Write(new string(' ', emptyBars));
        console.Write($"] {progressFraction:P0}");
    }
    private void DisplaySummary(int filesCount, int filesCopied, Stopwatch stopwatch, int filesToDeleteCount, Stopwatch deleteStopwatch,
        bool? verbose, CopyReporter reporter)
    {
        console.WriteLine($"{Environment.NewLine}{filesCount} files processed in {stopwatch.Elapsed}. {filesCopied} files were copied.");
        DisplayCopiedFilesDetails(verbose, reporter);
        console.WriteLine($"{filesToDeleteCount} files deleted in {deleteStopwatch.Elapsed}.");
        DisplayDeletedFilesDetails(verbose, reporter);
    }

    private void DisplayCopiedFilesDetails(bool? verbose, CopyReporter reporter)
    {
        if (!verbose.HasValue || !verbose.Value)
        {
            return;
        }

        foreach (var copiedFile in reporter.GetProcessedFile(CopyState.Copied))
        {
            Console.ForegroundColor = ConsoleColor.White;
            console.WriteLine($"    {copiedFile.Key}");
            Console.ResetColor();
            foreach (var destination in copiedFile.Value)
            {
                console.WriteLine($"    --> {destination}");
            }
        }
    }

    private void DisplayDeletedFilesDetails(bool? verbose, CopyReporter reporter)
    {
        if (!verbose.HasValue || !verbose.Value)
        {
            return;
        }

        foreach (var deletedFile in reporter.GetProcessedFile(CopyState.Deleted))
        {
            console.WriteLine($"    {deletedFile.Key}");
        }
    }

    private void DisplayScanningForCopying()
    {
        console.Write($"Scanning files for copying...");
    }

    private void DisplayScanningForDeletion()
    {
        console.Write($"{Environment.NewLine}Scanning files for deletion...");
    }

    private void DisplayNewLine()
    {
        console.WriteLine(Environment.NewLine);
    }
}
