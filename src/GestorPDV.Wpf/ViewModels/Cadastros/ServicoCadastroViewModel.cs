using System.Collections.ObjectModel;
using System.Windows.Input;
using GestorPDV.Application.Cadastros;
using GestorPDV.Application.Seguranca;
using GestorPDV.Domain.Cadastros;
using GestorPDV.Wpf.Helpers;

namespace GestorPDV.Wpf.ViewModels.Cadastros;

public class ServicoCadastroViewModel : CadastroViewModelBase
{
    private readonly IServicoRepository _repositorio;

    public ObservableCollection<Servico> Itens { get; } = new();

    private Servico? _selecionado;
    public Servico? Selecionado
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

    private Servico _entidadeEmEdicao = NovoServico();
    public Servico EntidadeEmEdicao
    {
        get => _entidadeEmEdicao;
        private set => SetField(ref _entidadeEmEdicao, value);
    }

    private string? _filtro;
    public string? Filtro
    {
        get => _filtro;
        set => SetField(ref _filtro, value);
    }

    public ICommand PesquisarCommand { get; }
    public ICommand NovoCommand { get; }
    public ICommand SalvarCommand { get; }

    public ServicoCadastroViewModel(IServicoRepository repositorio, SessaoUsuario sessao, ShellViewModel shell)
        : base(sessao, shell)
    {
        _repositorio = repositorio;
        PesquisarCommand = new RelayCommand(CarregarAsync);
        NovoCommand = new RelayCommand(Novo);
        SalvarCommand = new RelayCommand(SalvarAsync);
        _ = CarregarAsync();
    }

    private void Novo() => EntidadeEmEdicao = NovoServico();

    private static Servico NovoServico() => new() { Ativo = true };

    private async Task CarregarAsync()
    {
        Carregando = true;
        try
        {
            var servicos = await _repositorio.ListarAsync(Filtro);
            Itens.Clear();
            foreach (var servico in servicos)
            {
                Itens.Add(servico);
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

            Mensagem = "Serviço salvo com sucesso.";
            await CarregarAsync();
            Novo();
        }
        catch (Exception ex)
        {
            Mensagem = $"Erro ao salvar serviço: {ex.Message}";
        }
        finally
        {
            Carregando = false;
        }
    }
}
