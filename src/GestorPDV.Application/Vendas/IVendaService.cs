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

    Task<Result<long>> FinalizarVendaAsync(
        Venda venda, long formaPagamentoId, CancellationToken cancellationToken = default);

    Task<Result> CancelarVendaAsync(
        long vendaId, long usuarioId, string motivo, CancellationToken cancellationToken = default);
}
