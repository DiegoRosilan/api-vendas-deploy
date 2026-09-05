using GestorPDV.Application.Cadastros;
using GestorPDV.Domain.Cadastros;
using GestorPDV.Infrastructure.Database;
using Npgsql;

namespace GestorPDV.Data.Postgres.Repositories;

public class ProdutoRepository : IProdutoRepository
{
    private const string Colunas = """
        id, codigo, codigo_barras, descricao, categoria_id, unidade, ncm, cest,
        preco_custo, preco_custo_medio, preco_venda, preco_minimo, preco_promocional,
        markup_pct, margem_contribuicao_pct, estoque_minimo, estoque_maximo, localizacao,
        controla_estoque, controla_grade, controla_lote, controla_serial,
        desconto_maximo_pct, bloquear_desconto, ativo
        """;

    private readonly NpgsqlConnectionFactory _connectionFactory;

    public ProdutoRepository(NpgsqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Produto>> ListarAsync(string? filtro, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            SELECT {Colunas} FROM cad_produto
            WHERE @filtro::text IS NULL OR descricao ILIKE @filtro OR codigo ILIKE @filtro OR codigo_barras ILIKE @filtro
            ORDER BY descricao
            """;

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue(
            "filtro", (object?)(string.IsNullOrWhiteSpace(filtro) ? null : $"%{filtro}%") ?? DBNull.Value);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var produtos = new List<Produto>();
        while (await reader.ReadAsync(cancellationToken))
        {
            produtos.Add(MapProduto(reader));
        }

        return produtos;
    }

    public async Task<Produto?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var sql = $"SELECT {Colunas} FROM cad_produto WHERE id = @id";

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapProduto(reader) : null;
    }

    public async Task<Produto?> ObterPorCodigoBarrasAsync(string codigoBarras, CancellationToken cancellationToken = default)
    {
        var sql = $"SELECT {Colunas} FROM cad_produto WHERE codigo_barras = @codigoBarras";

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("codigoBarras", codigoBarras);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapProduto(reader) : null;
    }

    public async Task<long> InserirAsync(Produto produto, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO cad_produto
                (codigo, codigo_barras, descricao, categoria_id, unidade, ncm, cest,
                 preco_custo, preco_custo_medio, preco_venda, preco_minimo, preco_promocional,
                 markup_pct, margem_contribuicao_pct, estoque_minimo, estoque_maximo, localizacao,
                 controla_estoque, controla_grade, controla_lote, controla_serial,
                 desconto_maximo_pct, bloquear_desconto, ativo)
            VALUES
                (@codigo, @codigoBarras, @descricao, @categoriaId, @unidade, @ncm, @cest,
                 @precoCusto, @precoCustoMedio, @precoVenda, @precoMinimo, @precoPromocional,
                 @markupPct, @margemContribuicaoPct, @estoqueMinimo, @estoqueMaximo, @localizacao,
                 @controlaEstoque, @controlaGrade, @controlaLote, @controlaSerial,
                 @descontoMaximoPct, @bloquearDesconto, @ativo)
            RETURNING id
            """;

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        AdicionarParametros(command, produto);
        return (long)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    public async Task AtualizarAsync(Produto produto, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE cad_produto SET
                codigo = @codigo, codigo_barras = @codigoBarras, descricao = @descricao,
                categoria_id = @categoriaId, unidade = @unidade, ncm = @ncm, cest = @cest,
                preco_custo = @precoCusto, preco_custo_medio = @precoCustoMedio, preco_venda = @precoVenda,
                preco_minimo = @precoMinimo, preco_promocional = @precoPromocional,
                markup_pct = @markupPct, margem_contribuicao_pct = @margemContribuicaoPct,
                estoque_minimo = @estoqueMinimo, estoque_maximo = @estoqueMaximo, localizacao = @localizacao,
                controla_estoque = @controlaEstoque, controla_grade = @controlaGrade,
                controla_lote = @controlaLote, controla_serial = @controlaSerial,
                desconto_maximo_pct = @descontoMaximoPct, bloquear_desconto = @bloquearDesconto,
                ativo = @ativo, atualizado_em = now()
            WHERE id = @id
            """;

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", produto.Id);
        AdicionarParametros(command, produto);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AdicionarParametros(NpgsqlCommand command, Produto produto)
    {
        command.Parameters.AddWithValue("codigo", produto.Codigo);
        command.Parameters.AddWithValue("codigoBarras", (object?)produto.CodigoBarras ?? DBNull.Value);
        command.Parameters.AddWithValue("descricao", produto.Descricao);
        command.Parameters.AddWithValue("categoriaId", (object?)produto.CategoriaId ?? DBNull.Value);
        command.Parameters.AddWithValue("unidade", produto.Unidade);
        command.Parameters.AddWithValue("ncm", (object?)produto.Ncm ?? DBNull.Value);
        command.Parameters.AddWithValue("cest", (object?)produto.Cest ?? DBNull.Value);
        command.Parameters.AddWithValue("precoCusto", produto.PrecoCusto);
        command.Parameters.AddWithValue("precoCustoMedio", produto.PrecoCustoMedio);
        command.Parameters.AddWithValue("precoVenda", produto.PrecoVenda);
        command.Parameters.AddWithValue("precoMinimo", (object?)produto.PrecoMinimo ?? DBNull.Value);
        command.Parameters.AddWithValue("precoPromocional", (object?)produto.PrecoPromocional ?? DBNull.Value);
        command.Parameters.AddWithValue("markupPct", (object?)produto.MarkupPct ?? DBNull.Value);
        command.Parameters.AddWithValue("margemContribuicaoPct", (object?)produto.MargemContribuicaoPct ?? DBNull.Value);
        command.Parameters.AddWithValue("estoqueMinimo", produto.EstoqueMinimo);
        command.Parameters.AddWithValue("estoqueMaximo", (object?)produto.EstoqueMaximo ?? DBNull.Value);
        command.Parameters.AddWithValue("localizacao", (object?)produto.Localizacao ?? DBNull.Value);
        command.Parameters.AddWithValue("controlaEstoque", produto.ControlaEstoque);
        command.Parameters.AddWithValue("controlaGrade", produto.ControlaGrade);
        command.Parameters.AddWithValue("controlaLote", produto.ControlaLote);
        command.Parameters.AddWithValue("controlaSerial", produto.ControlaSerial);
        command.Parameters.AddWithValue("descontoMaximoPct", produto.DescontoMaximoPct);
        command.Parameters.AddWithValue("bloquearDesconto", produto.BloquearDesconto);
        command.Parameters.AddWithValue("ativo", produto.Ativo);
    }

    private static Produto MapProduto(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetInt64(0),
        Codigo = reader.GetString(1),
        CodigoBarras = reader.IsDBNull(2) ? null : reader.GetString(2),
        Descricao = reader.GetString(3),
        CategoriaId = reader.IsDBNull(4) ? null : reader.GetInt64(4),
        Unidade = reader.GetString(5),
        Ncm = reader.IsDBNull(6) ? null : reader.GetString(6),
        Cest = reader.IsDBNull(7) ? null : reader.GetString(7),
        PrecoCusto = reader.GetDecimal(8),
        PrecoCustoMedio = reader.GetDecimal(9),
        PrecoVenda = reader.GetDecimal(10),
        PrecoMinimo = reader.IsDBNull(11) ? null : reader.GetDecimal(11),
        PrecoPromocional = reader.IsDBNull(12) ? null : reader.GetDecimal(12),
        MarkupPct = reader.IsDBNull(13) ? null : reader.GetDecimal(13),
        MargemContribuicaoPct = reader.IsDBNull(14) ? null : reader.GetDecimal(14),
        EstoqueMinimo = reader.GetDecimal(15),
        EstoqueMaximo = reader.IsDBNull(16) ? null : reader.GetDecimal(16),
        Localizacao = reader.IsDBNull(17) ? null : reader.GetString(17),
        ControlaEstoque = reader.GetBoolean(18),
        ControlaGrade = reader.GetBoolean(19),
        ControlaLote = reader.GetBoolean(20),
        ControlaSerial = reader.GetBoolean(21),
        DescontoMaximoPct = reader.GetDecimal(22),
        BloquearDesconto = reader.GetBoolean(23),
        Ativo = reader.GetBoolean(24)
    };
}
