namespace GestorPDV.Application.Seguranca;

// Sessão do usuário autenticado: dados exibidos na interface e a lista de
// permissões efetivas, usada para liberar/bloquear ações e botões conforme
// o perfil e as permissões específicas do usuário (RN-SEG-001).
public class SessaoUsuario
{
    public long UsuarioId { get; init; }
    public string Login { get; init; } = string.Empty;
    public string Nome { get; init; } = string.Empty;
    public long? PerfilId { get; init; }
    public long? FilialId { get; init; }
    public bool ExigeTrocaSenha { get; set; }
    public IReadOnlyList<string> Permissoes { get; init; } = Array.Empty<string>();

    public bool TemPermissao(string codigo) =>
        Permissoes.Contains(codigo, StringComparer.OrdinalIgnoreCase);
}
