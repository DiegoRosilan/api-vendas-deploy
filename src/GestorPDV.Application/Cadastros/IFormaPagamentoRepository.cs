using GestorPDV.Domain.Cadastros;

namespace GestorPDV.Application.Cadastros;

public interface IFormaPagamentoRepository
{
    Task<IReadOnlyList<FormaPagamento>> ListarAsync(CancellationToken cancellationToken = default);
    Task<FormaPagamento?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default);
    Task<long> InserirAsync(FormaPagamento formaPagamento, CancellationToken cancellationToken = default);
    Task AtualizarAsync(FormaPagamento formaPagamento, CancellationToken cancellationToken = default);
}
