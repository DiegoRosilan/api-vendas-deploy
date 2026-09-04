namespace GestorPDV.Domain.Cadastros;

public class Servico
{
    public long Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal Preco { get; set; }
    public decimal AliquotaIssPct { get; set; }
    public bool Ativo { get; set; } = true;
}
