using System.Data;
using System.Globalization;
using FastReport;
using GestorPDV.Application.Common;
using GestorPDV.Application.Financeiro;
using GestorPDV.Application.Relatorios;

namespace GestorPDV.Relatorios.Servicos;

public class RelatorioService : IRelatorioService
{
    private static readonly CultureInfo Moeda = CultureInfo.GetCultureInfo("pt-BR");

    private readonly IRelatorioRepository _relatorioRepository;
    private readonly IFinanceiroService _financeiroService;

    public RelatorioService(IRelatorioRepository relatorioRepository, IFinanceiroService financeiroService)
    {
        _relatorioRepository = relatorioRepository;
        _financeiroService = financeiroService;
    }

    public async Task<Result<byte[]>> GerarRelatorioVendasAsync(
        long filialId, DateOnly dataInicio, DateOnly dataFim, CancellationToken cancellationToken = default)
    {
        if (dataFim < dataInicio)
        {
            return Result<byte[]>.Falha("A data final não pode ser anterior à data inicial.");
        }

        var itens = await _relatorioRepository.ListarVendasAsync(filialId, dataInicio, dataFim, cancellationToken);

        var tabela = new DataTable("Vendas");
        tabela.Columns.Add("numero", typeof(string));
        tabela.Columns.Add("data", typeof(string));
        tabela.Columns.Add("cliente", typeof(string));
        tabela.Columns.Add("vendedor", typeof(string));
        tabela.Columns.Add("total", typeof(string));
        tabela.Columns.Add("status", typeof(string));

        foreach (var item in itens)
        {
            tabela.Rows.Add(
                item.Numero.ToString(),
                item.DataVenda.ToString("dd/MM/yyyy HH:mm"),
                item.ClienteNome,
                item.VendedorNome,
                item.Total.ToString("C", Moeda),
                item.Status);
        }

        var colunas = new[]
        {
            new RelatorioTabularBuilder.Coluna("Número", "numero", 2f),
            new RelatorioTabularBuilder.Coluna("Data", "data", 3.5f),
            new RelatorioTabularBuilder.Coluna("Cliente", "cliente", 6f),
            new RelatorioTabularBuilder.Coluna("Vendedor", "vendedor", 5f),
            new RelatorioTabularBuilder.Coluna("Total", "total", 3f, HorzAlign.Right),
            new RelatorioTabularBuilder.Coluna("Status", "status", 3f)
        };

        var titulo = $"Relatório de vendas — {dataInicio:dd/MM/yyyy} a {dataFim:dd/MM/yyyy}";
        var pdf = RelatorioTabularBuilder.GerarPdf(titulo, tabela, colunas);
        return Result<byte[]>.Ok(pdf);
    }

    public async Task<Result<byte[]>> GerarRelatorioEstoqueAsync(long filialId, CancellationToken cancellationToken = default)
    {
        var itens = await _relatorioRepository.ListarEstoqueAtualAsync(filialId, cancellationToken);

        var tabela = new DataTable("Estoque");
        tabela.Columns.Add("codigoBarras", typeof(string));
        tabela.Columns.Add("descricao", typeof(string));
        tabela.Columns.Add("quantidade", typeof(string));
        tabela.Columns.Add("precoVenda", typeof(string));
        tabela.Columns.Add("valorTotal", typeof(string));

        foreach (var item in itens)
        {
            tabela.Rows.Add(
                item.CodigoBarras ?? string.Empty,
                item.ProdutoDescricao,
                item.QuantidadeAtual.ToString("N3", Moeda),
                item.PrecoVenda.ToString("C", Moeda),
                item.ValorTotal.ToString("C", Moeda));
        }

        var colunas = new[]
        {
            new RelatorioTabularBuilder.Coluna("Cód. barras", "codigoBarras", 3f),
            new RelatorioTabularBuilder.Coluna("Produto", "descricao", 8f),
            new RelatorioTabularBuilder.Coluna("Quantidade", "quantidade", 3f, HorzAlign.Right),
            new RelatorioTabularBuilder.Coluna("Preço venda", "precoVenda", 3.5f, HorzAlign.Right),
            new RelatorioTabularBuilder.Coluna("Valor total", "valorTotal", 3.5f, HorzAlign.Right)
        };

        var pdf = RelatorioTabularBuilder.GerarPdf("Relatório de estoque atual", tabela, colunas);
        return Result<byte[]>.Ok(pdf);
    }

    public async Task<Result<byte[]>> GerarRelatorioContasReceberAsync(
        long filialId, CancellationToken cancellationToken = default)
    {
        var itens = await _relatorioRepository.ListarContasReceberAsync(filialId, cancellationToken);
        var hoje = DateOnly.FromDateTime(DateTime.Now);

        var tabela = new DataTable("ContasReceber");
        tabela.Columns.Add("documento", typeof(string));
        tabela.Columns.Add("cliente", typeof(string));
        tabela.Columns.Add("vencimento", typeof(string));
        tabela.Columns.Add("diasAtraso", typeof(string));
        tabela.Columns.Add("valorAtualizado", typeof(string));
        tabela.Columns.Add("situacao", typeof(string));

        foreach (var item in itens)
        {
            var (juros, multa) = _financeiroService.CalcularEncargos(item.Valor, item.Vencimento, hoje);
            item.Juros = juros;
            item.Multa = multa;

            var diasAtraso = Math.Max(0, hoje.DayNumber - item.Vencimento.DayNumber);
            tabela.Rows.Add(
                $"{item.NumeroDocumento}/{item.NumeroParcela}",
                item.ClienteNome,
                item.Vencimento.ToString("dd/MM/yyyy"),
                diasAtraso.ToString(),
                item.ValorAtualizado.ToString("C", Moeda),
                item.Situacao);
        }

        var colunas = new[]
        {
            new RelatorioTabularBuilder.Coluna("Documento", "documento", 3f),
            new RelatorioTabularBuilder.Coluna("Cliente", "cliente", 7f),
            new RelatorioTabularBuilder.Coluna("Vencimento", "vencimento", 3f),
            new RelatorioTabularBuilder.Coluna("Dias atraso", "diasAtraso", 2.5f, HorzAlign.Right),
            new RelatorioTabularBuilder.Coluna("Valor atualizado", "valorAtualizado", 3.5f, HorzAlign.Right),
            new RelatorioTabularBuilder.Coluna("Situação", "situacao", 2.5f)
        };

        var pdf = RelatorioTabularBuilder.GerarPdf("Relatório de contas a receber em aberto", tabela, colunas);
        return Result<byte[]>.Ok(pdf);
    }
}
