namespace GestorPDV.Application.Relatorios;

// Linha do relatório de vendas por período — já achatada (join com pessoa/
// funcionário feito em SQL) para não exigir N+1 consultas ao montar o
// relatório.
public class VendaRelatorioItem
{
    public long Id { get; set; }
    public long Numero { get; set; }
    public DateTimeOffset DataVenda { get; set; }
    public string ClienteNome { get; set; } = "Consumidor final";
    public string VendedorNome { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal Desconto { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = string.Empty;
}
