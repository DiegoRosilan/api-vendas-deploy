using GestorPDV.Application.Common;
using GestorPDV.Domain.Cadastros;
using GestorPDV.Domain.Estoque;

namespace GestorPDV.Application.Estoque;

// Casos de uso de estoque com as validações de domínio (ex.: produto que
// não controla estoque não gera movimentação). A persistência pura fica em
// IEstoqueRepository/GestorPDV.Data.Postgres.
public interface IEstoqueService
{
    Task<MovimentacaoEstoque?> BaixarEstoqueAsync(
        Produto produto,
        long filialId,
        decimal quantidade,
        OrigemMovimentacaoEstoque origem,
        string documentoTipo,
        long documentoId,
        long usuarioId,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken = default);

    Task EstornarAsync(
        string documentoTipo, long documentoId, long usuarioId, IUnitOfWork unitOfWork,
        CancellationToken cancellationToken = default);
}
