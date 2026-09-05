using GestorPDV.Application.Common;
using GestorPDV.Application.Financeiro;
using GestorPDV.Domain.Financeiro;

namespace GestorPDV.Financeiro.Servicos;

// RN-FIN-001/002/003, RN-CLI-001.
//
// Suposições registradas (docs/ROADMAP.md): multa fixa de 2% e juros de
// 0,033%/dia (~1% ao mês) sobre o valor da parcela — não há, ainda, SQL
// exata do sistema de referência para confirmar esses percentuais.
public class FinanceiroService : IFinanceiroService
{
    private const decimal MultaPercentual = 2m;
    private const decimal JurosPercentualAoDia = 0.033m;

    private readonly IFinanceiroRepository _financeiroRepository;
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public FinanceiroService(IFinanceiroRepository financeiroRepository, IUnitOfWorkFactory unitOfWorkFactory)
    {
        _financeiroRepository = financeiroRepository;
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    public (decimal Juros, decimal Multa) CalcularEncargos(decimal valorParcela, DateOnly vencimento, DateOnly dataReferencia)
    {
        var diasAtraso = dataReferencia.DayNumber - vencimento.DayNumber;
        if (diasAtraso <= 0)
        {
            return (0m, 0m);
        }

        var multa = Math.Round(valorParcela * MultaPercentual / 100m, 2, MidpointRounding.AwayFromZero);
        var juros = Math.Round(valorParcela * JurosPercentualAoDia / 100m * diasAtraso, 2, MidpointRounding.AwayFromZero);
        return (juros, multa);
    }

    public async Task<Result<long>> GerarContaReceberAsync(
        long pessoaId,
        long filialId,
        long? vendaId,
        decimal valorTotal,
        int numeroParcelas,
        int intervaloDias,
        DateOnly primeiroVencimento,
        CancellationToken cancellationToken = default)
    {
        if (numeroParcelas < 1)
        {
            return Result<long>.Falha("O número de parcelas deve ser pelo menos 1.");
        }

        var documento = GeradorDocumentoFinanceiro.Gerar(
            pessoaId, filialId, vendaId, valorTotal, numeroParcelas, intervaloDias, primeiroVencimento);

        await using var unitOfWork = _unitOfWorkFactory.Criar();
        await unitOfWork.BeginAsync(cancellationToken);
        try
        {
            await _financeiroRepository.GerarDocumentoAsync(documento, unitOfWork, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
            return Result<long>.Ok(documento.Id);
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            return Result<long>.Falha($"Erro ao gerar conta a receber: {ex.Message}");
        }
    }

    public async Task<Result> VerificarBloqueioClienteAsync(
        long pessoaId, int? bloquearVendaDiasVencido, CancellationToken cancellationToken = default)
    {
        if (!bloquearVendaDiasVencido.HasValue || bloquearVendaDiasVencido.Value <= 0)
        {
            return Result.Ok();
        }

        var diasAtraso = await _financeiroRepository.ObterDiasAtrasoMaximoAsync(
            pessoaId, DateOnly.FromDateTime(DateTime.Now), cancellationToken);

        if (diasAtraso > bloquearVendaDiasVencido.Value)
        {
            return Result.Falha(
                $"Cliente bloqueado para venda a prazo: parcela em atraso há {diasAtraso} dias " +
                $"(limite configurado: {bloquearVendaDiasVencido.Value} dias).");
        }

        return Result.Ok();
    }

    public async Task<Result> BaixarParcelaAsync(
        long parcelaId, long usuarioId, long formaPagamentoId, DateOnly dataBaixa,
        CancellationToken cancellationToken = default)
    {
        var parcela = await _financeiroRepository.ObterParcelaAsync(parcelaId, cancellationToken);
        if (parcela is null)
        {
            return Result.Falha("Parcela não encontrada.");
        }

        if (parcela.Situacao is SituacaoParcela.Baixado or SituacaoParcela.Cancelado)
        {
            return Result.Falha("Esta parcela já foi baixada ou cancelada.");
        }

        var (juros, multa) = CalcularEncargos(parcela.Valor, parcela.Vencimento, dataBaixa);

        var baixa = new Baixa
        {
            ParcelaId = parcela.Id,
            DocumentoId = parcela.DocumentoId,
            ValorPago = parcela.Valor + juros + multa,
            ValorJuros = juros,
            ValorMulta = multa,
            FormaPagamentoId = formaPagamentoId,
            UsuarioId = usuarioId
        };

        await using var unitOfWork = _unitOfWorkFactory.Criar();
        await unitOfWork.BeginAsync(cancellationToken);
        try
        {
            await _financeiroRepository.RegistrarBaixaAsync(baixa, unitOfWork, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            return Result.Falha($"Erro ao baixar parcela: {ex.Message}");
        }
    }
}
