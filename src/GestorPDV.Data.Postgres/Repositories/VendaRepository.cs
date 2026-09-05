using GestorPDV.Application.Common;
using GestorPDV.Application.Vendas;
using GestorPDV.Domain.Vendas;
using GestorPDV.Infrastructure.Database;
using Npgsql;

namespace GestorPDV.Data.Postgres.Repositories;

public class VendaRepository : IVendaRepository
{
    private readonly NpgsqlConnectionFactory _connectionFactory;

    public VendaRepository(NpgsqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task InserirAsync(Venda venda, IUnitOfWork unitOfWork, CancellationToken cancellationToken = default)
    {
        var uow = (NpgsqlUnitOfWork)unitOfWork;
        var connection = uow.Connection ?? throw new InvalidOperationException("Transação não iniciada.");
        var transaction = uow.Transaction ?? throw new InvalidOperationException("Transação não iniciada.");

        // Trava a tabela para gerar o próximo número de venda da filial sem
        // duplicidade. Suficiente para o volume de um comércio de pequeno/
        // médio porte; se a concorrência crescer, substituir por uma
        // sequência dedicada por filial.
        await using (var lockCommand = new NpgsqlCommand("LOCK TABLE mv_venda IN SHARE ROW EXCLUSIVE MODE", connection, transaction))
        {
            await lockCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var numeroCommand = new NpgsqlCommand(
            "SELECT COALESCE(MAX(numero), 0) + 1 FROM mv_venda WHERE filial_id = @filialId", connection, transaction))
        {
            numeroCommand.Parameters.AddWithValue("filialId", venda.FilialId);
            venda.Numero = (long)(await numeroCommand.ExecuteScalarAsync(cancellationToken))!;
        }

        const string sqlVenda = """
            INSERT INTO mv_venda
                (numero, filial_id, cliente_id, vendedor_id, tipo, status, tabela_preco_id,
                 condicao_pagamento_id, subtotal, desconto, acrescimo, total, data_venda, usuario_abertura_id)
            VALUES
                (@numero, @filialId, @clienteId, @vendedorId, @tipo, @status, @tabelaPrecoId,
                 @condicaoPagamentoId, @subtotal, @desconto, @acrescimo, @total, now(), @usuarioAberturaId)
            RETURNING id
            """;

        await using (var command = new NpgsqlCommand(sqlVenda, connection, transaction))
        {
            command.Parameters.AddWithValue("numero", venda.Numero);
            command.Parameters.AddWithValue("filialId", venda.FilialId);
            command.Parameters.AddWithValue("clienteId", (object?)venda.ClienteId ?? DBNull.Value);
            command.Parameters.AddWithValue("vendedorId", venda.VendedorId);
            command.Parameters.AddWithValue("tipo", TipoParaTexto(venda.Tipo));
            command.Parameters.AddWithValue("status", StatusParaTexto(venda.Status));
            command.Parameters.AddWithValue("tabelaPrecoId", (object?)venda.TabelaPrecoId ?? DBNull.Value);
            command.Parameters.AddWithValue("condicaoPagamentoId", (object?)venda.CondicaoPagamentoId ?? DBNull.Value);
            command.Parameters.AddWithValue("subtotal", venda.Subtotal);
            command.Parameters.AddWithValue("desconto", venda.Desconto);
            command.Parameters.AddWithValue("acrescimo", venda.Acrescimo);
            command.Parameters.AddWithValue("total", venda.Total);
            command.Parameters.AddWithValue("usuarioAberturaId", venda.UsuarioAberturaId);
            venda.Id = (long)(await command.ExecuteScalarAsync(cancellationToken))!;
        }

        const string sqlItem = """
            INSERT INTO mv_venda_produto
                (venda_id, item_numero, produto_id, servico_id, quantidade, valor_unitario, valor_unitario_final,
                 desconto, acrescimo, subtotal, total)
            VALUES
                (@vendaId, @itemNumero, @produtoId, @servicoId, @quantidade, @valorUnitario, @valorUnitarioFinal,
                 @desconto, @acrescimo, @subtotal, @total)
            RETURNING id
            """;

        for (var indice = 0; indice < venda.Itens.Count; indice++)
        {
            var item = venda.Itens[indice];
            item.VendaId = venda.Id;
            item.ItemNumero = indice + 1;

            await using var command = new NpgsqlCommand(sqlItem, connection, transaction);
            command.Parameters.AddWithValue("vendaId", item.VendaId);
            command.Parameters.AddWithValue("itemNumero", item.ItemNumero);
            command.Parameters.AddWithValue("produtoId", (object?)item.ProdutoId ?? DBNull.Value);
            command.Parameters.AddWithValue("servicoId", (object?)item.ServicoId ?? DBNull.Value);
            command.Parameters.AddWithValue("quantidade", item.Quantidade);
            command.Parameters.AddWithValue("valorUnitario", item.ValorUnitario);
            command.Parameters.AddWithValue("valorUnitarioFinal", item.ValorUnitarioFinal);
            command.Parameters.AddWithValue("desconto", item.Desconto);
            command.Parameters.AddWithValue("acrescimo", item.Acrescimo);
            command.Parameters.AddWithValue("subtotal", item.Subtotal);
            command.Parameters.AddWithValue("total", item.Total);
            item.Id = (long)(await command.ExecuteScalarAsync(cancellationToken))!;
        }

        const string sqlPagamento = """
            INSERT INTO mv_venda_pagamento (venda_id, forma_pagamento_id, condicao_pagamento_id, valor, parcelas, status)
            VALUES (@vendaId, @formaPagamentoId, @condicaoPagamentoId, @valor, @parcelas, @status)
            RETURNING id
            """;

        foreach (var pagamento in venda.Pagamentos)
        {
            pagamento.VendaId = venda.Id;

            await using var command = new NpgsqlCommand(sqlPagamento, connection, transaction);
            command.Parameters.AddWithValue("vendaId", pagamento.VendaId);
            command.Parameters.AddWithValue("formaPagamentoId", pagamento.FormaPagamentoId);
            command.Parameters.AddWithValue("condicaoPagamentoId", (object?)pagamento.CondicaoPagamentoId ?? DBNull.Value);
            command.Parameters.AddWithValue("valor", pagamento.Valor);
            command.Parameters.AddWithValue("parcelas", pagamento.Parcelas);
            command.Parameters.AddWithValue("status", StatusPagamentoParaTexto(pagamento.Status));
            pagamento.Id = (long)(await command.ExecuteScalarAsync(cancellationToken))!;
        }
    }

    public async Task<Venda?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default)
    {
        const string sqlVenda = """
            SELECT id, numero, filial_id, cliente_id, vendedor_id, tipo, status, tabela_preco_id,
                   condicao_pagamento_id, subtotal, desconto, acrescimo, total, data_venda,
                   data_cancelamento, motivo_cancelamento, usuario_abertura_id, usuario_cancelamento_id
            FROM mv_venda WHERE id = @id
            """;

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);

        Venda venda;
        await using (var command = new NpgsqlCommand(sqlVenda, connection))
        {
            command.Parameters.AddWithValue("id", id);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            venda = MapVenda(reader);
        }

        const string sqlItens = """
            SELECT id, venda_id, item_numero, produto_id, servico_id, quantidade, valor_unitario,
                   valor_unitario_final, desconto, acrescimo, subtotal, total, cancelado
            FROM mv_venda_produto WHERE venda_id = @vendaId ORDER BY item_numero
            """;

        await using (var command = new NpgsqlCommand(sqlItens, connection))
        {
            command.Parameters.AddWithValue("vendaId", id);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                venda.Itens.Add(MapItem(reader));
            }
        }

        const string sqlPagamentos = """
            SELECT id, venda_id, forma_pagamento_id, condicao_pagamento_id, valor, parcelas, status
            FROM mv_venda_pagamento WHERE venda_id = @vendaId
            """;

        await using (var command = new NpgsqlCommand(sqlPagamentos, connection))
        {
            command.Parameters.AddWithValue("vendaId", id);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                venda.Pagamentos.Add(MapPagamento(reader));
            }
        }

        return venda;
    }

    public async Task<IReadOnlyList<Venda>> ListarPorFilialEDataAsync(
        long filialId, DateOnly data, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, numero, filial_id, cliente_id, vendedor_id, tipo, status, tabela_preco_id,
                   condicao_pagamento_id, subtotal, desconto, acrescimo, total, data_venda,
                   data_cancelamento, motivo_cancelamento, usuario_abertura_id, usuario_cancelamento_id
            FROM mv_venda
            WHERE filial_id = @filialId AND data_venda >= @inicio AND data_venda < @fim
            ORDER BY numero DESC
            """;

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("filialId", filialId);
        command.Parameters.AddWithValue("inicio", data.ToDateTime(TimeOnly.MinValue));
        command.Parameters.AddWithValue("fim", data.AddDays(1).ToDateTime(TimeOnly.MinValue));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var vendas = new List<Venda>();
        while (await reader.ReadAsync(cancellationToken))
        {
            vendas.Add(MapVenda(reader));
        }

        return vendas;
    }

    public async Task CancelarAsync(
        long vendaId, long usuarioId, string motivo, IUnitOfWork unitOfWork, CancellationToken cancellationToken = default)
    {
        var uow = (NpgsqlUnitOfWork)unitOfWork;
        var connection = uow.Connection ?? throw new InvalidOperationException("Transação não iniciada.");
        var transaction = uow.Transaction ?? throw new InvalidOperationException("Transação não iniciada.");

        const string sql = """
            UPDATE mv_venda
            SET status = 'cancelada', data_cancelamento = now(), motivo_cancelamento = @motivo,
                usuario_cancelamento_id = @usuarioId
            WHERE id = @id AND status <> 'cancelada'
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("motivo", motivo);
        command.Parameters.AddWithValue("usuarioId", usuarioId);
        command.Parameters.AddWithValue("id", vendaId);
        var linhasAfetadas = await command.ExecuteNonQueryAsync(cancellationToken);

        if (linhasAfetadas == 0)
        {
            throw new InvalidOperationException($"Venda {vendaId} não encontrada ou já está cancelada.");
        }
    }

    private static Venda MapVenda(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetInt64(0),
        Numero = reader.GetInt64(1),
        FilialId = reader.GetInt64(2),
        ClienteId = reader.IsDBNull(3) ? null : reader.GetInt64(3),
        VendedorId = reader.GetInt64(4),
        Tipo = TextoParaTipo(reader.GetString(5)),
        Status = TextoParaStatus(reader.GetString(6)),
        TabelaPrecoId = reader.IsDBNull(7) ? null : reader.GetInt64(7),
        CondicaoPagamentoId = reader.IsDBNull(8) ? null : reader.GetInt64(8),
        Subtotal = reader.GetDecimal(9),
        Desconto = reader.GetDecimal(10),
        Acrescimo = reader.GetDecimal(11),
        Total = reader.GetDecimal(12),
        DataVenda = reader.GetFieldValue<DateTimeOffset>(13),
        DataCancelamento = reader.IsDBNull(14) ? null : reader.GetFieldValue<DateTimeOffset>(14),
        MotivoCancelamento = reader.IsDBNull(15) ? null : reader.GetString(15),
        UsuarioAberturaId = reader.GetInt64(16),
        UsuarioCancelamentoId = reader.IsDBNull(17) ? null : reader.GetInt64(17)
    };

    private static VendaProduto MapItem(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetInt64(0),
        VendaId = reader.GetInt64(1),
        ItemNumero = reader.GetInt32(2),
        ProdutoId = reader.IsDBNull(3) ? null : reader.GetInt64(3),
        ServicoId = reader.IsDBNull(4) ? null : reader.GetInt64(4),
        Quantidade = reader.GetDecimal(5),
        ValorUnitario = reader.GetDecimal(6),
        ValorUnitarioFinal = reader.GetDecimal(7),
        Desconto = reader.GetDecimal(8),
        Acrescimo = reader.GetDecimal(9),
        Subtotal = reader.GetDecimal(10),
        Total = reader.GetDecimal(11),
        Cancelado = reader.GetBoolean(12)
    };

    private static VendaPagamento MapPagamento(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetInt64(0),
        VendaId = reader.GetInt64(1),
        FormaPagamentoId = reader.GetInt64(2),
        CondicaoPagamentoId = reader.IsDBNull(3) ? null : reader.GetInt64(3),
        Valor = reader.GetDecimal(4),
        Parcelas = reader.GetInt32(5),
        Status = TextoParaStatusPagamento(reader.GetString(6))
    };

    private static string TipoParaTexto(TipoVenda tipo) => tipo == TipoVenda.PreVenda ? "pre_venda" : "venda";

    private static TipoVenda TextoParaTipo(string texto) => texto == "pre_venda" ? TipoVenda.PreVenda : TipoVenda.Venda;

    private static string StatusParaTexto(StatusVenda status) => status switch
    {
        StatusVenda.Aberta => "aberta",
        StatusVenda.Finalizada => "finalizada",
        StatusVenda.Cancelada => "cancelada",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    private static StatusVenda TextoParaStatus(string texto) => texto switch
    {
        "aberta" => StatusVenda.Aberta,
        "finalizada" => StatusVenda.Finalizada,
        "cancelada" => StatusVenda.Cancelada,
        _ => throw new ArgumentOutOfRangeException(nameof(texto))
    };

    private static string StatusPagamentoParaTexto(StatusVendaPagamento status) => status switch
    {
        StatusVendaPagamento.Confirmado => "confirmado",
        StatusVendaPagamento.Cancelado => "cancelado",
        StatusVendaPagamento.Estornado => "estornado",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    private static StatusVendaPagamento TextoParaStatusPagamento(string texto) => texto switch
    {
        "confirmado" => StatusVendaPagamento.Confirmado,
        "cancelado" => StatusVendaPagamento.Cancelado,
        "estornado" => StatusVendaPagamento.Estornado,
        _ => throw new ArgumentOutOfRangeException(nameof(texto))
    };
}
