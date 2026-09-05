using GestorPDV.Application.Common;
using GestorPDV.Application.Seguranca;
using GestorPDV.Wpf.Helpers;
using GestorPDV.Wpf.ViewModels.Cadastros;

namespace GestorPDV.Wpf.ViewModels;

// Controla a navegação entre as telas da aplicação (verificação de banco,
// login, troca de senha obrigatória, tela inicial pós-login e o menu de
// cadastros). Telas futuras (vendas, etc. — Fases 6+) serão adicionadas
// como novos métodos NavigateToXxx aqui.
public class ShellViewModel : ObservableObject
{
    private readonly IDatabaseInitializer _databaseInitializer;
    private readonly IAutenticacaoService _autenticacaoService;
    private readonly CadastroRepositorios _cadastroRepositorios;

    private object? _currentViewModel;
    public object? CurrentViewModel
    {
        get => _currentViewModel;
        private set => SetField(ref _currentViewModel, value);
    }

    public ShellViewModel(
        IDatabaseInitializer databaseInitializer,
        IAutenticacaoService autenticacaoService,
        CadastroRepositorios cadastroRepositorios)
    {
        _databaseInitializer = databaseInitializer;
        _autenticacaoService = autenticacaoService;
        _cadastroRepositorios = cadastroRepositorios;
    }

    public async Task IniciarAsync()
    {
        var dbStatusViewModel = new DbStatusViewModel(IniciarAsync);
        CurrentViewModel = dbStatusViewModel;

        DatabaseStatus status;
        try
        {
            status = await _databaseInitializer.InicializarAsync();
        }
        catch (Exception ex)
        {
            status = new DatabaseStatus
            {
                ConexaoOk = false,
                SchemaOk = false,
                Mensagem = $"Erro inesperado ao inicializar o banco de dados: {ex.Message}"
            };
        }

        if (status.ConexaoOk && status.SchemaOk)
        {
            NavigateToLogin();
        }
        else
        {
            dbStatusViewModel.AplicarStatus(status);
        }
    }

    public void NavigateToLogin() => CurrentViewModel = new LoginViewModel(_autenticacaoService, this);

    public void NavigateToTrocarSenha(SessaoUsuario sessao) =>
        CurrentViewModel = new TrocarSenhaViewModel(_autenticacaoService, sessao, this);

    public void NavigateToHome(SessaoUsuario sessao) => CurrentViewModel = new HomeViewModel(sessao, this);

    public void NavigateToCadastrosMenu(SessaoUsuario sessao) =>
        CurrentViewModel = new CadastrosMenuViewModel(sessao, this);

    public void NavigateToProdutos(SessaoUsuario sessao) =>
        CurrentViewModel = new ProdutoCadastroViewModel(_cadastroRepositorios.Produtos, sessao, this);

    public void NavigateToServicos(SessaoUsuario sessao) =>
        CurrentViewModel = new ServicoCadastroViewModel(_cadastroRepositorios.Servicos, sessao, this);

    public void NavigateToClientes(SessaoUsuario sessao) =>
        CurrentViewModel = new ClienteCadastroViewModel(_cadastroRepositorios.Clientes, sessao, this);

    public void NavigateToFornecedores(SessaoUsuario sessao) =>
        CurrentViewModel = new FornecedorCadastroViewModel(_cadastroRepositorios.Fornecedores, sessao, this);

    public void NavigateToFuncionarios(SessaoUsuario sessao) =>
        CurrentViewModel = new FuncionarioCadastroViewModel(
            _cadastroRepositorios.Funcionarios, _cadastroRepositorios.Filiais, sessao, this);

    public void NavigateToFiliais(SessaoUsuario sessao) =>
        CurrentViewModel = new FilialCadastroViewModel(_cadastroRepositorios.Filiais, sessao, this);

    public void NavigateToFormasPagamento(SessaoUsuario sessao) =>
        CurrentViewModel = new FormaPagamentoCadastroViewModel(_cadastroRepositorios.FormasPagamento, sessao, this);

    public void NavigateToCondicoesPagamento(SessaoUsuario sessao) =>
        CurrentViewModel = new CondicaoPagamentoCadastroViewModel(_cadastroRepositorios.CondicoesPagamento, sessao, this);

    public void NavigateToTabelasPreco(SessaoUsuario sessao) =>
        CurrentViewModel = new TabelaPrecoCadastroViewModel(
            _cadastroRepositorios.TabelasPreco, _cadastroRepositorios.Filiais, sessao, this);
}
