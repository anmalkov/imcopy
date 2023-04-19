using Micopy.Configuration;
using Micopy.Services;
using System.CommandLine;
using System.CommandLine.IO;

var configFileOption = new Option<string?>(new[] { "--config", "-c" }, () => null, "Path to the YAML configuration file.");
var sourceOption = new Option<string?>(new[] { "--source", "-s" }, () => null, "Source directory path.");
var destinationOption = new Option<string?>(new[] { "--destination", "-d" }, () => null, "Destination directory path.");
var parallelOption = new Option<int?>(new[] { "--parallel", "-p" }, () => null, "Degree of parallelism. Leave empty for default, specify an integer for custom parallelism.");

var rootCommand = new RootCommand("A powerful tool for copying and synchronizing directories.")
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

    if (string.IsNullOrEmpty(configFile))
    {
        copyService.Copy(new MicopyConfiguration(
            new[] { new FolderConfiguration(source!, destination!, null) },
            null,
            parallel
        ));
        context.ExitCode = 0;
        return;
    }

    var (configuration, error) = await ConfigurationService.LoadAsync(configFile);
    if (error is not null)
    {
        context.Console.WriteLine($"ERROR: {configFile} file format is incorrect.{Environment.NewLine}\t{error}");
        context.ExitCode = 1;
        return;
    }

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
