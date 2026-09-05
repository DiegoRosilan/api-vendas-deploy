using GestorPDV.Application.Cadastros;
using GestorPDV.Domain.Cadastros;
using GestorPDV.Infrastructure.Database;
using Npgsql;

namespace GestorPDV.Data.Postgres.Repositories;

public class FormaPagamentoRepository : IFormaPagamentoRepository
{
    private const string Colunas =
        "id, codigo, descricao, tipo, permite_parcelamento, gera_financeiro, movimenta_caixa, ativo";

    private readonly NpgsqlConnectionFactory _connectionFactory;

    public FormaPagamentoRepository(NpgsqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<FormaPagamento>> ListarAsync(CancellationToken cancellationToken = default)
    {
        var sql = $"SELECT {Colunas} FROM cad_forma_pagamento ORDER BY descricao";

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var formas = new List<FormaPagamento>();
        while (await reader.ReadAsync(cancellationToken))
        {
            formas.Add(MapFormaPagamento(reader));
        }

        return formas;
    }

    public async Task<FormaPagamento?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var sql = $"SELECT {Colunas} FROM cad_forma_pagamento WHERE id = @id";

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapFormaPagamento(reader) : null;
    }

    public async Task<long> InserirAsync(FormaPagamento formaPagamento, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO cad_forma_pagamento
                (codigo, descricao, tipo, permite_parcelamento, gera_financeiro, movimenta_caixa, ativo)
            VALUES
                (@codigo, @descricao, @tipo, @permiteParcelamento, @geraFinanceiro, @movimentaCaixa, @ativo)
            RETURNING id
            """;

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        AdicionarParametros(command, formaPagamento);
        return (long)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    public async Task AtualizarAsync(FormaPagamento formaPagamento, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE cad_forma_pagamento SET
                codigo = @codigo, descricao = @descricao, tipo = @tipo,
                permite_parcelamento = @permiteParcelamento, gera_financeiro = @geraFinanceiro,
                movimenta_caixa = @movimentaCaixa, ativo = @ativo
            WHERE id = @id
            """;

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", formaPagamento.Id);
        AdicionarParametros(command, formaPagamento);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AdicionarParametros(NpgsqlCommand command, FormaPagamento formaPagamento)
    {
        command.Parameters.AddWithValue("codigo", formaPagamento.Codigo);
        command.Parameters.AddWithValue("descricao", formaPagamento.Descricao);
        command.Parameters.AddWithValue("tipo", TipoParaTexto(formaPagamento.Tipo));
        command.Parameters.AddWithValue("permiteParcelamento", formaPagamento.PermiteParcelamento);
        command.Parameters.AddWithValue("geraFinanceiro", formaPagamento.GeraFinanceiro);
        command.Parameters.AddWithValue("movimentaCaixa", formaPagamento.MovimentaCaixa);
        command.Parameters.AddWithValue("ativo", formaPagamento.Ativo);
    }

    private static FormaPagamento MapFormaPagamento(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetInt64(0),
        Codigo = reader.GetString(1),
        Descricao = reader.GetString(2),
        Tipo = TextoParaTipo(reader.GetString(3)),
        PermiteParcelamento = reader.GetBoolean(4),
        GeraFinanceiro = reader.GetBoolean(5),
        MovimentaCaixa = reader.GetBoolean(6),
        Ativo = reader.GetBoolean(7)
    };

    private static string TipoParaTexto(TipoFormaPagamento tipo) => tipo switch
    {
        TipoFormaPagamento.Dinheiro => "dinheiro",
        TipoFormaPagamento.CartaoCredito => "cartao_credito",
        TipoFormaPagamento.CartaoDebito => "cartao_debito",
        TipoFormaPagamento.Pix => "pix",
        TipoFormaPagamento.Boleto => "boleto",
        TipoFormaPagamento.Cheque => "cheque",
        TipoFormaPagamento.Crediario => "crediario",
        TipoFormaPagamento.Transferencia => "transferencia",
        _ => throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "Tipo de forma de pagamento desconhecido.")
    };

    private static TipoFormaPagamento TextoParaTipo(string texto) => texto switch
    {
        "dinheiro" => TipoFormaPagamento.Dinheiro,
        "cartao_credito" => TipoFormaPagamento.CartaoCredito,
        "cartao_debito" => TipoFormaPagamento.CartaoDebito,
        "pix" => TipoFormaPagamento.Pix,
        "boleto" => TipoFormaPagamento.Boleto,
        "cheque" => TipoFormaPagamento.Cheque,
        "crediario" => TipoFormaPagamento.Crediario,
        "transferencia" => TipoFormaPagamento.Transferencia,
        _ => throw new ArgumentOutOfRangeException(nameof(texto), texto, "Tipo de forma de pagamento desconhecido.")
    };
}
