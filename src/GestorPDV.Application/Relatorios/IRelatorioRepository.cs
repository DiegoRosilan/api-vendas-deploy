namespace GestorPDV.Application.Relatorios;

public interface IRelatorioRepository
{
    Task<IReadOnlyList<VendaRelatorioItem>> ListarVendasAsync(
        long filialId, DateOnly dataInicio, DateOnly dataFim, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EstoqueRelatorioItem>> ListarEstoqueAtualAsync(
        long filialId, CancellationToken cancellationToken = default);

    // Não inclui juros/multa (calculados em GestorPDV.Financeiro a partir
    // do valor/vencimento de cada parcela, RN-FIN-002/003).
    Task<IReadOnlyList<ContaReceberRelatorioItem>> ListarContasReceberAsync(
        long filialId, CancellationToken cancellationToken = default);
}
