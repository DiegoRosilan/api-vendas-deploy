using System.Windows.Input;

namespace GestorPDV.Wpf.Helpers;

// Implementação mínima de ICommand com suporte a execução assíncrona e
// proteção contra reentrância (evita clicar "Entrar" duas vezes durante o
// login), sem depender de um pacote de MVVM externo.
public class RelayCommand : ICommand
{
    private readonly Func<Task> _executeAsync;
    private readonly Func<bool>? _canExecute;
    private bool _estaExecutando;

    public RelayCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
    {
        _executeAsync = executeAsync;
        _canExecute = canExecute;
    }

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
        : this(
            () =>
            {
                execute();
                return Task.CompletedTask;
            },
            canExecute)
    {
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => !_estaExecutando && (_canExecute?.Invoke() ?? true);

    public async void Execute(object? parameter)
    {
        _estaExecutando = true;
        CommandManager.InvalidateRequerySuggested();
        try
        {
            await _executeAsync();
        }
        finally
        {
            _estaExecutando = false;
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
