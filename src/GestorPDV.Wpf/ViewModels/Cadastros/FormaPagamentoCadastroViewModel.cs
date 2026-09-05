using System.Collections.ObjectModel;
using System.Windows.Input;
using GestorPDV.Application.Cadastros;
using GestorPDV.Application.Seguranca;
using GestorPDV.Domain.Cadastros;
using GestorPDV.Wpf.Helpers;

namespace GestorPDV.Wpf.ViewModels.Cadastros;

public class FormaPagamentoCadastroViewModel : CadastroViewModelBase
{
    private readonly IFormaPagamentoRepository _repositorio;

    public ObservableCollection<FormaPagamento> Itens { get; } = new();
    public IReadOnlyList<TipoFormaPagamento> TiposDisponiveis { get; } = Enum.GetValues<TipoFormaPagamento>();

    private FormaPagamento? _selecionado;
    public FormaPagamento? Selecionado
    {
        get => _selecionado;
        set
        {
            if (SetField(ref _selecionado, value) && value is not null)
            {
                EntidadeEmEdicao = value;
            }
        }
    }

    private FormaPagamento _entidadeEmEdicao = NovaFormaPagamento();
    public FormaPagamento EntidadeEmEdicao
    {
        get => _entidadeEmEdicao;
        private set => SetField(ref _entidadeEmEdicao, value);
    }

    public ICommand CarregarCommand { get; }
    public ICommand NovoCommand { get; }
    public ICommand SalvarCommand { get; }

    public FormaPagamentoCadastroViewModel(IFormaPagamentoRepository repositorio, SessaoUsuario sessao, ShellViewModel shell)
        : base(sessao, shell)
    {
        _repositorio = repositorio;
        CarregarCommand = new RelayCommand(CarregarAsync);
        NovoCommand = new RelayCommand(Novo);
        SalvarCommand = new RelayCommand(SalvarAsync);
        _ = CarregarAsync();
    }

    private void Novo() => EntidadeEmEdicao = NovaFormaPagamento();

    private static FormaPagamento NovaFormaPagamento() => new()
    {
        Ativo = true,
        GeraFinanceiro = true,
        MovimentaCaixa = true
    };

    private async Task CarregarAsync()
    {
        Carregando = true;
        try
        {
            var formas = await _repositorio.ListarAsync();
            Itens.Clear();
            foreach (var forma in formas)
            {
                Itens.Add(forma);
            }
        }
        finally
        {
            Carregando = false;
        }
    }

    private async Task SalvarAsync()
    {
        Mensagem = null;

        if (string.IsNullOrWhiteSpace(EntidadeEmEdicao.Codigo) || string.IsNullOrWhiteSpace(EntidadeEmEdicao.Descricao))
        {
            Mensagem = "Informe ao menos código e descrição.";
            return;
        }

        Carregando = true;
        try
        {
            if (EntidadeEmEdicao.Id == 0)
            {
                EntidadeEmEdicao.Id = await _repositorio.InserirAsync(EntidadeEmEdicao);
            }
            else
            {
                await _repositorio.AtualizarAsync(EntidadeEmEdicao);
            }

            Mensagem = "Forma de pagamento salva com sucesso.";
            await CarregarAsync();
            Novo();
        }
        catch (Exception ex)
        {
            Mensagem = $"Erro ao salvar forma de pagamento: {ex.Message}";
        }
        finally
        {
            Carregando = false;
        }
    }
}
