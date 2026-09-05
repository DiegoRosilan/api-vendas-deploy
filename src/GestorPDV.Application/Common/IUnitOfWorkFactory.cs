namespace GestorPDV.Application.Common;

// Cria unidades de trabalho para operações que precisam gravar em mais de
// um repositório dentro da mesma transação (ex.: finalizar venda grava
// mv_venda, baixa de estoque e comissão de uma vez — item 3 do escopo).
public interface IUnitOfWorkFactory
{
    IUnitOfWork Criar();
}
