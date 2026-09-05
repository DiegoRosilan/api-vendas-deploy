using GestorPDV.Application.Relatorios;
using GestorPDV.Infrastructure.Database;
using Npgsql;

namespace GestorPDV.Data.Postgres.Repositories;

public class RelatorioRepository : IRelatorioRepository
{
    private readonly NpgsqlConnectionFactory _connectionFactory;

    public RelatorioRepository(NpgsqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<VendaRelatorioItem>> ListarVendasAsync(
        long filialId, DateOnly dataInicio, DateOnly dataFim, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT v.id, v.numero, v.data_venda, cli.nome AS cliente_nome, vend.nome AS vendedor_nome,
                   v.subtotal, v.desconto, v.total, v.status
            FROM mv_venda v
            LEFT JOIN cad_pessoa cli ON cli.id = v.cliente_id
            JOIN cad_pessoa vend ON vend.id = v.vendedor_id
            WHERE v.filial_id = @filialId AND v.data_venda >= @inicio AND v.data_venda < @fim
            ORDER BY v.data_venda
            """;

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("filialId", filialId);
        command.Parameters.AddWithValue("inicio", dataInicio.ToDateTime(TimeOnly.MinValue));
        command.Parameters.AddWithValue("fim", dataFim.AddDays(1).ToDateTime(TimeOnly.MinValue));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var itens = new List<VendaRelatorioItem>();
        while (await reader.ReadAsync(cancellationToken))
        {
            itens.Add(new VendaRelatorioItem
            {
                Id = reader.GetInt64(0),
                Numero = reader.GetInt64(1),
                DataVenda = reader.GetFieldValue<DateTimeOffset>(2),
                ClienteNome = reader.IsDBNull(3) ? "Consumidor final" : reader.GetString(3),
                VendedorNome = reader.GetString(4),
                Subtotal = reader.GetDecimal(5),
                Desconto = reader.GetDecimal(6),
                Total = reader.GetDecimal(7),
                Status = TextoParaStatus(reader.GetString(8))
            });
        }

        return itens;
    }

    public async Task<IReadOnlyList<EstoqueRelatorioItem>> ListarEstoqueAtualAsync(
        long filialId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT p.id, p.descricao, p.codigo_barras, SUM(e.quantidade) AS quantidade, p.preco_venda
            FROM est_estoque e
            JOIN cad_produto p ON p.id = e.produto_id
            JOIN est_local_estoque l ON l.id = e.local_estoque_id
            WHERE l.filial_id = @filialId
            GROUP BY p.id, p.descricao, p.codigo_barras, p.preco_venda
            ORDER BY p.descricao
            """;

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("filialId", filialId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var itens = new List<EstoqueRelatorioItem>();
        while (await reader.ReadAsync(cancellationToken))
        {
            itens.Add(new EstoqueRelatorioItem
            {
                ProdutoId = reader.GetInt64(0),
                ProdutoDescricao = reader.GetString(1),
                CodigoBarras = reader.IsDBNull(2) ? null : reader.GetString(2),
                QuantidadeAtual = reader.GetDecimal(3),
                PrecoVenda = reader.GetDecimal(4)
            });
        }

        return itens;
    }

    public async Task<IReadOnlyList<ContaReceberRelatorioItem>> ListarContasReceberAsync(
        long filialId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT d.id, d.numero_documento, pa.numero_parcela, cli.nome, pa.vencimento, pa.valor, pa.situacao
            FROM fin_parcela pa
            JOIN crb_documento d ON d.id = pa.documento_id
            JOIN cad_pessoa cli ON cli.id = d.pessoa_id
            WHERE d.filial_id = @filialId AND pa.situacao IN ('aberto', 'parcial')
            ORDER BY pa.vencimento
            """;

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("filialId", filialId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var itens = new List<ContaReceberRelatorioItem>();
        while (await reader.ReadAsync(cancellationToken))
        {
            itens.Add(new ContaReceberRelatorioItem
            {
                DocumentoId = reader.GetInt64(0),
                NumeroDocumento = reader.GetString(1),
                NumeroParcela = reader.GetInt32(2),
                ClienteNome = reader.GetString(3),
                Vencimento = reader.GetFieldValue<DateOnly>(4),
                Valor = reader.GetDecimal(5),
                Situacao = reader.GetString(6) == "parcial" ? "Parcial" : "Aberto"
            });
        }

        return itens;
    }

    private static string TextoParaStatus(string texto) => texto switch
    {
        "aberta" => "Aberta",
        "finalizada" => "Finalizada",
        "cancelada" => "Cancelada",
        _ => texto
    };
}
