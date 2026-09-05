namespace GestorPDV.Application.Relatorios;

// Linha do relatório de contas a receber em aberto, com juros/multa por
// atraso já calculados na data de geração do relatório (RN-FIN-002/003).
public class ContaReceberRelatorioItem
{
    public long DocumentoId { get; set; }
    public string NumeroDocumento { get; set; } = string.Empty;
    public int NumeroParcela { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public DateOnly Vencimento { get; set; }
    public decimal Valor { get; set; }
    public int DiasAtraso { get; set; }
    public decimal Juros { get; set; }
    public decimal Multa { get; set; }
    public decimal ValorAtualizado => Valor + Juros + Multa;
    public string Situacao { get; set; } = string.Empty;
}
