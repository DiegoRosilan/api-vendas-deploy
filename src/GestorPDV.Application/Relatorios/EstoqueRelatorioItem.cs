namespace GestorPDV.Application.Relatorios;

// Linha do relatório de estoque atual: quantidade somada entre todos os
// locais de estoque da filial (grade/lote não são discriminados aqui).
public class EstoqueRelatorioItem
{
    public long ProdutoId { get; set; }
    public string ProdutoDescricao { get; set; } = string.Empty;
    public string? CodigoBarras { get; set; }
    public decimal QuantidadeAtual { get; set; }
    public decimal PrecoVenda { get; set; }
    public decimal ValorTotal => QuantidadeAtual * PrecoVenda;
}
