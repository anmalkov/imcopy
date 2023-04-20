using Micopy.Configuration;
using Micopy.Services;
using System.CommandLine;
using System.CommandLine.IO;

var configFileOption = new Option<string?>(new[] { "--file", "-f" }, () => null, "Path to the YAML configuration file. If a file is specified, all other options will be ignored.");
var sourceOption = new Option<string?>(new[] { "--source", "-s" }, () => null, "Source directory path.");
var destinationOption = new Option<string?>(new[] { "--destination", "-d" }, () => null, "Destination directory path.");
var parallelOption = new Option<int?>(new[] { "--parallel", "-p" }, () => null, $"Degree of parallelism. If option is not specified or left empty, the default value ({CopyService.DefaultParallelism}) will be used. Specify an integer for custom parallelism.");

var rootCommand = new RootCommand("A powerful and efficient CLI tool designed to simplify the process of copying and synchronizing files between directories")
{
    configFileOption,
    sourceOption,
    destinationOption,
    parallelOption
};

rootCommand.SetHandler(async context =>
{
    var configFile = context.ParseResult.GetValueForOption(configFileOption);
    var source = context.ParseResult.GetValueForOption(sourceOption);
    var destination = context.ParseResult.GetValueForOption(destinationOption);
    var parallel = context.ParseResult.GetValueForOption(parallelOption);

    var parametersAreValid = ValidateParameters(configFile, source, destination, context.Console);
    if (!parametersAreValid)
    {
        context.ExitCode = 1;
        return;
    }

    var copyService = new CopyService(context.Console);

    MicopyConfiguration configuration;
    if (string.IsNullOrEmpty(configFile))
    {
        configuration = new MicopyConfiguration {
            Folders = new[] { new FolderConfiguration { Source = source!, Destination = destination!, IgnorePatternName = null } },
            IgnorePatterns = null,
            Parallelism = parallel
        };
    }
    else
    {
        var loadResult = await ConfigurationService.LoadAsync(configFile);
        if (loadResult.Exception is not null)
        {
            context.Console.WriteLine($"ERROR: {configFile} file format is incorrect.{Environment.NewLine}\t{loadResult.Exception}");
            context.ExitCode = 1;
            return;
        }
        configuration = loadResult.Configuration!;
    }

    await copyService.CopyAsync(configuration);
    context.ExitCode = 0;
});

await rootCommand.InvokeAsync(args);

static bool ValidateParameters(string? configFile, string? source, string? destination, IConsole console)
{
    if (string.IsNullOrEmpty(configFile) && string.IsNullOrEmpty(source) && string.IsNullOrEmpty(destination))
    {
        console.WriteLine("ERROR: Either configuration file or source and destination folders must be specified. Use option --help to get more details.");
        return false;
    }

    if (string.IsNullOrEmpty(configFile))
    {
        if (string.IsNullOrEmpty(source))
        {
            console.WriteLine("ERROR: Source folder must be specified. Use option --help to get more details.");
            return false;
        }
        if (string.IsNullOrEmpty(destination))
        {
            console.WriteLine("ERROR: Destination folder must be specified. Use option --help to get more details.");
            return false;
        }
    } 
    else
    {
        if (!string.IsNullOrEmpty(source) || !string.IsNullOrEmpty(destination))
        {
            console.WriteLine("INFO: Source and destination folders will be ignored because configuration file is specified.");
        }
    }

    return true;
}
