using GestorPDV.Application.Cadastros;
using GestorPDV.Domain.Cadastros;
using GestorPDV.Infrastructure.Database;
using Npgsql;

namespace GestorPDV.Data.Postgres.Repositories;

public class ServicoRepository : IServicoRepository
{
    private const string Colunas = "id, codigo, descricao, preco, aliquota_iss_pct, ativo";

    private readonly NpgsqlConnectionFactory _connectionFactory;

    public ServicoRepository(NpgsqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Servico>> ListarAsync(string? filtro, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            SELECT {Colunas} FROM cad_servico
            WHERE @filtro IS NULL OR descricao ILIKE @filtro OR codigo ILIKE @filtro
            ORDER BY descricao
            """;

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue(
            "filtro", (object?)(string.IsNullOrWhiteSpace(filtro) ? null : $"%{filtro}%") ?? DBNull.Value);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var servicos = new List<Servico>();
        while (await reader.ReadAsync(cancellationToken))
        {
            servicos.Add(MapServico(reader));
        }

        return servicos;
    }

    public async Task<Servico?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var sql = $"SELECT {Colunas} FROM cad_servico WHERE id = @id";

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapServico(reader) : null;
    }

    public async Task<long> InserirAsync(Servico servico, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO cad_servico (codigo, descricao, preco, aliquota_iss_pct, ativo)
            VALUES (@codigo, @descricao, @preco, @aliquotaIssPct, @ativo)
            RETURNING id
            """;

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        AdicionarParametros(command, servico);
        return (long)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    public async Task AtualizarAsync(Servico servico, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE cad_servico
            SET codigo = @codigo, descricao = @descricao, preco = @preco,
                aliquota_iss_pct = @aliquotaIssPct, ativo = @ativo
            WHERE id = @id
            """;

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", servico.Id);
        AdicionarParametros(command, servico);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AdicionarParametros(NpgsqlCommand command, Servico servico)
    {
        command.Parameters.AddWithValue("codigo", servico.Codigo);
        command.Parameters.AddWithValue("descricao", servico.Descricao);
        command.Parameters.AddWithValue("preco", servico.Preco);
        command.Parameters.AddWithValue("aliquotaIssPct", servico.AliquotaIssPct);
        command.Parameters.AddWithValue("ativo", servico.Ativo);
    }

    private static Servico MapServico(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetInt64(0),
        Codigo = reader.GetString(1),
        Descricao = reader.GetString(2),
        Preco = reader.GetDecimal(3),
        AliquotaIssPct = reader.GetDecimal(4),
        Ativo = reader.GetBoolean(5)
    };
}
