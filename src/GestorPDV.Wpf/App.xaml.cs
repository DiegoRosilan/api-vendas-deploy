using System.IO;
using System.Windows;
using GestorPDV.Application.Common;
using GestorPDV.Infrastructure.Configuration;
using GestorPDV.Infrastructure.Database;
using GestorPDV.Wpf.ViewModels;

namespace GestorPDV.Wpf;

// Composition root: é o único ponto da aplicação que conhece as
// implementações concretas de Infrastructure/Data.Postgres — as telas e
// ViewModels só dependem de abstrações da camada Application (item 7 do
// escopo: regras/infraestrutura fora dos formulários).
public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var configuration = ConfigurationLoader.Carregar(AppContext.BaseDirectory);
        var databaseOptions = ConfigurationLoader.ObterOpcoesBanco(configuration);

        var connectionFactory = new NpgsqlConnectionFactory(databaseOptions);
        var scriptsPath = Path.Combine(AppContext.BaseDirectory, "database", "schema");
        var schemaScriptRunner = new SchemaScriptRunner(scriptsPath);
        IDatabaseInitializer databaseInitializer = new DatabaseInitializer(connectionFactory, schemaScriptRunner);

        var viewModel = new MainViewModel();

        try
        {
            var status = await databaseInitializer.InicializarAsync();
            viewModel.AplicarStatus(status);
        }
        catch (Exception ex)
        {
            viewModel.AplicarStatus(new DatabaseStatus
            {
                ConexaoOk = false,
                SchemaOk = false,
                Mensagem = $"Erro inesperado ao inicializar o banco de dados: {ex.Message}"
            });
        }

        var mainWindow = new MainWindow { DataContext = viewModel };
        MainWindow = mainWindow;
        mainWindow.Show();
    }
}
