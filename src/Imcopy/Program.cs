using Imcopy.Configuration;
using Imcopy.Services;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.IO;
using System.CommandLine.Parsing;

var configFileOption = new Option<string?>(new[] { "--file", "-f" }, () => null, "Path to the YAML configuration file. If a file is specified, all other options will be ignored.");
var sourceOption = new Option<string?>(new[] { "--source", "-s" }, () => null, "Source directory path.");
var destinationOption = new Option<string?>(new[] { "--destination", "-d" }, () => null, "Destination directory path.");
var parallelOption = new Option<int?>(new[] { "--parallel", "-p" }, () => null, $"Degree of parallelism. If option is not specified or left empty, the default value ({CopyService.DefaultParallelism}) will be used. Specify an integer for custom parallelism.");
var overwriteBehaviorOption = new Option<OverwriteBehavior?>(new[] { "--overwrite", "-o" }, () => OverwriteBehavior.IfNewer, $"Overwrite behavior:\n- always:  Overwrite all the files in the destination directory.\n- ifNewer: Overwrite a file in the destination directory only if a file in the source directory is newer.\n- never:   Do not copy a file if it already exists in the destination directory.\nIf option is not specified, the default value will be used.");

var rootCommand = new RootCommand("A powerful and efficient CLI tool designed to simplify the process of copying and synchronizing files between directories")
{
    configFileOption,
    sourceOption,
    destinationOption,
    parallelOption,
    overwriteBehaviorOption
};

rootCommand.SetHandler(async context =>
{
    var configFile = context.ParseResult.GetValueForOption(configFileOption);
    var source = context.ParseResult.GetValueForOption(sourceOption);
    var destination = context.ParseResult.GetValueForOption(destinationOption);
    var parallel = context.ParseResult.GetValueForOption(parallelOption);
    var overwriteBehavior = context.ParseResult.GetValueForOption(overwriteBehaviorOption);

    var parametersAreValid = ValidateParameters(configFile, source, destination, context.Console);
    if (!parametersAreValid)
    {
        context.ExitCode = 1;
        return;
    }

    var copyService = new CopyService(context.Console);

    ImcopyConfiguration configuration;
    if (string.IsNullOrEmpty(configFile))
    {
        configuration = new ImcopyConfiguration {
            Directories = new[] { new DirectoryConfiguration { Source = source!, Destinations = new[] { destination! }, IgnorePattern = null, OverwriteBehavior = overwriteBehavior } },
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

var parser = new CommandLineBuilder(rootCommand)
    .UseDefaults()
    .UseHelp(context =>
    {
        context.HelpBuilder.CustomizeSymbol(overwriteBehaviorOption, firstColumnText: "-o, --overwrite <always|ifNewer|never>", defaultValue: "ifNewer");
    })
    .Build();

await parser.InvokeAsync(args);

//await rootCommand.InvokeAsync(args);

static bool ValidateParameters(string? configFile, string? source, string? destination, IConsole console)
{
    if (string.IsNullOrEmpty(configFile) && string.IsNullOrEmpty(source) && string.IsNullOrEmpty(destination))
    {
        console.WriteLine("ERROR: Either configuration file or source and destination directories must be specified. Use option --help to get more details.");
        return false;
    }

    if (string.IsNullOrEmpty(configFile))
    {
        if (string.IsNullOrEmpty(source))
        {
            console.WriteLine("ERROR: Source directory must be specified. Use option --help to get more details.");
            return false;
        }
        if (string.IsNullOrEmpty(destination))
        {
            console.WriteLine("ERROR: Destination directory must be specified. Use option --help to get more details.");
            return false;
        }
    } 
    else
    {
        if (!string.IsNullOrEmpty(source) || !string.IsNullOrEmpty(destination))
        {
            console.WriteLine("INFO: Source and destination directories will be ignored because configuration file is specified.");
        }
    }

    return true;
}
