using Imcopy.Configuration;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;

namespace Imcopy.Services;

internal static class ConfigurationService
{
    public static async Task<(ImcopyConfiguration? Configuration, Exception? Exception)> LoadAsync(string path)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        using var reader = new StreamReader(path);
        var yaml = await reader.ReadToEndAsync();

        try
        {
            return (deserializer.Deserialize<ImcopyConfiguration>(yaml), null);
        }
        catch (Exception ex)
        {
            return (null, ex);
        }
    }
}
