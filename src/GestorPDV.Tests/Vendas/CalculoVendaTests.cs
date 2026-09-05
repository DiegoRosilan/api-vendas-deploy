using GestorPDV.Domain.Cadastros;
using GestorPDV.Vendas.Calculos;
using Xunit;

namespace GestorPDV.Tests.Vendas;

public class CalculoVendaTests
{
    [Fact]
    public void CalcularItem_SemDescontoOuAcrescimo_SubtotalIgualATotal()
    {
        var resultado = CalculoVenda.CalcularItem(quantidade: 3, valorUnitario: 10m, descontoPercentual: 0, acrescimoValor: 0);

        Assert.Equal(30m, resultado.Subtotal);
        Assert.Equal(0m, resultado.DescontoValor);
        Assert.Equal(30m, resultado.Total);
        Assert.Equal(10m, resultado.ValorUnitarioFinal);
    }

    [Fact]
    public void CalcularItem_ComDescontoPercentual_AplicaSobreOSubtotal()
    {
        // RN-VEN-002/004: subtotal = 5 x 20 = 100; desconto de 10% = 10;
        // total = 100 - 10 = 90.
        var resultado = CalculoVenda.CalcularItem(quantidade: 5, valorUnitario: 20m, descontoPercentual: 10, acrescimoValor: 0);

        Assert.Equal(100m, resultado.Subtotal);
        Assert.Equal(10m, resultado.DescontoValor);
        Assert.Equal(90m, resultado.Total);
    }

    [Fact]
    public void CalcularItem_ComAcrescimoTotal_DistribuiComoAcrescimoUnitario()
    {
        // RN-VEN-003: AcréscimoUnitário = Acréscimo / Quantidade;
        // ValorUnitárioFinal = ValorUnitário + AcréscimoUnitário.
        var resultado = CalculoVenda.CalcularItem(quantidade: 4, valorUnitario: 10m, descontoPercentual: 0, acrescimoValor: 8m);

        Assert.Equal(2m, resultado.ValorUnitarioFinal - 10m);
        Assert.Equal(12m, resultado.ValorUnitarioFinal);
        Assert.Equal(48m, resultado.Total);
    }

    [Fact]
    public void CalcularSubTotal_SomaOsSubtotaisDosItens()
    {
        var subtotal = CalculoVenda.CalcularSubTotal(new[] { 30m, 90m, 48m });

        Assert.Equal(168m, subtotal);
    }

    [Fact]
    public void CalcularTotal_AplicaDescontoEAcrescimoGeraisSobreOSubtotal()
    {
        var total = CalculoVenda.CalcularTotal(subtotal: 200m, descontoGeral: 20m, acrescimoGeral: 5m);

        Assert.Equal(185m, total);
    }

    [Fact]
    public void ValidarDesconto_ComDescontoZero_SempreValido_MesmoSeProdutoBloqueiaDesconto()
    {
        var produto = new Produto { Descricao = "Produto X", BloquearDesconto = true, DescontoMaximoPct = 0 };

        var resultado = CalculoVenda.ValidarDesconto(produto, descontoPercentual: 0, usuarioPodeAutorizar: false);

        Assert.True(resultado.Sucesso);
    }

    [Fact]
    public void ValidarDesconto_ComProdutoQueBloqueiaDesconto_Falha()
    {
        var produto = new Produto { Descricao = "Produto X", BloquearDesconto = true, DescontoMaximoPct = 10 };

        var resultado = CalculoVenda.ValidarDesconto(produto, descontoPercentual: 5, usuarioPodeAutorizar: false);

        Assert.False(resultado.Sucesso);
    }

    [Fact]
    public void ValidarDesconto_DentroDoLimite_Valido()
    {
        var produto = new Produto { Descricao = "Produto X", DescontoMaximoPct = 10 };

        var resultado = CalculoVenda.ValidarDesconto(produto, descontoPercentual: 10, usuarioPodeAutorizar: false);

        Assert.True(resultado.Sucesso);
    }

    [Fact]
    public void ValidarDesconto_AcimaDoLimiteSemAutorizacao_Falha()
    {
        // RN-VEN-005: acima do limite exige autorização.
        var produto = new Produto { Descricao = "Produto X", DescontoMaximoPct = 10 };

        var resultado = CalculoVenda.ValidarDesconto(produto, descontoPercentual: 15, usuarioPodeAutorizar: false);

        Assert.False(resultado.Sucesso);
    }

    [Fact]
    public void ValidarDesconto_AcimaDoLimiteComAutorizacao_Valido()
    {
        var produto = new Produto { Descricao = "Produto X", DescontoMaximoPct = 10 };

        var resultado = CalculoVenda.ValidarDesconto(produto, descontoPercentual: 15, usuarioPodeAutorizar: true);

        Assert.True(resultado.Sucesso);
    }
}
