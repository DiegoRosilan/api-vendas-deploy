using GestorPDV.Application.Vendas;

namespace GestorPDV.Wpf;

// Mesma ideia de CadastroRepositorios: agrupa as dependências da tela de
// venda para simplificar a passagem entre o composition root e o
// ShellViewModel — não contém lógica própria.
public class VendaContexto
{
    public IVendaService VendaService { get; }
    public IVendaRepository VendaRepository { get; }

    public VendaContexto(IVendaService vendaService, IVendaRepository vendaRepository)
    {
        VendaService = vendaService;
        VendaRepository = vendaRepository;
    }
}
