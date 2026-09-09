using System.Xml.Linq;
using System.Reflection;

namespace PlakaTanimaliOtoparkOtomasyonu.Helpers;

public static class AppConfigYardimcisi
{
    public const string BaglantiOrtamiDegiskeni = "OTOPARK_DB_CONNECTION_STRING";

    private const string VarsayilanBaglanti =
        "Server=localhost;Database=PlakaTanimaliOtoparkDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";

    public static string BaglantiCumlesiAl(string name = "OtoparkDb")
    {
        var ortamDegeri = Environment.GetEnvironmentVariable(BaglantiOrtamiDegiskeni);
        if (!string.IsNullOrWhiteSpace(ortamDegeri))
        {
            return ortamDegeri;
        }

        try
        {
            var config = ConfigYukle();
            var value = config?
                .Descendants("connectionStrings")
                .Elements("add")
                .FirstOrDefault(x => string.Equals((string?)x.Attribute("name"), name, StringComparison.OrdinalIgnoreCase))
                ?.Attribute("connectionString")
                ?.Value;

            return string.IsNullOrWhiteSpace(value) ? VarsayilanBaglanti : value;
        }
        catch
        {
            return VarsayilanBaglanti;
        }
    }

    public static string AyarAl(string key, string varsayilan = "")
    {
        try
        {
            var config = ConfigYukle();
            var value = config?
                .Descendants("appSettings")
                .Elements("add")
                .FirstOrDefault(x => string.Equals((string?)x.Attribute("key"), key, StringComparison.OrdinalIgnoreCase))
                ?.Attribute("value")
                ?.Value;

            return string.IsNullOrWhiteSpace(value) ? varsayilan : value;
        }
        catch
        {
            return varsayilan;
        }
    }

    private static XDocument? ConfigYukle()
    {
        var entryName = Assembly.GetEntryAssembly()?.GetName().Name ?? "PlakaTanimaliOtoparkOtomasyonu";
        var adaylar = new[]
        {
            Path.Combine(AppContext.BaseDirectory, $"{entryName}.dll.config"),
            Path.Combine(AppContext.BaseDirectory, $"{entryName}.exe.config"),
            Path.Combine(AppContext.BaseDirectory, "App.config"),
            Path.Combine(Directory.GetCurrentDirectory(), "App.config")
        };

        var configPath = adaylar.FirstOrDefault(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path));
        return configPath is null ? null : XDocument.Load(configPath);
    }
}
