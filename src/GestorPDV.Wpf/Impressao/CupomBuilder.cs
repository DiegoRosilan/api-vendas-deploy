using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using GestorPDV.Wpf.ViewModels.Vendas;

namespace GestorPDV.Wpf.Impressao;

// Monta o cupom não fiscal de uma venda finalizada como um FlowDocument
// simples (texto monoespaçado, largura de impressora térmica de 80mm),
// para impressão via ImpressoraHelper. Não é um cupom fiscal (NFC-e) — a
// emissão fiscal depende do motor tributário (GestorPDV.Fiscal), que ainda
// não foi implementado (ver docs/ROADMAP.md).
public static class CupomBuilder
{
    // 80mm a 96 DPI (1 unidade WPF = 1/96 polegada).
    private const double LarguraCupom = 80d / 25.4d * 96d;
    private const int LarguraDescricao = 20;
    private const int LarguraValor = 10;

    public static FlowDocument Montar(
        string nomeFilial,
        long numeroVenda,
        DateTimeOffset dataVenda,
        string clienteNome,
        IReadOnlyList<ItemCarrinhoExibicao> itens,
        IReadOnlyList<PagamentoExibicao> pagamentos,
        decimal subtotal,
        decimal desconto,
        decimal total)
    {
        var documento = new FlowDocument
        {
            PageWidth = LarguraCupom,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 11,
            PagePadding = new Thickness(4)
        };

        var separador = new string('-', LarguraDescricao + LarguraValor);

        var cabecalho = new Paragraph(new Run(nomeFilial)) { TextAlignment = TextAlignment.Center, FontWeight = FontWeights.Bold };
        documento.Blocks.Add(cabecalho);

        documento.Blocks.Add(new Paragraph(new Run(separador)));
        documento.Blocks.Add(new Paragraph(new Run($"Venda nº {numeroVenda}")));
        documento.Blocks.Add(new Paragraph(new Run(dataVenda.ToString("dd/MM/yyyy HH:mm"))));
        documento.Blocks.Add(new Paragraph(new Run($"Cliente: {clienteNome}")));
        documento.Blocks.Add(new Paragraph(new Run(separador)));

        foreach (var item in itens)
        {
            var linhaDescricao = Truncar(item.Descricao, LarguraDescricao + LarguraValor);
            var linhaQuantidadeValor =
                $"{item.Quantidade.ToString("0.###").PadRight(8)}x {item.ValorUnitario:0.00}".PadRight(LarguraDescricao) +
                item.Total.ToString("0.00").PadLeft(LarguraValor);

            documento.Blocks.Add(new Paragraph(new Run(linhaDescricao)) { Margin = new Thickness(0, 4, 0, 0) });
            documento.Blocks.Add(new Paragraph(new Run(linhaQuantidadeValor)) { Margin = new Thickness(0) });
        }

        documento.Blocks.Add(new Paragraph(new Run(separador)) { Margin = new Thickness(0, 6, 0, 0) });
        documento.Blocks.Add(LinhaValor("Subtotal:", subtotal));
        if (desconto > 0)
        {
            documento.Blocks.Add(LinhaValor("Desconto:", -desconto));
        }

        var linhaTotal = LinhaValor("TOTAL:", total);
        linhaTotal.FontWeight = FontWeights.Bold;
        linhaTotal.FontSize = 13;
        documento.Blocks.Add(linhaTotal);

        documento.Blocks.Add(new Paragraph(new Run(separador)) { Margin = new Thickness(0, 6, 0, 0) });
        documento.Blocks.Add(new Paragraph(new Run("Pagamento")) { FontWeight = FontWeights.Bold });
        foreach (var pagamento in pagamentos)
        {
            var descricaoParcelas = pagamento.Parcelas > 1 ? $"{pagamento.FormaPagamentoDescricao} ({pagamento.Parcelas}x)" : pagamento.FormaPagamentoDescricao;
            documento.Blocks.Add(LinhaValor(descricaoParcelas, pagamento.Valor));
        }

        documento.Blocks.Add(new Paragraph(new Run(separador)) { Margin = new Thickness(0, 6, 0, 0) });
        documento.Blocks.Add(new Paragraph(new Run("Obrigado pela preferência!")) { TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 6, 0, 0) });

        return documento;
    }

    private static Paragraph LinhaValor(string rotulo, decimal valor)
    {
        var texto = Truncar(rotulo, LarguraDescricao).PadRight(LarguraDescricao) + valor.ToString("0.00").PadLeft(LarguraValor);
        return new Paragraph(new Run(texto)) { Margin = new Thickness(0) };
    }

    private static string Truncar(string texto, int tamanho) => texto.Length <= tamanho ? texto : texto[..tamanho];
}
