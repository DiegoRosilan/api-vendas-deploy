namespace GestorPDV.Domain.Estoque;

public enum TipoMovimentacaoEstoque
{
    Entrada,
    Saida,
    Transferencia,
    Perda,
    Inventario,
    Estorno
}

public enum OrigemMovimentacaoEstoque
{
    Venda,
    Compra,
    Ajuste,
    Devolucao,
    Producao,
    Transferencia,
    Inventario
}

// RN-EST-002/RN-EST-003: baixa e estorno de estoque. O cálculo
// (BaixarEstoque/BaixarEstoqueRealTime/Estornar) é implementado em
// GestorPDV.Estoque a partir da Fase 6.
public class MovimentacaoEstoque
{
    public long Id { get; set; }
    public long ProdutoId { get; set; }
    public long? ProdutoGradeId { get; set; }
    public long? ProdutoLoteId { get; set; }
    public long? ProdutoSerialId { get; set; }
    public long LocalEstoqueId { get; set; }
    public TipoMovimentacaoEstoque Tipo { get; set; }
    public OrigemMovimentacaoEstoque Origem { get; set; }
    public string? DocumentoTipo { get; set; }
    public long? DocumentoId { get; set; }
    public decimal Quantidade { get; set; }
    public decimal QuantidadeAnterior { get; set; }
    public decimal QuantidadeAtual { get; set; }
    public decimal? CustoUnitario { get; set; }
    public long UsuarioId { get; set; }
    public DateTimeOffset DataMovimento { get; set; }
    public bool Estornado { get; set; }
    public long? MovimentacaoEstornoId { get; set; }
    public string? Observacao { get; set; }
}
