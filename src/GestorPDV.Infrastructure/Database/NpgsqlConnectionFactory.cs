using GestorPDV.Infrastructure.Configuration;
using Npgsql;

namespace GestorPDV.Infrastructure.Database;

public class NpgsqlConnectionFactory
{
    private readonly DatabaseOptions _options;

    public NpgsqlConnectionFactory(DatabaseOptions options)
    {
        _options = options;
    }

    public async Task<NpgsqlConnection> CriarAsync(CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
