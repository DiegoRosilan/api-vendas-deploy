using GestorPDV.Domain.Cadastros;

namespace GestorPDV.Application.Cadastros;

public interface IClienteRepository
{
    Task<IReadOnlyList<Cliente>> ListarAsync(string? filtro, CancellationToken cancellationToken = default);
    Task<Cliente?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default);
    Task<long> InserirAsync(Cliente cliente, CancellationToken cancellationToken = default);
    Task AtualizarAsync(Cliente cliente, CancellationToken cancellationToken = default);
}
