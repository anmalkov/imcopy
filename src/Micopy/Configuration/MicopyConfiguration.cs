namespace Micopy.Configuration;

internal record MicopyConfiguration(
    IEnumerable<FolderConfiguration> Folders,
    IEnumerable<IgnorePatternConfiguration>? IgnorePatterns,
    int? Parallel = 8
);
