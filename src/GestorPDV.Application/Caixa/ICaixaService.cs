using GestorPDV.Application.Common;
// Alias necessário: dentro de GestorPDV.Application.Caixa, "Caixa" sem
// qualificação resolve para o namespace, não para a classe de domínio.
using CaixaEntidade = GestorPDV.Domain.Caixa.Caixa;

namespace GestorPDV.Application.Caixa;

// RN-CAI-001: abertura, movimentação e fechamento de caixa.
public interface ICaixaService
{
    Task<Result<CaixaEntidade>> AbrirCaixaAsync(
        long filialId, long usuarioId, decimal valorAbertura, CancellationToken cancellationToken = default);

    Task<Result> FecharCaixaAsync(
        long caixaId, long usuarioId, decimal valorInformado, CancellationToken cancellationToken = default);

    Task<Result> RegistrarSangriaAsync(
        long caixaId, long usuarioId, decimal valor, string? observacao, CancellationToken cancellationToken = default);

    Task<Result> RegistrarSuprimentoAsync(
        long caixaId, long usuarioId, decimal valor, string? observacao, CancellationToken cancellationToken = default);
}
