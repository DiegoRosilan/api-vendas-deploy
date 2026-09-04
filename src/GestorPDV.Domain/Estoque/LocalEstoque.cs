namespace GestorPDV.Domain.Estoque;

public class LocalEstoque
{
    public long Id { get; set; }
    public long FilialId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
}
