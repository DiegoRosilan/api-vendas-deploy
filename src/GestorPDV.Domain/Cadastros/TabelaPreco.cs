namespace GestorPDV.Domain.Cadastros;

public class TabelaPreco
{
    public long Id { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public long? FilialId { get; set; }
    public DateOnly? VigenciaInicio { get; set; }
    public DateOnly? VigenciaFim { get; set; }
    public bool Ativo { get; set; } = true;
}

public class TabelaPrecoItem
{
    public long Id { get; set; }
    public long TabelaPrecoId { get; set; }
    public long ProdutoId { get; set; }
    public decimal Preco { get; set; }
}
