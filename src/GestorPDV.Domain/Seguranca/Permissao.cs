namespace GestorPDV.Domain.Seguranca;

public class Permissao
{
    public long Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Modulo { get; set; } = string.Empty;
}
