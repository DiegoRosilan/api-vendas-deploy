using GestorPDV.Application.Caixa;
using GestorPDV.Application.Common;
using GestorPDV.Domain.Caixa;
// Alias necessário: dentro de GestorPDV.Caixa.Servicos, "Caixa" sem
// qualificação resolve para o namespace GestorPDV.Caixa, não para a classe
// de domínio — ver comentário equivalente em ICaixaRepository.
using CaixaEntidade = GestorPDV.Domain.Caixa.Caixa;

namespace GestorPDV.Caixa.Servicos;

// RN-CAI-001: abertura, movimentação e fechamento de caixa.
public class CaixaService : ICaixaService
{
    private readonly ICaixaRepository _caixaRepository;

    public CaixaService(ICaixaRepository caixaRepository)
    {
        _caixaRepository = caixaRepository;
    }

    public async Task<Result<CaixaEntidade>> AbrirCaixaAsync(
        long filialId, long usuarioId, decimal valorAbertura, CancellationToken cancellationToken = default)
    {
        if (valorAbertura < 0)
        {
            return Result<CaixaEntidade>.Falha("O valor de abertura não pode ser negativo.");
        }

        var caixaAberto = await _caixaRepository.ObterAbertoAsync(filialId, cancellationToken);
        if (caixaAberto is not null)
        {
            return Result<CaixaEntidade>.Falha($"Já existe um caixa aberto para esta filial (nº {caixaAberto.Id}).");
        }

        var novoCaixa = new CaixaEntidade
        {
            FilialId = filialId,
            UsuarioAberturaId = usuarioId,
            ValorAbertura = valorAbertura,
            Status = StatusCaixa.Aberto,
            DataAbertura = DateTimeOffset.Now
        };

        novoCaixa.Id = await _caixaRepository.AbrirAsync(novoCaixa, cancellationToken);
        return Result<CaixaEntidade>.Ok(novoCaixa);
    }

    public async Task<Result> FecharCaixaAsync(
        long caixaId, long usuarioId, decimal valorInformado, CancellationToken cancellationToken = default)
    {
        var valorCalculado = await _caixaRepository.ObterSaldoAsync(caixaId, cancellationToken);
        var diferenca = valorInformado - valorCalculado;

        await _caixaRepository.FecharAsync(caixaId, usuarioId, valorInformado, valorCalculado, diferenca, cancellationToken);
        return Result.Ok();
    }

    public async Task<Result> RegistrarSangriaAsync(
        long caixaId, long usuarioId, decimal valor, string? observacao, CancellationToken cancellationToken = default)
    {
        if (valor <= 0)
        {
            return Result.Falha("O valor da sangria deve ser maior que zero.");
        }

        await _caixaRepository.RegistrarMovimentoAsync(
            caixaId, TipoMovimentoCaixa.Sangria, formaPagamentoId: null, -valor, usuarioId,
            documentoReferenciaTipo: null, documentoReferenciaId: null, observacao,
            unitOfWork: null, cancellationToken);

        return Result.Ok();
    }

    public async Task<Result> RegistrarSuprimentoAsync(
        long caixaId, long usuarioId, decimal valor, string? observacao, CancellationToken cancellationToken = default)
    {
        if (valor <= 0)
        {
            return Result.Falha("O valor do suprimento deve ser maior que zero.");
        }

        await _caixaRepository.RegistrarMovimentoAsync(
            caixaId, TipoMovimentoCaixa.Suprimento, formaPagamentoId: null, valor, usuarioId,
            documentoReferenciaTipo: null, documentoReferenciaId: null, observacao,
            unitOfWork: null, cancellationToken);

        return Result.Ok();
    }
}
