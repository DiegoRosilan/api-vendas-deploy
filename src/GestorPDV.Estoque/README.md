# GestorPDV.Estoque

Movimentação e saldo de estoque (RN-EST-001/002/003), implementado na
Fase 6 na medida do necessário para a venda:

- `Servicos/EstoqueService.cs`: baixa o estoque na finalização da venda
  (produtos com `controla_estoque = false` não geram movimentação) e
  estorna todas as movimentações de um documento no cancelamento —
  sempre lançando o movimento inverso (`est_movimentacao` tipo `estorno`),
  nunca apagando o histórico original.

Grade, lote, serial, transferência entre filiais, inventário e promoção
(RN-EST-004/005/006, `est_promocao`) ainda não têm cadastro nem UI — ver
`docs/ROADMAP.md`.
