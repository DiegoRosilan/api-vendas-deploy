using GestorPDV.Application.Common;

namespace GestorPDV.Application.Relatorios;

// Gera relatórios em PDF (FastReport.OpenSource — GestorPDV.Relatorios) a
// partir de dados lidos diretamente do PostgreSQL. Retorna os bytes do PDF
// em vez de um caminho de arquivo: quem chama decide onde salvar/abrir
// (mantém o serviço testável sem tocar em disco).
public interface IRelatorioService
{
    Task<Result<byte[]>> GerarRelatorioVendasAsync(
        long filialId, DateOnly dataInicio, DateOnly dataFim, CancellationToken cancellationToken = default);

    Task<Result<byte[]>> GerarRelatorioEstoqueAsync(long filialId, CancellationToken cancellationToken = default);

    Task<Result<byte[]>> GerarRelatorioContasReceberAsync(long filialId, CancellationToken cancellationToken = default);
}
