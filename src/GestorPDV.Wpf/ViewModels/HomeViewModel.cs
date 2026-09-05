using System.Windows.Input;
using GestorPDV.Application.Seguranca;
using GestorPDV.Wpf.Helpers;

namespace GestorPDV.Wpf.ViewModels;

public class HomeViewModel : ObservableObject
{
    private readonly ShellViewModel _shell;

    public SessaoUsuario Sessao { get; }

    public string PermissoesTexto => Sessao.Permissoes.Count == 0
        ? "Nenhuma permissão atribuída."
        : string.Join(", ", Sessao.Permissoes);

    // RN-SEG-001: bloqueio de ações/botões conforme a permissão do usuário.
    public bool PodeGerenciarCadastros => Sessao.TemPermissao("CADASTRO_GERENCIAR");

    public ICommand AbrirCadastrosCommand { get; }
    public ICommand SairCommand { get; }

    public HomeViewModel(SessaoUsuario sessao, ShellViewModel shell)
    {
        Sessao = sessao;
        _shell = shell;
        AbrirCadastrosCommand = new RelayCommand(() => _shell.NavigateToCadastrosMenu(Sessao));
        SairCommand = new RelayCommand(() => _shell.NavigateToLogin());
    }
}
