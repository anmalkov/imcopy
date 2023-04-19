namespace Micopy.Configuration;

internal record FolderConfiguration(
    string Source,
    string Destination,
    string? IgnorePatternName
);
