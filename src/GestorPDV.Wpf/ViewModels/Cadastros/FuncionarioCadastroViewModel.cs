using System.Collections.ObjectModel;
using System.Windows.Input;
using GestorPDV.Application.Cadastros;
using GestorPDV.Application.Seguranca;
using GestorPDV.Domain.Cadastros;
using GestorPDV.Wpf.Helpers;

namespace GestorPDV.Wpf.ViewModels.Cadastros;

public class FuncionarioCadastroViewModel : CadastroViewModelBase
{
    private readonly IFuncionarioRepository _repositorio;
    private readonly IFilialRepository _filialRepositorio;

    public ObservableCollection<Funcionario> Itens { get; } = new();
    public ObservableCollection<Filial> Filiais { get; } = new();
    public IReadOnlyList<TipoPessoa> TiposPessoaDisponiveis { get; } = Enum.GetValues<TipoPessoa>();

    private Funcionario? _selecionado;
    public Funcionario? Selecionado
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

    private Funcionario _entidadeEmEdicao = NovoFuncionario();
    public Funcionario EntidadeEmEdicao
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

    public FuncionarioCadastroViewModel(
        IFuncionarioRepository repositorio, IFilialRepository filialRepositorio, SessaoUsuario sessao, ShellViewModel shell)
        : base(sessao, shell)
    {
        _repositorio = repositorio;
        _filialRepositorio = filialRepositorio;
        PesquisarCommand = new RelayCommand(CarregarAsync);
        NovoCommand = new RelayCommand(Novo);
        SalvarCommand = new RelayCommand(SalvarAsync);
        _ = CarregarAsync();
        _ = CarregarFiliaisAsync();
    }

    private void Novo() => EntidadeEmEdicao = NovoFuncionario();

    private static Funcionario NovoFuncionario() => new()
    {
        Pessoa = new Pessoa { TipoPessoa = TipoPessoa.Fisica, Ativo = true }
    };

    private async Task CarregarFiliaisAsync()
    {
        var filiais = await _filialRepositorio.ListarAsync();
        Filiais.Clear();
        foreach (var filial in filiais)
        {
            Filiais.Add(filial);
        }
    }

    private async Task CarregarAsync()
    {
        Carregando = true;
        try
        {
            var funcionarios = await _repositorio.ListarAsync(Filtro);
            Itens.Clear();
            foreach (var funcionario in funcionarios)
            {
                Itens.Add(funcionario);
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
            Mensagem = "Informe o nome do funcionário.";
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

            Mensagem = "Funcionário salvo com sucesso.";
            await CarregarAsync();
            Novo();
        }
        catch (Exception ex)
        {
            Mensagem = $"Erro ao salvar funcionário: {ex.Message}";
        }
        finally
        {
            Carregando = false;
        }
    }
}
