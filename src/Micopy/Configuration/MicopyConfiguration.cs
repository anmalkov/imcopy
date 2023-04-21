namespace Micopy.Configuration;

public class MicopyConfiguration
{
    public IEnumerable<DirectoryConfiguration> Directories { get; set; } = new List<DirectoryConfiguration>();
    public IEnumerable<IgnorePatternConfiguration>? IgnorePatterns { get; set; }
    public int? Parallelism { get; set; }
}
