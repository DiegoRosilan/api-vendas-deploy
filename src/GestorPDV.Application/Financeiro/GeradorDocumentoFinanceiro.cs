using GestorPDV.Domain.Financeiro;

namespace GestorPDV.Application.Financeiro;

// RN-FIN-001: monta um DocumentoFinanceiro com suas parcelas, dividindo o
// valor total igualmente (a última parcela absorve o resto do
// arredondamento). Função pura, compartilhada por FinanceiroService (conta
// a receber avulsa) e VendaService (conta a receber gerada dentro da
// mesma transação da venda).
public static class GeradorDocumentoFinanceiro
{
    public static DocumentoFinanceiro Gerar(
        long pessoaId,
        long filialId,
        long? vendaId,
        decimal valorTotal,
        int numeroParcelas,
        int intervaloDias,
        DateOnly primeiroVencimento)
    {
        var documento = new DocumentoFinanceiro
        {
            Tipo = TipoDocumentoFinanceiro.Receber,
            PessoaId = pessoaId,
            FilialId = filialId,
            NumeroDocumento = vendaId.HasValue ? $"VENDA-{vendaId}" : $"MANUAL-{DateTime.Now:yyyyMMddHHmmss}",
            ValorOriginal = valorTotal,
            DataEmissao = DateOnly.FromDateTime(DateTime.Now),
            DataVencimento = primeiroVencimento,
            Situacao = SituacaoDocumentoFinanceiro.Aberto,
            Origem = vendaId.HasValue ? OrigemDocumentoFinanceiro.Venda : OrigemDocumentoFinanceiro.Manual,
            VendaId = vendaId
        };

        var valorParcela = Math.Round(valorTotal / numeroParcelas, 2, MidpointRounding.AwayFromZero);
        var somaParcelas = 0m;
        for (var numero = 1; numero <= numeroParcelas; numero++)
        {
            var valor = numero == numeroParcelas ? valorTotal - somaParcelas : valorParcela;
            somaParcelas += valor;

            documento.Parcelas.Add(new Parcela
            {
                NumeroParcela = numero,
                Valor = valor,
                Vencimento = primeiroVencimento.AddDays(intervaloDias * (numero - 1)),
                Situacao = SituacaoParcela.Aberto
            });
        }

        return documento;
    }
}
