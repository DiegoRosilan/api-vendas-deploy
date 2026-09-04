namespace GestorPDV.Domain.Cadastros;

public class Fornecedor
{
    public long Id { get; set; }
    public Pessoa? Pessoa { get; set; }
    public string? Banco { get; set; }
    public string? Agencia { get; set; }
    public string? Conta { get; set; }
    public string? Observacao { get; set; }
}
