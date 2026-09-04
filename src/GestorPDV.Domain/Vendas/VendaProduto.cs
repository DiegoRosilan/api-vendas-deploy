namespace GestorPDV.Domain.Vendas;

// RN-VEN-001/002/003/004: item de venda (produto ou serviço). Campos
// fiscais (ICMS/ICMS-ST/IPI/PIS/COFINS/ISS) são o resultado do motor
// tributário de GestorPDV.Fiscal, aplicado a partir da Fase 6/8.
public class VendaProduto
{
    public long Id { get; set; }
    public long VendaId { get; set; }
    public int ItemNumero { get; set; }
    public long? ProdutoId { get; set; }
    public long? ServicoId { get; set; }
    public decimal Quantidade { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal ValorUnitarioFinal { get; set; }
    public decimal Desconto { get; set; }
    public decimal Acrescimo { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
    public bool Cancelado { get; set; }
}
