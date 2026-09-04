using GestorPDV.Domain.Seguranca;

namespace GestorPDV.Application.Seguranca;

public interface IUsuarioRepository
{
    Task<Usuario?> ObterPorLoginAsync(string login, CancellationToken cancellationToken = default);
    Task<Usuario?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<long>> ObterCodigosPermissaoAsync(long usuarioId, CancellationToken cancellationToken = default);
    Task AtualizarUltimoAcessoAsync(long usuarioId, DateTimeOffset dataAcesso, CancellationToken cancellationToken = default);
}
