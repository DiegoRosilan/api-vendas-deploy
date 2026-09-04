using GestorPDV.Domain.Cadastros;

namespace GestorPDV.Application.Cadastros;

public interface IProdutoRepository
{
    Task<IReadOnlyList<Produto>> ListarAsync(string? filtro, CancellationToken cancellationToken = default);
    Task<Produto?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Produto?> ObterPorCodigoBarrasAsync(string codigoBarras, CancellationToken cancellationToken = default);
    Task<long> InserirAsync(Produto produto, CancellationToken cancellationToken = default);
    Task AtualizarAsync(Produto produto, CancellationToken cancellationToken = default);
}
