using System.CommandLine;

namespace Micopy.Services;

public class CopyServiceOld
{
    private const int DefaultParallelism = 8;

    private readonly IConsole console;

    public CopyServiceOld(IConsole console)
    {
        this.console = console;
    }

    public async Task CopyDirectoryAsync(string sourceFolder, string destinationFolder, int? parallelism)
    {
        if (parallelism.HasValue && parallelism.Value == 0)
        {
            CopyDirectory(sourceFolder, destinationFolder);
            return;
        }

        if (!parallelism.HasValue)
        {
            parallelism = DefaultParallelism;
        }

        await CopyDirectoryAsync(sourceFolder, destinationFolder, parallelism.Value);
    }

    private async Task CopyDirectoryAsync(string sourceFolder, string destinationFolder, int parallelism)
    {
        var filesCount = await GetFilesCountForDirectoryAsync(sourceFolder, parallelism);
        var filesCopied = 0;
        //CopyFilesAsync(sourceFolder, destinationFolder, parallelism, ref filesCopied, filesCount);
    }

    private async Task<int> GetFilesCountForDirectoryAsync(string folderPath, int parallelism)
    {
        var subdirectories = Directory.GetDirectories(folderPath, "*", SearchOption.AllDirectories);

        int totalFileCount = 0;

        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = parallelism };
        await Task.Run(() =>
        {
            Parallel.ForEach(subdirectories, parallelOptions, subdirectory =>
            {
                var directoryFilesCount = Directory.GetFiles(subdirectory).Length;
                Interlocked.Add(ref totalFileCount, directoryFilesCount);
            });
        });

        return totalFileCount;
    }

    private void CopyDirectory(string sourceFolder, string destinationFolder)
    {
        var filesCount = GetFilesCountForDirectory(sourceFolder);
        var filesCopied = 0;
        CopyFiles(sourceFolder, destinationFolder, ref filesCopied, filesCount);
    }

    private void CopyDirectoryNew(string sourceFolder, string destinationFolder)
    {
        var directoryInfo = new DirectoryInfo(sourceFolder);
        var files = directoryInfo.GetFiles("*.*", SearchOption.AllDirectories);
        var filesCount = files.Length;
        var filesCopied = 0;

        foreach (var file in files)
        {
            var relativePath = file.FullName.Substring(sourceFolder.Length + 1);
            var targetFilePath = Path.Combine(destinationFolder, relativePath);
            var targetFileDirectory = Path.GetDirectoryName(targetFilePath);

            if (!string.IsNullOrEmpty(targetFileDirectory) && !Directory.Exists(targetFileDirectory))
            {
                Directory.CreateDirectory(targetFileDirectory);
            }

            file.CopyTo(targetFilePath, overwrite: true);
            filesCopied++;
            DisplayProgressBar(filesCopied, filesCount);
        }
    }

    private static int GetFilesCountForDirectory(string folderPath)
    {
        var directoryInfo = new DirectoryInfo(folderPath);
        int count = directoryInfo.GetFiles("*.*", SearchOption.AllDirectories).Length;
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

    //private async Task CopyFilesAsync(string sourceFolder, string destinationFolder, int parallelism, ref int filesCopied, int filesCount)
    //{
    //    if (!Directory.Exists(destinationFolder))
    //    {
    //        Directory.CreateDirectory(destinationFolder);
    //    }

    //    var directoryInfo = new DirectoryInfo(sourceFolder);

    //    foreach (var file in directoryInfo.GetFiles())
    //    {
    //        var destinationPath = Path.Combine(destinationFolder, file.Name);
    //        file.CopyTo(destinationPath, overwrite: true);
    //        filesCopied++;
    //        DisplayProgressBar(filesCopied, filesCount);
    //    }

    //    foreach (var subDirectory in directoryInfo.GetDirectories())
    //    {
    //        var nextSourceFolder = Path.Combine(sourceFolder, subDirectory.Name);
    //        var nextDestinationFolder = Path.Combine(destinationFolder, subDirectory.Name);
    //        CopyFiles(nextSourceFolder, nextDestinationFolder, ref filesCopied, filesCount);
    //    }
    //}

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
