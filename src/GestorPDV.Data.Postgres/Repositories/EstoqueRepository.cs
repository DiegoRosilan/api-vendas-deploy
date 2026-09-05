using GestorPDV.Application.Common;
using GestorPDV.Application.Estoque;
using GestorPDV.Domain.Estoque;
using GestorPDV.Infrastructure.Database;
using Npgsql;

namespace GestorPDV.Data.Postgres.Repositories;

public class EstoqueRepository : IEstoqueRepository
{
    private readonly NpgsqlConnectionFactory _connectionFactory;

    public EstoqueRepository(NpgsqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<LocalEstoque?> ObterLocalPadraoAsync(long filialId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, filial_id, descricao, ativo FROM est_local_estoque
            WHERE filial_id = @filialId AND ativo = TRUE
            ORDER BY id
            LIMIT 1
            """;

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("filialId", filialId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new LocalEstoque
        {
            Id = reader.GetInt64(0),
            FilialId = reader.GetInt64(1),
            Descricao = reader.GetString(2),
            Ativo = reader.GetBoolean(3)
        };
    }

    public async Task<decimal> ObterSaldoAsync(
        long produtoId, long localEstoqueId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT quantidade FROM est_estoque
            WHERE produto_id = @produtoId AND local_estoque_id = @localEstoqueId
                AND produto_grade_id IS NULL AND produto_lote_id IS NULL
            """;

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("produtoId", produtoId);
        command.Parameters.AddWithValue("localEstoqueId", localEstoqueId);

        var resultado = await command.ExecuteScalarAsync(cancellationToken);
        return resultado is null ? 0m : (decimal)resultado;
    }

    public async Task<MovimentacaoEstoque> RegistrarMovimentacaoAsync(
        long produtoId,
        long localEstoqueId,
        decimal quantidade,
        TipoMovimentacaoEstoque tipo,
        OrigemMovimentacaoEstoque origem,
        string documentoTipo,
        long documentoId,
        long usuarioId,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = Desempacotar(unitOfWork);

        // Bloqueia a linha de saldo (se existir) para evitar concorrência
        // entre duas vendas baixando o mesmo produto ao mesmo tempo. Não
        // usamos INSERT ... ON CONFLICT aqui porque o UNIQUE de est_estoque
        // inclui colunas anuláveis (produto_grade_id/produto_lote_id) e o
        // Postgres não considera NULLs iguais para fins de conflito.
        const string sqlSelecionar = """
            SELECT id, quantidade FROM est_estoque
            WHERE produto_id = @produtoId AND local_estoque_id = @localEstoqueId
                AND produto_grade_id IS NULL AND produto_lote_id IS NULL
            FOR UPDATE
            """;

        long? estoqueId = null;
        var quantidadeAnterior = 0m;

        await using (var selecionar = new NpgsqlCommand(sqlSelecionar, connection, transaction))
        {
            selecionar.Parameters.AddWithValue("produtoId", produtoId);
            selecionar.Parameters.AddWithValue("localEstoqueId", localEstoqueId);
            await using var reader = await selecionar.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                estoqueId = reader.GetInt64(0);
                quantidadeAnterior = reader.GetDecimal(1);
            }
        }

        var quantidadeAtual = quantidadeAnterior + quantidade;

        if (estoqueId is null)
        {
            const string sqlInserirSaldo = """
                INSERT INTO est_estoque (produto_id, local_estoque_id, quantidade)
                VALUES (@produtoId, @localEstoqueId, @quantidade)
                """;
            await using var inserirSaldo = new NpgsqlCommand(sqlInserirSaldo, connection, transaction);
            inserirSaldo.Parameters.AddWithValue("produtoId", produtoId);
            inserirSaldo.Parameters.AddWithValue("localEstoqueId", localEstoqueId);
            inserirSaldo.Parameters.AddWithValue("quantidade", quantidadeAtual);
            await inserirSaldo.ExecuteNonQueryAsync(cancellationToken);
        }
        else
        {
            const string sqlAtualizarSaldo =
                "UPDATE est_estoque SET quantidade = @quantidade, atualizado_em = now() WHERE id = @id";
            await using var atualizarSaldo = new NpgsqlCommand(sqlAtualizarSaldo, connection, transaction);
            atualizarSaldo.Parameters.AddWithValue("quantidade", quantidadeAtual);
            atualizarSaldo.Parameters.AddWithValue("id", estoqueId.Value);
            await atualizarSaldo.ExecuteNonQueryAsync(cancellationToken);
        }

        const string sqlMovimentacao = """
            INSERT INTO est_movimentacao
                (produto_id, local_estoque_id, tipo, origem, documento_tipo, documento_id,
                 quantidade, quantidade_anterior, quantidade_atual, usuario_id, data_movimento)
            VALUES
                (@produtoId, @localEstoqueId, @tipo, @origem, @documentoTipo, @documentoId,
                 @quantidade, @quantidadeAnterior, @quantidadeAtual, @usuarioId, now())
            RETURNING id, data_movimento
            """;

        await using var inserirMovimentacao = new NpgsqlCommand(sqlMovimentacao, connection, transaction);
        inserirMovimentacao.Parameters.AddWithValue("produtoId", produtoId);
        inserirMovimentacao.Parameters.AddWithValue("localEstoqueId", localEstoqueId);
        inserirMovimentacao.Parameters.AddWithValue("tipo", TipoParaTexto(tipo));
        inserirMovimentacao.Parameters.AddWithValue("origem", OrigemParaTexto(origem));
        inserirMovimentacao.Parameters.AddWithValue("documentoTipo", documentoTipo);
        inserirMovimentacao.Parameters.AddWithValue("documentoId", documentoId);
        inserirMovimentacao.Parameters.AddWithValue("quantidade", quantidade);
        inserirMovimentacao.Parameters.AddWithValue("quantidadeAnterior", quantidadeAnterior);
        inserirMovimentacao.Parameters.AddWithValue("quantidadeAtual", quantidadeAtual);
        inserirMovimentacao.Parameters.AddWithValue("usuarioId", usuarioId);

        await using var reader2 = await inserirMovimentacao.ExecuteReaderAsync(cancellationToken);
        await reader2.ReadAsync(cancellationToken);

        return new MovimentacaoEstoque
        {
            Id = reader2.GetInt64(0),
            ProdutoId = produtoId,
            LocalEstoqueId = localEstoqueId,
            Tipo = tipo,
            Origem = origem,
            DocumentoTipo = documentoTipo,
            DocumentoId = documentoId,
            Quantidade = quantidade,
            QuantidadeAnterior = quantidadeAnterior,
            QuantidadeAtual = quantidadeAtual,
            UsuarioId = usuarioId,
            DataMovimento = reader2.GetFieldValue<DateTimeOffset>(1)
        };
    }

    public async Task EstornarMovimentacaoAsync(
        long movimentacaoId, long usuarioId, IUnitOfWork unitOfWork, CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = Desempacotar(unitOfWork);

        const string sqlObter = """
            SELECT produto_id, local_estoque_id, quantidade, tipo, origem, documento_tipo, documento_id, estornado
            FROM est_movimentacao WHERE id = @id
            FOR UPDATE
            """;

        long produtoId;
        long localEstoqueId;
        decimal quantidade;
        string origemTexto;
        string documentoTipo;
        long documentoId;

        await using (var obter = new NpgsqlCommand(sqlObter, connection, transaction))
        {
            obter.Parameters.AddWithValue("id", movimentacaoId);
            await using var reader = await obter.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException($"Movimentação de estoque {movimentacaoId} não encontrada.");
            }

            if (reader.GetBoolean(7))
            {
                throw new InvalidOperationException($"Movimentação de estoque {movimentacaoId} já foi estornada.");
            }

            produtoId = reader.GetInt64(0);
            localEstoqueId = reader.GetInt64(1);
            quantidade = reader.GetDecimal(2);
            // Coluna 3 (tipo) não é necessária: o movimento de reversão
            // sempre usa o tipo "estorno", independente do tipo original.
            origemTexto = reader.GetString(4);
            documentoTipo = reader.GetString(5);
            documentoId = reader.GetInt64(6);
        }

        // Lança o movimento inverso (mesma quantidade em módulo, sinal
        // trocado, tipo "estorno") em vez de apagar o histórico original
        // (RN-EST-003). A origem é preservada para rastrear de onde veio.
        var movimentoEstorno = await RegistrarMovimentacaoAsync(
            produtoId, localEstoqueId, -quantidade, TipoMovimentacaoEstoque.Estorno, TextoParaOrigem(origemTexto),
            documentoTipo, documentoId, usuarioId, unitOfWork, cancellationToken);

        const string sqlMarcarEstornado = """
            UPDATE est_movimentacao SET estornado = TRUE, movimentacao_estorno_id = @estornoId WHERE id = @id
            """;
        await using var marcar = new NpgsqlCommand(sqlMarcarEstornado, connection, transaction);
        marcar.Parameters.AddWithValue("estornoId", movimentoEstorno.Id);
        marcar.Parameters.AddWithValue("id", movimentacaoId);
        await marcar.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MovimentacaoEstoque>> ListarPorDocumentoAsync(
        string documentoTipo, long documentoId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, produto_id, local_estoque_id, tipo, origem, documento_tipo, documento_id,
                   quantidade, quantidade_anterior, quantidade_atual, usuario_id, data_movimento, estornado
            FROM est_movimentacao
            WHERE documento_tipo = @documentoTipo AND documento_id = @documentoId
            ORDER BY id
            """;

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("documentoTipo", documentoTipo);
        command.Parameters.AddWithValue("documentoId", documentoId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var movimentacoes = new List<MovimentacaoEstoque>();
        while (await reader.ReadAsync(cancellationToken))
        {
            movimentacoes.Add(new MovimentacaoEstoque
            {
                Id = reader.GetInt64(0),
                ProdutoId = reader.GetInt64(1),
                LocalEstoqueId = reader.GetInt64(2),
                Tipo = TextoParaTipo(reader.GetString(3)),
                Origem = TextoParaOrigem(reader.GetString(4)),
                DocumentoTipo = reader.GetString(5),
                DocumentoId = reader.GetInt64(6),
                Quantidade = reader.GetDecimal(7),
                QuantidadeAnterior = reader.GetDecimal(8),
                QuantidadeAtual = reader.GetDecimal(9),
                UsuarioId = reader.GetInt64(10),
                DataMovimento = reader.GetFieldValue<DateTimeOffset>(11),
                Estornado = reader.GetBoolean(12)
            });
        }

        return movimentacoes;
    }

    private static (NpgsqlConnection Connection, NpgsqlTransaction Transaction) Desempacotar(IUnitOfWork unitOfWork)
    {
        var uow = (NpgsqlUnitOfWork)unitOfWork;
        var connection = uow.Connection;
        var transaction = uow.Transaction;
        if (connection is null || transaction is null)
        {
            throw new InvalidOperationException("A transação não foi iniciada (chame BeginAsync antes).");
        }

        return (connection, transaction);
    }

    private static string TipoParaTexto(TipoMovimentacaoEstoque tipo) => tipo switch
    {
        TipoMovimentacaoEstoque.Entrada => "entrada",
        TipoMovimentacaoEstoque.Saida => "saida",
        TipoMovimentacaoEstoque.Transferencia => "transferencia",
        TipoMovimentacaoEstoque.Perda => "perda",
        TipoMovimentacaoEstoque.Inventario => "inventario",
        TipoMovimentacaoEstoque.Estorno => "estorno",
        _ => throw new ArgumentOutOfRangeException(nameof(tipo))
    };

    private static TipoMovimentacaoEstoque TextoParaTipo(string texto) => texto switch
    {
        "entrada" => TipoMovimentacaoEstoque.Entrada,
        "saida" => TipoMovimentacaoEstoque.Saida,
        "transferencia" => TipoMovimentacaoEstoque.Transferencia,
        "perda" => TipoMovimentacaoEstoque.Perda,
        "inventario" => TipoMovimentacaoEstoque.Inventario,
        "estorno" => TipoMovimentacaoEstoque.Estorno,
        _ => throw new ArgumentOutOfRangeException(nameof(texto))
    };

    private static string OrigemParaTexto(OrigemMovimentacaoEstoque origem) => origem switch
    {
        OrigemMovimentacaoEstoque.Venda => "venda",
        OrigemMovimentacaoEstoque.Compra => "compra",
        OrigemMovimentacaoEstoque.Ajuste => "ajuste",
        OrigemMovimentacaoEstoque.Devolucao => "devolucao",
        OrigemMovimentacaoEstoque.Producao => "producao",
        OrigemMovimentacaoEstoque.Transferencia => "transferencia",
        OrigemMovimentacaoEstoque.Inventario => "inventario",
        _ => throw new ArgumentOutOfRangeException(nameof(origem))
    };

    private static OrigemMovimentacaoEstoque TextoParaOrigem(string texto) => texto switch
    {
        "venda" => OrigemMovimentacaoEstoque.Venda,
        "compra" => OrigemMovimentacaoEstoque.Compra,
        "ajuste" => OrigemMovimentacaoEstoque.Ajuste,
        "devolucao" => OrigemMovimentacaoEstoque.Devolucao,
        "producao" => OrigemMovimentacaoEstoque.Producao,
        "transferencia" => OrigemMovimentacaoEstoque.Transferencia,
        "inventario" => OrigemMovimentacaoEstoque.Inventario,
        _ => throw new ArgumentOutOfRangeException(nameof(texto))
    };
}
