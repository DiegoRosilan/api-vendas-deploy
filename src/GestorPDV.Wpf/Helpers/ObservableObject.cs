using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GestorPDV.Wpf.Helpers;

public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetField<T>(ref T campo, T valor, [CallerMemberName] string? nomePropriedade = null)
    {
        if (EqualityComparer<T>.Default.Equals(campo, valor))
        {
            return false;
        }

        campo = valor;
        OnPropertyChanged(nomePropriedade);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? nomePropriedade = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nomePropriedade));
}
