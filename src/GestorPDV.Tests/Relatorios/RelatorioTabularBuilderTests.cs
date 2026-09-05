using System.Data;
using System.Text;
using FastReport;
using GestorPDV.Relatorios;
using Xunit;

namespace GestorPDV.Tests.Relatorios;

public class RelatorioTabularBuilderTests
{
    [Fact]
    public void GerarPdf_ComDadosBasicos_ProduzUmPdfValido()
    {
        var tabela = new DataTable("Itens");
        tabela.Columns.Add("nome", typeof(string));
        tabela.Columns.Add("valor", typeof(string));
        tabela.Rows.Add("Produto A", "R$ 10,00");
        tabela.Rows.Add("Produto B", "R$ 25,50");

        var colunas = new[]
        {
            new RelatorioTabularBuilder.Coluna("Nome", "nome", 10f),
            new RelatorioTabularBuilder.Coluna("Valor", "valor", 5f, HorzAlign.Right)
        };

        var pdf = RelatorioTabularBuilder.GerarPdf("Relatório de teste", tabela, colunas);

        Assert.NotEmpty(pdf);
        var cabecalho = Encoding.ASCII.GetString(pdf, 0, 5);
        Assert.Equal("%PDF-", cabecalho);
    }

    [Fact]
    public void GerarPdf_SemLinhas_AindaAssimProduzUmPdfValido()
    {
        var tabela = new DataTable("Itens");
        tabela.Columns.Add("nome", typeof(string));

        var colunas = new[] { new RelatorioTabularBuilder.Coluna("Nome", "nome", 10f) };

        var pdf = RelatorioTabularBuilder.GerarPdf("Relatório vazio", tabela, colunas);

        Assert.NotEmpty(pdf);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(pdf, 0, 5));
    }
}
