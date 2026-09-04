namespace GestorPDV.Domain.Estoque;

// RN-EST-001: saldo de estoque por produto/local (e opcionalmente
// grade/lote). Estruturas completas de grade, lote e serial
// (RN-EST-004) são adicionadas a partir da Fase 6, quando o fluxo de
// venda/estoque for implementado.
public class SaldoEstoque
{
    public long Id { get; set; }
    public long ProdutoId { get; set; }
    public long? ProdutoGradeId { get; set; }
    public long? ProdutoLoteId { get; set; }
    public long LocalEstoqueId { get; set; }
    public decimal Quantidade { get; set; }
    public decimal QuantidadeReservada { get; set; }
    public DateTimeOffset AtualizadoEm { get; set; }
}
