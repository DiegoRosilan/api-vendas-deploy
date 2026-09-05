using GestorPDV.Application.Common;
using GestorPDV.Domain.Financeiro;

namespace GestorPDV.Application.Financeiro;

public interface IFinanceiroRepository
{
    // Grava crb_documento + fin_parcela(s) dentro da transação informada e
    // preenche documento.Id e cada parcela.Id.
    Task GerarDocumentoAsync(DocumentoFinanceiro documento, IUnitOfWork unitOfWork, CancellationToken cancellationToken = default);

    Task<DocumentoFinanceiro?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default);

    Task<Parcela?> ObterParcelaAsync(long parcelaId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentoFinanceiro>> ListarEmAbertoPorPessoaAsync(
        long pessoaId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentoFinanceiro>> ListarEmAbertoAsync(
        long filialId, CancellationToken cancellationToken = default);

    // RN-CLI-001: maior número de dias de atraso entre as parcelas em
    // aberto do cliente, na data de referência informada (0 = sem atraso).
    Task<int> ObterDiasAtrasoMaximoAsync(long pessoaId, DateOnly dataReferencia, CancellationToken cancellationToken = default);

    // RN-FIN-001: grava a baixa da parcela e atualiza a situação dela e do
    // documento (parcial/baixado, conforme as demais parcelas do documento).
    Task RegistrarBaixaAsync(Baixa baixa, IUnitOfWork unitOfWork, CancellationToken cancellationToken = default);

    // RN-CAN-001: cancela os documentos/parcelas ainda não baixados gerados
    // por uma venda cancelada.
    Task CancelarDocumentosPorVendaAsync(long vendaId, IUnitOfWork unitOfWork, CancellationToken cancellationToken = default);
}
