using GestorPDV.Infrastructure.Configuration;
using Xunit;

namespace GestorPDV.Tests;

public class DatabaseOptionsTests
{
    [Fact]
    public void ConnectionString_DeveConterBancoGestordbPorPadrao()
    {
        var options = new DatabaseOptions();

        Assert.Contains("Database=gestordb", options.ConnectionString);
        Assert.Contains("Host=localhost", options.ConnectionString);
        Assert.Contains("Port=5432", options.ConnectionString);
    }

    [Fact]
    public void ConnectionString_DeveRefletirValoresPersonalizados()
    {
        var options = new DatabaseOptions
        {
            Host = "10.0.0.5",
            Port = 5433,
            Database = "gestordb_homolog",
            Username = "gestor",
            Password = "segredo"
        };

        var connectionString = options.ConnectionString;

        Assert.Contains("Host=10.0.0.5", connectionString);
        Assert.Contains("Port=5433", connectionString);
        Assert.Contains("Database=gestordb_homolog", connectionString);
        Assert.Contains("Username=gestor", connectionString);
    }
}
