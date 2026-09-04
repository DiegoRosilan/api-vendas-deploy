namespace GestorPDV.Domain.Vendas;

public enum StatusVendaPagamento
{
    Confirmado,
    Cancelado,
    Estornado
}

// RN-PAG-001/002: múltiplas formas de pagamento por venda, com dados de
// cartão/Pix. FinalizaFormaPagamento/CalculaPagamentos são implementados em
// GestorPDV.Financeiro a partir da Fase 7.
public class VendaPagamento
{
    public long Id { get; set; }
    public long VendaId { get; set; }
    public long FormaPagamentoId { get; set; }
    public long? CondicaoPagamentoId { get; set; }
    public decimal Valor { get; set; }
    public int Parcelas { get; set; } = 1;
    public string? CnpjCredenciadora { get; set; }
    public string? Nsu { get; set; }
    public string? NsuPos { get; set; }
    public string? Rede { get; set; }
    public string? PixE2E { get; set; }
    public string? PixTxId { get; set; }
    public StatusVendaPagamento Status { get; set; } = StatusVendaPagamento.Confirmado;
}
