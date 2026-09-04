namespace GestorPDV.Domain.Caixa;

public enum TipoMovimentoCaixa
{
    Venda,
    Sangria,
    Suprimento,
    Recebimento,
    Pagamento,
    Estorno
}

public class MovimentoCaixa
{
    public long Id { get; set; }
    public long CaixaId { get; set; }
    public TipoMovimentoCaixa Tipo { get; set; }
    public long? FormaPagamentoId { get; set; }
    public decimal Valor { get; set; }
    public DateTimeOffset DataMovimento { get; set; }
    public long UsuarioId { get; set; }
    public string? DocumentoReferenciaTipo { get; set; }
    public long? DocumentoReferenciaId { get; set; }
    public string? Observacao { get; set; }
    public bool Estornado { get; set; }
}
