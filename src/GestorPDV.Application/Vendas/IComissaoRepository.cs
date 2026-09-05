using GestorPDV.Application.Common;
using GestorPDV.Domain.Vendas;

namespace GestorPDV.Application.Vendas;

public interface IComissaoRepository
{
    Task InserirAsync(Comissao comissao, IUnitOfWork unitOfWork, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Comissao>> ListarPorVendaAsync(long vendaId, CancellationToken cancellationToken = default);
}
