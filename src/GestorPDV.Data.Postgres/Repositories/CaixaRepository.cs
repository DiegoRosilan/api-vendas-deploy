using GestorPDV.Application.Caixa;
using GestorPDV.Application.Common;
using GestorPDV.Domain.Caixa;
using GestorPDV.Infrastructure.Database;
using Npgsql;
// Alias necessário: ICaixaRepository já usa CaixaEntidade como nome da
// classe de domínio — mantemos o mesmo alias aqui por consistência e para
// evitar qualquer ambiguidade com os demais namespaces "...Caixa".
using CaixaEntidade = GestorPDV.Domain.Caixa.Caixa;

namespace GestorPDV.Data.Postgres.Repositories;

public class CaixaRepository : ICaixaRepository
{
    private const string ColunasCaixa = """
        id, filial_id, usuario_abertura_id, data_abertura, valor_abertura, usuario_fechamento_id,
        data_fechamento, valor_fechamento_informado, valor_fechamento_calculado, diferenca, status
        """;

    private readonly NpgsqlConnectionFactory _connectionFactory;

    public CaixaRepository(NpgsqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<CaixaEntidade?> ObterAbertoAsync(long filialId, CancellationToken cancellationToken = default)
    {
        var sql = $"SELECT {ColunasCaixa} FROM cx_caixa WHERE filial_id = @filialId AND status = 'aberto' ORDER BY id DESC LIMIT 1";

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("filialId", filialId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapCaixa(reader) : null;
    }

    public async Task<long> AbrirAsync(CaixaEntidade caixa, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO cx_caixa (filial_id, usuario_abertura_id, data_abertura, valor_abertura, status)
            VALUES (@filialId, @usuarioAberturaId, now(), @valorAbertura, 'aberto')
            RETURNING id
            """;

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("filialId", caixa.FilialId);
        command.Parameters.AddWithValue("usuarioAberturaId", caixa.UsuarioAberturaId);
        command.Parameters.AddWithValue("valorAbertura", caixa.ValorAbertura);

        return (long)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    public async Task FecharAsync(
        long caixaId, long usuarioId, decimal valorInformado, decimal valorCalculado, decimal diferenca,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE cx_caixa
            SET status = 'fechado', usuario_fechamento_id = @usuarioId, data_fechamento = now(),
                valor_fechamento_informado = @valorInformado, valor_fechamento_calculado = @valorCalculado,
                diferenca = @diferenca
            WHERE id = @id AND status = 'aberto'
            """;

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("usuarioId", usuarioId);
        command.Parameters.AddWithValue("valorInformado", valorInformado);
        command.Parameters.AddWithValue("valorCalculado", valorCalculado);
        command.Parameters.AddWithValue("diferenca", diferenca);
        command.Parameters.AddWithValue("id", caixaId);

        var linhasAfetadas = await command.ExecuteNonQueryAsync(cancellationToken);
        if (linhasAfetadas == 0)
        {
            throw new InvalidOperationException($"Caixa {caixaId} não encontrado ou já está fechado.");
        }
    }

    public async Task<MovimentoCaixa> RegistrarMovimentoAsync(
        long caixaId,
        TipoMovimentoCaixa tipo,
        long? formaPagamentoId,
        decimal valorComSinal,
        long usuarioId,
        string? documentoReferenciaTipo,
        long? documentoReferenciaId,
        string? observacao,
        IUnitOfWork? unitOfWork = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO cx_movimento
                (caixa_id, tipo, forma_pagamento_id, valor, data_movimento, usuario_id,
                 documento_referencia_tipo, documento_referencia_id, observacao)
            VALUES
                (@caixaId, @tipo, @formaPagamentoId, @valor, now(), @usuarioId,
                 @documentoReferenciaTipo, @documentoReferenciaId, @observacao)
            RETURNING id, data_movimento
            """;

        NpgsqlConnection connection;
        NpgsqlTransaction? transaction = null;
        var conexaoPropria = unitOfWork is null;

        if (unitOfWork is null)
        {
            connection = await _connectionFactory.CriarAsync(cancellationToken);
        }
        else
        {
            var uow = (NpgsqlUnitOfWork)unitOfWork;
            connection = uow.Connection ?? throw new InvalidOperationException("Transação não iniciada.");
            transaction = uow.Transaction ?? throw new InvalidOperationException("Transação não iniciada.");
        }

        try
        {
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("caixaId", caixaId);
            command.Parameters.AddWithValue("tipo", TipoParaTexto(tipo));
            command.Parameters.AddWithValue("formaPagamentoId", (object?)formaPagamentoId ?? DBNull.Value);
            command.Parameters.AddWithValue("valor", valorComSinal);
            command.Parameters.AddWithValue("usuarioId", usuarioId);
            command.Parameters.AddWithValue("documentoReferenciaTipo", (object?)documentoReferenciaTipo ?? DBNull.Value);
            command.Parameters.AddWithValue("documentoReferenciaId", (object?)documentoReferenciaId ?? DBNull.Value);
            command.Parameters.AddWithValue("observacao", (object?)observacao ?? DBNull.Value);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);

            return new MovimentoCaixa
            {
                Id = reader.GetInt64(0),
                CaixaId = caixaId,
                Tipo = tipo,
                FormaPagamentoId = formaPagamentoId,
                Valor = valorComSinal,
                DataMovimento = reader.GetFieldValue<DateTimeOffset>(1),
                UsuarioId = usuarioId,
                DocumentoReferenciaTipo = documentoReferenciaTipo,
                DocumentoReferenciaId = documentoReferenciaId,
                Observacao = observacao
            };
        }
        finally
        {
            if (conexaoPropria)
            {
                await connection.DisposeAsync();
            }
        }
    }

    public async Task<decimal> ObterSaldoAsync(long caixaId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COALESCE(c.valor_abertura, 0) + COALESCE(SUM(m.valor), 0)
            FROM cx_caixa c
            LEFT JOIN cx_movimento m ON m.caixa_id = c.id
            WHERE c.id = @caixaId
            GROUP BY c.valor_abertura
            """;

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("caixaId", caixaId);

        var resultado = await command.ExecuteScalarAsync(cancellationToken);
        return resultado is null ? 0m : (decimal)resultado;
    }

    public async Task<IReadOnlyList<MovimentoCaixa>> ListarMovimentosAsync(
        long caixaId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, caixa_id, tipo, forma_pagamento_id, valor, data_movimento, usuario_id,
                   documento_referencia_tipo, documento_referencia_id, observacao, estornado
            FROM cx_movimento WHERE caixa_id = @caixaId ORDER BY id
            """;

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("caixaId", caixaId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var movimentos = new List<MovimentoCaixa>();
        while (await reader.ReadAsync(cancellationToken))
        {
            movimentos.Add(MapMovimento(reader));
        }

        return movimentos;
    }

    public async Task<IReadOnlyList<MovimentoCaixa>> ListarPorDocumentoAsync(
        string documentoReferenciaTipo, long documentoReferenciaId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, caixa_id, tipo, forma_pagamento_id, valor, data_movimento, usuario_id,
                   documento_referencia_tipo, documento_referencia_id, observacao, estornado
            FROM cx_movimento
            WHERE documento_referencia_tipo = @tipo AND documento_referencia_id = @id
            ORDER BY id
            """;

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("tipo", documentoReferenciaTipo);
        command.Parameters.AddWithValue("id", documentoReferenciaId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var movimentos = new List<MovimentoCaixa>();
        while (await reader.ReadAsync(cancellationToken))
        {
            movimentos.Add(MapMovimento(reader));
        }

        return movimentos;
    }

    public async Task EstornarMovimentosPorDocumentoAsync(
        string documentoReferenciaTipo, long documentoReferenciaId, long usuarioId, IUnitOfWork unitOfWork,
        CancellationToken cancellationToken = default)
    {
        var uow = (NpgsqlUnitOfWork)unitOfWork;
        var connection = uow.Connection ?? throw new InvalidOperationException("Transação não iniciada.");
        var transaction = uow.Transaction ?? throw new InvalidOperationException("Transação não iniciada.");

        const string sqlSelecionar = """
            SELECT id, caixa_id, forma_pagamento_id, valor
            FROM cx_movimento
            WHERE documento_referencia_tipo = @tipo AND documento_referencia_id = @id AND estornado = FALSE
            FOR UPDATE
            """;

        var movimentos = new List<(long Id, long CaixaId, long? FormaPagamentoId, decimal Valor)>();
        await using (var selecionar = new NpgsqlCommand(sqlSelecionar, connection, transaction))
        {
            selecionar.Parameters.AddWithValue("tipo", documentoReferenciaTipo);
            selecionar.Parameters.AddWithValue("id", documentoReferenciaId);
            await using var reader = await selecionar.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                movimentos.Add((
                    reader.GetInt64(0),
                    reader.GetInt64(1),
                    reader.IsDBNull(2) ? null : reader.GetInt64(2),
                    reader.GetDecimal(3)));
            }
        }

        foreach (var movimento in movimentos)
        {
            await RegistrarMovimentoAsync(
                movimento.CaixaId, TipoMovimentoCaixa.Estorno, movimento.FormaPagamentoId, -movimento.Valor, usuarioId,
                documentoReferenciaTipo, documentoReferenciaId, "Estorno de cancelamento", unitOfWork, cancellationToken);

            await using var marcar = new NpgsqlCommand(
                "UPDATE cx_movimento SET estornado = TRUE, data_estorno = now() WHERE id = @id", connection, transaction);
            marcar.Parameters.AddWithValue("id", movimento.Id);
            await marcar.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static CaixaEntidade MapCaixa(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetInt64(0),
        FilialId = reader.GetInt64(1),
        UsuarioAberturaId = reader.GetInt64(2),
        DataAbertura = reader.GetFieldValue<DateTimeOffset>(3),
        ValorAbertura = reader.GetDecimal(4),
        UsuarioFechamentoId = reader.IsDBNull(5) ? null : reader.GetInt64(5),
        DataFechamento = reader.IsDBNull(6) ? null : reader.GetFieldValue<DateTimeOffset>(6),
        ValorFechamentoInformado = reader.IsDBNull(7) ? null : reader.GetDecimal(7),
        ValorFechamentoCalculado = reader.IsDBNull(8) ? null : reader.GetDecimal(8),
        Diferenca = reader.IsDBNull(9) ? null : reader.GetDecimal(9),
        Status = reader.GetString(10) == "fechado" ? StatusCaixa.Fechado : StatusCaixa.Aberto
    };

    private static MovimentoCaixa MapMovimento(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetInt64(0),
        CaixaId = reader.GetInt64(1),
        Tipo = TextoParaTipo(reader.GetString(2)),
        FormaPagamentoId = reader.IsDBNull(3) ? null : reader.GetInt64(3),
        Valor = reader.GetDecimal(4),
        DataMovimento = reader.GetFieldValue<DateTimeOffset>(5),
        UsuarioId = reader.GetInt64(6),
        DocumentoReferenciaTipo = reader.IsDBNull(7) ? null : reader.GetString(7),
        DocumentoReferenciaId = reader.IsDBNull(8) ? null : reader.GetInt64(8),
        Observacao = reader.IsDBNull(9) ? null : reader.GetString(9),
        Estornado = reader.GetBoolean(10)
    };

    private static string TipoParaTexto(TipoMovimentoCaixa tipo) => tipo switch
    {
        TipoMovimentoCaixa.Venda => "venda",
        TipoMovimentoCaixa.Sangria => "sangria",
        TipoMovimentoCaixa.Suprimento => "suprimento",
        TipoMovimentoCaixa.Recebimento => "recebimento",
        TipoMovimentoCaixa.Pagamento => "pagamento",
        TipoMovimentoCaixa.Estorno => "estorno",
        _ => throw new ArgumentOutOfRangeException(nameof(tipo))
    };

    private static TipoMovimentoCaixa TextoParaTipo(string texto) => texto switch
    {
        "venda" => TipoMovimentoCaixa.Venda,
        "sangria" => TipoMovimentoCaixa.Sangria,
        "suprimento" => TipoMovimentoCaixa.Suprimento,
        "recebimento" => TipoMovimentoCaixa.Recebimento,
        "pagamento" => TipoMovimentoCaixa.Pagamento,
        "estorno" => TipoMovimentoCaixa.Estorno,
        _ => throw new ArgumentOutOfRangeException(nameof(texto))
    };
}
