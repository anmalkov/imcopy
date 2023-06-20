namespace Imcopy.Configuration;

public class ImcopyConfiguration
{
    public IEnumerable<DirectoryConfiguration> Directories { get; set; } = new List<DirectoryConfiguration>();
    public IEnumerable<IgnorePatternConfiguration>? IgnorePatterns { get; set; }
    public int? Parallelism { get; set; }
    public bool? Verbose { get; set; }
}
