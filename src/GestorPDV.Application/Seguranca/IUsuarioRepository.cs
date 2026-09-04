using GestorPDV.Domain.Seguranca;

namespace GestorPDV.Application.Seguranca;

public interface IUsuarioRepository
{
    Task<Usuario?> ObterPorLoginAsync(string login, CancellationToken cancellationToken = default);
    Task<Usuario?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default);

    // Códigos (sec_permissao.codigo) efetivos do usuário: os do perfil,
    // sobrepostos pelas permissões específicas do usuário (RN-SEG-001).
    Task<IReadOnlyList<string>> ObterCodigosPermissaoAsync(long usuarioId, CancellationToken cancellationToken = default);

    Task AtualizarUltimoAcessoAsync(long usuarioId, DateTimeOffset dataAcesso, CancellationToken cancellationToken = default);

    Task AtualizarSenhaAsync(
        long usuarioId, string novaSenhaHash, bool exigeTrocaSenha, CancellationToken cancellationToken = default);
}
