namespace GestorPDV.Domain.Caixa;

public enum StatusCaixa
{
    Aberto,
    Fechado
}

// RN-CAI-001: abertura, movimentação e fechamento de caixa. As operações de
// sangria/suprimento/conferência são implementadas em GestorPDV.Caixa a
// partir da Fase 7.
public class Caixa
{
    public long Id { get; set; }
    public long FilialId { get; set; }
    public long UsuarioAberturaId { get; set; }
    public DateTimeOffset DataAbertura { get; set; }
    public decimal ValorAbertura { get; set; }
    public long? UsuarioFechamentoId { get; set; }
    public DateTimeOffset? DataFechamento { get; set; }
    public decimal? ValorFechamentoInformado { get; set; }
    public decimal? ValorFechamentoCalculado { get; set; }
    public decimal? Diferenca { get; set; }
    public StatusCaixa Status { get; set; } = StatusCaixa.Aberto;
}
