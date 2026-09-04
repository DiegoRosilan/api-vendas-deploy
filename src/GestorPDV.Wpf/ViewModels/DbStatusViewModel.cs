using System.Windows.Input;
using GestorPDV.Application.Common;
using GestorPDV.Wpf.Helpers;

namespace GestorPDV.Wpf.ViewModels;

public class DbStatusViewModel : ObservableObject
{
    private string _mensagem = "Verificando conexão com o PostgreSQL...";
    public string Mensagem
    {
        get => _mensagem;
        private set => SetField(ref _mensagem, value);
    }

    private bool _falhou;
    public bool Falhou
    {
        get => _falhou;
        private set => SetField(ref _falhou, value);
    }

    public ICommand TentarNovamenteCommand { get; }

    public DbStatusViewModel(Func<Task> tentarNovamenteAsync)
    {
        TentarNovamenteCommand = new RelayCommand(tentarNovamenteAsync);
    }

    public void AplicarStatus(DatabaseStatus status)
    {
        Falhou = !(status.ConexaoOk && status.SchemaOk);
        Mensagem = status.Mensagem ?? (Falhou ? "Falha desconhecida ao inicializar o banco de dados." : "Conexão e schema OK.");
    }
}
