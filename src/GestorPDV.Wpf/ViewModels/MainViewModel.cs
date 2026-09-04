using System.ComponentModel;
using System.Runtime.CompilerServices;
using GestorPDV.Application.Common;

namespace GestorPDV.Wpf.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private string _statusMensagem = "Verificando conexão com o PostgreSQL...";
    public string StatusMensagem
    {
        get => _statusMensagem;
        private set => SetField(ref _statusMensagem, value);
    }

    private bool _statusOk;
    public bool StatusOk
    {
        get => _statusOk;
        private set => SetField(ref _statusOk, value);
    }

    public void AplicarStatus(DatabaseStatus status)
    {
        StatusOk = status.ConexaoOk && status.SchemaOk;
        StatusMensagem = status.Mensagem ?? (StatusOk ? "Conexão e schema OK." : "Falha desconhecida ao inicializar o banco.");
    }

    private void SetField<T>(ref T campo, T valor, [CallerMemberName] string? nomePropriedade = null)
    {
        if (EqualityComparer<T>.Default.Equals(campo, valor))
        {
            return;
        }

        campo = valor;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nomePropriedade));
    }
}
