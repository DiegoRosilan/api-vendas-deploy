using GestorPDV.Application.Cadastros;

namespace GestorPDV.Wpf;

// Agrupa os repositórios de cadastro só para simplificar a passagem de
// dependências entre o composition root (App.xaml.cs) e as telas de
// cadastro — não contém lógica própria.
public class CadastroRepositorios
{
    public IProdutoRepository Produtos { get; }
    public IServicoRepository Servicos { get; }
    public IClienteRepository Clientes { get; }
    public IFornecedorRepository Fornecedores { get; }
    public IFuncionarioRepository Funcionarios { get; }
    public IFilialRepository Filiais { get; }
    public IFormaPagamentoRepository FormasPagamento { get; }
    public ICondicaoPagamentoRepository CondicoesPagamento { get; }
    public ITabelaPrecoRepository TabelasPreco { get; }

    public CadastroRepositorios(
        IProdutoRepository produtos,
        IServicoRepository servicos,
        IClienteRepository clientes,
        IFornecedorRepository fornecedores,
        IFuncionarioRepository funcionarios,
        IFilialRepository filiais,
        IFormaPagamentoRepository formasPagamento,
        ICondicaoPagamentoRepository condicoesPagamento,
        ITabelaPrecoRepository tabelasPreco)
    {
        Produtos = produtos;
        Servicos = servicos;
        Clientes = clientes;
        Fornecedores = fornecedores;
        Funcionarios = funcionarios;
        Filiais = filiais;
        FormasPagamento = formasPagamento;
        CondicoesPagamento = condicoesPagamento;
        TabelasPreco = tabelasPreco;
    }
}
