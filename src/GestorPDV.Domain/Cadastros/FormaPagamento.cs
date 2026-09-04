namespace GestorPDV.Domain.Cadastros;

public enum TipoFormaPagamento
{
    Dinheiro,
    CartaoCredito,
    CartaoDebito,
    Pix,
    Boleto,
    Cheque,
    Crediario,
    Transferencia
}

public class FormaPagamento
{
    public long Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public TipoFormaPagamento Tipo { get; set; }
    public bool PermiteParcelamento { get; set; }
    public bool GeraFinanceiro { get; set; } = true;
    public bool MovimentaCaixa { get; set; } = true;
    public bool Ativo { get; set; } = true;
}
