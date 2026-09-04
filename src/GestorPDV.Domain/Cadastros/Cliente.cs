namespace GestorPDV.Domain.Cadastros;

public class Cliente
{
    // Mesmo Id da Pessoa correspondente (cad_cliente.id referencia cad_pessoa.id).
    public long Id { get; set; }
    public Pessoa? Pessoa { get; set; }
    public decimal LimiteCredito { get; set; }

    // RN-CLI-001: dias de atraso a partir dos quais a venda a prazo é bloqueada.
    public int? BloquearVendaDiasVencido { get; set; }
    public long? TabelaPrecoId { get; set; }
    public string? Observacao { get; set; }
}
