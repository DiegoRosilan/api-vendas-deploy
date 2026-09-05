using GestorPDV.Domain.Cadastros;

namespace GestorPDV.Application.Cadastros;

public interface IFilialRepository
{
    Task<IReadOnlyList<Filial>> ListarAsync(CancellationToken cancellationToken = default);
    Task<Filial?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default);
    Task<long> InserirAsync(Filial filial, CancellationToken cancellationToken = default);
    Task AtualizarAsync(Filial filial, CancellationToken cancellationToken = default);
}
