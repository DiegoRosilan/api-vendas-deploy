using System.Collections.ObjectModel;
using System.Windows.Input;
using GestorPDV.Application.Cadastros;
using GestorPDV.Application.Financeiro;
using GestorPDV.Application.Seguranca;
using GestorPDV.Domain.Cadastros;
using GestorPDV.Domain.Financeiro;
using GestorPDV.Wpf.Helpers;
using GestorPDV.Wpf.ViewModels.Cadastros;

namespace GestorPDV.Wpf.ViewModels.Financeiro;

public class ParcelaExibicao
{
    public long ParcelaId { get; init; }
    public long DocumentoId { get; init; }
    public string ClienteNome { get; init; } = string.Empty;
    public string NumeroDocumento { get; init; } = string.Empty;
    public int NumeroParcela { get; init; }
    public DateOnly Vencimento { get; init; }
    public decimal Valor { get; init; }
    public int DiasAtraso { get; init; }
    public decimal Juros { get; init; }
    public decimal Multa { get; init; }
    public decimal ValorAtualizado => Valor + Juros + Multa;
    public string Situacao { get; init; } = string.Empty;
}

// Tela de contas a receber (RN-FIN-001/002/003): lista as parcelas em
// aberto/parcial da filial do usuário, com juros e multa por atraso já
// calculados na data de hoje, e permite dar baixa individual de uma
// parcela. Renegociação e contas a pagar ficam para uma fase futura — ver
// docs/ROADMAP.md.
public class FinanceiroViewModel : CadastroViewModelBase
{
    private readonly IFinanceiroService _financeiroService;
    private readonly IFinanceiroRepository _financeiroRepository;
    private readonly IClienteRepository _clienteRepository;
    private readonly IFormaPagamentoRepository _formaPagamentoRepository;

    public ObservableCollection<ParcelaExibicao> Parcelas { get; } = new();
    public ObservableCollection<FormaPagamento> FormasPagamento { get; } = new();

    private ParcelaExibicao? _parcelaSelecionada;
    public ParcelaExibicao? ParcelaSelecionada
    {
        get => _parcelaSelecionada;
        set => SetField(ref _parcelaSelecionada, value);
    }

    private FormaPagamento? _formaPagamentoSelecionada;
    public FormaPagamento? FormaPagamentoSelecionada
    {
        get => _formaPagamentoSelecionada;
        set => SetField(ref _formaPagamentoSelecionada, value);
    }

    public ICommand AtualizarCommand { get; }
    public ICommand BaixarParcelaCommand { get; }

    public FinanceiroViewModel(
        IFinanceiroService financeiroService,
        IFinanceiroRepository financeiroRepository,
        IClienteRepository clienteRepository,
        IFormaPagamentoRepository formaPagamentoRepository,
        SessaoUsuario sessao,
        ShellViewModel shell)
        : base(sessao, shell, () => shell.NavigateToHome(sessao))
    {
        _financeiroService = financeiroService;
        _financeiroRepository = financeiroRepository;
        _clienteRepository = clienteRepository;
        _formaPagamentoRepository = formaPagamentoRepository;

        AtualizarCommand = new RelayCommand(CarregarAsync);
        BaixarParcelaCommand = new RelayCommand(
            BaixarParcelaAsync, () => ParcelaSelecionada is not null && FormaPagamentoSelecionada is not null);

        _ = CarregarAsync();
    }

    private async Task CarregarAsync()
    {
        if (Sessao.FilialId is null)
        {
            Mensagem = "Seu usuário não está associado a uma filial.";
            return;
        }

        Mensagem = null;
        Carregando = true;
        try
        {
            var formas = await _formaPagamentoRepository.ListarAsync();
            FormasPagamento.Clear();
            foreach (var forma in formas.Where(f => f.Ativo))
            {
                FormasPagamento.Add(forma);
            }

            var documentos = await _financeiroRepository.ListarEmAbertoAsync(Sessao.FilialId.Value);
            var hoje = DateOnly.FromDateTime(DateTime.Now);
            var nomesClientes = new Dictionary<long, string>();

            Parcelas.Clear();
            foreach (var documento in documentos)
            {
                if (!nomesClientes.TryGetValue(documento.PessoaId, out var nomeCliente))
                {
                    var cliente = await _clienteRepository.ObterPorIdAsync(documento.PessoaId);
                    nomeCliente = cliente?.Pessoa?.Nome ?? $"Cliente #{documento.PessoaId}";
                    nomesClientes[documento.PessoaId] = nomeCliente;
                }

                foreach (var parcela in documento.Parcelas.Where(
                             p => p.Situacao is SituacaoParcela.Aberto or SituacaoParcela.Parcial))
                {
                    var (juros, multa) = _financeiroService.CalcularEncargos(parcela.Valor, parcela.Vencimento, hoje);

                    Parcelas.Add(new ParcelaExibicao
                    {
                        ParcelaId = parcela.Id,
                        DocumentoId = documento.Id,
                        ClienteNome = nomeCliente,
                        NumeroDocumento = documento.NumeroDocumento,
                        NumeroParcela = parcela.NumeroParcela,
                        Vencimento = parcela.Vencimento,
                        Valor = parcela.Valor,
                        DiasAtraso = Math.Max(0, hoje.DayNumber - parcela.Vencimento.DayNumber),
                        Juros = juros,
                        Multa = multa,
                        Situacao = parcela.Situacao == SituacaoParcela.Parcial ? "Parcial" : "Aberto"
                    });
                }
            }
        }
        finally
        {
            Carregando = false;
        }
    }

    private async Task BaixarParcelaAsync()
    {
        if (ParcelaSelecionada is null || FormaPagamentoSelecionada is null)
        {
            return;
        }

        Mensagem = null;
        Carregando = true;
        try
        {
            var resultado = await _financeiroService.BaixarParcelaAsync(
                ParcelaSelecionada.ParcelaId, Sessao.UsuarioId, FormaPagamentoSelecionada.Id,
                DateOnly.FromDateTime(DateTime.Now));

            if (!resultado.Sucesso)
            {
                Mensagem = resultado.Erro;
                return;
            }

            Mensagem = $"Parcela {ParcelaSelecionada.NumeroParcela} do documento {ParcelaSelecionada.NumeroDocumento} baixada com sucesso.";
            ParcelaSelecionada = null;
            await CarregarAsync();
        }
        finally
        {
            Carregando = false;
        }
    }
}
