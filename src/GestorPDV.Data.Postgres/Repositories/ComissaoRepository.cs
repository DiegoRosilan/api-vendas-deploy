using GestorPDV.Application.Common;
using GestorPDV.Application.Vendas;
using GestorPDV.Domain.Vendas;
using GestorPDV.Infrastructure.Database;
using Npgsql;

namespace GestorPDV.Data.Postgres.Repositories;

public class ComissaoRepository : IComissaoRepository
{
    private readonly NpgsqlConnectionFactory _connectionFactory;

    public ComissaoRepository(NpgsqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task InserirAsync(Comissao comissao, IUnitOfWork unitOfWork, CancellationToken cancellationToken = default)
    {
        var uow = (NpgsqlUnitOfWork)unitOfWork;
        var connection = uow.Connection ?? throw new InvalidOperationException("Transação não iniciada.");
        var transaction = uow.Transaction ?? throw new InvalidOperationException("Transação não iniciada.");

        const string sql = """
            INSERT INTO com_comissao
                (venda_id, funcionario_id, tipo, percentual, valor_base, valor_comissao, data_referencia, status)
            VALUES
                (@vendaId, @funcionarioId, @tipo, @percentual, @valorBase, @valorComissao, @dataReferencia, @status)
            RETURNING id
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("vendaId", comissao.VendaId);
        command.Parameters.AddWithValue("funcionarioId", comissao.FuncionarioId);
        command.Parameters.AddWithValue("tipo", comissao.Tipo == TipoComissao.Gerente ? "gerente" : "vendedor");
        command.Parameters.AddWithValue("percentual", comissao.Percentual);
        command.Parameters.AddWithValue("valorBase", comissao.ValorBase);
        command.Parameters.AddWithValue("valorComissao", comissao.ValorComissao);
        command.Parameters.AddWithValue("dataReferencia", comissao.DataReferencia);
        command.Parameters.AddWithValue("status", "pendente");
        comissao.Id = (long)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    public async Task<IReadOnlyList<Comissao>> ListarPorVendaAsync(
        long vendaId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, venda_id, funcionario_id, tipo, percentual, valor_base, valor_comissao,
                   data_referencia, status, data_pagamento
            FROM com_comissao WHERE venda_id = @vendaId
            """;

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("vendaId", vendaId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var comissoes = new List<Comissao>();
        while (await reader.ReadAsync(cancellationToken))
        {
            comissoes.Add(new Comissao
            {
                Id = reader.GetInt64(0),
                VendaId = reader.GetInt64(1),
                FuncionarioId = reader.GetInt64(2),
                Tipo = reader.GetString(3) == "gerente" ? TipoComissao.Gerente : TipoComissao.Vendedor,
                Percentual = reader.GetDecimal(4),
                ValorBase = reader.GetDecimal(5),
                ValorComissao = reader.GetDecimal(6),
                DataReferencia = reader.GetFieldValue<DateOnly>(7),
                Status = reader.GetString(8) switch
                {
                    "pago" => StatusComissao.Pago,
                    "cancelado" => StatusComissao.Cancelado,
                    _ => StatusComissao.Pendente
                },
                DataPagamento = reader.IsDBNull(9) ? null : reader.GetFieldValue<DateOnly>(9)
            });
        }

        return comissoes;
    }
}
