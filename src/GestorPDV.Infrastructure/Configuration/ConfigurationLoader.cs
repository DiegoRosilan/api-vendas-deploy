using Microsoft.Extensions.Configuration;

namespace GestorPDV.Infrastructure.Configuration;

public static class ConfigurationLoader
{
    // Carrega appsettings.json (obrigatório), appsettings.{ambiente}.json
    // (opcional) e variáveis de ambiente com prefixo GESTORPDV_ — nessa
    // ordem de precedência, a última vence.
    public static IConfiguration Carregar(string basePath, string? ambiente = null)
    {
        ambiente ??= Environment.GetEnvironmentVariable("GESTORPDV_ENVIRONMENT") ?? "Production";

        var builder = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{ambiente}.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables(prefix: "GESTORPDV_");

        return builder.Build();
    }

    public static DatabaseOptions ObterOpcoesBanco(IConfiguration configuration)
    {
        var options = new DatabaseOptions();
        configuration.GetSection(DatabaseOptions.SectionName).Bind(options);
        return options;
    }
}
