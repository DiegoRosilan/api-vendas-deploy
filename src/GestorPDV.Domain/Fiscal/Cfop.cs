namespace GestorPDV.Domain.Fiscal;

public enum TipoOperacaoFiscal
{
    Entrada,
    Saida
}

// RN-FIS-008: CFOP de operação/devolução. O motor tributário completo
// (ICMS, ICMS-ST, PIS/COFINS, IPI, ISS, DIFAL, FCP, IBS/CBS) é implementado
// em GestorPDV.Fiscal a partir da Fase 6/8.
public class Cfop
{
    public long Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public TipoOperacaoFiscal TipoOperacao { get; set; }
    public bool Devolucao { get; set; }
}
