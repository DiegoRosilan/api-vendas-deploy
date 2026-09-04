namespace GestorPDV.Domain.Financeiro;

public enum SituacaoParcela
{
    Aberto,
    Parcial,
    Baixado,
    Cancelado
}

public class Parcela
{
    public long Id { get; set; }
    public long DocumentoId { get; set; }
    public int NumeroParcela { get; set; }
    public decimal Valor { get; set; }
    public DateOnly Vencimento { get; set; }
    public SituacaoParcela Situacao { get; set; } = SituacaoParcela.Aberto;
}
