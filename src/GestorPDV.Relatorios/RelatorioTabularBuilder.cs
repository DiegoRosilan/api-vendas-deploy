using System.Data;
using System.Drawing;
using FastReport;
using FastReport.Export.PdfSimple;

namespace GestorPDV.Relatorios;

// Monta um relatório tabular simples (título, cabeçalho de colunas, uma
// linha por registro da tabela e rodapé com data/paginação) e exporta para
// PDF. Construído via API do FastReport (não via designer/.frx) porque
// este ambiente de desenvolvimento não tem acesso ao FastReport Designer —
// ver docs/ROADMAP.md.
public static class RelatorioTabularBuilder
{
    public sealed record Coluna(string Cabecalho, string CampoDados, float LarguraCm, HorzAlign Alinhamento = HorzAlign.Left);

    public static byte[] GerarPdf(string titulo, DataTable dados, IReadOnlyList<Coluna> colunas)
    {
        var cm = FastReport.Utils.Units.Centimeters;
        var nomeDataset = string.IsNullOrEmpty(dados.TableName) ? "Dados" : dados.TableName;

        var report = new Report();
        report.RegisterData(dados, nomeDataset);
        report.GetDataSource(nomeDataset)!.Enabled = true;

        var page = new ReportPage { Name = "Page1" };
        report.Pages.Add(page);

        var tituloBand = new ReportTitleBand { Name = "ReportTitle1", Height = 1.5f * cm };
        page.Bands.Add(tituloBand);
        page.ReportTitle = tituloBand;
        tituloBand.Objects.Add(new TextObject
        {
            Name = "Titulo",
            Bounds = new RectangleF(0, 0, 25 * cm, 0.9f * cm),
            Text = titulo,
            Font = new Font("Arial", 14, FontStyle.Bold)
        });

        var cabecalho = new PageHeaderBand { Name = "PageHeader1", Height = 0.7f * cm };
        page.Bands.Add(cabecalho);
        page.PageHeader = cabecalho;

        var linhaDados = new DataBand
        {
            Name = "Data1",
            Height = 0.55f * cm,
            DataSource = report.GetDataSource(nomeDataset)
        };
        page.Bands.Add(linhaDados);

        var rodape = new PageFooterBand { Name = "PageFooter1", Height = 0.6f * cm };
        page.Bands.Add(rodape);
        page.PageFooter = rodape;
        rodape.Objects.Add(new TextObject
        {
            Name = "RodapeData",
            Bounds = new RectangleF(0, 0, 15 * cm, 0.5f * cm),
            // Texto literal calculado em C#, não "[Date] [Time]" do FastReport:
            // "[Time]" não é um campo de sistema reconhecido nesta versão do
            // FastReport.OpenSource (só "[Date]" é) — confirmado por teste real
            // em GestorPDV.Tests/Relatorios/RelatorioTabularBuilderTests.cs.
            Text = $"Gerado em {DateTime.Now:dd/MM/yyyy HH:mm}",
            Font = new Font("Arial", 8)
        });
        rodape.Objects.Add(new TextObject
        {
            Name = "RodapePagina",
            Bounds = new RectangleF(15 * cm, 0, 10 * cm, 0.5f * cm),
            Text = "Página [Page#] de [TotalPages#]",
            HorzAlign = HorzAlign.Right,
            Font = new Font("Arial", 8)
        });

        var x = 0f;
        for (var indice = 0; indice < colunas.Count; indice++)
        {
            var coluna = colunas[indice];
            var largura = coluna.LarguraCm * cm;

            cabecalho.Objects.Add(new TextObject
            {
                Name = $"Cabecalho{indice}",
                Bounds = new RectangleF(x, 0, largura, 0.5f * cm),
                Text = coluna.Cabecalho,
                HorzAlign = coluna.Alinhamento,
                Font = new Font("Arial", 9, FontStyle.Bold)
            });

            linhaDados.Objects.Add(new TextObject
            {
                Name = $"Coluna{indice}",
                Bounds = new RectangleF(x, 0, largura, 0.45f * cm),
                Text = $"[{nomeDataset}.{coluna.CampoDados}]",
                HorzAlign = coluna.Alinhamento,
                Font = new Font("Arial", 9)
            });

            x += largura;
        }

        report.Prepare();

        using var stream = new MemoryStream();
        report.Export(new PDFSimpleExport(), stream);
        return stream.ToArray();
    }
}
