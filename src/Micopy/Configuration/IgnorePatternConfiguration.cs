namespace Micopy.Configuration;

public class IgnorePatternConfiguration
{
    public string Name { get; set; }
    public IEnumerable<string> Patterns { get; set; }
}
