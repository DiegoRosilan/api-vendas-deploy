using GestorPDV.Application.Cadastros;
using GestorPDV.Application.Caixa;
using GestorPDV.Application.Common;
using GestorPDV.Application.Estoque;
using GestorPDV.Application.Financeiro;
using GestorPDV.Application.Vendas;
using GestorPDV.Domain.Cadastros;
using GestorPDV.Domain.Caixa;
using GestorPDV.Domain.Estoque;
using GestorPDV.Domain.Vendas;
using GestorPDV.Vendas.Calculos;
// Alias necessário: mesma convenção adotada em ICaixaRepository/CaixaService
// para evitar qualquer ambiguidade com os namespaces "...Caixa".
using CaixaEntidade = GestorPDV.Domain.Caixa.Caixa;

namespace GestorPDV.Vendas.Servicos;

public class VendaService : IVendaService
{
    private readonly IVendaRepository _vendaRepository;
    private readonly IEstoqueService _estoqueService;
    private readonly ICaixaRepository _caixaRepository;
    private readonly IFinanceiroRepository _financeiroRepository;
    private readonly IFinanceiroService _financeiroService;
    private readonly IProdutoRepository _produtoRepository;
    private readonly IServicoRepository _servicoRepository;
    private readonly IClienteRepository _clienteRepository;
    private readonly IFormaPagamentoRepository _formaPagamentoRepository;
    private readonly ITabelaPrecoRepository _tabelaPrecoRepository;
    private readonly IFuncionarioRepository _funcionarioRepository;
    private readonly IComissaoRepository _comissaoRepository;
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public VendaService(
        IVendaRepository vendaRepository,
        IEstoqueService estoqueService,
        ICaixaRepository caixaRepository,
        IFinanceiroRepository financeiroRepository,
        IFinanceiroService financeiroService,
        IProdutoRepository produtoRepository,
        IServicoRepository servicoRepository,
        IClienteRepository clienteRepository,
        IFormaPagamentoRepository formaPagamentoRepository,
        ITabelaPrecoRepository tabelaPrecoRepository,
        IFuncionarioRepository funcionarioRepository,
        IComissaoRepository comissaoRepository,
        IUnitOfWorkFactory unitOfWorkFactory)
    {
        _vendaRepository = vendaRepository;
        _estoqueService = estoqueService;
        _caixaRepository = caixaRepository;
        _financeiroRepository = financeiroRepository;
        _financeiroService = financeiroService;
        _produtoRepository = produtoRepository;
        _servicoRepository = servicoRepository;
        _clienteRepository = clienteRepository;
        _formaPagamentoRepository = formaPagamentoRepository;
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
        Venda venda, IReadOnlyList<VendaPagamento> pagamentos, CancellationToken cancellationToken = default)
    {
        if (venda.Itens.Count == 0)
        {
            return Result<long>.Falha("Inclua ao menos um item antes de finalizar a venda.");
        }

        if (pagamentos.Count == 0)
        {
            return Result<long>.Falha("Informe ao menos uma forma de pagamento.");
        }

        var somaPagamentos = pagamentos.Sum(p => p.Valor);
        if (Math.Abs(somaPagamentos - venda.Total) > 0.01m)
        {
            return Result<long>.Falha(
                $"A soma dos pagamentos ({somaPagamentos:C}) não confere com o total da venda ({venda.Total:C}).");
        }

        // Resolve as formas de pagamento antes de abrir a transação, para
        // validar RN-PAG-001/RN-CLI-001 com uma mensagem clara.
        var formasPorId = new Dictionary<long, FormaPagamento>();
        foreach (var pagamento in pagamentos)
        {
            if (!formasPorId.ContainsKey(pagamento.FormaPagamentoId))
            {
                var forma = await _formaPagamentoRepository.ObterPorIdAsync(pagamento.FormaPagamentoId, cancellationToken);
                if (forma is null)
                {
                    return Result<long>.Falha("Forma de pagamento inválida.");
                }

                formasPorId[pagamento.FormaPagamentoId] = forma;
            }
        }

        var exigeCliente = pagamentos.Any(p => formasPorId[p.FormaPagamentoId].GeraFinanceiro);
        if (exigeCliente)
        {
            if (!venda.ClienteId.HasValue)
            {
                return Result<long>.Falha("Venda a prazo (crediário/boleto) exige um cliente informado.");
            }

            var cliente = await _clienteRepository.ObterPorIdAsync(venda.ClienteId.Value, cancellationToken);
            if (cliente is null)
            {
                return Result<long>.Falha("Cliente não encontrado.");
            }

            var bloqueio = await _financeiroService.VerificarBloqueioClienteAsync(
                cliente.Id, cliente.BloquearVendaDiasVencido, cancellationToken);
            if (!bloqueio.Sucesso)
            {
                return Result<long>.Falha(bloqueio.Erro!);
            }
        }

        CaixaEntidade? caixaAberto = null;
        if (pagamentos.Any(p => formasPorId[p.FormaPagamentoId].MovimentaCaixa))
        {
            caixaAberto = await _caixaRepository.ObterAbertoAsync(venda.FilialId, cancellationToken);
            if (caixaAberto is null)
            {
                return Result<long>.Falha("Não há caixa aberto para esta filial. Abra o caixa antes de finalizar a venda.");
            }
        }

        venda.Pagamentos.Clear();
        foreach (var pagamento in pagamentos)
        {
            venda.Pagamentos.Add(pagamento);
        }
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

            foreach (var pagamento in venda.Pagamentos)
            {
                var forma = formasPorId[pagamento.FormaPagamentoId];

                if (forma.MovimentaCaixa && caixaAberto is not null)
                {
                    await _caixaRepository.RegistrarMovimentoAsync(
                        caixaAberto.Id, TipoMovimentoCaixa.Venda, forma.Id, pagamento.Valor, venda.UsuarioAberturaId,
                        "mv_venda", venda.Id, null, unitOfWork, cancellationToken);
                }

                if (forma.GeraFinanceiro)
                {
                    // RN-FIN-001: uma parcela por forma de pagamento "a
                    // prazo" na venda; condição de pagamento define o
                    // intervalo entre parcelas quando o pagamento tem mais
                    // de uma (padrão de 30 dias quando não informada).
                    var documento = GeradorDocumentoFinanceiro.Gerar(
                        venda.ClienteId!.Value, venda.FilialId, venda.Id, pagamento.Valor,
                        Math.Max(pagamento.Parcelas, 1), 30, DateOnly.FromDateTime(DateTime.Now).AddDays(30));

                    await _financeiroRepository.GerarDocumentoAsync(documento, unitOfWork, cancellationToken);
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
            await _caixaRepository.EstornarMovimentosPorDocumentoAsync("mv_venda", vendaId, usuarioId, unitOfWork, cancellationToken);
            await _financeiroRepository.CancelarDocumentosPorVendaAsync(vendaId, unitOfWork, cancellationToken);
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
