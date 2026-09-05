namespace GestorPDV.Domain.Vendas;

public enum TipoComissao
{
    Vendedor,
    Gerente
}

public enum StatusComissao
{
    Pendente,
    Pago,
    Cancelado
}

// RN-COM-001: comissão calculada a partir do total da venda e do percentual
// padrão do funcionário. Regras completas por vendedor/gerente (ex.: tabela
// de comissão por faixa, comissão de gerente sobre a equipe) ficam para uma
// fase futura — ver docs/ROADMAP.md.
public class Comissao
{
    public long Id { get; set; }
    public long VendaId { get; set; }
    public long FuncionarioId { get; set; }
    public TipoComissao Tipo { get; set; } = TipoComissao.Vendedor;
    public decimal Percentual { get; set; }
    public decimal ValorBase { get; set; }
    public decimal ValorComissao { get; set; }
    public DateOnly DataReferencia { get; set; }
    public StatusComissao Status { get; set; } = StatusComissao.Pendente;
    public DateOnly? DataPagamento { get; set; }
}
