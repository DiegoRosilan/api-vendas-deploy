using System.Collections.ObjectModel;
using System.Windows.Input;
using GestorPDV.Application.Cadastros;
using GestorPDV.Application.Seguranca;
using GestorPDV.Domain.Cadastros;
using GestorPDV.Wpf.Helpers;

namespace GestorPDV.Wpf.ViewModels.Cadastros;

public class FornecedorCadastroViewModel : CadastroViewModelBase
{
    private readonly IFornecedorRepository _repositorio;

    public ObservableCollection<Fornecedor> Itens { get; } = new();
    public IReadOnlyList<TipoPessoa> TiposPessoaDisponiveis { get; } = Enum.GetValues<TipoPessoa>();

    private Fornecedor? _selecionado;
    public Fornecedor? Selecionado
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

    private Fornecedor _entidadeEmEdicao = NovoFornecedor();
    public Fornecedor EntidadeEmEdicao
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

    public FornecedorCadastroViewModel(IFornecedorRepository repositorio, SessaoUsuario sessao, ShellViewModel shell)
        : base(sessao, shell)
    {
        _repositorio = repositorio;
        PesquisarCommand = new RelayCommand(CarregarAsync);
        NovoCommand = new RelayCommand(Novo);
        SalvarCommand = new RelayCommand(SalvarAsync);
        _ = CarregarAsync();
    }

    private void Novo() => EntidadeEmEdicao = NovoFornecedor();

    private static Fornecedor NovoFornecedor() => new()
    {
        Pessoa = new Pessoa { TipoPessoa = TipoPessoa.Juridica, Ativo = true }
    };

    private async Task CarregarAsync()
    {
        Carregando = true;
        try
        {
            var fornecedores = await _repositorio.ListarAsync(Filtro);
            Itens.Clear();
            foreach (var fornecedor in fornecedores)
            {
                Itens.Add(fornecedor);
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

        if (string.IsNullOrWhiteSpace(EntidadeEmEdicao.Pessoa?.Nome))
        {
            Mensagem = "Informe a razão social/nome do fornecedor.";
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

            Mensagem = "Fornecedor salvo com sucesso.";
            await CarregarAsync();
            Novo();
        }
        catch (Exception ex)
        {
            Mensagem = $"Erro ao salvar fornecedor: {ex.Message}";
        }
        finally
        {
            Carregando = false;
        }
    }
}
