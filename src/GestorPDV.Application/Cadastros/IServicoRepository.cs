using GestorPDV.Domain.Cadastros;

namespace GestorPDV.Application.Cadastros;

public interface IServicoRepository
{
    Task<IReadOnlyList<Servico>> ListarAsync(string? filtro, CancellationToken cancellationToken = default);
    Task<Servico?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default);
    Task<long> InserirAsync(Servico servico, CancellationToken cancellationToken = default);
    Task AtualizarAsync(Servico servico, CancellationToken cancellationToken = default);
}
