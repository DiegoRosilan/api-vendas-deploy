namespace GestorPDV.Domain.Financeiro;

public enum TipoDocumentoFinanceiro
{
    Receber,
    Pagar
}

public enum SituacaoDocumentoFinanceiro
{
    Aberto,
    Parcial,
    Baixado,
    Cancelado,
    Renegociado
}

public enum OrigemDocumentoFinanceiro
{
    Venda,
    Manual,
    Renegociacao
}

// RN-FIN-001/002/003/004: contas a receber/pagar. Juros, multa e
// renegociação (CalculaJuros, CalculaMulta, Renegocia) são implementados em
// GestorPDV.Financeiro a partir da Fase 7.
public class DocumentoFinanceiro
{
    public long Id { get; set; }
    public TipoDocumentoFinanceiro Tipo { get; set; }
    public long PessoaId { get; set; }
    public long FilialId { get; set; }
    public string NumeroDocumento { get; set; } = string.Empty;
    public decimal ValorOriginal { get; set; }
    public DateOnly DataEmissao { get; set; }
    public DateOnly DataVencimento { get; set; }
    public SituacaoDocumentoFinanceiro Situacao { get; set; } = SituacaoDocumentoFinanceiro.Aberto;
    public OrigemDocumentoFinanceiro Origem { get; set; } = OrigemDocumentoFinanceiro.Manual;
    public long? VendaId { get; set; }
    public string? Observacao { get; set; }

    public List<Parcela> Parcelas { get; set; } = new();
}
