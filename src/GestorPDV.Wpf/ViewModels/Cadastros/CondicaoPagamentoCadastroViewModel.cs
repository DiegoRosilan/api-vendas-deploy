using System.Collections.ObjectModel;
using System.Windows.Input;
using GestorPDV.Application.Cadastros;
using GestorPDV.Application.Seguranca;
using GestorPDV.Domain.Cadastros;
using GestorPDV.Wpf.Helpers;

namespace GestorPDV.Wpf.ViewModels.Cadastros;

public class CondicaoPagamentoCadastroViewModel : CadastroViewModelBase
{
    private readonly ICondicaoPagamentoRepository _repositorio;

    public ObservableCollection<CondicaoPagamento> Itens { get; } = new();

    private CondicaoPagamento? _selecionado;
    public CondicaoPagamento? Selecionado
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

    private CondicaoPagamento _entidadeEmEdicao = NovaCondicaoPagamento();
    public CondicaoPagamento EntidadeEmEdicao
    {
        get => _entidadeEmEdicao;
        private set => SetField(ref _entidadeEmEdicao, value);
    }

    public ICommand CarregarCommand { get; }
    public ICommand NovoCommand { get; }
    public ICommand SalvarCommand { get; }

    public CondicaoPagamentoCadastroViewModel(
        ICondicaoPagamentoRepository repositorio, SessaoUsuario sessao, ShellViewModel shell)
        : base(sessao, shell)
    {
        _repositorio = repositorio;
        CarregarCommand = new RelayCommand(CarregarAsync);
        NovoCommand = new RelayCommand(Novo);
        SalvarCommand = new RelayCommand(SalvarAsync);
        _ = CarregarAsync();
    }

    private void Novo() => EntidadeEmEdicao = NovaCondicaoPagamento();

    private static CondicaoPagamento NovaCondicaoPagamento() => new() { Ativo = true, NumeroParcelas = 1, IntervaloDias = 30 };

    private async Task CarregarAsync()
    {
        Carregando = true;
        try
        {
            var condicoes = await _repositorio.ListarAsync();
            Itens.Clear();
            foreach (var condicao in condicoes)
            {
                Itens.Add(condicao);
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

        if (string.IsNullOrWhiteSpace(EntidadeEmEdicao.Descricao))
        {
            Mensagem = "Informe a descrição da condição de pagamento.";
            return;
        }

        if (EntidadeEmEdicao.NumeroParcelas < 1)
        {
            Mensagem = "O número de parcelas deve ser pelo menos 1.";
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

            Mensagem = "Condição de pagamento salva com sucesso.";
            await CarregarAsync();
            Novo();
        }
        catch (Exception ex)
        {
            Mensagem = $"Erro ao salvar condição de pagamento: {ex.Message}";
        }
        finally
        {
            Carregando = false;
        }
    }
}
