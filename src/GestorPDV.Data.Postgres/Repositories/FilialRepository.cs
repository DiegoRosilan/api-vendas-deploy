using GestorPDV.Application.Cadastros;
using GestorPDV.Domain.Cadastros;
using GestorPDV.Infrastructure.Database;
using Npgsql;

namespace GestorPDV.Data.Postgres.Repositories;

public class FilialRepository : IFilialRepository
{
    private const string ColunasFilial =
        "id, codigo, razao_social, nome_fantasia, cnpj, inscricao_estadual, endereco, " +
        "numero, bairro, municipio, uf, cep, telefone, ativo";

    private readonly NpgsqlConnectionFactory _connectionFactory;

    public FilialRepository(NpgsqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Filial>> ListarAsync(CancellationToken cancellationToken = default)
    {
        var sql = $"SELECT {ColunasFilial} FROM cad_filial ORDER BY codigo";

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var filiais = new List<Filial>();
        while (await reader.ReadAsync(cancellationToken))
        {
            filiais.Add(MapFilial(reader));
        }

        return filiais;
    }

    public async Task<Filial?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var sql = $"SELECT {ColunasFilial} FROM cad_filial WHERE id = @id";

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapFilial(reader) : null;
    }

    private static Filial MapFilial(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetInt64(0),
        Codigo = reader.GetString(1),
        RazaoSocial = reader.GetString(2),
        NomeFantasia = reader.IsDBNull(3) ? null : reader.GetString(3),
        Cnpj = reader.IsDBNull(4) ? null : reader.GetString(4),
        InscricaoEstadual = reader.IsDBNull(5) ? null : reader.GetString(5),
        Endereco = reader.IsDBNull(6) ? null : reader.GetString(6),
        Numero = reader.IsDBNull(7) ? null : reader.GetString(7),
        Bairro = reader.IsDBNull(8) ? null : reader.GetString(8),
        Municipio = reader.IsDBNull(9) ? null : reader.GetString(9),
        Uf = reader.IsDBNull(10) ? null : reader.GetString(10),
        Cep = reader.IsDBNull(11) ? null : reader.GetString(11),
        Telefone = reader.IsDBNull(12) ? null : reader.GetString(12),
        Ativo = reader.GetBoolean(13)
    };
}
