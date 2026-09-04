using System.Windows.Input;
using GestorPDV.Application.Seguranca;
using GestorPDV.Wpf.Helpers;

namespace GestorPDV.Wpf.ViewModels;

public class TrocarSenhaViewModel : ObservableObject
{
    private readonly IAutenticacaoService _autenticacaoService;
    private readonly SessaoUsuario _sessao;
    private readonly ShellViewModel _shell;

    public string NomeUsuario => _sessao.Nome;

    private string _senhaAtual = string.Empty;
    public string SenhaAtual
    {
        get => _senhaAtual;
        set => SetField(ref _senhaAtual, value);
    }

    private string _novaSenha = string.Empty;
    public string NovaSenha
    {
        get => _novaSenha;
        set => SetField(ref _novaSenha, value);
    }

    private string _confirmarSenha = string.Empty;
    public string ConfirmarSenha
    {
        get => _confirmarSenha;
        set => SetField(ref _confirmarSenha, value);
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

    public ICommand ConfirmarCommand { get; }

    public TrocarSenhaViewModel(IAutenticacaoService autenticacaoService, SessaoUsuario sessao, ShellViewModel shell)
    {
        _autenticacaoService = autenticacaoService;
        _sessao = sessao;
        _shell = shell;
        ConfirmarCommand = new RelayCommand(ConfirmarAsync, () => !EstaCarregando);
    }

    private async Task ConfirmarAsync()
    {
        ErroMensagem = null;

        if (NovaSenha != ConfirmarSenha)
        {
            ErroMensagem = "As senhas não coincidem.";
            return;
        }

        EstaCarregando = true;
        try
        {
            var resultado = await _autenticacaoService.AlterarSenhaAsync(_sessao.UsuarioId, SenhaAtual, NovaSenha);
            if (!resultado.Sucesso)
            {
                ErroMensagem = resultado.Erro ?? "Não foi possível trocar a senha.";
                return;
            }

            _sessao.ExigeTrocaSenha = false;
            _shell.NavigateToHome(_sessao);
        }
        finally
        {
            EstaCarregando = false;
        }
    }
}
