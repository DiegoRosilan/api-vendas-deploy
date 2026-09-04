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

    public ICommand SairCommand { get; }

    public HomeViewModel(SessaoUsuario sessao, ShellViewModel shell)
    {
        Sessao = sessao;
        _shell = shell;
        SairCommand = new RelayCommand(() => _shell.NavigateToLogin());
    }
}
