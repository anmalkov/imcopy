namespace Micopy.Configuration;

public class DirectoryConfiguration
{
    public string Source { get; set; } = "";
    public string Destination { get; set; } = "";
    public string? IgnorePattern { get; set; }
}
