using System.Collections.ObjectModel;
using System.Windows.Input;
using GestorPDV.Application.Caixa;
using GestorPDV.Application.Seguranca;
using GestorPDV.Domain.Caixa;
using GestorPDV.Wpf.Helpers;
using GestorPDV.Wpf.ViewModels.Cadastros;

namespace GestorPDV.Wpf.ViewModels.Caixa;

// RN-CAI-001: abertura, movimentação, sangria/suprimento e fechamento.
public class CaixaViewModel : CadastroViewModelBase
{
    private readonly ICaixaService _caixaService;
    private readonly ICaixaRepository _caixaRepository;

    public ObservableCollection<MovimentoCaixa> Movimentos { get; } = new();

    private Domain.Caixa.Caixa? _caixaAtual;
    public Domain.Caixa.Caixa? CaixaAtual
    {
        get => _caixaAtual;
        private set
        {
            if (SetField(ref _caixaAtual, value))
            {
                OnPropertyChanged(nameof(CaixaAberto));
                OnPropertyChanged(nameof(CaixaFechado));
                OnPropertyChanged(nameof(StatusTexto));
            }
        }
    }

    public bool CaixaAberto => CaixaAtual is not null;
    public bool CaixaFechado => CaixaAtual is null;
    public string StatusTexto => CaixaAtual is null ? "Fechado" : $"Aberto (nº {CaixaAtual.Id})";

    private decimal _saldoAtual;
    public decimal SaldoAtual
    {
        get => _saldoAtual;
        private set => SetField(ref _saldoAtual, value);
    }

    private decimal _valorAbertura;
    public decimal ValorAbertura
    {
        get => _valorAbertura;
        set => SetField(ref _valorAbertura, value);
    }

    private decimal _valorFechamentoInformado;
    public decimal ValorFechamentoInformado
    {
        get => _valorFechamentoInformado;
        set => SetField(ref _valorFechamentoInformado, value);
    }

    private decimal _valorSangriaSuprimento;
    public decimal ValorSangriaSuprimento
    {
        get => _valorSangriaSuprimento;
        set => SetField(ref _valorSangriaSuprimento, value);
    }

    private string? _observacaoSangriaSuprimento;
    public string? ObservacaoSangriaSuprimento
    {
        get => _observacaoSangriaSuprimento;
        set => SetField(ref _observacaoSangriaSuprimento, value);
    }

    public ICommand AbrirCaixaCommand { get; }
    public ICommand FecharCaixaCommand { get; }
    public ICommand RegistrarSangriaCommand { get; }
    public ICommand RegistrarSuprimentoCommand { get; }
    public ICommand AtualizarCommand { get; }

    public CaixaViewModel(ICaixaService caixaService, ICaixaRepository caixaRepository, SessaoUsuario sessao, ShellViewModel shell)
        : base(sessao, shell, () => shell.NavigateToHome(sessao))
    {
        _caixaService = caixaService;
        _caixaRepository = caixaRepository;

        AbrirCaixaCommand = new RelayCommand(AbrirCaixaAsync, () => !CaixaAberto);
        FecharCaixaCommand = new RelayCommand(FecharCaixaAsync, () => CaixaAberto);
        RegistrarSangriaCommand = new RelayCommand(RegistrarSangriaAsync, () => CaixaAberto);
        RegistrarSuprimentoCommand = new RelayCommand(RegistrarSuprimentoAsync, () => CaixaAberto);
        AtualizarCommand = new RelayCommand(CarregarAsync);

        _ = CarregarAsync();
    }

    private async Task CarregarAsync()
    {
        if (Sessao.FilialId is null)
        {
            Mensagem = "Seu usuário não está associado a uma filial.";
            return;
        }

        Carregando = true;
        try
        {
            CaixaAtual = await _caixaRepository.ObterAbertoAsync(Sessao.FilialId.Value);
            Movimentos.Clear();

            if (CaixaAtual is not null)
            {
                SaldoAtual = await _caixaRepository.ObterSaldoAsync(CaixaAtual.Id);
                var movimentos = await _caixaRepository.ListarMovimentosAsync(CaixaAtual.Id);
                foreach (var movimento in movimentos)
                {
                    Movimentos.Add(movimento);
                }
            }
            else
            {
                SaldoAtual = 0;
            }
        }
        finally
        {
            Carregando = false;
        }
    }

    private async Task AbrirCaixaAsync()
    {
        if (Sessao.FilialId is null)
        {
            return;
        }

        Mensagem = null;
        Carregando = true;
        try
        {
            var resultado = await _caixaService.AbrirCaixaAsync(Sessao.FilialId.Value, Sessao.UsuarioId, ValorAbertura);
            if (!resultado.Sucesso)
            {
                Mensagem = resultado.Erro;
                return;
            }

            Mensagem = $"Caixa nº {resultado.Valor!.Id} aberto com sucesso.";
            ValorAbertura = 0;
            await CarregarAsync();
        }
        finally
        {
            Carregando = false;
        }
    }

    private async Task FecharCaixaAsync()
    {
        if (CaixaAtual is null)
        {
            return;
        }

        Mensagem = null;
        Carregando = true;
        try
        {
            var resultado = await _caixaService.FecharCaixaAsync(CaixaAtual.Id, Sessao.UsuarioId, ValorFechamentoInformado);
            if (!resultado.Sucesso)
            {
                Mensagem = resultado.Erro;
                return;
            }

            var diferenca = ValorFechamentoInformado - SaldoAtual;
            Mensagem = $"Caixa fechado. Saldo calculado: {SaldoAtual:C} — informado: {ValorFechamentoInformado:C} — diferença: {diferenca:C}.";
            ValorFechamentoInformado = 0;
            await CarregarAsync();
        }
        finally
        {
            Carregando = false;
        }
    }

    private async Task RegistrarSangriaAsync()
    {
        if (CaixaAtual is null)
        {
            return;
        }

        Mensagem = null;
        var resultado = await _caixaService.RegistrarSangriaAsync(
            CaixaAtual.Id, Sessao.UsuarioId, ValorSangriaSuprimento, ObservacaoSangriaSuprimento);

        Mensagem = resultado.Sucesso ? "Sangria registrada com sucesso." : resultado.Erro;
        if (resultado.Sucesso)
        {
            ValorSangriaSuprimento = 0;
            ObservacaoSangriaSuprimento = null;
            await CarregarAsync();
        }
    }

    private async Task RegistrarSuprimentoAsync()
    {
        if (CaixaAtual is null)
        {
            return;
        }

        Mensagem = null;
        var resultado = await _caixaService.RegistrarSuprimentoAsync(
            CaixaAtual.Id, Sessao.UsuarioId, ValorSangriaSuprimento, ObservacaoSangriaSuprimento);

        Mensagem = resultado.Sucesso ? "Suprimento registrado com sucesso." : resultado.Erro;
        if (resultado.Sucesso)
        {
            ValorSangriaSuprimento = 0;
            ObservacaoSangriaSuprimento = null;
            await CarregarAsync();
        }
    }
}
