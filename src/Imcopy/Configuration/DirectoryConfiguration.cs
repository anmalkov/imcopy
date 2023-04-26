namespace Imcopy.Configuration;

public class DirectoryConfiguration
{
    public string Source { get; set; } = "";
    public IEnumerable<string> Destinations { get; set; } = new List<string>();
    public string? IgnorePattern { get; set; }
    public OverwriteBehavior? OverwriteBehavior { get; set; }
    public RemoveBehavior? RemoveBehavior { get; set; }
}
