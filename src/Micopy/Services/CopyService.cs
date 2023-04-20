using Micopy.Configuration;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Microsoft.Extensions.FileSystemGlobbing;
using System.CommandLine;
using System.Diagnostics;

namespace Micopy.Services;

public record FileItem(
    string FileName,
    string SourceFolder,
    string DestinationFolder
);

public class CopyService
{
    private const int DefaultParallelism = 8;

    private readonly IConsole console;

    public CopyService(IConsole console)
    {
        this.console = console;
    }

    public void Copy(MicopyConfiguration configuration)
    {
        if (configuration.Parallelism.HasValue && configuration.Parallelism.Value == 0)
        {
            CopyDirectories(configuration.Folders, configuration.IgnorePatterns);
            return;
        }

        var parallelism = configuration.Parallelism.HasValue ? configuration.Parallelism.Value : DefaultParallelism;
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
            if (!Directory.Exists(file.DestinationFolder))
            {
                Directory.CreateDirectory(file.DestinationFolder);
            }
            CopyFile(file);
            filesCopied++;
            DisplayProgressBar(filesCopied, filesCount);
        }
        stopwatch.Stop();

        console.WriteLine($"{Environment.NewLine}{filesCount} files copied in {stopwatch.Elapsed}");
    }

    private static void CopyFile(FileItem file)
    {
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
                var relativeFolder = Path.GetDirectoryName(file.Path);
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
}
