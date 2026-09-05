using GestorPDV.Application.Caixa;

namespace GestorPDV.Wpf;

public class CaixaContexto
{
    public ICaixaService CaixaService { get; }
    public ICaixaRepository CaixaRepository { get; }

    public CaixaContexto(ICaixaService caixaService, ICaixaRepository caixaRepository)
    {
        CaixaService = caixaService;
        CaixaRepository = caixaRepository;
    }
}
