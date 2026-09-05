using GestorPDV.Application.Common;
using GestorPDV.Domain.Caixa;
// Alias necessário: dentro do namespace GestorPDV.Application.Caixa (e de
// qualquer outro sub-namespace "...Caixa"), o simple-name "Caixa" resolve
// para o namespace, não para esta classe — ver Domain.Caixa.Caixa.
using CaixaEntidade = GestorPDV.Domain.Caixa.Caixa;

namespace GestorPDV.Application.Caixa;

public interface ICaixaRepository
{
    Task<CaixaEntidade?> ObterAbertoAsync(long filialId, CancellationToken cancellationToken = default);

    Task<long> AbrirAsync(CaixaEntidade caixa, CancellationToken cancellationToken = default);

    Task FecharAsync(
        long caixaId, long usuarioId, decimal valorInformado, decimal valorCalculado, decimal diferenca,
        CancellationToken cancellationToken = default);

    // Valor com sinal: positivo aumenta o saldo do caixa (venda, suprimento,
    // recebimento), negativo diminui (sangria, pagamento, estorno de uma
    // entrada). unitOfWork é opcional — uma sangria/suprimento avulsa não
    // precisa de transação compartilhada, mas o lançamento de uma venda
    // deve usar a mesma transação da venda.
    Task<MovimentoCaixa> RegistrarMovimentoAsync(
        long caixaId,
        TipoMovimentoCaixa tipo,
        long? formaPagamentoId,
        decimal valorComSinal,
        long usuarioId,
        string? documentoReferenciaTipo,
        long? documentoReferenciaId,
        string? observacao,
        IUnitOfWork? unitOfWork = null,
        CancellationToken cancellationToken = default);

    Task<decimal> ObterSaldoAsync(long caixaId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MovimentoCaixa>> ListarMovimentosAsync(long caixaId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MovimentoCaixa>> ListarPorDocumentoAsync(
        string documentoReferenciaTipo, long documentoReferenciaId, CancellationToken cancellationToken = default);

    // RN-CAN-001: estorna (lançando o movimento inverso) todos os
    // movimentos ainda não estornados de um documento — usado no
    // cancelamento de venda.
    Task EstornarMovimentosPorDocumentoAsync(
        string documentoReferenciaTipo, long documentoReferenciaId, long usuarioId, IUnitOfWork unitOfWork,
        CancellationToken cancellationToken = default);
}
