# GestorPDV.Vendas

Motor de cálculo e fluxo de venda (RN-VEN-*, RN-COM-001, RN-CAN-001),
implementado na Fase 6.

- `Calculos/CalculoVenda.cs`: funções puras de `CalculaItemTotal`,
  `CalculaSubTotal`, `CalculaTotal` e a validação de limite de desconto
  (RN-VEN-002/003/004/005) — testadas sem banco em
  `GestorPDV.Tests/Vendas/CalculoVendaTests.cs`.
- `Servicos/VendaService.cs`: orquestra iniciar venda, adicionar/remover
  item (produto ou serviço), resolver o preço (tabela de preço do cliente,
  quando houver, senão o preço padrão do produto — promoção fica para
  quando houver cadastro de promoções), finalizar (grava a venda, baixa o
  estoque via `IEstoqueService` e lança a comissão do vendedor, tudo numa
  única transação) e cancelar (estorna o estoque).

Pré-venda, orçamento, pedido de venda e devolução (RN-ORC-001, RN-PED-001)
ainda não foram implementados — ver `docs/ROADMAP.md`.
