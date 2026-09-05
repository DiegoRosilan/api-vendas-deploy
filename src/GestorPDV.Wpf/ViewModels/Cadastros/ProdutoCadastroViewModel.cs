using System.Collections.ObjectModel;
using System.Windows.Input;
using GestorPDV.Application.Cadastros;
using GestorPDV.Application.Seguranca;
using GestorPDV.Domain.Cadastros;
using GestorPDV.Wpf.Helpers;

namespace GestorPDV.Wpf.ViewModels.Cadastros;

public class ProdutoCadastroViewModel : CadastroViewModelBase
{
    private readonly IProdutoRepository _repositorio;

    public ObservableCollection<Produto> Itens { get; } = new();

    private Produto? _selecionado;
    public Produto? Selecionado
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

    private Produto _entidadeEmEdicao = NovoProduto();
    public Produto EntidadeEmEdicao
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

    public ProdutoCadastroViewModel(IProdutoRepository repositorio, SessaoUsuario sessao, ShellViewModel shell)
        : base(sessao, shell)
    {
        _repositorio = repositorio;
        PesquisarCommand = new RelayCommand(CarregarAsync);
        NovoCommand = new RelayCommand(Novo);
        SalvarCommand = new RelayCommand(SalvarAsync);
        _ = CarregarAsync();
    }

    private void Novo() => EntidadeEmEdicao = NovoProduto();

    private static Produto NovoProduto() => new() { Ativo = true, Unidade = "UN", ControlaEstoque = true };

    private async Task CarregarAsync()
    {
        Carregando = true;
        try
        {
            var produtos = await _repositorio.ListarAsync(Filtro);
            Itens.Clear();
            foreach (var produto in produtos)
            {
                Itens.Add(produto);
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

            Mensagem = "Produto salvo com sucesso.";
            await CarregarAsync();
            Novo();
        }
        catch (Exception ex)
        {
            Mensagem = $"Erro ao salvar produto: {ex.Message}";
        }
        finally
        {
            Carregando = false;
        }
    }
}
