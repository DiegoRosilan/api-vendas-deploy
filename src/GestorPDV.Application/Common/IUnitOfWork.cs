namespace GestorPDV.Application.Common;

// Abstração de transação para operações críticas (venda, baixa de estoque,
// caixa, financeiro — item 3 do escopo). Implementada em
// GestorPDV.Data.Postgres sobre NpgsqlTransaction.
public interface IUnitOfWork : IAsyncDisposable
{
    Task BeginAsync(CancellationToken cancellationToken = default);
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
