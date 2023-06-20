using System.Collections.Concurrent;

namespace Imcopy.Reporters;

public enum CopyState
{
    Copied,
    Ignored,
    Deleted,
    Failed
}

public record FileReport(
    string Destination,
    CopyState State
);

public class CopyReporter
{
    private ConcurrentDictionary<string, IList<FileReport>> processedFiles = new();

    public void ReportFileProcessed(string source, string destination, CopyState state)
    {
        if (!processedFiles.ContainsKey(source))
        {
            processedFiles.TryAdd(source, new List<FileReport> { new FileReport(destination, state) });
            return;
        }

        var processedFile = processedFiles[source];
        processedFile.Add(new FileReport(destination, state));
    }
    public IDictionary<string, IEnumerable<string>> GetProcessedFile(CopyState state)
    {
          return processedFiles.Where(x => x.Value.Any(y => y.State == state))
            .ToDictionary(x => x.Key, x => x.Value.Where(y => y.State == state).Select(y => y.Destination));
    }
}
