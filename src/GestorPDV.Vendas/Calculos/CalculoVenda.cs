using GestorPDV.Application.Common;
using GestorPDV.Domain.Cadastros;

namespace GestorPDV.Vendas.Calculos;

// RN-VEN-001 a RN-VEN-005: cálculo de item, subtotal, total, desconto e
// acréscimo. Métodos puros (sem I/O) para poderem ser testados sem banco —
// ver GestorPDV.Tests/Vendas/CalculoVendaTests.cs.
//
// Suposição registrada (docs/ROADMAP.md): arredondamento monetário em 2
// casas decimais, MidpointRounding.AwayFromZero, aplicado a cada etapa do
// cálculo do item.
public static class CalculoVenda
{
    private const int CasasDecimais = 2;
    private const MidpointRounding ModoArredondamento = MidpointRounding.AwayFromZero;

    // RN-VEN-002/003/004: SubtotalItem = Quantidade × ValorUnitário;
    // desconto percentual convertido em valor sobre o subtotal;
    // TotalItem = SubtotalItem − Desconto + Acréscimo; quando o acréscimo é
    // um valor total do item, AcréscimoUnitário = Acréscimo / Quantidade e
    // ValorUnitárioFinal = ValorUnitário + AcréscimoUnitário.
    public static ItemCalculado CalcularItem(
        decimal quantidade, decimal valorUnitario, decimal descontoPercentual, decimal acrescimoValor)
    {
        var subtotal = Arredondar(quantidade * valorUnitario);
        var descontoValor = Arredondar(subtotal * (descontoPercentual / 100m));
        var total = subtotal - descontoValor + acrescimoValor;
        var acrescimoUnitario = quantidade == 0 ? 0 : acrescimoValor / quantidade;
        var valorUnitarioFinal = valorUnitario + acrescimoUnitario;

        return new ItemCalculado(subtotal, descontoValor, acrescimoValor, total, valorUnitarioFinal);
    }

    public static decimal CalcularSubTotal(IEnumerable<decimal> subtotaisDosItens) =>
        Arredondar(subtotaisDosItens.Sum());

    public static decimal CalcularTotal(decimal subtotal, decimal descontoGeral, decimal acrescimoGeral) =>
        Arredondar(subtotal - descontoGeral + acrescimoGeral);

    // RN-VEN-005: limite de desconto do produto. Acima do limite, só passa
    // com a permissão de autorização (VENDA_AUTORIZAR_DESCONTO).
    public static Result ValidarDesconto(Produto produto, decimal descontoPercentual, bool usuarioPodeAutorizar)
    {
        if (descontoPercentual <= 0)
        {
            return Result.Ok();
        }

        if (produto.BloquearDesconto)
        {
            return Result.Falha($"O produto \"{produto.Descricao}\" não permite desconto.");
        }

        if (descontoPercentual > produto.DescontoMaximoPct && !usuarioPodeAutorizar)
        {
            return Result.Falha(
                $"Desconto de {descontoPercentual:N2}% acima do limite de {produto.DescontoMaximoPct:N2}% " +
                $"para \"{produto.Descricao}\" exige autorização.");
        }

        return Result.Ok();
    }

    private static decimal Arredondar(decimal valor) => Math.Round(valor, CasasDecimais, ModoArredondamento);
}
