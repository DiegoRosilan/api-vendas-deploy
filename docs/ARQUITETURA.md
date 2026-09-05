# GestorPDV — Arquitetura do Sistema

Documento de Fase 1 (Análise e Arquitetura) do desenvolvimento do GestorPDV, um
ERP/PDV completo para varejo em geral, oficinas mecânicas, supermercados,
farmácias, armarinhos, bares e restaurantes, lojas de roupas, indústria e
segmentos similares.

Este documento é a base para as fases seguintes (banco de dados, estrutura de
projeto, login, cadastros, vendas, pagamentos, relatórios, impressão, testes e
publicação) e reflete as regras consolidadas em
`Especificacao_Regras_Negocio_GestorPDV` (rotinas, tabelas e regras
identificadas a partir do executável de referência `Gestores.exe`).

## 1. Objetivo

Substituir o executável de referência por um sistema novo, funcional e
compilável, preservando as regras de negócio identificadas (vendas, estoque,
financeiro, caixa e fiscal), com uma arquitetura em camadas que mantenha as
regras fora das telas.

## 2. Tecnologia

| Item          | Escolha                                   |
|---------------|--------------------------------------------|
| Linguagem     | C#                                          |
| Framework     | .NET 9 (`net9.0` / `net9.0-windows` para a UI) |
| Interface     | WPF (MVVM)                                  |
| Banco de dados| PostgreSQL (banco `gestordb`)               |
| Acesso a dados| Npgsql (ADO.NET), SQL parametrizado, sem ORM |
| Relatórios    | FastReport (integrado na Fase 8)            |
| IDE           | Visual Studio 2022+ (SDK .NET 9)            |

Não é usado um ORM (EF Core, Dapper etc.): o item 8 do escopo exige consultas
SQL parametrizadas e controle explícito de transações em operações críticas
(venda, baixa de estoque, caixa, financeiro), o que é mais previsível com
ADO.NET puro sobre Npgsql.

## 3. Visão em camadas

```
GestorPDV.Wpf              (apresentação: Views, ViewModels, MVVM)
        │
GestorPDV.Application       (casos de uso, orquestração, DTOs)
        │
        ├── GestorPDV.Vendas       (motor de cálculo e fluxo de venda/orçamento/pedido)
        ├── GestorPDV.Estoque      (movimentação, saldos, grade/lote/serial, transferência)
        ├── GestorPDV.Financeiro   (parcelas, baixas, juros, multa, renegociação)
        ├── GestorPDV.Caixa        (abertura, movimento, sangria, suprimento, fechamento)
        └── GestorPDV.Fiscal       (motor tributário: ICMS, ICMS-ST, PIS/COFINS, IPI, ISS, DIFAL, FCP, IBS/CBS, CFOP)
        │
GestorPDV.Domain             (entidades, enums e regras centrais, sem dependências externas)
        │
GestorPDV.Infrastructure      (configuração, segurança, inicialização/validação de banco)
        │
GestorPDV.Data.Postgres        (repositórios Npgsql, SQL parametrizado, transações)
        │
GestorPDV.Relatorios             (integração FastReport — Fase 8)

GestorPDV.Tests                   (testes de regras de negócio — xUnit)
```

Regra de dependência: `Domain` não depende de nada; `Application` e os
projetos de módulo (`Vendas`, `Estoque`, `Financeiro`, `Caixa`, `Fiscal`)
dependem de `Domain`; `Infrastructure` e `Data.Postgres` implementam
interfaces definidas em `Application`/`Domain`; `Wpf` depende apenas de
`Application` (nunca diretamente de `Data.Postgres`). Isso mantém a lógica de
negócio fora dos formulários, conforme exigido no item 7 do escopo.

Dentro de `GestorPDV.Wpf`, a separação interna segue: `Models` (modelos de
tela), `Views` (XAML), `ViewModels` (MVVM, chamam a camada `Application`),
`Helpers` (conversores, utilidades de UI). `Configuracoes` fica em
`Infrastructure/Configuration` e `Segurança` (hash de senha, permissões) em
`Infrastructure/Security`.

## 4. Módulos funcionais (mapa de negócio → projeto)

| Módulo (regra de negócio)                                   | Projeto responsável              |
|---------------------------------------------------------------|-----------------------------------|
| Cadastros: pessoas/clientes, produtos, serviços, fornecedores, funcionários, filiais, formas/condições de pagamento, tabelas de preço | `Application` + `Domain` (entidades) |
| Vendas, pré-venda, orçamento, pedido, devolução, cancelamento | `GestorPDV.Vendas` |
| Estoque: entrada, saída, transferência, perda, inventário, lote, grade, serial, promoção, tabela de preço | `GestorPDV.Estoque` |
| Financeiro: contas a receber/pagar, parcelas, baixas, juros, multas, renegociação | `GestorPDV.Financeiro` |
| Caixa: abertura, movimentação, sangria, suprimento, conferência, fechamento | `GestorPDV.Caixa` |
| Fiscal: NFC-e, NF-e, CFOP, ICMS, ICMS-ST, PIS, COFINS, IPI, ISS, DIFAL, FCP, IBS/CBS | `GestorPDV.Fiscal` |
| Comissão (vendedor/gerente)                                   | `GestorPDV.Vendas` (calculado a partir da venda) |
| Segurança: usuários, permissões, bloqueio de ações              | `Infrastructure.Security` + `Application` |
| Relatórios (vendas, estoque, financeiro, fiscal, impressões)  | `GestorPDV.Relatorios` (FastReport) |

## 5. Banco de dados

- Banco: `gestordb` em PostgreSQL.
- String de conexão em configuração externa (`appsettings.json` +
  variáveis de ambiente, nunca hard-coded), lida por
  `GestorPDV.Infrastructure.Configuration`.
- Na inicialização, `DatabaseInitializer` (`Infrastructure/Database`):
  1. valida a conexão com o PostgreSQL;
  2. verifica em `information_schema.tables` se as tabelas esperadas existem;
  3. executa os scripts de `database/schema/*.sql` (idempotentes, com
     `CREATE TABLE IF NOT EXISTS`) para criar o que estiver faltando;
  4. reporta erros de forma clara em vez de deixar a aplicação subir com
     schema incompleto.
- Operações críticas (finalizar venda, baixar estoque, movimentar caixa,
  baixar parcela) são executadas dentro de transações (`NpgsqlTransaction`)
  no `Data.Postgres`, com rollback em caso de exceção.
- Nomenclatura de tabelas segue os prefixos já identificados no sistema de
  referência (`est_`, `mv_`, `crb_`, `ctp_`, `cx_`, `ecf_`, `fis_`, `sec_`,
  `cad_`) para preservar as regras de negócio existentes, conforme pedido no
  item 12 do escopo.
- Detalhamento completo das tabelas: ver `database/schema` e
  `database/README.md`.

## 6. Regras de negócio — rastreamento

As ~40 regras de negócio (RN-VEN, RN-EST, RN-FIN, RN-CAI, RN-PAG, RN-COM,
RN-FIS, RN-ORC, RN-PED, RN-CAN, RN-SEG, RN-PRE, RN-DRE) e as 20 rotinas
críticas listadas na especificação (`CalculaItemTotal`, `CalculaSubTotal`,
`CalculaTotal`, `CalculaDesconto`, `CalculaAcrescimo`, `CalculaPrecoVenda`,
`BaixarEstoque`, `BaixarEstoqueRealTime`, `FinalizaFormaPagamento`,
`CalculaPagamentos`, `CancelarVenda`, `Estornar`, `CalculaIcms`,
`CalculaIcmsST`, `CalculaPisCofins`, `CalculaIBS_CBS`, `RetornaCFOP`,
`VerificaSeCalculaDIFAL`, `CalculaComissao`, `Renegocia`) serão implementadas
como métodos de domínio/serviço nos projetos `Vendas`, `Estoque`,
`Financeiro`, `Caixa` e `Fiscal` a partir da Fase 6 em diante, com testes em
`GestorPDV.Tests`.

Pontos marcados como "a validar" na especificação (arredondamento exato,
condição de autorização de desconto, momento exato da baixa de estoque por
tipo de operação, prioridade entre tabela de preço/promoção/preço manual,
regras completas de comissão, fluxo exato de NFC-e/NF-e) serão implementados
com a interpretação mais comum de mercado e documentados como suposição no
código e no `ROADMAP.md`, para validação posterior contra o sistema de
referência quando houver dados adicionais (prints, mensagens de erro, SQL
exata) — conforme item 12 do escopo.

## 7. Segurança e qualidade de código

- Tratamento de exceções em todas as operações de banco/IO.
- `async`/`await` em toda a comunicação com PostgreSQL.
- Consultas sempre parametrizadas (`NpgsqlParameter`), nunca concatenação de
  string — mitiga SQL injection.
- Senhas com hash (BCrypt) e nunca armazenadas em texto puro.
- Comentários apenas onde esclarecem uma decisão não óbvia.

## 8. Roteiro de fases

| Fase | Conteúdo                              | Status |
|------|----------------------------------------|--------|
| 1    | Análise e arquitetura                  | Concluída (este documento) |
| 2    | Banco de dados                         | Concluída (`database/`) |
| 3    | Estrutura do projeto                   | Concluída (`src/`, `.sln`) |
| 4    | Login (usuários, permissões, acesso)   | Concluída |
| 5    | Cadastros (produtos, clientes, etc.)   | Concluída |
| 6    | Vendas                                 | Concluída |
| 7    | Pagamentos                             | Concluída |
| 8    | Relatórios (FastReport)                | Concluída |
| 9    | Impressão                              | Concluída |
| 10   | Testes                                 | Concluída neste ambiente (schema/seed/fluxos críticos contra PostgreSQL real); falta testar as telas WPF numa máquina Windows |
| 11   | Publicação                             | Pendente |

Ver `ROADMAP.md` para o detalhamento de cada fase concluída/pendente e o
registro de decisões/suposições tomadas ao longo do desenvolvimento.

## 9. Limitação conhecida deste ambiente

A partir da Fase 8, este ambiente passou a ter acesso ao SDK do .NET (10.0,
instalado via `apt`) e a rede liberada para o NuGet, o que permitiu compilar
e testar de verdade os 10 projetos que não são WPF (`Domain`, `Application`,
`Infrastructure`, `Data.Postgres`, `Vendas`, `Estoque`, `Financeiro`,
`Caixa`, `Fiscal`, `Relatorios`) e o projeto de testes (`dotnet build` +
`dotnet test`, 0 erros/0 avisos, todos os testes passando) — antes disso,
a validação era só manual (XML bem-formado, balanceamento de chaves,
conferência cruzada de assinaturas). Essa primeira compilação real já
encontrou e corrigiu dois bugs que a validação manual não pegou:

1. Um `class App : Application` em `GestorPDV.Wpf/App.xaml.cs` que não
   compilava: como o projeto de camada de aplicação se chama
   `GestorPDV.Application`, o nome simples `Application` resolvia para esse
   namespace (irmão de `GestorPDV.Wpf` sob o namespace comum `GestorPDV`) em
   vez da classe base do WPF — a mesma causa raiz da colisão de
   `GestorPDV.Domain.Caixa.Caixa` documentada no `ROADMAP.md` (Fase 7).
   Corrigido qualificando como `System.Windows.Application`.
2. As classes-dublê de teste (`UsuarioRepositoryFake`,
   `FinanceiroRepositoryFake` etc.) declaradas como `file class` não podiam
   aparecer em assinaturas de métodos de uma classe de teste que não fosse
   também `file`-scoped (erro `CS0118`/`CS9051`) — corrigido removendo o
   modificador `file`.

O projeto `GestorPDV.Wpf` compila normalmente até a etapa de XAML→BAML: o
código C# (ViewModels, code-behind, composition root) compila sem erros,
mas o passo de compilação de marcação (`MarkupCompilePass1/2`) falha com
erros de resolução de tipo local (`MC3050`/`MC3074`) mesmo usando
`-p:EnableWindowsTargeting=true` — uma limitação conhecida e documentada do
próprio `dotnet/wpf` ao tentar compilar XAML fora do Windows (o assembly
temporário usado para resolver tipos locais durante a geração do BAML tem
nome aleatório nessa plataforma). Isso não é um erro do código: os dois
tipos apontados pelo compilador (`DbStatusViewModel`,
`BoolParaVisibilidadeConverter`) existem, são públicos e estão no namespace
correto referenciado pelo XAML. A compilação final do `GestorPDV.Wpf`
(incluindo BAML) continua exigindo Visual Studio/uma máquina Windows com o
.NET 9 SDK — mas agora com muito mais confiança, já que todo o código C#
por trás das telas (inclusive o composition root em `App.xaml.cs`) já foi
compilado com sucesso neste ambiente.
