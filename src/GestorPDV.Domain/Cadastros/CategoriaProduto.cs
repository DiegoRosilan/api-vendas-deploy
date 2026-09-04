namespace GestorPDV.Domain.Cadastros;

public class CategoriaProduto
{
    public long Id { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public long? CategoriaPaiId { get; set; }
}
