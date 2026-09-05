using GestorPDV.Application.Common;
using GestorPDV.Application.Financeiro;
using GestorPDV.Domain.Financeiro;
using GestorPDV.Financeiro.Servicos;
using Xunit;

namespace GestorPDV.Tests.Financeiro;

class UnitOfWorkFake : IUnitOfWork
{
    public Task BeginAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

class UnitOfWorkFactoryFake : IUnitOfWorkFactory
{
    public IUnitOfWork Criar() => new UnitOfWorkFake();
}

// Dublê em memória com o mínimo necessário para exercitar FinanceiroService
// sem depender de PostgreSQL (mesma abordagem de AutenticacaoServiceTests).
class FinanceiroRepositoryFake : IFinanceiroRepository
{
    private long _proximoIdBaixa = 1;

    public List<DocumentoFinanceiro> Documentos { get; } = new();
    public List<Baixa> Baixas { get; } = new();

    public Task GerarDocumentoAsync(
        DocumentoFinanceiro documento, IUnitOfWork unitOfWork, CancellationToken cancellationToken = default)
    {
        documento.Id = Documentos.Count + 1;
        foreach (var parcela in documento.Parcelas)
        {
            parcela.DocumentoId = documento.Id;
        }

        Documentos.Add(documento);
        return Task.CompletedTask;
    }

    public Task<DocumentoFinanceiro?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Documentos.FirstOrDefault(d => d.Id == id));

    public Task<Parcela?> ObterParcelaAsync(long parcelaId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Documentos.SelectMany(d => d.Parcelas).FirstOrDefault(p => p.Id == parcelaId));

    public Task<IReadOnlyList<DocumentoFinanceiro>> ListarEmAbertoPorPessoaAsync(
        long pessoaId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DocumentoFinanceiro>>(Documentos
            .Where(d => d.PessoaId == pessoaId && d.Situacao is SituacaoDocumentoFinanceiro.Aberto or SituacaoDocumentoFinanceiro.Parcial)
            .ToList());

    public Task<IReadOnlyList<DocumentoFinanceiro>> ListarEmAbertoAsync(
        long filialId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DocumentoFinanceiro>>(Documentos
            .Where(d => d.FilialId == filialId && d.Situacao is SituacaoDocumentoFinanceiro.Aberto or SituacaoDocumentoFinanceiro.Parcial)
            .ToList());

    public Task<int> ObterDiasAtrasoMaximoAsync(
        long pessoaId, DateOnly dataReferencia, CancellationToken cancellationToken = default)
    {
        var maiorAtraso = Documentos
            .Where(d => d.PessoaId == pessoaId)
            .SelectMany(d => d.Parcelas)
            .Where(p => p.Situacao is SituacaoParcela.Aberto or SituacaoParcela.Parcial && p.Vencimento < dataReferencia)
            .Select(p => dataReferencia.DayNumber - p.Vencimento.DayNumber)
            .DefaultIfEmpty(0)
            .Max();

        return Task.FromResult(maiorAtraso);
    }

    public Task RegistrarBaixaAsync(Baixa baixa, IUnitOfWork unitOfWork, CancellationToken cancellationToken = default)
    {
        baixa.Id = _proximoIdBaixa++;
        baixa.DataBaixa = DateTimeOffset.Now;
        Baixas.Add(baixa);

        var parcela = Documentos.SelectMany(d => d.Parcelas).First(p => p.Id == baixa.ParcelaId);
        parcela.Situacao = SituacaoParcela.Baixado;

        var documento = Documentos.First(d => d.Id == baixa.DocumentoId);
        documento.Situacao = documento.Parcelas.All(p => p.Situacao is SituacaoParcela.Baixado or SituacaoParcela.Cancelado)
            ? SituacaoDocumentoFinanceiro.Baixado
            : SituacaoDocumentoFinanceiro.Parcial;

        return Task.CompletedTask;
    }

    public Task CancelarDocumentosPorVendaAsync(long vendaId, IUnitOfWork unitOfWork, CancellationToken cancellationToken = default)
    {
        foreach (var documento in Documentos.Where(d => d.VendaId == vendaId))
        {
            foreach (var parcela in documento.Parcelas.Where(p => p.Situacao is SituacaoParcela.Aberto or SituacaoParcela.Parcial))
            {
                parcela.Situacao = SituacaoParcela.Cancelado;
            }

            documento.Situacao = SituacaoDocumentoFinanceiro.Cancelado;
        }

        return Task.CompletedTask;
    }
}

public class FinanceiroServiceTests
{
    private static FinanceiroService CriarServico(out FinanceiroRepositoryFake repositorio)
    {
        repositorio = new FinanceiroRepositoryFake();
        return new FinanceiroService(repositorio, new UnitOfWorkFactoryFake());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CalcularEncargos_SemAtrasoOuAntecipado_NaoCobraJurosNemMulta(int diasAposVencimento)
    {
        var servico = CriarServico(out _);
        var vencimento = new DateOnly(2026, 1, 10);

        var (juros, multa) = servico.CalcularEncargos(1000m, vencimento, vencimento.AddDays(diasAposVencimento));

        Assert.Equal(0m, juros);
        Assert.Equal(0m, multa);
    }

    [Fact]
    public void CalcularEncargos_ComDezDiasDeAtraso_CobraMultaFixaEJurosProporcionais()
    {
        // RN-FIN-002/003 (suposição documentada no ROADMAP): multa fixa de
        // 2% + juros de 0,033%/dia sobre o valor da parcela.
        var servico = CriarServico(out _);
        var vencimento = new DateOnly(2026, 1, 10);

        var (juros, multa) = servico.CalcularEncargos(1000m, vencimento, vencimento.AddDays(10));

        Assert.Equal(20m, multa);
        Assert.Equal(3.30m, juros);
    }

    [Fact]
    public void CalcularEncargos_UmDiaDeAtraso_JaCobraMultaEJurosDeUmDia()
    {
        var servico = CriarServico(out _);
        var vencimento = new DateOnly(2026, 1, 10);

        var (juros, multa) = servico.CalcularEncargos(500m, vencimento, vencimento.AddDays(1));

        Assert.Equal(10m, multa);
        Assert.Equal(0.17m, juros);
    }

    [Fact]
    public async Task VerificarBloqueioCliente_SemLimiteConfigurado_NuncaBloqueia()
    {
        var servico = CriarServico(out _);

        var resultado = await servico.VerificarBloqueioClienteAsync(pessoaId: 1, bloquearVendaDiasVencido: null);

        Assert.True(resultado.Sucesso);
    }

    [Fact]
    public async Task VerificarBloqueioCliente_ComAtrasoDentroDoLimite_NaoBloqueia()
    {
        var servico = CriarServico(out var repositorio);
        var hoje = DateOnly.FromDateTime(DateTime.Now);
        repositorio.Documentos.Add(new DocumentoFinanceiro
        {
            Id = 1,
            PessoaId = 1,
            FilialId = 1,
            Situacao = SituacaoDocumentoFinanceiro.Aberto,
            Parcelas = { new Parcela { Id = 1, DocumentoId = 1, Valor = 100m, Vencimento = hoje.AddDays(-5), Situacao = SituacaoParcela.Aberto } }
        });

        var resultado = await servico.VerificarBloqueioClienteAsync(pessoaId: 1, bloquearVendaDiasVencido: 10);

        Assert.True(resultado.Sucesso);
    }

    [Fact]
    public async Task VerificarBloqueioCliente_ComAtrasoAcimaDoLimite_Bloqueia()
    {
        // RN-CLI-001.
        var servico = CriarServico(out var repositorio);
        var hoje = DateOnly.FromDateTime(DateTime.Now);
        repositorio.Documentos.Add(new DocumentoFinanceiro
        {
            Id = 1,
            PessoaId = 1,
            FilialId = 1,
            Situacao = SituacaoDocumentoFinanceiro.Aberto,
            Parcelas = { new Parcela { Id = 1, DocumentoId = 1, Valor = 100m, Vencimento = hoje.AddDays(-15), Situacao = SituacaoParcela.Aberto } }
        });

        var resultado = await servico.VerificarBloqueioClienteAsync(pessoaId: 1, bloquearVendaDiasVencido: 10);

        Assert.False(resultado.Sucesso);
    }

    [Fact]
    public async Task BaixarParcela_EmDia_NaoCobraEncargosEMarcaParcelaEDocumentoComoBaixados()
    {
        var servico = CriarServico(out var repositorio);
        var hoje = DateOnly.FromDateTime(DateTime.Now);
        repositorio.Documentos.Add(new DocumentoFinanceiro
        {
            Id = 1,
            PessoaId = 1,
            FilialId = 1,
            Situacao = SituacaoDocumentoFinanceiro.Aberto,
            Parcelas = { new Parcela { Id = 1, DocumentoId = 1, Valor = 200m, Vencimento = hoje.AddDays(5), Situacao = SituacaoParcela.Aberto } }
        });

        var resultado = await servico.BaixarParcelaAsync(parcelaId: 1, usuarioId: 1, formaPagamentoId: 1, dataBaixa: hoje);

        Assert.True(resultado.Sucesso);
        var baixa = Assert.Single(repositorio.Baixas);
        Assert.Equal(200m, baixa.ValorPago);
        Assert.Equal(0m, baixa.ValorJuros);
        Assert.Equal(0m, baixa.ValorMulta);
        Assert.Equal(SituacaoParcela.Baixado, repositorio.Documentos[0].Parcelas[0].Situacao);
        Assert.Equal(SituacaoDocumentoFinanceiro.Baixado, repositorio.Documentos[0].Situacao);
    }

    [Fact]
    public async Task BaixarParcela_EmAtraso_SomaJurosEMultaAoValorPago()
    {
        var servico = CriarServico(out var repositorio);
        var vencimento = new DateOnly(2026, 1, 10);
        repositorio.Documentos.Add(new DocumentoFinanceiro
        {
            Id = 1,
            PessoaId = 1,
            FilialId = 1,
            Situacao = SituacaoDocumentoFinanceiro.Aberto,
            Parcelas = { new Parcela { Id = 1, DocumentoId = 1, Valor = 1000m, Vencimento = vencimento, Situacao = SituacaoParcela.Aberto } }
        });

        var resultado = await servico.BaixarParcelaAsync(
            parcelaId: 1, usuarioId: 1, formaPagamentoId: 1, dataBaixa: vencimento.AddDays(10));

        Assert.True(resultado.Sucesso);
        var baixa = Assert.Single(repositorio.Baixas);
        Assert.Equal(20m, baixa.ValorMulta);
        Assert.Equal(3.30m, baixa.ValorJuros);
        Assert.Equal(1023.30m, baixa.ValorPago);
    }

    [Fact]
    public async Task BaixarParcela_JaBaixada_Falha()
    {
        var servico = CriarServico(out var repositorio);
        var hoje = DateOnly.FromDateTime(DateTime.Now);
        repositorio.Documentos.Add(new DocumentoFinanceiro
        {
            Id = 1,
            PessoaId = 1,
            FilialId = 1,
            Situacao = SituacaoDocumentoFinanceiro.Baixado,
            Parcelas = { new Parcela { Id = 1, DocumentoId = 1, Valor = 200m, Vencimento = hoje, Situacao = SituacaoParcela.Baixado } }
        });

        var resultado = await servico.BaixarParcelaAsync(parcelaId: 1, usuarioId: 1, formaPagamentoId: 1, dataBaixa: hoje);

        Assert.False(resultado.Sucesso);
        Assert.Empty(repositorio.Baixas);
    }

    [Fact]
    public async Task BaixarParcela_Inexistente_Falha()
    {
        var servico = CriarServico(out _);

        var resultado = await servico.BaixarParcelaAsync(
            parcelaId: 999, usuarioId: 1, formaPagamentoId: 1, dataBaixa: DateOnly.FromDateTime(DateTime.Now));

        Assert.False(resultado.Sucesso);
    }
}
