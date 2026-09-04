namespace GestorPDV.Domain.Seguranca;

public class Usuario
{
    public long Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string? Email { get; set; }
    public long? PerfilId { get; set; }
    public long? FilialId { get; set; }
    public bool Ativo { get; set; } = true;
    public bool Bloqueado { get; set; }
    public bool ExigeTrocaSenha { get; set; }
    public DateTimeOffset? UltimoAcessoEm { get; set; }
    public DateTimeOffset CriadoEm { get; set; }
    public DateTimeOffset AtualizadoEm { get; set; }

    public List<PermissaoUsuario> PermissoesEspecificas { get; set; } = new();
}

public class PermissaoUsuario
{
    public long UsuarioId { get; set; }
    public long PermissaoId { get; set; }
    public bool Permitido { get; set; }
}
