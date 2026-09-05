using GestorPDV.Application.Common;
using GestorPDV.Domain.Vendas;

namespace GestorPDV.Application.Vendas;

public interface IVendaRepository
{
    // Gera o número sequencial da venda (por filial) e grava mv_venda,
    // mv_venda_produto e mv_venda_pagamento dentro da transação informada.
    // Preenche venda.Numero e venda.Id antes de retornar.
    Task InserirAsync(Venda venda, IUnitOfWork unitOfWork, CancellationToken cancellationToken = default);

    Task<Venda?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Venda>> ListarPorFilialEDataAsync(
        long filialId, DateOnly data, CancellationToken cancellationToken = default);

    // RN-CAN-001: cancelamento de venda (não é exclusão física).
    Task CancelarAsync(
        long vendaId, long usuarioId, string motivo, IUnitOfWork unitOfWork, CancellationToken cancellationToken = default);
}
