using GestorPDV.Application.Financeiro;

namespace GestorPDV.Wpf;

public class FinanceiroContexto
{
    public IFinanceiroService FinanceiroService { get; }
    public IFinanceiroRepository FinanceiroRepository { get; }

    public FinanceiroContexto(IFinanceiroService financeiroService, IFinanceiroRepository financeiroRepository)
    {
        FinanceiroService = financeiroService;
        FinanceiroRepository = financeiroRepository;
    }
}
