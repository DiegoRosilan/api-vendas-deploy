namespace GestorPDV.Domain.Cadastros;

public class Produto
{
    public long Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string? CodigoBarras { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public long? CategoriaId { get; set; }
    public string Unidade { get; set; } = "UN";
    public string? Ncm { get; set; }
    public string? Cest { get; set; }
    public decimal PrecoCusto { get; set; }
    public decimal PrecoCustoMedio { get; set; }
    public decimal PrecoVenda { get; set; }
    public decimal? PrecoMinimo { get; set; }
    public decimal? PrecoPromocional { get; set; }
    public decimal? MarkupPct { get; set; }
    public decimal? MargemContribuicaoPct { get; set; }
    public decimal EstoqueMinimo { get; set; }
    public decimal? EstoqueMaximo { get; set; }
    public string? Localizacao { get; set; }
    public bool ControlaEstoque { get; set; } = true;
    public bool ControlaGrade { get; set; }
    public bool ControlaLote { get; set; }
    public bool ControlaSerial { get; set; }
    public decimal DescontoMaximoPct { get; set; }
    public bool BloquearDesconto { get; set; }
    public bool Ativo { get; set; } = true;
}
