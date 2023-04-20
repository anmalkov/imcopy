namespace Micopy.Configuration;

public class MicopyConfiguration
{
    public IEnumerable<FolderConfiguration> Folders { get; set; }
    public IEnumerable<IgnorePatternConfiguration>? IgnorePatterns { get; set; }
    public int? Parallelism { get; set; }
}
