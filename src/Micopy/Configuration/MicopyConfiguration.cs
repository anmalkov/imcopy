namespace Micopy.Configuration;

public record MicopyConfiguration(
    IEnumerable<FolderConfiguration> Folders,
    IEnumerable<IgnorePatternConfiguration>? IgnorePatterns,
    int? Parallelism
);
