using Micopy.Configuration;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;

namespace Micopy.Services;

internal static class ConfigurationService
{
    public static async Task<(MicopyConfiguration? Configuration, Exception? Exception)> LoadAsync(string path)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        using var reader = new StreamReader(path);
        var yaml = await reader.ReadToEndAsync();

        try
        {
            return (deserializer.Deserialize<MicopyConfiguration>(yaml), null);
        }
        catch (Exception ex)
        {
            return (null, ex);
        }
    }
}
