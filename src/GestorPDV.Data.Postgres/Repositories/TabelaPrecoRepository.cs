using GestorPDV.Application.Cadastros;
using GestorPDV.Domain.Cadastros;
using GestorPDV.Infrastructure.Database;
using Npgsql;

namespace GestorPDV.Data.Postgres.Repositories;

public class TabelaPrecoRepository : ITabelaPrecoRepository
{
    private const string Colunas = "id, descricao, filial_id, vigencia_inicio, vigencia_fim, ativo";

    private readonly NpgsqlConnectionFactory _connectionFactory;

    public TabelaPrecoRepository(NpgsqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<TabelaPreco>> ListarAsync(CancellationToken cancellationToken = default)
    {
        var sql = $"SELECT {Colunas} FROM cad_tabela_preco ORDER BY descricao";

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var tabelas = new List<TabelaPreco>();
        while (await reader.ReadAsync(cancellationToken))
        {
            tabelas.Add(MapTabelaPreco(reader));
        }

        return tabelas;
    }

    public async Task<TabelaPreco?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var sql = $"SELECT {Colunas} FROM cad_tabela_preco WHERE id = @id";

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapTabelaPreco(reader) : null;
    }

    public async Task<long> InserirAsync(TabelaPreco tabelaPreco, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO cad_tabela_preco (descricao, filial_id, vigencia_inicio, vigencia_fim, ativo)
            VALUES (@descricao, @filialId, @vigenciaInicio, @vigenciaFim, @ativo)
            RETURNING id
            """;

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        AdicionarParametros(command, tabelaPreco);
        return (long)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    public async Task AtualizarAsync(TabelaPreco tabelaPreco, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE cad_tabela_preco
            SET descricao = @descricao, filial_id = @filialId,
                vigencia_inicio = @vigenciaInicio, vigencia_fim = @vigenciaFim, ativo = @ativo
            WHERE id = @id
            """;

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", tabelaPreco.Id);
        AdicionarParametros(command, tabelaPreco);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TabelaPrecoItem>> ListarItensAsync(
        long tabelaPrecoId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT id, tabela_preco_id, produto_id, preco FROM cad_tabela_preco_item WHERE tabela_preco_id = @tabelaPrecoId";

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("tabelaPrecoId", tabelaPrecoId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var itens = new List<TabelaPrecoItem>();
        while (await reader.ReadAsync(cancellationToken))
        {
            itens.Add(new TabelaPrecoItem
            {
                Id = reader.GetInt64(0),
                TabelaPrecoId = reader.GetInt64(1),
                ProdutoId = reader.GetInt64(2),
                Preco = reader.GetDecimal(3)
            });
        }

        return itens;
    }

    public async Task<TabelaPrecoItem?> ObterItemAsync(
        long tabelaPrecoId, long produtoId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, tabela_preco_id, produto_id, preco FROM cad_tabela_preco_item
            WHERE tabela_preco_id = @tabelaPrecoId AND produto_id = @produtoId
            """;

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("tabelaPrecoId", tabelaPrecoId);
        command.Parameters.AddWithValue("produtoId", produtoId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new TabelaPrecoItem
        {
            Id = reader.GetInt64(0),
            TabelaPrecoId = reader.GetInt64(1),
            ProdutoId = reader.GetInt64(2),
            Preco = reader.GetDecimal(3)
        };
    }

    public async Task DefinirItemAsync(
        long tabelaPrecoId, long produtoId, decimal preco, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO cad_tabela_preco_item (tabela_preco_id, produto_id, preco)
            VALUES (@tabelaPrecoId, @produtoId, @preco)
            ON CONFLICT (tabela_preco_id, produto_id) DO UPDATE SET preco = excluded.preco
            """;

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("tabelaPrecoId", tabelaPrecoId);
        command.Parameters.AddWithValue("produtoId", produtoId);
        command.Parameters.AddWithValue("preco", preco);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task RemoverItemAsync(long tabelaPrecoId, long produtoId, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM cad_tabela_preco_item WHERE tabela_preco_id = @tabelaPrecoId AND produto_id = @produtoId";

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("tabelaPrecoId", tabelaPrecoId);
        command.Parameters.AddWithValue("produtoId", produtoId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AdicionarParametros(NpgsqlCommand command, TabelaPreco tabelaPreco)
    {
        command.Parameters.AddWithValue("descricao", tabelaPreco.Descricao);
        command.Parameters.AddWithValue("filialId", (object?)tabelaPreco.FilialId ?? DBNull.Value);
        command.Parameters.AddWithValue("vigenciaInicio", (object?)tabelaPreco.VigenciaInicio ?? DBNull.Value);
        command.Parameters.AddWithValue("vigenciaFim", (object?)tabelaPreco.VigenciaFim ?? DBNull.Value);
        command.Parameters.AddWithValue("ativo", tabelaPreco.Ativo);
    }

    private static TabelaPreco MapTabelaPreco(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetInt64(0),
        Descricao = reader.GetString(1),
        FilialId = reader.IsDBNull(2) ? null : reader.GetInt64(2),
        VigenciaInicio = reader.IsDBNull(3) ? null : reader.GetFieldValue<DateOnly>(3),
        VigenciaFim = reader.IsDBNull(4) ? null : reader.GetFieldValue<DateOnly>(4),
        Ativo = reader.GetBoolean(5)
    };
}
