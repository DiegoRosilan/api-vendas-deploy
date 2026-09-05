using GestorPDV.Application.Common;
using GestorPDV.Application.Estoque;
using GestorPDV.Domain.Cadastros;
using GestorPDV.Domain.Estoque;

namespace GestorPDV.Estoque.Servicos;

public class EstoqueService : IEstoqueService
{
    private readonly IEstoqueRepository _estoqueRepository;

    public EstoqueService(IEstoqueRepository estoqueRepository)
    {
        _estoqueRepository = estoqueRepository;
    }

    // RN-EST-002: baixa de estoque na finalização da venda. Produtos que
    // não controlam estoque (ex.: item genérico) não geram movimentação.
    public async Task<MovimentacaoEstoque?> BaixarEstoqueAsync(
        Produto produto,
        long filialId,
        decimal quantidade,
        OrigemMovimentacaoEstoque origem,
        string documentoTipo,
        long documentoId,
        long usuarioId,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken = default)
    {
        if (!produto.ControlaEstoque)
        {
            return null;
        }

        var local = await _estoqueRepository.ObterLocalPadraoAsync(filialId, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Filial {filialId} não possui local de estoque cadastrado (est_local_estoque).");

        return await _estoqueRepository.RegistrarMovimentacaoAsync(
            produto.Id, local.Id, -quantidade, TipoMovimentacaoEstoque.Saida, origem,
            documentoTipo, documentoId, usuarioId, unitOfWork, cancellationToken);
    }

    // RN-EST-003: estorna todas as movimentações (ainda não estornadas) de
    // um documento — usado no cancelamento de venda.
    public async Task EstornarAsync(
        string documentoTipo, long documentoId, long usuarioId, IUnitOfWork unitOfWork,
        CancellationToken cancellationToken = default)
    {
        var movimentacoes = await _estoqueRepository.ListarPorDocumentoAsync(documentoTipo, documentoId, cancellationToken);

        foreach (var movimentacao in movimentacoes)
        {
            if (movimentacao.Estornado || movimentacao.Tipo == TipoMovimentacaoEstoque.Estorno)
            {
                continue;
            }

            await _estoqueRepository.EstornarMovimentacaoAsync(movimentacao.Id, usuarioId, unitOfWork, cancellationToken);
        }
    }
}
