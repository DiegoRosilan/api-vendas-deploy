using GestorPDV.Application.Cadastros;
using GestorPDV.Application.Common;
using GestorPDV.Application.Estoque;
using GestorPDV.Application.Vendas;
using GestorPDV.Domain.Estoque;
using GestorPDV.Domain.Vendas;
using GestorPDV.Vendas.Calculos;

namespace GestorPDV.Vendas.Servicos;

public class VendaService : IVendaService
{
    private readonly IVendaRepository _vendaRepository;
    private readonly IEstoqueService _estoqueService;
    private readonly IProdutoRepository _produtoRepository;
    private readonly IServicoRepository _servicoRepository;
    private readonly ITabelaPrecoRepository _tabelaPrecoRepository;
    private readonly IFuncionarioRepository _funcionarioRepository;
    private readonly IComissaoRepository _comissaoRepository;
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public VendaService(
        IVendaRepository vendaRepository,
        IEstoqueService estoqueService,
        IProdutoRepository produtoRepository,
        IServicoRepository servicoRepository,
        ITabelaPrecoRepository tabelaPrecoRepository,
        IFuncionarioRepository funcionarioRepository,
        IComissaoRepository comissaoRepository,
        IUnitOfWorkFactory unitOfWorkFactory)
    {
        _vendaRepository = vendaRepository;
        _estoqueService = estoqueService;
        _produtoRepository = produtoRepository;
        _servicoRepository = servicoRepository;
        _tabelaPrecoRepository = tabelaPrecoRepository;
        _funcionarioRepository = funcionarioRepository;
        _comissaoRepository = comissaoRepository;
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    public Venda IniciarVenda(long filialId, long vendedorId, long usuarioAberturaId, long? clienteId, long? tabelaPrecoId) =>
        new()
        {
            FilialId = filialId,
            VendedorId = vendedorId,
            UsuarioAberturaId = usuarioAberturaId,
            ClienteId = clienteId,
            TabelaPrecoId = tabelaPrecoId,
            Tipo = TipoVenda.Venda,
            Status = StatusVenda.Aberta,
            DataVenda = DateTimeOffset.Now
        };

    public async Task<Result> AdicionarItemProdutoAsync(
        Venda venda,
        long produtoId,
        decimal quantidade,
        decimal descontoPct,
        bool usuarioPodeAutorizarDesconto,
        CancellationToken cancellationToken = default)
    {
        if (quantidade <= 0)
        {
            return Result.Falha("A quantidade deve ser maior que zero.");
        }

        var produto = await _produtoRepository.ObterPorIdAsync(produtoId, cancellationToken);
        if (produto is null || !produto.Ativo)
        {
            return Result.Falha("Produto não encontrado ou inativo.");
        }

        var validacaoDesconto = CalculoVenda.ValidarDesconto(produto, descontoPct, usuarioPodeAutorizarDesconto);
        if (!validacaoDesconto.Sucesso)
        {
            return validacaoDesconto;
        }

        var valorUnitario = await ResolverPrecoAsync(produto.Id, produto.PrecoVenda, venda.TabelaPrecoId, cancellationToken);
        var calculado = CalculoVenda.CalcularItem(quantidade, valorUnitario, descontoPct, 0);

        venda.Itens.Add(new VendaProduto
        {
            ItemNumero = venda.Itens.Count + 1,
            ProdutoId = produto.Id,
            Quantidade = quantidade,
            ValorUnitario = valorUnitario,
            ValorUnitarioFinal = calculado.ValorUnitarioFinal,
            Desconto = calculado.DescontoValor,
            Acrescimo = calculado.AcrescimoValor,
            Subtotal = calculado.Subtotal,
            Total = calculado.Total
        });

        RecalcularTotais(venda);
        return Result.Ok();
    }

    public async Task<Result> AdicionarItemServicoAsync(
        Venda venda, long servicoId, decimal quantidade, decimal descontoPct, CancellationToken cancellationToken = default)
    {
        if (quantidade <= 0)
        {
            return Result.Falha("A quantidade deve ser maior que zero.");
        }

        var servico = await _servicoRepository.ObterPorIdAsync(servicoId, cancellationToken);
        if (servico is null || !servico.Ativo)
        {
            return Result.Falha("Serviço não encontrado ou inativo.");
        }

        // Serviços não têm limite de desconto por cadastro (RN-VEN-005 é
        // uma regra de produto); aqui só aplicamos o percentual informado.
        var calculado = CalculoVenda.CalcularItem(quantidade, servico.Preco, descontoPct, 0);

        venda.Itens.Add(new VendaProduto
        {
            ItemNumero = venda.Itens.Count + 1,
            ServicoId = servico.Id,
            Quantidade = quantidade,
            ValorUnitario = servico.Preco,
            ValorUnitarioFinal = calculado.ValorUnitarioFinal,
            Desconto = calculado.DescontoValor,
            Acrescimo = calculado.AcrescimoValor,
            Subtotal = calculado.Subtotal,
            Total = calculado.Total
        });

        RecalcularTotais(venda);
        return Result.Ok();
    }

    public void RemoverItem(Venda venda, int itemNumero)
    {
        venda.Itens.RemoveAll(item => item.ItemNumero == itemNumero);

        for (var indice = 0; indice < venda.Itens.Count; indice++)
        {
            venda.Itens[indice].ItemNumero = indice + 1;
        }

        RecalcularTotais(venda);
    }

    public async Task<Result<long>> FinalizarVendaAsync(
        Venda venda, long formaPagamentoId, CancellationToken cancellationToken = default)
    {
        if (venda.Itens.Count == 0)
        {
            return Result<long>.Falha("Inclua ao menos um item antes de finalizar a venda.");
        }

        venda.Pagamentos.Clear();
        venda.Pagamentos.Add(new VendaPagamento
        {
            FormaPagamentoId = formaPagamentoId,
            Valor = venda.Total,
            Parcelas = 1,
            Status = StatusVendaPagamento.Confirmado
        });
        venda.Status = StatusVenda.Finalizada;

        await using var unitOfWork = _unitOfWorkFactory.Criar();
        await unitOfWork.BeginAsync(cancellationToken);

        try
        {
            await _vendaRepository.InserirAsync(venda, unitOfWork, cancellationToken);

            foreach (var item in venda.Itens.Where(i => i.ProdutoId.HasValue))
            {
                var produto = await _produtoRepository.ObterPorIdAsync(item.ProdutoId!.Value, cancellationToken);
                if (produto is not null)
                {
                    await _estoqueService.BaixarEstoqueAsync(
                        produto, venda.FilialId, item.Quantidade, OrigemMovimentacaoEstoque.Venda,
                        "mv_venda", venda.Id, venda.UsuarioAberturaId, unitOfWork, cancellationToken);
                }
            }

            await RegistrarComissaoAsync(venda, unitOfWork, cancellationToken);

            await unitOfWork.CommitAsync(cancellationToken);
            return Result<long>.Ok(venda.Id);
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            venda.Status = StatusVenda.Aberta;
            return Result<long>.Falha($"Erro ao finalizar a venda: {ex.Message}");
        }
    }

    public async Task<Result> CancelarVendaAsync(
        long vendaId, long usuarioId, string motivo, CancellationToken cancellationToken = default)
    {
        await using var unitOfWork = _unitOfWorkFactory.Criar();
        await unitOfWork.BeginAsync(cancellationToken);

        try
        {
            await _vendaRepository.CancelarAsync(vendaId, usuarioId, motivo, unitOfWork, cancellationToken);
            await _estoqueService.EstornarAsync("mv_venda", vendaId, usuarioId, unitOfWork, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            return Result.Falha($"Erro ao cancelar a venda: {ex.Message}");
        }
    }

    // RN-VEN-006/007/008 (simplificado): preço da tabela da venda, quando
    // existir um item para o produto, senão o preço padrão do produto.
    // Promoção (RN-VEN-007) fica para quando houver cadastro de promoções
    // — ver docs/ROADMAP.md.
    private async Task<decimal> ResolverPrecoAsync(
        long produtoId, decimal precoPadrao, long? tabelaPrecoId, CancellationToken cancellationToken)
    {
        if (!tabelaPrecoId.HasValue)
        {
            return precoPadrao;
        }

        var item = await _tabelaPrecoRepository.ObterItemAsync(tabelaPrecoId.Value, produtoId, cancellationToken);
        return item?.Preco ?? precoPadrao;
    }

    private async Task RegistrarComissaoAsync(Venda venda, IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        var funcionario = await _funcionarioRepository.ObterPorIdAsync(venda.VendedorId, cancellationToken);
        if (funcionario is null || funcionario.ComissaoPadraoPct <= 0)
        {
            return;
        }

        var comissao = new Comissao
        {
            VendaId = venda.Id,
            FuncionarioId = funcionario.Id,
            Tipo = funcionario.EhGerente ? TipoComissao.Gerente : TipoComissao.Vendedor,
            Percentual = funcionario.ComissaoPadraoPct,
            ValorBase = venda.Total,
            ValorComissao = Math.Round(venda.Total * funcionario.ComissaoPadraoPct / 100m, 2, MidpointRounding.AwayFromZero),
            DataReferencia = DateOnly.FromDateTime(DateTime.Now),
            Status = StatusComissao.Pendente
        };

        await _comissaoRepository.InserirAsync(comissao, unitOfWork, cancellationToken);
    }

    private static void RecalcularTotais(Venda venda)
    {
        venda.Subtotal = CalculoVenda.CalcularSubTotal(venda.Itens.Select(item => item.Subtotal));
        venda.Desconto = venda.Itens.Sum(item => item.Desconto);
        venda.Acrescimo = venda.Itens.Sum(item => item.Acrescimo);
        venda.Total = CalculoVenda.CalcularTotal(venda.Subtotal, venda.Desconto, venda.Acrescimo);
    }
}
