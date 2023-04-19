using System.CommandLine;

namespace Micopy.Services;

internal class CopyService
{
    private const int DefaultParallelism = 8;

    private readonly IConsole console;

    public CopyService(IConsole console)
    {
        this.console = console;
    }

    public void CopyDirectory(string sourceFolder, string destinationFolder, int? parallelism)
    {
        if (parallelism.HasValue && parallelism.Value == 0)
        {
            CopyDirectorySynchronously(sourceFolder, destinationFolder);
            return;
        }

        if (!parallelism.HasValue)
        {
            parallelism = DefaultParallelism;
        }

        CopyDirectoryInParallel(sourceFolder, destinationFolder, parallelism.Value);
    }

    private void CopyDirectoryInParallel(string sourceFolder, string destinationFolder, int parallelism)
    {
        throw new NotImplementedException();
    }

    private void CopyDirectorySynchronously(string sourceFolder, string destinationFolder)
    {
        var filesCount = GetFilesCountForDirectory(sourceFolder);
        var filesCopied = 0;
        CopyFiles(sourceFolder, destinationFolder, ref filesCopied, filesCount);
    }

    private static int GetFilesCountForDirectory(string folderPath)
    {
        var directoryInfo = new DirectoryInfo(folderPath);
        int count = directoryInfo.GetFiles("*.*",SearchOption.AllDirectories).Length;
        return count;
    }

    private void CopyFiles(string sourceFolder, string destinationFolder, ref int filesCopied, int filesCount)
    {
        if (!Directory.Exists(destinationFolder))
        {
            Directory.CreateDirectory(destinationFolder);
        }

        var directoryInfo = new DirectoryInfo(sourceFolder);

        foreach (var file in directoryInfo.GetFiles())
        {
            var destinationPath = Path.Combine(destinationFolder, file.Name);
            file.CopyTo(destinationPath, overwrite: true);
            filesCopied++;
            DisplayProgressBar(filesCopied, filesCount);
        }

        foreach (var subDirectory in directoryInfo.GetDirectories())
        {
            var nextSourceFolder = Path.Combine(sourceFolder, subDirectory.Name);
            var nextDestinationFolder = Path.Combine(destinationFolder, subDirectory.Name);
            CopyFiles(nextSourceFolder, nextDestinationFolder, ref filesCopied, filesCount);
        }
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
