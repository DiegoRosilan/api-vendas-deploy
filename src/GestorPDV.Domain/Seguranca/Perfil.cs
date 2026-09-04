namespace GestorPDV.Domain.Seguranca;

public class Perfil
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public bool Ativo { get; set; } = true;

    public List<long> PermissaoIds { get; set; } = new();
}
