using GestorPDV.Domain.Cadastros;

namespace GestorPDV.Application.Cadastros;

public interface ICondicaoPagamentoRepository
{
    Task<IReadOnlyList<CondicaoPagamento>> ListarAsync(CancellationToken cancellationToken = default);
    Task<CondicaoPagamento?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default);
    Task<long> InserirAsync(CondicaoPagamento condicaoPagamento, CancellationToken cancellationToken = default);
    Task AtualizarAsync(CondicaoPagamento condicaoPagamento, CancellationToken cancellationToken = default);
}
