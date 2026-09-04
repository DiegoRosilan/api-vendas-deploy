using System.Windows.Input;
using GestorPDV.Application.Seguranca;
using GestorPDV.Wpf.Helpers;

namespace GestorPDV.Wpf.ViewModels;

public class LoginViewModel : ObservableObject
{
    private readonly IAutenticacaoService _autenticacaoService;
    private readonly ShellViewModel _shell;

    private string _login = string.Empty;
    public string Login
    {
        get => _login;
        set => SetField(ref _login, value);
    }

    private string _senha = string.Empty;
    public string Senha
    {
        get => _senha;
        set => SetField(ref _senha, value);
    }

    private string? _erroMensagem;
    public string? ErroMensagem
    {
        get => _erroMensagem;
        private set => SetField(ref _erroMensagem, value);
    }

    private bool _estaCarregando;
    public bool EstaCarregando
    {
        get => _estaCarregando;
        private set => SetField(ref _estaCarregando, value);
    }

    public ICommand EntrarCommand { get; }

    public LoginViewModel(IAutenticacaoService autenticacaoService, ShellViewModel shell)
    {
        _autenticacaoService = autenticacaoService;
        _shell = shell;
        EntrarCommand = new RelayCommand(EntrarAsync, () => !EstaCarregando);
    }

    private async Task EntrarAsync()
    {
        ErroMensagem = null;

        if (string.IsNullOrWhiteSpace(Login) || string.IsNullOrWhiteSpace(Senha))
        {
            ErroMensagem = "Informe usuário e senha.";
            return;
        }

        EstaCarregando = true;
        try
        {
            var resultado = await _autenticacaoService.AutenticarAsync(Login.Trim(), Senha);
            if (!resultado.Sucesso || resultado.Valor is null)
            {
                ErroMensagem = resultado.Erro ?? "Não foi possível autenticar.";
                return;
            }

            var sessao = resultado.Valor;
            if (sessao.ExigeTrocaSenha)
            {
                _shell.NavigateToTrocarSenha(sessao);
            }
            else
            {
                _shell.NavigateToHome(sessao);
            }
        }
        finally
        {
            EstaCarregando = false;
        }
    }
}
