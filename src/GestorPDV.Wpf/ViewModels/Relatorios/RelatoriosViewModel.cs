using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using GestorPDV.Application.Common;
using GestorPDV.Application.Relatorios;
using GestorPDV.Application.Seguranca;
using GestorPDV.Wpf.Helpers;
using GestorPDV.Wpf.ViewModels.Cadastros;

namespace GestorPDV.Wpf.ViewModels.Relatorios;

// Tela de relatórios (Fase 8): gera o PDF em memória via IRelatorioService e
// abre no visualizador padrão do sistema operacional — não há preview
// embutido nesta fase (chega na Fase 9, junto com impressão).
public class RelatoriosViewModel : CadastroViewModelBase
{
    private readonly IRelatorioService _relatorioService;

    private DateOnly _dataInicioVendas = DateOnly.FromDateTime(DateTime.Now).AddDays(-30);
    public DateOnly DataInicioVendas
    {
        get => _dataInicioVendas;
        set => SetField(ref _dataInicioVendas, value);
    }

    private DateOnly _dataFimVendas = DateOnly.FromDateTime(DateTime.Now);
    public DateOnly DataFimVendas
    {
        get => _dataFimVendas;
        set => SetField(ref _dataFimVendas, value);
    }

    public ICommand GerarRelatorioVendasCommand { get; }
    public ICommand GerarRelatorioEstoqueCommand { get; }
    public ICommand GerarRelatorioContasReceberCommand { get; }

    public RelatoriosViewModel(IRelatorioService relatorioService, SessaoUsuario sessao, ShellViewModel shell)
        : base(sessao, shell, () => shell.NavigateToHome(sessao))
    {
        _relatorioService = relatorioService;

        GerarRelatorioVendasCommand = new RelayCommand(GerarRelatorioVendasAsync);
        GerarRelatorioEstoqueCommand = new RelayCommand(GerarRelatorioEstoqueAsync);
        GerarRelatorioContasReceberCommand = new RelayCommand(GerarRelatorioContasReceberAsync);
    }

    private async Task GerarRelatorioVendasAsync()
    {
        if (Sessao.FilialId is null)
        {
            Mensagem = "Seu usuário não está associado a uma filial.";
            return;
        }

        await ExecutarEAbrirAsync(
            "vendas",
            () => _relatorioService.GerarRelatorioVendasAsync(Sessao.FilialId.Value, DataInicioVendas, DataFimVendas));
    }

    private async Task GerarRelatorioEstoqueAsync()
    {
        if (Sessao.FilialId is null)
        {
            Mensagem = "Seu usuário não está associado a uma filial.";
            return;
        }

        await ExecutarEAbrirAsync("estoque", () => _relatorioService.GerarRelatorioEstoqueAsync(Sessao.FilialId.Value));
    }

    private async Task GerarRelatorioContasReceberAsync()
    {
        if (Sessao.FilialId is null)
        {
            Mensagem = "Seu usuário não está associado a uma filial.";
            return;
        }

        await ExecutarEAbrirAsync(
            "contas-a-receber", () => _relatorioService.GerarRelatorioContasReceberAsync(Sessao.FilialId.Value));
    }

    private async Task ExecutarEAbrirAsync(string nomeArquivo, Func<Task<Result<byte[]>>> gerar)
    {
        Mensagem = null;
        Carregando = true;
        try
        {
            var resultado = await gerar();
            if (!resultado.Sucesso)
            {
                Mensagem = resultado.Erro;
                return;
            }

            var caminho = Path.Combine(Path.GetTempPath(), $"relatorio-{nomeArquivo}-{DateTime.Now:yyyyMMddHHmmss}.pdf");
            await File.WriteAllBytesAsync(caminho, resultado.Valor!);

            try
            {
                Process.Start(new ProcessStartInfo(caminho) { UseShellExecute = true });
                Mensagem = $"Relatório gerado: {caminho}";
            }
            catch (Exception ex)
            {
                Mensagem = $"Relatório gerado em {caminho}, mas não foi possível abri-lo automaticamente: {ex.Message}";
            }
        }
        catch (Exception ex)
        {
            Mensagem = $"Erro ao gerar o relatório: {ex.Message}";
        }
        finally
        {
            Carregando = false;
        }
    }
}
