using System.Windows.Input;
using GestorPDV.Application.Seguranca;
using GestorPDV.Wpf.Helpers;

namespace GestorPDV.Wpf.ViewModels.Cadastros;

public class CadastrosMenuViewModel : ObservableObject
{
    private readonly SessaoUsuario _sessao;
    private readonly ShellViewModel _shell;

    public ICommand AbrirProdutosCommand { get; }
    public ICommand AbrirServicosCommand { get; }
    public ICommand AbrirClientesCommand { get; }
    public ICommand AbrirFornecedoresCommand { get; }
    public ICommand AbrirFuncionariosCommand { get; }
    public ICommand AbrirFiliaisCommand { get; }
    public ICommand AbrirFormasPagamentoCommand { get; }
    public ICommand AbrirCondicoesPagamentoCommand { get; }
    public ICommand AbrirTabelasPrecoCommand { get; }
    public ICommand VoltarCommand { get; }

    public CadastrosMenuViewModel(SessaoUsuario sessao, ShellViewModel shell)
    {
        _sessao = sessao;
        _shell = shell;

        AbrirProdutosCommand = new RelayCommand(() => _shell.NavigateToProdutos(_sessao));
        AbrirServicosCommand = new RelayCommand(() => _shell.NavigateToServicos(_sessao));
        AbrirClientesCommand = new RelayCommand(() => _shell.NavigateToClientes(_sessao));
        AbrirFornecedoresCommand = new RelayCommand(() => _shell.NavigateToFornecedores(_sessao));
        AbrirFuncionariosCommand = new RelayCommand(() => _shell.NavigateToFuncionarios(_sessao));
        AbrirFiliaisCommand = new RelayCommand(() => _shell.NavigateToFiliais(_sessao));
        AbrirFormasPagamentoCommand = new RelayCommand(() => _shell.NavigateToFormasPagamento(_sessao));
        AbrirCondicoesPagamentoCommand = new RelayCommand(() => _shell.NavigateToCondicoesPagamento(_sessao));
        AbrirTabelasPrecoCommand = new RelayCommand(() => _shell.NavigateToTabelasPreco(_sessao));
        VoltarCommand = new RelayCommand(() => _shell.NavigateToHome(_sessao));
    }
}
