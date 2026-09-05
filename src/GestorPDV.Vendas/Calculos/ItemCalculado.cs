namespace GestorPDV.Vendas.Calculos;

public readonly record struct ItemCalculado(
    decimal Subtotal,
    decimal DescontoValor,
    decimal AcrescimoValor,
    decimal Total,
    decimal ValorUnitarioFinal);
