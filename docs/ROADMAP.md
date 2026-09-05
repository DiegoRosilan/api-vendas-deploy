# GestorPDV — Roteiro de Fases

Controle de progresso do desenvolvimento em etapas (item 11 do escopo). Cada
fase só avança se a anterior não tiver erro crítico pendente.

**Nota (a partir da Fase 8)**: o ambiente de desenvolvimento passou a ter o
SDK do .NET disponível, permitindo `dotnet build`/`dotnet test` reais para
os 10 projetos que não são WPF e para `GestorPDV.Tests` (ver
`docs/ARQUITETURA.md`, seção 9, para o detalhamento e os dois bugs que essa
primeira compilação real encontrou e corrigiu nas Fases 4 e 7). A partir de
agora, cada fase inclui build/testes reais dessas camadas antes de ser
marcada como concluída; `GestorPDV.Wpf` continua exigindo Windows para a
etapa final de compilação de XAML (limitação da própria ferramenta WPF, não
do código).

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
- **Fase 4 — Login**: `AutenticacaoService` (`GestorPDV.Application.Seguranca`)
  valida usuário/senha (hash BCrypt via `IPasswordHasher`), bloqueia
  usuário inativo/bloqueado, carrega os códigos de permissão efetivos
  (perfil + sobreposição por usuário, RN-SEG-001) em `SessaoUsuario`, e
  implementa a troca de senha obrigatória (`exige_troca_senha`). A WPF
  (`GestorPDV.Wpf`) ganhou um `ShellViewModel` que navega entre a tela de
  status do banco, login, troca de senha e uma tela inicial pós-login que
  lista as permissões do usuário autenticado — ainda sem cadastros/vendas
  reais, que chegam nas próximas fases. Testado com dublês de repositório
  em `GestorPDV.Tests/Seguranca/AutenticacaoServiceTests.cs` (sem
  dependência de PostgreSQL).
- **Fase 5 — Cadastros**: CRUD completo (Application + Data.Postgres + WPF)
  para produtos, serviços, clientes, fornecedores, funcionários, filiais,
  formas de pagamento, condições de pagamento e tabelas de preço (cabeçalho).
  Cliente/Fornecedor/Funcionário compartilham o cadastro de pessoa
  (`cad_pessoa`) via `PessoaRepositoryHelper`, com inserção/atualização
  transacional (pessoa + registro específico em uma única transação). Uma
  tela "Cadastros" no Home (visível só com a permissão `CADASTRO_GERENCIAR`,
  RN-SEG-001) dá acesso às 9 telas, cada uma com lista + formulário de
  edição. Não há exclusão física em nenhum cadastro — desativar é editar o
  registro e desmarcar "Ativo", preservando o histórico de vendas/movimentos
  que referenciam esses registros.
- **Fase 6 — Vendas**: `CalculoVenda` (`GestorPDV.Vendas.Calculos`) implementa
  `CalculaItemTotal`/`CalculaSubTotal`/`CalculaTotal`/limite de desconto
  como funções puras, testadas sem banco em
  `GestorPDV.Tests/Vendas/CalculoVendaTests.cs`. `VendaService`
  (`GestorPDV.Vendas.Servicos`) orquestra iniciar venda, adicionar/remover
  item (produto ou serviço), resolver preço pela tabela do cliente,
  finalizar (grava mv_venda + itens + um pagamento único, baixa o estoque
  via `EstoqueService` e lança a comissão do vendedor — tudo numa
  transação via o novo `IUnitOfWorkFactory`) e cancelar (estorna o estoque
  lançando o movimento inverso, nunca apagando o original). Tela "Venda"
  no Home (permissão `VENDA_INCLUIR`) com busca de produto/serviço,
  carrinho, totais ao vivo, forma de pagamento e uma lista de vendas do
  dia com cancelamento (permissão `VENDA_CANCELAR`).
- **Fase 7 — Pagamentos**: `VendaService.FinalizarVendaAsync` passou a
  aceitar uma lista de `VendaPagamento` (RN-PAG-001), validando que a soma
  bate com o total da venda antes de abrir a transação. Para cada
  pagamento: se a forma `MovimentaCaixa`, lança um `MovimentoCaixa` do tipo
  `Venda` no caixa aberto da filial (`ICaixaRepository`,
  `GestorPDV.Caixa.Servicos.CaixaService` — RN-CAI-001); se `GeraFinanceiro`,
  gera um `DocumentoFinanceiro` com parcelas via `GeradorDocumentoFinanceiro`
  (helper compartilhado com `FinanceiroService`, RN-FIN-001), exigindo cliente
  informado e checando bloqueio por inadimplência (RN-CLI-001,
  `IFinanceiroService.VerificarBloqueioClienteAsync`) antes de finalizar.
  Cancelar venda agora também estorna os movimentos de caixa
  (`EstornarMovimentosPorDocumentoAsync`, lançando o inverso, nunca
  apagando) e cancela os documentos financeiros ainda em aberto gerados
  pela venda. Tela "Caixa" (permissão `CAIXA_ABRIR`) para abrir, registrar
  sangria/suprimento e fechar o caixa da filial, com o extrato de
  movimentos. Tela "Financeiro" (permissão `FINANCEIRO_BAIXAR`) lista as
  parcelas em aberto/parcial da filial com juros/multa por atraso já
  calculados (RN-FIN-002/003) e permite dar baixa individual de uma
  parcela. `FinanceiroService.CalcularEncargos` (função pura) e o fluxo de
  baixa são testados sem banco em
  `GestorPDV.Tests/Financeiro/FinanceiroServiceTests.cs`.

## Pendentes
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

1. Arredondamento monetário: 2 casas decimais, `MidpointRounding.AwayFromZero`
   — aplicado em `CalculoVenda` (Fase 6).
2. Autorização de desconto acima do limite: implementada como a permissão
   `VENDA_AUTORIZAR_DESCONTO` (`CalculoVenda.ValidarDesconto`, RN-VEN-005).
3. Momento da baixa de estoque: implementado na finalização da venda (não na
   inclusão do item). `BaixarEstoqueRealTime` (baixa item a item, antes de
   finalizar) não foi implementado — avaliar se é necessário quando houver
   caso de uso concreto que exija reservar estoque durante a montagem do
   carrinho.
4. Prioridade de preço (RN-VEN-006/007/008): implementado apenas
   "preço da tabela do cliente, senão preço padrão do produto"
   (`VendaService.ResolverPrecoAsync`). Promoção (RN-VEN-007) e preço manual
   editável no carrinho ainda não existem — dependem de um cadastro de
   promoções (`est_promocao`), que não foi construído.
5. Bloqueio de cliente inadimplente (RN-CLI-001,
   `bloquear_venda_dias_vencido`): implementado na Fase 7
   (`FinanceiroService.VerificarBloqueioClienteAsync`), calculado a partir
   do maior atraso entre as parcelas em aberto do cliente na data atual.
6. Regras fiscais completas (ICMS-ST, DIFAL, IBS/CBS, e mesmo o ICMS normal)
   não são calculadas na Fase 6: os campos fiscais de `mv_venda_produto`
   ficam em zero. Calcular impostos exige o motor tributário
   (`GestorPDV.Fiscal`), que ainda não foi implementado — nenhuma fase do
   escopo original (doc "Desenvolvimento de sistema completo") reserva uma
   fase dedicada a isso; entra como trabalho futuro quando solicitado.
7. Senha: mínimo de 6 caracteres (`AutenticacaoService.TamanhoMinimoSenha`) —
   sem outras regras de complexidade por ora. Trocar a senha exige informar a
   senha atual mesmo quando `exige_troca_senha` está ativo (nunca é permitido
   trocar às cegas), o que é mais seguro mas deve ser confirmado contra o
   comportamento do executável de referência.
8. Permissões: um código de permissão específico do usuário
   (`sec_usuario_permissao`) sempre sobrepõe o valor herdado do perfil
   (`sec_perfil_permissao`) para o mesmo código. Ainda não há tela para
   editar perfis/permissões em si (só o uso delas para liberar/bloquear
   botões) — avaliar se entra numa fase futura ou fica como manutenção
   direta no banco.
9. Tabela de preço: só o cabeçalho (`cad_tabela_preco`) tem tela de cadastro
   (Fase 5). A associação produto → preço (`cad_tabela_preco_item`) tem
   repositório completo (`ITabelaPrecoRepository`) e já é usada por
   `VendaService.ResolverPrecoAsync` na Fase 6, mas ainda não tem tela para
   o operador montar a tabela (adicionar/remover produtos e preços) — por
   ora isso só pode ser feito escrevendo direto na tabela.
10. Funcionário: o campo "usuário do sistema" (`cad_funcionario.usuario_id`)
    ainda não tem seletor na tela (não há listagem de usuários exposta por
    `IUsuarioRepository` hoje) — associar um funcionário a um login deve
    ser feito diretamente no banco até essa tela existir. O seed
    (`seed_inicial.sql`) já cria esse vínculo para o usuário `admin`, para
    a tela de Venda (Fase 6) funcionar sem esse passo manual.
11. Número da venda: gerado com `LOCK TABLE mv_venda IN SHARE ROW EXCLUSIVE
    MODE` dentro da transação de finalização, para evitar número duplicado
    por filial. Funciona bem para o volume esperado de um comércio
    pequeno/médio; se a concorrência de finalizações simultâneas crescer
    muito, trocar por uma sequência dedicada por filial.
12. Comissão (RN-COM-001): só a comissão padrão do vendedor
    (`cad_funcionario.comissao_padrao_pct`) sobre o total da venda. Regras
    de comissão de gerente sobre a equipe, faixas de comissão por
    valor/produto, etc. não foram implementadas.
13. Carrinho de venda: um único vendedor por venda, resolvido a partir do
    funcionário vinculado ao usuário logado — se não houver vínculo, a
    tela de Venda mostra aviso e não deixa iniciar.
14. Juros e multa (RN-FIN-002/003): multa fixa de 2% + juros de 0,033%/dia
    (~1% ao mês) sobre o valor da parcela, aplicados apenas quando a data de
    baixa é posterior ao vencimento (`FinanceiroService.CalcularEncargos`).
    Não há, ainda, SQL exata do sistema de referência para confirmar esses
    percentuais — ajustar quando essa informação existir.
15. Intervalo entre parcelas de uma venda a prazo: fixo em 30 dias
    (`VendaService.FinalizarVendaAsync` chama `GeradorDocumentoFinanceiro`
    com `intervaloDias = 30`), já que a condição de pagamento
    (`cad_condicao_pagamento`) ainda não está associada a cada pagamento
    da venda — apenas o número de parcelas informado pelo operador é usado.
16. Renegociação de dívida (RN-FIN-004, tabela `crb_renegociacao`) e contas
    a pagar (fornecedores) não foram implementadas na Fase 7 — a tela
    "Financeiro" cobre somente contas a receber geradas por venda.
17. Caixa: assume-se um único caixa aberto por filial por vez
    (`ICaixaRepository.ObterAbertoAsync`) — múltiplos caixas simultâneos
    (um por operador/PDV) não foram implementados. A conferência de caixa
    por forma de pagamento (`cx_conferencia`) tem tabela no schema mas
    ainda não tem tela; o fechamento registra apenas o valor total apurado
    informado pelo operador contra o saldo calculado.

Qualquer arquivo adicional do sistema de referência (SQL exato, prints,
mensagens de erro) enviado posteriormente deve ser usado para corrigir estas
suposições antes da Fase 10.
