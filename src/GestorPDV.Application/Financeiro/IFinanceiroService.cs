using GestorPDV.Application.Common;

namespace GestorPDV.Application.Financeiro;

public interface IFinanceiroService
{
    // RN-FIN-002/003: juros e multa de uma parcela em atraso na data de
    // referência (0/0 se ainda não venceu). Função pura — ver
    // GestorPDV.Tests/Financeiro/FinanceiroServiceTests.cs.
    (decimal Juros, decimal Multa) CalcularEncargos(decimal valorParcela, DateOnly vencimento, DateOnly dataReferencia);

    // RN-FIN-001: gera o documento e as parcelas de uma venda a prazo.
    Task<Result<long>> GerarContaReceberAsync(
        long pessoaId,
        long filialId,
        long? vendaId,
        decimal valorTotal,
        int numeroParcelas,
        int intervaloDias,
        DateOnly primeiroVencimento,
        CancellationToken cancellationToken = default);

    // RN-CLI-001: bloqueia venda a prazo se o cliente tiver parcela em
    // aberto vencida há mais dias do que o limite configurado.
    Task<Result> VerificarBloqueioClienteAsync(
        long pessoaId, int? bloquearVendaDiasVencido, CancellationToken cancellationToken = default);

    Task<Result> BaixarParcelaAsync(
        long parcelaId, long usuarioId, long formaPagamentoId, DateOnly dataBaixa,
        CancellationToken cancellationToken = default);
}
