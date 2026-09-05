using GestorPDV.Application.Common;
using GestorPDV.Domain.Vendas;

namespace GestorPDV.Application.Vendas;

// Fluxo de venda (RN-VEN-*, RN-EST-002/003, RN-COM-001, RN-CAN-001). A
// implementação (GestorPDV.Vendas.VendaService) orquestra cálculo,
// resolução de preço, persistência, baixa de estoque e comissão dentro de
// uma única transação ao finalizar.
public interface IVendaService
{
    Venda IniciarVenda(long filialId, long vendedorId, long usuarioAberturaId, long? clienteId, long? tabelaPrecoId);

    Task<Result> AdicionarItemProdutoAsync(
        Venda venda,
        long produtoId,
        decimal quantidade,
        decimal descontoPct,
        bool usuarioPodeAutorizarDesconto,
        CancellationToken cancellationToken = default);

    Task<Result> AdicionarItemServicoAsync(
        Venda venda,
        long servicoId,
        decimal quantidade,
        decimal descontoPct,
        CancellationToken cancellationToken = default);

    void RemoverItem(Venda venda, int itemNumero);

    // RN-PAG-001: uma venda pode ter múltiplas formas de pagamento; a soma
    // dos valores deve fechar com venda.Total. Formas que "geram
    // financeiro" (crediário/boleto) exigem venda.ClienteId e podem gerar
    // mais de uma parcela (VendaPagamento.Parcelas).
    Task<Result<long>> FinalizarVendaAsync(
        Venda venda, IReadOnlyList<VendaPagamento> pagamentos, CancellationToken cancellationToken = default);

    Task<Result> CancelarVendaAsync(
        long vendaId, long usuarioId, string motivo, CancellationToken cancellationToken = default);
}
