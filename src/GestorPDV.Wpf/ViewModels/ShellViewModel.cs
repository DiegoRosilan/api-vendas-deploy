using GestorPDV.Application.Common;
using GestorPDV.Application.Seguranca;
using GestorPDV.Wpf.Helpers;

namespace GestorPDV.Wpf.ViewModels;

// Controla a navegação entre as telas da aplicação (verificação de banco,
// login, troca de senha obrigatória e tela inicial pós-login). As telas
// futuras (cadastros, vendas, etc. — Fases 5+) serão adicionadas como novos
// métodos NavigateToXxx aqui.
public class ShellViewModel : ObservableObject
{
    private readonly IDatabaseInitializer _databaseInitializer;
    private readonly IAutenticacaoService _autenticacaoService;

    private object? _currentViewModel;
    public object? CurrentViewModel
    {
        get => _currentViewModel;
        private set => SetField(ref _currentViewModel, value);
    }

    public ShellViewModel(IDatabaseInitializer databaseInitializer, IAutenticacaoService autenticacaoService)
    {
        _databaseInitializer = databaseInitializer;
        _autenticacaoService = autenticacaoService;
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
}
