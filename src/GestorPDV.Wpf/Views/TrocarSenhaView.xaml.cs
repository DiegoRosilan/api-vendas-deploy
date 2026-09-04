using System.Windows.Controls;
using GestorPDV.Wpf.ViewModels;

namespace GestorPDV.Wpf.Views;

public partial class TrocarSenhaView : UserControl
{
    public TrocarSenhaView()
    {
        InitializeComponent();
    }

    private void CaixaSenhaAtual_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is TrocarSenhaViewModel viewModel)
        {
            viewModel.SenhaAtual = CaixaSenhaAtual.Password;
        }
    }

    private void CaixaNovaSenha_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is TrocarSenhaViewModel viewModel)
        {
            viewModel.NovaSenha = CaixaNovaSenha.Password;
        }
    }

    private void CaixaConfirmarSenha_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is TrocarSenhaViewModel viewModel)
        {
            viewModel.ConfirmarSenha = CaixaConfirmarSenha.Password;
        }
    }
}
