using GestorPDV.Application.Common;
using GestorPDV.Domain.Estoque;

namespace GestorPDV.Application.Estoque;

public interface IEstoqueRepository
{
    Task<LocalEstoque?> ObterLocalPadraoAsync(long filialId, CancellationToken cancellationToken = default);

    Task<decimal> ObterSaldoAsync(
        long produtoId, long localEstoqueId, CancellationToken cancellationToken = default);

    // RN-EST-002: registra a movimentação e atualiza o saldo (est_estoque)
    // dentro da transação informada. Quantidade negativa = saída.
    Task<MovimentacaoEstoque> RegistrarMovimentacaoAsync(
        long produtoId,
        long localEstoqueId,
        decimal quantidade,
        TipoMovimentacaoEstoque tipo,
        OrigemMovimentacaoEstoque origem,
        string documentoTipo,
        long documentoId,
        long usuarioId,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken = default);

    // RN-EST-003: estorna uma movimentação já registrada (lança o
    // movimento inverso e marca a original como estornada).
    Task EstornarMovimentacaoAsync(
        long movimentacaoId, long usuarioId, IUnitOfWork unitOfWork, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MovimentacaoEstoque>> ListarPorDocumentoAsync(
        string documentoTipo, long documentoId, CancellationToken cancellationToken = default);
}
