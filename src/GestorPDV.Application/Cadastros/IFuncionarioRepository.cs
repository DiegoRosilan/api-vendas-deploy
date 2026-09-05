using GestorPDV.Domain.Cadastros;

namespace GestorPDV.Application.Cadastros;

public interface IFuncionarioRepository
{
    Task<IReadOnlyList<Funcionario>> ListarAsync(string? filtro, CancellationToken cancellationToken = default);
    Task<Funcionario?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default);
    Task<long> InserirAsync(Funcionario funcionario, CancellationToken cancellationToken = default);
    Task AtualizarAsync(Funcionario funcionario, CancellationToken cancellationToken = default);
}
