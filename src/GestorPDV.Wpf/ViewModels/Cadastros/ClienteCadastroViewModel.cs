using System.Collections.ObjectModel;
using System.Windows.Input;
using GestorPDV.Application.Cadastros;
using GestorPDV.Application.Seguranca;
using GestorPDV.Domain.Cadastros;
using GestorPDV.Wpf.Helpers;

namespace GestorPDV.Wpf.ViewModels.Cadastros;

public class ClienteCadastroViewModel : CadastroViewModelBase
{
    private readonly IClienteRepository _repositorio;

    public ObservableCollection<Cliente> Itens { get; } = new();
    public IReadOnlyList<TipoPessoa> TiposPessoaDisponiveis { get; } = Enum.GetValues<TipoPessoa>();

    private Cliente? _selecionado;
    public Cliente? Selecionado
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

    private Cliente _entidadeEmEdicao = NovoCliente();
    public Cliente EntidadeEmEdicao
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

    public ClienteCadastroViewModel(IClienteRepository repositorio, SessaoUsuario sessao, ShellViewModel shell)
        : base(sessao, shell)
    {
        _repositorio = repositorio;
        PesquisarCommand = new RelayCommand(CarregarAsync);
        NovoCommand = new RelayCommand(Novo);
        SalvarCommand = new RelayCommand(SalvarAsync);
        _ = CarregarAsync();
    }

    private void Novo() => EntidadeEmEdicao = NovoCliente();

    private static Cliente NovoCliente() => new()
    {
        Pessoa = new Pessoa { TipoPessoa = TipoPessoa.Fisica, Ativo = true }
    };

    private async Task CarregarAsync()
    {
        Carregando = true;
        try
        {
            var clientes = await _repositorio.ListarAsync(Filtro);
            Itens.Clear();
            foreach (var cliente in clientes)
            {
                Itens.Add(cliente);
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
            Mensagem = "Informe o nome do cliente.";
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

            Mensagem = "Cliente salvo com sucesso.";
            await CarregarAsync();
            Novo();
        }
        catch (Exception ex)
        {
            Mensagem = $"Erro ao salvar cliente: {ex.Message}";
        }
        finally
        {
            Carregando = false;
        }
    }
}
