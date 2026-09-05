using System.Collections.ObjectModel;
using System.Windows.Input;
using GestorPDV.Application.Cadastros;
using GestorPDV.Application.Seguranca;
using GestorPDV.Domain.Cadastros;
using GestorPDV.Wpf.Helpers;

namespace GestorPDV.Wpf.ViewModels.Cadastros;

public class FilialCadastroViewModel : CadastroViewModelBase
{
    private readonly IFilialRepository _repositorio;

    public ObservableCollection<Filial> Itens { get; } = new();

    private Filial? _selecionado;
    public Filial? Selecionado
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

    private Filial _entidadeEmEdicao = NovaFilial();
    public Filial EntidadeEmEdicao
    {
        get => _entidadeEmEdicao;
        private set => SetField(ref _entidadeEmEdicao, value);
    }

    public ICommand CarregarCommand { get; }
    public ICommand NovoCommand { get; }
    public ICommand SalvarCommand { get; }

    public FilialCadastroViewModel(IFilialRepository repositorio, SessaoUsuario sessao, ShellViewModel shell)
        : base(sessao, shell)
    {
        _repositorio = repositorio;
        CarregarCommand = new RelayCommand(CarregarAsync);
        NovoCommand = new RelayCommand(Novo);
        SalvarCommand = new RelayCommand(SalvarAsync);
        _ = CarregarAsync();
    }

    private void Novo() => EntidadeEmEdicao = NovaFilial();

    private static Filial NovaFilial() => new() { Ativo = true };

    private async Task CarregarAsync()
    {
        Carregando = true;
        try
        {
            var filiais = await _repositorio.ListarAsync();
            Itens.Clear();
            foreach (var filial in filiais)
            {
                Itens.Add(filial);
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

        if (string.IsNullOrWhiteSpace(EntidadeEmEdicao.Codigo) || string.IsNullOrWhiteSpace(EntidadeEmEdicao.RazaoSocial))
        {
            Mensagem = "Informe ao menos código e razão social.";
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

            Mensagem = "Filial salva com sucesso.";
            await CarregarAsync();
            Novo();
        }
        catch (Exception ex)
        {
            Mensagem = $"Erro ao salvar filial: {ex.Message}";
        }
        finally
        {
            Carregando = false;
        }
    }
}
