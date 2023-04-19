namespace Micopy.Configuration;

public record FolderConfiguration(
    string Source,
    string Destination,
    string? IgnorePatternName
);
