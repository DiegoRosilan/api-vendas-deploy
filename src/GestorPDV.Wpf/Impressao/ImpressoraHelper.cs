using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Documents;
using GestorPDV.Application.Common;

namespace GestorPDV.Wpf.Impressao;

// Ponte entre a camada de impressão do WPF (PrintDialog/System.Printing) e
// o restante da aplicação — mensagens de erro amigáveis em vez de deixar
// exceções de impressora (sem impressora instalada, impressora offline,
// etc.) subirem cruas até a tela.
public static class ImpressoraHelper
{
    // Mostra o diálogo de seleção de impressora e imprime o documento.
    // Retorna Ok mesmo se o usuário cancelar o diálogo (não é um erro).
    public static Result Imprimir(FlowDocument documento, string nomeTrabalho)
    {
        try
        {
            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() != true)
            {
                return Result.Ok();
            }

            var paginador = ((IDocumentPaginatorSource)documento).DocumentPaginator;
            printDialog.PrintDocument(paginador, nomeTrabalho);
            return Result.Ok();
        }
        catch (Win32Exception ex)
        {
            return Result.Falha($"Não foi possível imprimir: {ex.Message}. Verifique se há uma impressora instalada e configurada como padrão.");
        }
        catch (Exception ex)
        {
            return Result.Falha($"Erro ao imprimir: {ex.Message}");
        }
    }

    // Envia um arquivo (ex.: PDF de relatório) direto para a impressora
    // padrão via o verbo "print" do sistema operacional, sem abrir o
    // visualizador — evita ter que renderizar o PDF na própria aplicação.
    public static Result ImprimirArquivo(string caminhoArquivo)
    {
        try
        {
            using var processo = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(caminhoArquivo)
            {
                Verb = "print",
                UseShellExecute = true,
                CreateNoWindow = true
            });
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Falha(
                $"Não foi possível enviar o arquivo para a impressora automaticamente: {ex.Message}. " +
                "Abra o arquivo e imprima manualmente.");
        }
    }
}
