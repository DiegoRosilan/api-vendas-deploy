using System.Collections.ObjectModel;
using System.Windows.Input;
using GestorPDV.Application.Cadastros;
using GestorPDV.Application.Caixa;
using GestorPDV.Application.Seguranca;
using GestorPDV.Application.Vendas;
using GestorPDV.Domain.Cadastros;
using GestorPDV.Domain.Vendas;
using GestorPDV.Wpf.Helpers;
using GestorPDV.Wpf.Impressao;
using GestorPDV.Wpf.ViewModels.Cadastros;

namespace GestorPDV.Wpf.ViewModels.Vendas;

public class ItemCarrinhoExibicao
{
    public int ItemNumero { get; init; }
    public string Descricao { get; init; } = string.Empty;
    public decimal Quantidade { get; init; }
    public decimal ValorUnitario { get; init; }
    public decimal Desconto { get; init; }
    public decimal Total { get; init; }
}

public class PagamentoExibicao
{
    public string FormaPagamentoDescricao { get; init; } = string.Empty;
    public decimal Valor { get; init; }
    public int Parcelas { get; init; }
}

// Retrato de uma venda finalizada, capturado antes de IniciarNovaVenda()
// limpar o carrinho, para permitir (re)imprimir o cupom depois.
public record CupomPendente(
    long Numero,
    DateTimeOffset Data,
    string ClienteNome,
    IReadOnlyList<ItemCarrinhoExibicao> Itens,
    IReadOnlyList<PagamentoExibicao> Pagamentos,
    decimal Subtotal,
    decimal Desconto,
    decimal Total);

// Tela de venda (PDV): monta o carrinho e os pagamentos em memória via
// IVendaService e só grava no banco ao finalizar. RN-PAG-001: a venda pode
// ter mais de uma forma de pagamento, desde que a soma feche com o total.
public class VendaViewModel : CadastroViewModelBase
{
    private readonly IVendaService _vendaService;
    private readonly IVendaRepository _vendaRepository;
    private readonly ICaixaRepository _caixaRepository;
    private readonly IProdutoRepository _produtoRepository;
    private readonly IServicoRepository _servicoRepository;
    private readonly IClienteRepository _clienteRepository;
    private readonly IFormaPagamentoRepository _formaPagamentoRepository;
    private readonly IFuncionarioRepository _funcionarioRepository;
    private readonly IFilialRepository _filialRepository;
    private readonly Dictionary<VendaProduto, string> _descricoesPorItem = new();
    private readonly List<VendaPagamento> _pagamentosPendentes = new();
    private Filial? _filial;
    private CupomPendente? _cupomPendente;

    public ObservableCollection<Produto> ResultadosBusca { get; } = new();
    public ObservableCollection<Servico> Servicos { get; } = new();
    public ObservableCollection<Cliente> Clientes { get; } = new();
    public ObservableCollection<FormaPagamento> FormasPagamento { get; } = new();
    public ObservableCollection<ItemCarrinhoExibicao> Itens { get; } = new();
    public ObservableCollection<PagamentoExibicao> Pagamentos { get; } = new();
    public ObservableCollection<Venda> VendasDeHoje { get; } = new();

    private Venda? _venda;
    public Venda? Venda
    {
        get => _venda;
        private set => SetField(ref _venda, value);
    }

    private bool _caixaAberto;
    public bool CaixaAberto
    {
        get => _caixaAberto;
        private set => SetField(ref _caixaAberto, value);
    }

    private string? _termoBusca;
    public string? TermoBusca
    {
        get => _termoBusca;
        set => SetField(ref _termoBusca, value);
    }

    private Produto? _produtoSelecionado;
    public Produto? ProdutoSelecionado
    {
        get => _produtoSelecionado;
        set => SetField(ref _produtoSelecionado, value);
    }

    private decimal _quantidade = 1;
    public decimal Quantidade
    {
        get => _quantidade;
        set => SetField(ref _quantidade, value);
    }

    private decimal _descontoPct;
    public decimal DescontoPct
    {
        get => _descontoPct;
        set => SetField(ref _descontoPct, value);
    }

    private Servico? _servicoSelecionado;
    public Servico? ServicoSelecionado
    {
        get => _servicoSelecionado;
        set => SetField(ref _servicoSelecionado, value);
    }

    private Cliente? _clienteSelecionado;
    public Cliente? ClienteSelecionado
    {
        get => _clienteSelecionado;
        set
        {
            if (SetField(ref _clienteSelecionado, value) && Venda is not null)
            {
                Venda.ClienteId = value?.Id;
                Venda.TabelaPrecoId = value?.TabelaPrecoId;
            }
        }
    }

    private FormaPagamento? _formaPagamentoParaAdicionar;
    public FormaPagamento? FormaPagamentoParaAdicionar
    {
        get => _formaPagamentoParaAdicionar;
        set => SetField(ref _formaPagamentoParaAdicionar, value);
    }

    private decimal _valorPagamentoParaAdicionar;
    public decimal ValorPagamentoParaAdicionar
    {
        get => _valorPagamentoParaAdicionar;
        set => SetField(ref _valorPagamentoParaAdicionar, value);
    }

    private int _parcelasPagamentoParaAdicionar = 1;
    public int ParcelasPagamentoParaAdicionar
    {
        get => _parcelasPagamentoParaAdicionar;
        set => SetField(ref _parcelasPagamentoParaAdicionar, value);
    }

    private ItemCarrinhoExibicao? _itemCarrinhoSelecionado;
    public ItemCarrinhoExibicao? ItemCarrinhoSelecionado
    {
        get => _itemCarrinhoSelecionado;
        set => SetField(ref _itemCarrinhoSelecionado, value);
    }

    private PagamentoExibicao? _pagamentoSelecionado;
    public PagamentoExibicao? PagamentoSelecionado
    {
        get => _pagamentoSelecionado;
        set => SetField(ref _pagamentoSelecionado, value);
    }

    private Venda? _vendaHistoricoSelecionada;
    public Venda? VendaHistoricoSelecionada
    {
        get => _vendaHistoricoSelecionada;
        set => SetField(ref _vendaHistoricoSelecionada, value);
    }

    public decimal Subtotal => Venda?.Subtotal ?? 0;
    public decimal DescontoTotal => Venda?.Desconto ?? 0;
    public decimal Total => Venda?.Total ?? 0;
    public decimal TotalPago => _pagamentosPendentes.Sum(p => p.Valor);
    public decimal RestanteAPagar => Total - TotalPago;

    // RN-SEG-001: bloqueio de ações conforme permissão.
    public bool PodeAutorizarDesconto => Sessao.TemPermissao("VENDA_AUTORIZAR_DESCONTO");
    public bool PodeCancelarVenda => Sessao.TemPermissao("VENDA_CANCELAR");

    public ICommand BuscarProdutoCommand { get; }
    public ICommand AdicionarProdutoCommand { get; }
    public ICommand AdicionarServicoCommand { get; }
    public ICommand RemoverItemCommand { get; }
    public ICommand AdicionarPagamentoCommand { get; }
    public ICommand RemoverPagamentoCommand { get; }
    public ICommand FinalizarCommand { get; }
    public ICommand CancelarVendaSelecionadaCommand { get; }
    public ICommand ReimprimirCupomCommand { get; }

    public VendaViewModel(
        IVendaService vendaService,
        IVendaRepository vendaRepository,
        ICaixaRepository caixaRepository,
        IProdutoRepository produtoRepository,
        IServicoRepository servicoRepository,
        IClienteRepository clienteRepository,
        IFormaPagamentoRepository formaPagamentoRepository,
        IFuncionarioRepository funcionarioRepository,
        IFilialRepository filialRepository,
        SessaoUsuario sessao,
        ShellViewModel shell)
        : base(sessao, shell, () => shell.NavigateToHome(sessao))
    {
        _vendaService = vendaService;
        _vendaRepository = vendaRepository;
        _caixaRepository = caixaRepository;
        _produtoRepository = produtoRepository;
        _servicoRepository = servicoRepository;
        _clienteRepository = clienteRepository;
        _formaPagamentoRepository = formaPagamentoRepository;
        _funcionarioRepository = funcionarioRepository;
        _filialRepository = filialRepository;

        BuscarProdutoCommand = new RelayCommand(BuscarProdutoAsync);
        AdicionarProdutoCommand = new RelayCommand(AdicionarProdutoAsync, () => ProdutoSelecionado is not null && Venda is not null);
        AdicionarServicoCommand = new RelayCommand(AdicionarServicoAsync, () => ServicoSelecionado is not null && Venda is not null);
        RemoverItemCommand = new RelayCommand(RemoverItemSelecionado, () => ItemCarrinhoSelecionado is not null);
        AdicionarPagamentoCommand = new RelayCommand(AdicionarPagamento, () => FormaPagamentoParaAdicionar is not null);
        RemoverPagamentoCommand = new RelayCommand(RemoverPagamentoSelecionado, () => PagamentoSelecionado is not null);
        FinalizarCommand = new RelayCommand(
            FinalizarAsync, () => Venda is { Itens.Count: > 0 } && _pagamentosPendentes.Count > 0 && RestanteAPagar == 0);
        CancelarVendaSelecionadaCommand = new RelayCommand(CancelarVendaSelecionadaAsync, () => VendaHistoricoSelecionada is not null);
        ReimprimirCupomCommand = new RelayCommand(() => Mensagem = ImprimirCupom(_cupomPendente), () => _cupomPendente is not null);

        _ = InicializarAsync();
    }

    private async Task InicializarAsync()
    {
        Carregando = true;
        try
        {
            if (Sessao.FilialId is null)
            {
                Mensagem = "Seu usuário não está associado a uma filial. Peça para o administrador configurar em Cadastros.";
                return;
            }

            var funcionarios = await _funcionarioRepository.ListarAsync(null);
            var funcionarioVendedor = funcionarios.FirstOrDefault(f => f.UsuarioId == Sessao.UsuarioId);
            if (funcionarioVendedor is null)
            {
                Mensagem = "Seu usuário não está associado a um funcionário vendedor. " +
                            "Peça para o administrador vincular em Cadastros > Funcionários.";
                return;
            }

            Venda = _vendaService.IniciarVenda(Sessao.FilialId.Value, funcionarioVendedor.Id, Sessao.UsuarioId, null, null);
            _filial = await _filialRepository.ObterPorIdAsync(Sessao.FilialId.Value);

            var caixaAberto = await _caixaRepository.ObterAbertoAsync(Sessao.FilialId.Value);
            CaixaAberto = caixaAberto is not null;
            if (!CaixaAberto)
            {
                Mensagem = "Não há caixa aberto para esta filial. Abra o caixa (menu Caixa) antes de vender.";
            }

            var clientes = await _clienteRepository.ListarAsync(null);
            Clientes.Clear();
            foreach (var cliente in clientes)
            {
                Clientes.Add(cliente);
            }

            var formasPagamento = await _formaPagamentoRepository.ListarAsync();
            FormasPagamento.Clear();
            foreach (var forma in formasPagamento.Where(f => f.Ativo))
            {
                FormasPagamento.Add(forma);
            }

            var servicos = await _servicoRepository.ListarAsync(null);
            Servicos.Clear();
            foreach (var servico in servicos.Where(s => s.Ativo))
            {
                Servicos.Add(servico);
            }

            await CarregarVendasDeHojeAsync();
        }
        finally
        {
            Carregando = false;
        }
    }

    private async Task BuscarProdutoAsync()
    {
        if (string.IsNullOrWhiteSpace(TermoBusca))
        {
            return;
        }

        Carregando = true;
        try
        {
            var produtos = await _produtoRepository.ListarAsync(TermoBusca);
            ResultadosBusca.Clear();
            foreach (var produto in produtos)
            {
                ResultadosBusca.Add(produto);
            }

            // Conveniência de PDV: leitura de código de barras costuma
            // retornar exatamente um produto — já seleciona automaticamente.
            if (ResultadosBusca.Count == 1)
            {
                ProdutoSelecionado = ResultadosBusca[0];
            }
        }
        finally
        {
            Carregando = false;
        }
    }

    private async Task AdicionarProdutoAsync()
    {
        if (Venda is null || ProdutoSelecionado is null)
        {
            return;
        }

        Mensagem = null;
        var resultado = await _vendaService.AdicionarItemProdutoAsync(
            Venda, ProdutoSelecionado.Id, Quantidade, DescontoPct, PodeAutorizarDesconto);

        if (!resultado.Sucesso)
        {
            Mensagem = resultado.Erro;
            return;
        }

        _descricoesPorItem[Venda.Itens[^1]] = ProdutoSelecionado.Descricao;
        AtualizarItens();
        Quantidade = 1;
        DescontoPct = 0;
    }

    private async Task AdicionarServicoAsync()
    {
        if (Venda is null || ServicoSelecionado is null)
        {
            return;
        }

        Mensagem = null;
        var resultado = await _vendaService.AdicionarItemServicoAsync(Venda, ServicoSelecionado.Id, Quantidade, DescontoPct);

        if (!resultado.Sucesso)
        {
            Mensagem = resultado.Erro;
            return;
        }

        _descricoesPorItem[Venda.Itens[^1]] = ServicoSelecionado.Descricao;
        AtualizarItens();
        Quantidade = 1;
        DescontoPct = 0;
    }

    private void RemoverItemSelecionado()
    {
        if (Venda is null || ItemCarrinhoSelecionado is null)
        {
            return;
        }

        var itemOriginal = Venda.Itens.FirstOrDefault(item => item.ItemNumero == ItemCarrinhoSelecionado.ItemNumero);
        _vendaService.RemoverItem(Venda, ItemCarrinhoSelecionado.ItemNumero);

        if (itemOriginal is not null)
        {
            _descricoesPorItem.Remove(itemOriginal);
        }

        ItemCarrinhoSelecionado = null;
        AtualizarItens();
    }

    private void AdicionarPagamento()
    {
        if (FormaPagamentoParaAdicionar is null)
        {
            return;
        }

        var valor = ValorPagamentoParaAdicionar > 0 ? ValorPagamentoParaAdicionar : RestanteAPagar;
        if (valor <= 0)
        {
            Mensagem = "Não há valor restante para pagar.";
            return;
        }

        _pagamentosPendentes.Add(new VendaPagamento
        {
            FormaPagamentoId = FormaPagamentoParaAdicionar.Id,
            Valor = valor,
            Parcelas = Math.Max(ParcelasPagamentoParaAdicionar, 1)
        });

        Pagamentos.Add(new PagamentoExibicao
        {
            FormaPagamentoDescricao = FormaPagamentoParaAdicionar.Descricao,
            Valor = valor,
            Parcelas = Math.Max(ParcelasPagamentoParaAdicionar, 1)
        });

        ValorPagamentoParaAdicionar = 0;
        ParcelasPagamentoParaAdicionar = 1;
        NotificarTotaisPagamento();
    }

    private void RemoverPagamentoSelecionado()
    {
        if (PagamentoSelecionado is null)
        {
            return;
        }

        var indice = Pagamentos.IndexOf(PagamentoSelecionado);
        if (indice >= 0)
        {
            Pagamentos.RemoveAt(indice);
            _pagamentosPendentes.RemoveAt(indice);
        }

        PagamentoSelecionado = null;
        NotificarTotaisPagamento();
    }

    private async Task FinalizarAsync()
    {
        if (Venda is null || _pagamentosPendentes.Count == 0)
        {
            return;
        }

        Mensagem = null;
        Carregando = true;
        try
        {
            var resultado = await _vendaService.FinalizarVendaAsync(Venda, _pagamentosPendentes);

            if (!resultado.Sucesso)
            {
                Mensagem = resultado.Erro;
                return;
            }

            var mensagemVenda = $"Venda nº {Venda.Numero} finalizada com sucesso — total {Venda.Total:C}.";

            _cupomPendente = new CupomPendente(
                Venda.Numero, Venda.DataVenda, ClienteSelecionado?.Pessoa?.Nome ?? "Consumidor final",
                Itens.ToList(), Pagamentos.ToList(), Venda.Subtotal, Venda.Desconto, Venda.Total);

            await CarregarVendasDeHojeAsync();
            IniciarNovaVenda();
            Mensagem = $"{mensagemVenda} {ImprimirCupom(_cupomPendente)}";
        }
        finally
        {
            Carregando = false;
        }
    }

    // A venda já foi gravada nesse ponto; uma falha ao imprimir (sem
    // impressora, impressora offline etc.) não desfaz a venda — só devolve
    // a mensagem de erro, deixando o operador tentar de novo com
    // ReimprimirCupomCommand. Retorna uma mensagem de status (sucesso ou
    // erro), nunca null, para o chamador sempre ter algo para mostrar.
    private string ImprimirCupom(CupomPendente? cupom)
    {
        if (cupom is null)
        {
            return "Nenhuma venda para reimprimir o cupom.";
        }

        var nomeFilial = _filial?.NomeFantasia ?? _filial?.RazaoSocial ?? "GestorPDV";
        var documento = CupomBuilder.Montar(
            nomeFilial, cupom.Numero, cupom.Data, cupom.ClienteNome, cupom.Itens, cupom.Pagamentos,
            cupom.Subtotal, cupom.Desconto, cupom.Total);

        var resultado = ImpressoraHelper.Imprimir(documento, $"Cupom venda nº {cupom.Numero}");
        return resultado.Sucesso ? "Cupom enviado para impressão." : resultado.Erro!;
    }

    private void IniciarNovaVenda()
    {
        if (Sessao.FilialId is null || Venda is null)
        {
            return;
        }

        Venda = _vendaService.IniciarVenda(
            Sessao.FilialId.Value, Venda.VendedorId, Sessao.UsuarioId, ClienteSelecionado?.Id, ClienteSelecionado?.TabelaPrecoId);
        _descricoesPorItem.Clear();
        _pagamentosPendentes.Clear();
        Pagamentos.Clear();
        ResultadosBusca.Clear();
        ProdutoSelecionado = null;
        ServicoSelecionado = null;
        AtualizarItens();
    }

    private async Task CancelarVendaSelecionadaAsync()
    {
        if (VendaHistoricoSelecionada is null)
        {
            return;
        }

        Mensagem = null;
        Carregando = true;
        try
        {
            var resultado = await _vendaService.CancelarVendaAsync(
                VendaHistoricoSelecionada.Id, Sessao.UsuarioId, "Cancelado pelo operador na tela de vendas.");

            Mensagem = resultado.Sucesso ? "Venda cancelada com sucesso." : resultado.Erro;
            if (resultado.Sucesso)
            {
                await CarregarVendasDeHojeAsync();
            }
        }
        finally
        {
            Carregando = false;
        }
    }

    private async Task CarregarVendasDeHojeAsync()
    {
        if (Sessao.FilialId is null)
        {
            return;
        }

        var vendas = await _vendaRepository.ListarPorFilialEDataAsync(Sessao.FilialId.Value, DateOnly.FromDateTime(DateTime.Now));
        VendasDeHoje.Clear();
        foreach (var venda in vendas)
        {
            VendasDeHoje.Add(venda);
        }
    }

    private void AtualizarItens()
    {
        Itens.Clear();
        if (Venda is not null)
        {
            foreach (var item in Venda.Itens)
            {
                Itens.Add(new ItemCarrinhoExibicao
                {
                    ItemNumero = item.ItemNumero,
                    Descricao = _descricoesPorItem.GetValueOrDefault(item, "(sem descrição)"),
                    Quantidade = item.Quantidade,
                    ValorUnitario = item.ValorUnitarioFinal,
                    Desconto = item.Desconto,
                    Total = item.Total
                });
            }
        }

        NotificarTotaisPagamento();
    }

    private void NotificarTotaisPagamento()
    {
        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(DescontoTotal));
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(TotalPago));
        OnPropertyChanged(nameof(RestanteAPagar));
    }
}
