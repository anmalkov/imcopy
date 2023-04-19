namespace Micopy.Configuration;

public record IgnorePatternConfiguration(
    string Name, 
    IEnumerable<string> Patterns
);
