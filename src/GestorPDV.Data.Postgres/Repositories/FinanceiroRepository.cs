using GestorPDV.Application.Common;
using GestorPDV.Application.Financeiro;
using GestorPDV.Domain.Financeiro;
using GestorPDV.Infrastructure.Database;
using Npgsql;

namespace GestorPDV.Data.Postgres.Repositories;

public class FinanceiroRepository : IFinanceiroRepository
{
    private const string ColunasDocumento = """
        id, tipo, pessoa_id, filial_id, numero_documento, valor_original, data_emissao,
        data_vencimento, situacao, origem, venda_id, observacao
        """;

    private const string ColunasParcela = "id, documento_id, numero_parcela, valor, vencimento, situacao";

    private readonly NpgsqlConnectionFactory _connectionFactory;

    public FinanceiroRepository(NpgsqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task GerarDocumentoAsync(
        DocumentoFinanceiro documento, IUnitOfWork unitOfWork, CancellationToken cancellationToken = default)
    {
        var uow = (NpgsqlUnitOfWork)unitOfWork;
        var connection = uow.Connection ?? throw new InvalidOperationException("Transação não iniciada.");
        var transaction = uow.Transaction ?? throw new InvalidOperationException("Transação não iniciada.");

        const string sqlDocumento = """
            INSERT INTO crb_documento
                (tipo, pessoa_id, filial_id, numero_documento, valor_original, data_emissao,
                 data_vencimento, situacao, origem, venda_id, observacao)
            VALUES
                (@tipo, @pessoaId, @filialId, @numeroDocumento, @valorOriginal, @dataEmissao,
                 @dataVencimento, @situacao, @origem, @vendaId, @observacao)
            RETURNING id
            """;

        await using (var command = new NpgsqlCommand(sqlDocumento, connection, transaction))
        {
            command.Parameters.AddWithValue("tipo", documento.Tipo == TipoDocumentoFinanceiro.Pagar ? "pagar" : "receber");
            command.Parameters.AddWithValue("pessoaId", documento.PessoaId);
            command.Parameters.AddWithValue("filialId", documento.FilialId);
            command.Parameters.AddWithValue("numeroDocumento", documento.NumeroDocumento);
            command.Parameters.AddWithValue("valorOriginal", documento.ValorOriginal);
            command.Parameters.AddWithValue("dataEmissao", documento.DataEmissao);
            command.Parameters.AddWithValue("dataVencimento", documento.DataVencimento);
            command.Parameters.AddWithValue("situacao", SituacaoParaTexto(documento.Situacao));
            command.Parameters.AddWithValue("origem", OrigemParaTexto(documento.Origem));
            command.Parameters.AddWithValue("vendaId", (object?)documento.VendaId ?? DBNull.Value);
            command.Parameters.AddWithValue("observacao", (object?)documento.Observacao ?? DBNull.Value);
            documento.Id = (long)(await command.ExecuteScalarAsync(cancellationToken))!;
        }

        const string sqlParcela = """
            INSERT INTO fin_parcela (documento_id, numero_parcela, valor, vencimento, situacao)
            VALUES (@documentoId, @numeroParcela, @valor, @vencimento, @situacao)
            RETURNING id
            """;

        foreach (var parcela in documento.Parcelas)
        {
            parcela.DocumentoId = documento.Id;

            await using var command = new NpgsqlCommand(sqlParcela, connection, transaction);
            command.Parameters.AddWithValue("documentoId", parcela.DocumentoId);
            command.Parameters.AddWithValue("numeroParcela", parcela.NumeroParcela);
            command.Parameters.AddWithValue("valor", parcela.Valor);
            command.Parameters.AddWithValue("vencimento", parcela.Vencimento);
            command.Parameters.AddWithValue("situacao", SituacaoParcelaParaTexto(parcela.Situacao));
            parcela.Id = (long)(await command.ExecuteScalarAsync(cancellationToken))!;
        }
    }

    public async Task<DocumentoFinanceiro?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);

        DocumentoFinanceiro documento;
        await using (var command = new NpgsqlCommand($"SELECT {ColunasDocumento} FROM crb_documento WHERE id = @id", connection))
        {
            command.Parameters.AddWithValue("id", id);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            documento = MapDocumento(reader);
        }

        await using (var command = new NpgsqlCommand(
            $"SELECT {ColunasParcela} FROM fin_parcela WHERE documento_id = @documentoId ORDER BY numero_parcela", connection))
        {
            command.Parameters.AddWithValue("documentoId", id);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                documento.Parcelas.Add(MapParcela(reader));
            }
        }

        return documento;
    }

    public async Task<Parcela?> ObterParcelaAsync(long parcelaId, CancellationToken cancellationToken = default)
    {
        var sql = $"SELECT {ColunasParcela} FROM fin_parcela WHERE id = @id";

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", parcelaId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapParcela(reader) : null;
    }

    public async Task<IReadOnlyList<DocumentoFinanceiro>> ListarEmAbertoPorPessoaAsync(
        long pessoaId, CancellationToken cancellationToken = default)
    {
        var sql = $"SELECT {ColunasDocumento} FROM crb_documento WHERE pessoa_id = @pessoaId AND situacao IN ('aberto','parcial') ORDER BY data_vencimento";
        return await ListarComParcelasAsync(sql, "pessoaId", pessoaId, cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentoFinanceiro>> ListarEmAbertoAsync(
        long filialId, CancellationToken cancellationToken = default)
    {
        var sql = $"SELECT {ColunasDocumento} FROM crb_documento WHERE filial_id = @filialId AND situacao IN ('aberto','parcial') ORDER BY data_vencimento";
        return await ListarComParcelasAsync(sql, "filialId", filialId, cancellationToken);
    }

    private async Task<IReadOnlyList<DocumentoFinanceiro>> ListarComParcelasAsync(
        string sqlDocumentos, string nomeParametro, long valorParametro, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);

        var documentos = new List<DocumentoFinanceiro>();
        await using (var command = new NpgsqlCommand(sqlDocumentos, connection))
        {
            command.Parameters.AddWithValue(nomeParametro, valorParametro);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                documentos.Add(MapDocumento(reader));
            }
        }

        foreach (var documento in documentos)
        {
            await using var command = new NpgsqlCommand(
                $"SELECT {ColunasParcela} FROM fin_parcela WHERE documento_id = @documentoId ORDER BY numero_parcela", connection);
            command.Parameters.AddWithValue("documentoId", documento.Id);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                documento.Parcelas.Add(MapParcela(reader));
            }
        }

        return documentos;
    }

    public async Task<int> ObterDiasAtrasoMaximoAsync(
        long pessoaId, DateOnly dataReferencia, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COALESCE(MAX(@dataReferencia - p.vencimento), 0)
            FROM fin_parcela p
            JOIN crb_documento d ON d.id = p.documento_id
            WHERE d.pessoa_id = @pessoaId AND p.situacao IN ('aberto', 'parcial') AND p.vencimento < @dataReferencia
            """;

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("pessoaId", pessoaId);
        command.Parameters.AddWithValue("dataReferencia", dataReferencia);

        var resultado = await command.ExecuteScalarAsync(cancellationToken);
        return resultado is null ? 0 : (int)resultado;
    }

    public async Task RegistrarBaixaAsync(Baixa baixa, IUnitOfWork unitOfWork, CancellationToken cancellationToken = default)
    {
        var uow = (NpgsqlUnitOfWork)unitOfWork;
        var connection = uow.Connection ?? throw new InvalidOperationException("Transação não iniciada.");
        var transaction = uow.Transaction ?? throw new InvalidOperationException("Transação não iniciada.");

        const string sqlBaixa = """
            INSERT INTO crb_documento_baixa
                (documento_id, parcela_id, valor_baixa, valor_juros, valor_multa, valor_desconto,
                 data_baixa, forma_pagamento_id, usuario_id)
            VALUES
                (@documentoId, @parcelaId, @valorBaixa, @valorJuros, @valorMulta, @valorDesconto,
                 now(), @formaPagamentoId, @usuarioId)
            RETURNING id, data_baixa
            """;

        await using (var command = new NpgsqlCommand(sqlBaixa, connection, transaction))
        {
            command.Parameters.AddWithValue("documentoId", baixa.DocumentoId);
            command.Parameters.AddWithValue("parcelaId", baixa.ParcelaId);
            command.Parameters.AddWithValue("valorBaixa", baixa.ValorPago);
            command.Parameters.AddWithValue("valorJuros", baixa.ValorJuros);
            command.Parameters.AddWithValue("valorMulta", baixa.ValorMulta);
            command.Parameters.AddWithValue("valorDesconto", baixa.ValorDesconto);
            command.Parameters.AddWithValue("formaPagamentoId", baixa.FormaPagamentoId);
            command.Parameters.AddWithValue("usuarioId", baixa.UsuarioId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);
            baixa.Id = reader.GetInt64(0);
            baixa.DataBaixa = reader.GetFieldValue<DateTimeOffset>(1);
        }

        await using (var command = new NpgsqlCommand(
            "UPDATE fin_parcela SET situacao = 'baixado' WHERE id = @id", connection, transaction))
        {
            command.Parameters.AddWithValue("id", baixa.ParcelaId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        // Documento fica "baixado" quando todas as parcelas estiverem
        // baixadas/canceladas, senão "parcial" (já tem ao menos uma baixa).
        const string sqlContarPendentes = """
            SELECT COUNT(*) FROM fin_parcela WHERE documento_id = @documentoId AND situacao IN ('aberto', 'parcial')
            """;

        long parcelasPendentes;
        await using (var command = new NpgsqlCommand(sqlContarPendentes, connection, transaction))
        {
            command.Parameters.AddWithValue("documentoId", baixa.DocumentoId);
            parcelasPendentes = (long)(await command.ExecuteScalarAsync(cancellationToken))!;
        }

        var novaSituacao = parcelasPendentes == 0 ? "baixado" : "parcial";
        await using (var command = new NpgsqlCommand(
            "UPDATE crb_documento SET situacao = @situacao WHERE id = @id", connection, transaction))
        {
            command.Parameters.AddWithValue("situacao", novaSituacao);
            command.Parameters.AddWithValue("id", baixa.DocumentoId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task CancelarDocumentosPorVendaAsync(
        long vendaId, IUnitOfWork unitOfWork, CancellationToken cancellationToken = default)
    {
        var uow = (NpgsqlUnitOfWork)unitOfWork;
        var connection = uow.Connection ?? throw new InvalidOperationException("Transação não iniciada.");
        var transaction = uow.Transaction ?? throw new InvalidOperationException("Transação não iniciada.");

        const string sqlParcelas = """
            UPDATE fin_parcela SET situacao = 'cancelado'
            WHERE situacao IN ('aberto', 'parcial')
                AND documento_id IN (SELECT id FROM crb_documento WHERE venda_id = @vendaId)
            """;
        await using (var command = new NpgsqlCommand(sqlParcelas, connection, transaction))
        {
            command.Parameters.AddWithValue("vendaId", vendaId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        const string sqlDocumentos = """
            UPDATE crb_documento SET situacao = 'cancelado'
            WHERE venda_id = @vendaId AND situacao IN ('aberto', 'parcial')
            """;
        await using (var command = new NpgsqlCommand(sqlDocumentos, connection, transaction))
        {
            command.Parameters.AddWithValue("vendaId", vendaId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static DocumentoFinanceiro MapDocumento(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetInt64(0),
        Tipo = reader.GetString(1) == "pagar" ? TipoDocumentoFinanceiro.Pagar : TipoDocumentoFinanceiro.Receber,
        PessoaId = reader.GetInt64(2),
        FilialId = reader.GetInt64(3),
        NumeroDocumento = reader.GetString(4),
        ValorOriginal = reader.GetDecimal(5),
        DataEmissao = reader.GetFieldValue<DateOnly>(6),
        DataVencimento = reader.GetFieldValue<DateOnly>(7),
        Situacao = TextoParaSituacao(reader.GetString(8)),
        Origem = TextoParaOrigem(reader.GetString(9)),
        VendaId = reader.IsDBNull(10) ? null : reader.GetInt64(10),
        Observacao = reader.IsDBNull(11) ? null : reader.GetString(11)
    };

    private static Parcela MapParcela(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetInt64(0),
        DocumentoId = reader.GetInt64(1),
        NumeroParcela = reader.GetInt32(2),
        Valor = reader.GetDecimal(3),
        Vencimento = reader.GetFieldValue<DateOnly>(4),
        Situacao = TextoParaSituacaoParcela(reader.GetString(5))
    };

    private static string SituacaoParaTexto(SituacaoDocumentoFinanceiro situacao) => situacao switch
    {
        SituacaoDocumentoFinanceiro.Aberto => "aberto",
        SituacaoDocumentoFinanceiro.Parcial => "parcial",
        SituacaoDocumentoFinanceiro.Baixado => "baixado",
        SituacaoDocumentoFinanceiro.Cancelado => "cancelado",
        SituacaoDocumentoFinanceiro.Renegociado => "renegociado",
        _ => throw new ArgumentOutOfRangeException(nameof(situacao))
    };

    private static SituacaoDocumentoFinanceiro TextoParaSituacao(string texto) => texto switch
    {
        "aberto" => SituacaoDocumentoFinanceiro.Aberto,
        "parcial" => SituacaoDocumentoFinanceiro.Parcial,
        "baixado" => SituacaoDocumentoFinanceiro.Baixado,
        "cancelado" => SituacaoDocumentoFinanceiro.Cancelado,
        "renegociado" => SituacaoDocumentoFinanceiro.Renegociado,
        _ => throw new ArgumentOutOfRangeException(nameof(texto))
    };

    private static string OrigemParaTexto(OrigemDocumentoFinanceiro origem) => origem switch
    {
        OrigemDocumentoFinanceiro.Venda => "venda",
        OrigemDocumentoFinanceiro.Manual => "manual",
        OrigemDocumentoFinanceiro.Renegociacao => "renegociacao",
        _ => throw new ArgumentOutOfRangeException(nameof(origem))
    };

    private static OrigemDocumentoFinanceiro TextoParaOrigem(string texto) => texto switch
    {
        "venda" => OrigemDocumentoFinanceiro.Venda,
        "manual" => OrigemDocumentoFinanceiro.Manual,
        "renegociacao" => OrigemDocumentoFinanceiro.Renegociacao,
        _ => throw new ArgumentOutOfRangeException(nameof(texto))
    };

    private static string SituacaoParcelaParaTexto(SituacaoParcela situacao) => situacao switch
    {
        SituacaoParcela.Aberto => "aberto",
        SituacaoParcela.Parcial => "parcial",
        SituacaoParcela.Baixado => "baixado",
        SituacaoParcela.Cancelado => "cancelado",
        _ => throw new ArgumentOutOfRangeException(nameof(situacao))
    };

    private static SituacaoParcela TextoParaSituacaoParcela(string texto) => texto switch
    {
        "aberto" => SituacaoParcela.Aberto,
        "parcial" => SituacaoParcela.Parcial,
        "baixado" => SituacaoParcela.Baixado,
        "cancelado" => SituacaoParcela.Cancelado,
        _ => throw new ArgumentOutOfRangeException(nameof(texto))
    };
}
