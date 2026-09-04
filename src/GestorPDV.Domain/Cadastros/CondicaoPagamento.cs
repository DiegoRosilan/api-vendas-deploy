namespace GestorPDV.Domain.Cadastros;

public class CondicaoPagamento
{
    public long Id { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public int NumeroParcelas { get; set; } = 1;
    public int IntervaloDias { get; set; } = 30;
    public decimal EntradaPct { get; set; }
    public bool Ativo { get; set; } = true;
}
