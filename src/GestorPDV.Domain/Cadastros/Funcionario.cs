namespace GestorPDV.Domain.Cadastros;

public class Funcionario
{
    public long Id { get; set; }
    public Pessoa? Pessoa { get; set; }
    public long? FilialId { get; set; }
    public long? UsuarioId { get; set; }
    public string? Cargo { get; set; }
    public decimal ComissaoPadraoPct { get; set; }
    public bool EhGerente { get; set; }
    public DateOnly? DataAdmissao { get; set; }
    public DateOnly? DataDemissao { get; set; }
}
