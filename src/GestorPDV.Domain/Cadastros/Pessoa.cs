namespace GestorPDV.Domain.Cadastros;

// Cadastro base compartilhado por Cliente, Fornecedor e Funcionario
// (item 5 do escopo: "Pessoas / Clientes").
public class Pessoa
{
    public long Id { get; set; }
    public TipoPessoa TipoPessoa { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? NomeFantasia { get; set; }
    public string? CpfCnpj { get; set; }
    public string? RgIe { get; set; }
    public string? Email { get; set; }
    public string? Telefone { get; set; }
    public string? Endereco { get; set; }
    public string? Numero { get; set; }
    public string? Bairro { get; set; }
    public string? Municipio { get; set; }
    public string? Uf { get; set; }
    public string? Cep { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTimeOffset CriadoEm { get; set; }
}
