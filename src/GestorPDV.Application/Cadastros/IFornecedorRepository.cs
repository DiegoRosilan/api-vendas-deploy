using GestorPDV.Domain.Cadastros;

namespace GestorPDV.Application.Cadastros;

public interface IFornecedorRepository
{
    Task<IReadOnlyList<Fornecedor>> ListarAsync(string? filtro, CancellationToken cancellationToken = default);
    Task<Fornecedor?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default);
    Task<long> InserirAsync(Fornecedor fornecedor, CancellationToken cancellationToken = default);
    Task AtualizarAsync(Fornecedor fornecedor, CancellationToken cancellationToken = default);
}
