using System.Collections.ObjectModel;
using System.Windows.Input;
using GestorPDV.Application.Cadastros;
using GestorPDV.Application.Seguranca;
using GestorPDV.Domain.Cadastros;
using GestorPDV.Wpf.Helpers;

namespace GestorPDV.Wpf.ViewModels.Cadastros;

// Cadastro do cabeçalho da tabela de preço. A associação de produtos e
// preços (cad_tabela_preco_item) é usada pela resolução de preço na venda
// e ganha tela própria na Fase 6, junto com promoções.
public class TabelaPrecoCadastroViewModel : CadastroViewModelBase
{
    private readonly ITabelaPrecoRepository _repositorio;
    private readonly IFilialRepository _filialRepositorio;

    public ObservableCollection<TabelaPreco> Itens { get; } = new();
    public ObservableCollection<Filial> Filiais { get; } = new();

    private TabelaPreco? _selecionado;
    public TabelaPreco? Selecionado
    {
        get => _selecionado;
        set
        {
            if (SetField(ref _selecionado, value) && value is not null)
            {
                EntidadeEmEdicao = value;
            }
        }
    }

    private TabelaPreco _entidadeEmEdicao = NovaTabelaPreco();
    public TabelaPreco EntidadeEmEdicao
    {
        get => _entidadeEmEdicao;
        private set => SetField(ref _entidadeEmEdicao, value);
    }

    public ICommand CarregarCommand { get; }
    public ICommand NovoCommand { get; }
    public ICommand SalvarCommand { get; }

    public TabelaPrecoCadastroViewModel(
        ITabelaPrecoRepository repositorio, IFilialRepository filialRepositorio, SessaoUsuario sessao, ShellViewModel shell)
        : base(sessao, shell)
    {
        _repositorio = repositorio;
        _filialRepositorio = filialRepositorio;
        CarregarCommand = new RelayCommand(CarregarAsync);
        NovoCommand = new RelayCommand(Novo);
        SalvarCommand = new RelayCommand(SalvarAsync);
        _ = CarregarAsync();
        _ = CarregarFiliaisAsync();
    }

    private void Novo() => EntidadeEmEdicao = NovaTabelaPreco();

    private static TabelaPreco NovaTabelaPreco() => new() { Ativo = true };

    private async Task CarregarFiliaisAsync()
    {
        var filiais = await _filialRepositorio.ListarAsync();
        Filiais.Clear();
        foreach (var filial in filiais)
        {
            Filiais.Add(filial);
        }
    }

    private async Task CarregarAsync()
    {
        Carregando = true;
        try
        {
            var tabelas = await _repositorio.ListarAsync();
            Itens.Clear();
            foreach (var tabela in tabelas)
            {
                Itens.Add(tabela);
            }
        }
        finally
        {
            Carregando = false;
        }
    }

    private async Task SalvarAsync()
    {
        Mensagem = null;

        if (string.IsNullOrWhiteSpace(EntidadeEmEdicao.Descricao))
        {
            Mensagem = "Informe a descrição da tabela de preço.";
            return;
        }

        Carregando = true;
        try
        {
            if (EntidadeEmEdicao.Id == 0)
            {
                EntidadeEmEdicao.Id = await _repositorio.InserirAsync(EntidadeEmEdicao);
            }
            else
            {
                await _repositorio.AtualizarAsync(EntidadeEmEdicao);
            }

            Mensagem = "Tabela de preço salva com sucesso.";
            await CarregarAsync();
            Novo();
        }
        catch (Exception ex)
        {
            Mensagem = $"Erro ao salvar tabela de preço: {ex.Message}";
        }
        finally
        {
            Carregando = false;
        }
    }
}
