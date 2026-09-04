namespace GestorPDV.Domain.Vendas;

public enum TipoVenda
{
    Venda,
    PreVenda
}

public enum StatusVenda
{
    Aberta,
    Finalizada,
    Cancelada
}

// RN-VEN-*: cabeçalho da venda. O cálculo de item/subtotal/total
// (CalculaItemTotal, CalculaSubTotal, CalculaTotal, CalculaDesconto,
// CalculaAcrescimo) é implementado em GestorPDV.Vendas a partir da Fase 6.
public class Venda
{
    public long Id { get; set; }
    public long Numero { get; set; }
    public long FilialId { get; set; }
    public long? ClienteId { get; set; }
    public long VendedorId { get; set; }
    public TipoVenda Tipo { get; set; } = TipoVenda.Venda;
    public StatusVenda Status { get; set; } = StatusVenda.Aberta;
    public long? TabelaPrecoId { get; set; }
    public long? CondicaoPagamentoId { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Desconto { get; set; }
    public decimal Acrescimo { get; set; }
    public decimal Total { get; set; }
    public DateTimeOffset DataVenda { get; set; }
    public DateTimeOffset? DataCancelamento { get; set; }
    public string? MotivoCancelamento { get; set; }
    public long UsuarioAberturaId { get; set; }
    public long? UsuarioCancelamentoId { get; set; }

    public List<VendaProduto> Itens { get; set; } = new();
    public List<VendaPagamento> Pagamentos { get; set; } = new();
}
