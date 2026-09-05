using System.Collections.ObjectModel;
using System.Windows.Input;
using GestorPDV.Application.Cadastros;
using GestorPDV.Application.Seguranca;
using GestorPDV.Application.Vendas;
using GestorPDV.Domain.Cadastros;
using GestorPDV.Domain.Vendas;
using GestorPDV.Wpf.Helpers;
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

// Tela de venda (PDV): monta o carrinho em memória via IVendaService e só
// grava no banco ao finalizar. RemoverItem/AdicionarItem recalculam os
// totais a cada mudança (RN-VEN-002/003/004).
public class VendaViewModel : CadastroViewModelBase
{
    private readonly IVendaService _vendaService;
    private readonly IVendaRepository _vendaRepository;
    private readonly IProdutoRepository _produtoRepository;
    private readonly IServicoRepository _servicoRepository;
    private readonly IClienteRepository _clienteRepository;
    private readonly IFormaPagamentoRepository _formaPagamentoRepository;
    private readonly IFuncionarioRepository _funcionarioRepository;
    private readonly Dictionary<VendaProduto, string> _descricoesPorItem = new();

    public ObservableCollection<Produto> ResultadosBusca { get; } = new();
    public ObservableCollection<Servico> Servicos { get; } = new();
    public ObservableCollection<Cliente> Clientes { get; } = new();
    public ObservableCollection<FormaPagamento> FormasPagamento { get; } = new();
    public ObservableCollection<ItemCarrinhoExibicao> Itens { get; } = new();
    public ObservableCollection<Venda> VendasDeHoje { get; } = new();

    private Venda? _venda;
    public Venda? Venda
    {
        get => _venda;
        private set => SetField(ref _venda, value);
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

    private FormaPagamento? _formaPagamentoSelecionada;
    public FormaPagamento? FormaPagamentoSelecionada
    {
        get => _formaPagamentoSelecionada;
        set => SetField(ref _formaPagamentoSelecionada, value);
    }

    private ItemCarrinhoExibicao? _itemCarrinhoSelecionado;
    public ItemCarrinhoExibicao? ItemCarrinhoSelecionado
    {
        get => _itemCarrinhoSelecionado;
        set => SetField(ref _itemCarrinhoSelecionado, value);
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

    // RN-SEG-001: bloqueio de ações conforme permissão.
    public bool PodeAutorizarDesconto => Sessao.TemPermissao("VENDA_AUTORIZAR_DESCONTO");
    public bool PodeCancelarVenda => Sessao.TemPermissao("VENDA_CANCELAR");

    public ICommand BuscarProdutoCommand { get; }
    public ICommand AdicionarProdutoCommand { get; }
    public ICommand AdicionarServicoCommand { get; }
    public ICommand RemoverItemCommand { get; }
    public ICommand FinalizarCommand { get; }
    public ICommand CancelarVendaSelecionadaCommand { get; }

    public VendaViewModel(
        IVendaService vendaService,
        IVendaRepository vendaRepository,
        IProdutoRepository produtoRepository,
        IServicoRepository servicoRepository,
        IClienteRepository clienteRepository,
        IFormaPagamentoRepository formaPagamentoRepository,
        IFuncionarioRepository funcionarioRepository,
        SessaoUsuario sessao,
        ShellViewModel shell)
        : base(sessao, shell, () => shell.NavigateToHome(sessao))
    {
        _vendaService = vendaService;
        _vendaRepository = vendaRepository;
        _produtoRepository = produtoRepository;
        _servicoRepository = servicoRepository;
        _clienteRepository = clienteRepository;
        _formaPagamentoRepository = formaPagamentoRepository;
        _funcionarioRepository = funcionarioRepository;

        BuscarProdutoCommand = new RelayCommand(BuscarProdutoAsync);
        AdicionarProdutoCommand = new RelayCommand(AdicionarProdutoAsync, () => ProdutoSelecionado is not null && Venda is not null);
        AdicionarServicoCommand = new RelayCommand(AdicionarServicoAsync, () => ServicoSelecionado is not null && Venda is not null);
        RemoverItemCommand = new RelayCommand(RemoverItemSelecionado, () => ItemCarrinhoSelecionado is not null);
        FinalizarCommand = new RelayCommand(FinalizarAsync, () => Venda is { Itens.Count: > 0 } && FormaPagamentoSelecionada is not null);
        CancelarVendaSelecionadaCommand = new RelayCommand(CancelarVendaSelecionadaAsync, () => VendaHistoricoSelecionada is not null);

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

    private async Task FinalizarAsync()
    {
        if (Venda is null || FormaPagamentoSelecionada is null)
        {
            return;
        }

        Mensagem = null;
        Carregando = true;
        try
        {
            var resultado = await _vendaService.FinalizarVendaAsync(Venda, FormaPagamentoSelecionada.Id);

            if (!resultado.Sucesso)
            {
                Mensagem = resultado.Erro;
                return;
            }

            Mensagem = $"Venda nº {Venda.Numero} finalizada com sucesso — total {Venda.Total:C}.";
            await CarregarVendasDeHojeAsync();
            IniciarNovaVenda();
        }
        finally
        {
            Carregando = false;
        }
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

        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(DescontoTotal));
        OnPropertyChanged(nameof(Total));
    }
}
