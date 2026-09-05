using GestorPDV.Domain.Cadastros;

namespace GestorPDV.Application.Cadastros;

public interface ITabelaPrecoRepository
{
    Task<IReadOnlyList<TabelaPreco>> ListarAsync(CancellationToken cancellationToken = default);
    Task<TabelaPreco?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default);
    Task<long> InserirAsync(TabelaPreco tabelaPreco, CancellationToken cancellationToken = default);
    Task AtualizarAsync(TabelaPreco tabelaPreco, CancellationToken cancellationToken = default);

    // Itens (produto + preço) da tabela — a UI de associação de produtos
    // chega junto com a resolução de preço na Fase 6 (Vendas); por ora só a
    // persistência está disponível.
    Task<IReadOnlyList<TabelaPrecoItem>> ListarItensAsync(long tabelaPrecoId, CancellationToken cancellationToken = default);
    Task DefinirItemAsync(long tabelaPrecoId, long produtoId, decimal preco, CancellationToken cancellationToken = default);
    Task RemoverItemAsync(long tabelaPrecoId, long produtoId, CancellationToken cancellationToken = default);
}
