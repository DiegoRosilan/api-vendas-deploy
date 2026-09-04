# GestorPDV — Roteiro de Fases

Controle de progresso do desenvolvimento em etapas (item 11 do escopo). Cada
fase só avança se a anterior não tiver erro crítico pendente.

## Concluídas

- **Fase 1 — Análise e arquitetura**: `docs/ARQUITETURA.md`.
- **Fase 2 — Banco de dados**: scripts em `database/schema/`, executados por
  `database/schema/run.sql` ou automaticamente pelo `DatabaseInitializer` na
  inicialização da aplicação. Ver `database/README.md`.
- **Fase 3 — Estrutura do projeto**: solução `.NET` em `src/` com os
  projetos `Domain`, `Application`, `Infrastructure`, `Data.Postgres`,
  `Vendas`, `Estoque`, `Financeiro`, `Caixa`, `Fiscal`, `Relatorios`, `Wpf`
  e `Tests`. Inclui configuração externa de conexão, validação de conexão e
  de schema na inicialização, e uma janela WPF inicial que reporta o status
  dessa validação (sem telas de negócio ainda).

## Pendentes

- **Fase 4 — Login**: tela de autenticação, hash/validação de senha,
  carregamento de permissões (`sec_usuario`, `sec_permissao`) e bloqueio de
  ações conforme perfil.
- **Fase 5 — Cadastros**: telas e serviços de produtos, clientes,
  fornecedores, funcionários, filiais, formas/condições de pagamento e
  tabelas de preço, sobre as tabelas já criadas na Fase 2.
- **Fase 6 — Vendas**: fluxo completo de inclusão de item, cálculo de
  subtotal/total/desconto/acréscimo, promoção/tabela de preço, baixa de
  estoque, pré-venda/orçamento/pedido, devolução e cancelamento
  (rotinas `CalculaItemTotal`, `CalculaSubTotal`, `CalculaTotal`,
  `CalculaDesconto`, `CalculaAcrescimo`, `CalculaPrecoVenda`,
  `BaixarEstoque`, `CancelarVenda`, `Estornar`).
- **Fase 7 — Pagamentos**: múltiplas formas de pagamento por venda,
  parcelamento, dados de cartão/Pix, juros/multa/renegociação, caixa
  (`FinalizaFormaPagamento`, `CalculaPagamentos`, `Renegocia`).
- **Fase 8 — Relatórios**: integração FastReport lendo diretamente do
  PostgreSQL (vendas, estoque, financeiro, fiscal).
- **Fase 9 — Impressão**: impressão de cupom/relatórios e tratamento de
  erros de impressora.
- **Fase 10 — Testes**: compilação real em ambiente com .NET 9 SDK/Visual
  Studio, testes das regras de negócio em `GestorPDV.Tests`, validação de
  cálculos e conexões, correção de problemas encontrados.
- **Fase 11 — Publicação**: publish do `GestorPDV.Wpf` (self-contained ou
  framework-dependent) e instruções finais de instalação.

## Suposições registradas (a validar contra o sistema de referência)

Estes pontos foram implementados com a interpretação mais comum de mercado,
por não haver, ainda, SQL exata, prints ou mensagens de erro do executável de
referência que permitam confirmar a regra exata (ver seção 7 de
`Especificacao_Regras_Negocio_GestorPDV`):

1. Arredondamento monetário: 2 casas decimais, `MidpointRounding.AwayFromZero`.
2. Autorização de desconto acima do limite: será modelada como permissão de
   usuário (`sec_permissao` = `AUTORIZAR_DESCONTO`) na Fase 6.
3. Momento da baixa de estoque: na finalização da venda (não na inclusão do
   item), com possibilidade de baixa em tempo real configurável por parâmetro
   (`BaixarEstoqueRealTime`), a implementar na Fase 6.
4. Prioridade de preço: preço manual > promoção vigente > tabela de preço da
   filial > preço de tabela padrão do produto — a confirmar na Fase 6.
5. Bloqueio de cliente inadimplente: comparação de dias de atraso da parcela
   mais antiga em aberto contra `bloquear_venda_dias_vencido`, a implementar
   na Fase 5/6.
6. Regras fiscais completas (ICMS-ST, DIFAL, IBS/CBS) exigem tabelas de
   alíquota por NCM/UF que serão carregadas via cadastro (Fase 8+), não
   fixadas em código.

Qualquer arquivo adicional do sistema de referência (SQL exato, prints,
mensagens de erro) enviado posteriormente deve ser usado para corrigir estas
suposições antes da Fase 10.
