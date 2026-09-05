using System.IO;
using System.Windows;
using GestorPDV.Application.Seguranca;
using GestorPDV.Data.Postgres;
using GestorPDV.Data.Postgres.Repositories;
using GestorPDV.Estoque.Servicos;
using GestorPDV.Infrastructure.Configuration;
using GestorPDV.Infrastructure.Database;
using GestorPDV.Infrastructure.Security;
using GestorPDV.Vendas.Servicos;
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
        var databaseInitializer = new DatabaseInitializer(connectionFactory, schemaScriptRunner);

        var passwordHasher = new BCryptPasswordHasher();
        var usuarioRepository = new UsuarioRepository(connectionFactory);
        IAutenticacaoService autenticacaoService = new AutenticacaoService(usuarioRepository, passwordHasher);

        var produtoRepository = new ProdutoRepository(connectionFactory);
        var servicoRepository = new ServicoRepository(connectionFactory);
        var tabelaPrecoRepository = new TabelaPrecoRepository(connectionFactory);
        var funcionarioRepository = new FuncionarioRepository(connectionFactory);

        var cadastroRepositorios = new CadastroRepositorios(
            produtos: produtoRepository,
            servicos: servicoRepository,
            clientes: new ClienteRepository(connectionFactory),
            fornecedores: new FornecedorRepository(connectionFactory),
            funcionarios: funcionarioRepository,
            filiais: new FilialRepository(connectionFactory),
            formasPagamento: new FormaPagamentoRepository(connectionFactory),
            condicoesPagamento: new CondicaoPagamentoRepository(connectionFactory),
            tabelasPreco: tabelaPrecoRepository);

        var unitOfWorkFactory = new NpgsqlUnitOfWorkFactory(connectionFactory);
        var estoqueService = new EstoqueService(new EstoqueRepository(connectionFactory));
        var vendaRepository = new VendaRepository(connectionFactory);
        var comissaoRepository = new ComissaoRepository(connectionFactory);
        var vendaService = new VendaService(
            vendaRepository, estoqueService, produtoRepository, servicoRepository, tabelaPrecoRepository,
            funcionarioRepository, comissaoRepository, unitOfWorkFactory);
        var vendaContexto = new VendaContexto(vendaService, vendaRepository);

        var shellViewModel = new ShellViewModel(databaseInitializer, autenticacaoService, cadastroRepositorios, vendaContexto);

        var mainWindow = new MainWindow { DataContext = shellViewModel };
        MainWindow = mainWindow;
        mainWindow.Show();

        await shellViewModel.IniciarAsync();
    }
}
