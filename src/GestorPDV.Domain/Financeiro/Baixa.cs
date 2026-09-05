namespace GestorPDV.Domain.Financeiro;

// RN-FIN-001/002/003: baixa de uma parcela, com os encargos (juros/multa)
// apurados no momento do pagamento quando em atraso.
public class Baixa
{
    public long Id { get; set; }
    public long ParcelaId { get; set; }
    public long DocumentoId { get; set; }
    public decimal ValorPago { get; set; }
    public decimal ValorJuros { get; set; }
    public decimal ValorMulta { get; set; }
    public decimal ValorDesconto { get; set; }
    public DateTimeOffset DataBaixa { get; set; }
    public long FormaPagamentoId { get; set; }
    public long UsuarioId { get; set; }
    public bool Estornado { get; set; }
    public DateTimeOffset? DataEstorno { get; set; }
}
