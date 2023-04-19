namespace Micopy.Configuration;

internal record IgnorePatternConfiguration(
    string Name, 
    IEnumerable<string> Patterns
);
