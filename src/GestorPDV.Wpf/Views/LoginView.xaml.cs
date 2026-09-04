using System.Windows.Controls;
using GestorPDV.Wpf.ViewModels;

namespace GestorPDV.Wpf.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
    }

    // PasswordBox.Password não é uma DependencyProperty (por segurança, não
    // pode ser alvo de binding), então repassamos o valor para o ViewModel
    // manualmente a cada alteração.
    private void CaixaSenha_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel viewModel)
        {
            viewModel.Senha = CaixaSenha.Password;
        }
    }
}
