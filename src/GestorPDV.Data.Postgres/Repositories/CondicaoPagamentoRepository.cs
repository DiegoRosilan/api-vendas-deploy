using GestorPDV.Application.Cadastros;
using GestorPDV.Domain.Cadastros;
using GestorPDV.Infrastructure.Database;
using Npgsql;

namespace GestorPDV.Data.Postgres.Repositories;

public class CondicaoPagamentoRepository : ICondicaoPagamentoRepository
{
    private const string Colunas = "id, descricao, numero_parcelas, intervalo_dias, entrada_pct, ativo";

    private readonly NpgsqlConnectionFactory _connectionFactory;

    public CondicaoPagamentoRepository(NpgsqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<CondicaoPagamento>> ListarAsync(CancellationToken cancellationToken = default)
    {
        var sql = $"SELECT {Colunas} FROM cad_condicao_pagamento ORDER BY descricao";

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var condicoes = new List<CondicaoPagamento>();
        while (await reader.ReadAsync(cancellationToken))
        {
            condicoes.Add(MapCondicaoPagamento(reader));
        }

        return condicoes;
    }

    public async Task<CondicaoPagamento?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var sql = $"SELECT {Colunas} FROM cad_condicao_pagamento WHERE id = @id";

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapCondicaoPagamento(reader) : null;
    }

    public async Task<long> InserirAsync(CondicaoPagamento condicaoPagamento, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO cad_condicao_pagamento (descricao, numero_parcelas, intervalo_dias, entrada_pct, ativo)
            VALUES (@descricao, @numeroParcelas, @intervaloDias, @entradaPct, @ativo)
            RETURNING id
            """;

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        AdicionarParametros(command, condicaoPagamento);
        return (long)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    public async Task AtualizarAsync(CondicaoPagamento condicaoPagamento, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE cad_condicao_pagamento
            SET descricao = @descricao, numero_parcelas = @numeroParcelas,
                intervalo_dias = @intervaloDias, entrada_pct = @entradaPct, ativo = @ativo
            WHERE id = @id
            """;

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", condicaoPagamento.Id);
        AdicionarParametros(command, condicaoPagamento);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AdicionarParametros(NpgsqlCommand command, CondicaoPagamento condicaoPagamento)
    {
        command.Parameters.AddWithValue("descricao", condicaoPagamento.Descricao);
        command.Parameters.AddWithValue("numeroParcelas", condicaoPagamento.NumeroParcelas);
        command.Parameters.AddWithValue("intervaloDias", condicaoPagamento.IntervaloDias);
        command.Parameters.AddWithValue("entradaPct", condicaoPagamento.EntradaPct);
        command.Parameters.AddWithValue("ativo", condicaoPagamento.Ativo);
    }

    private static CondicaoPagamento MapCondicaoPagamento(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetInt64(0),
        Descricao = reader.GetString(1),
        NumeroParcelas = reader.GetInt32(2),
        IntervaloDias = reader.GetInt32(3),
        EntradaPct = reader.GetDecimal(4),
        Ativo = reader.GetBoolean(5)
    };
}
