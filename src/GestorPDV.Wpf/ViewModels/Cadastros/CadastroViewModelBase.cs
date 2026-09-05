using System.Windows.Input;
using GestorPDV.Application.Seguranca;
using GestorPDV.Wpf.Helpers;

namespace GestorPDV.Wpf.ViewModels.Cadastros;

// Comportamento comum às telas de cadastro (produtos, clientes, etc.):
// navegação de volta ao menu de cadastros, indicador de carregamento e
// mensagem de status/erro após salvar.
public abstract class CadastroViewModelBase : ObservableObject
{
    protected SessaoUsuario Sessao { get; }
    protected ShellViewModel Shell { get; }

    private bool _carregando;
    public bool Carregando
    {
        get => _carregando;
        protected set => SetField(ref _carregando, value);
    }

    private string? _mensagem;
    public string? Mensagem
    {
        get => _mensagem;
        protected set => SetField(ref _mensagem, value);
    }

    public ICommand VoltarCommand { get; }

    protected CadastroViewModelBase(SessaoUsuario sessao, ShellViewModel shell)
    {
        Sessao = sessao;
        Shell = shell;
        VoltarCommand = new RelayCommand(() => Shell.NavigateToCadastrosMenu(Sessao));
    }
}
