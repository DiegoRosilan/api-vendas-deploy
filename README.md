# GestorPDV

ERP/PDV completo para varejo em geral, oficina mecânica, supermercado,
farmácia, armarinho, bar/restaurante, loja de roupas, indústria e segmentos
similares — C#, .NET 9, WPF e PostgreSQL.

Este repositório está na **Fase 10** do desenvolvimento (de 11 fases — ver
`docs/ROADMAP.md`): análise/arquitetura, banco de dados, estrutura do
projeto, login, cadastros, vendas, pagamentos, relatórios, impressão e
testes concluídos — os fluxos críticos (login, venda, caixa, financeiro,
relatórios) já foram exercitados de ponta a ponta contra um PostgreSQL
real, o que encontrou e corrigiu dois bugs (ver `docs/ROADMAP.md`, Fase
10). Falta só testar as telas do `GestorPDV.Wpf` numa máquina Windows
(WPF não compila fora do Windows) e a publicação final.

## Documentação

- `docs/ARQUITETURA.md` — arquitetura em camadas, tecnologia e mapa de módulos.
- `docs/ROADMAP.md` — status de cada fase e suposições registradas.
- `database/README.md` — estrutura do banco e como criar o schema.

## Tecnologia

C# · .NET 9 (WPF) · PostgreSQL · Npgsql (SQL parametrizado, sem ORM) ·
FastReport (a partir da Fase 8).

## Pré-requisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Visual Studio 2022 (17.12+) com a carga de trabalho **.NET desktop
  development** (necessária para compilar `GestorPDV.Wpf`, que só compila em
  Windows) — ou `dotnet build`/`dotnet test` pela linha de comando para os
  demais projetos, que são multiplataforma.
- PostgreSQL 14+ acessível pela rede (local ou remoto).

## Configurando o PostgreSQL

```bash
# 1. Criar o banco
psql -h <host> -U <usuario> -d postgres -c "CREATE DATABASE gestordb;"

# 2. Criar o schema (todas as tabelas)
psql -h <host> -U <usuario> -d gestordb -f database/schema/run.sql

# 3. Carregar dados iniciais (filial, usuário admin, formas de pagamento, etc.)
psql -h <host> -U <usuario> -d gestordb -f database/seed/seed_inicial.sql
```

Veja `database/README.md` para detalhes de cada script e a convenção de
nomes das tabelas. A aplicação também tenta criar o schema automaticamente
na inicialização (`GestorPDV.Infrastructure.Database.DatabaseInitializer`),
então os passos 2/3 acima são um atalho, não uma etapa obrigatória separada
— mas rodá-los manualmente antes do primeiro build é recomendado para
confirmar que a conexão e as credenciais estão corretas.

## Configurando a conexão da aplicação

Edite `src/GestorPDV.Wpf/appsettings.json`:

```json
{
  "Database": {
    "Host": "localhost",
    "Port": 5432,
    "Database": "gestordb",
    "Username": "postgres",
    "Password": "",
    "TimeoutSegundos": 15
  }
}
```

Em vez de colocar a senha no `appsettings.json` (não recomendado fora de
desenvolvimento local), defina a variável de ambiente
`GESTORPDV_Database__Password` (ou crie um `appsettings.Local.json`, que já
está no `.gitignore`) — a aplicação lê configuração externa com precedência
`appsettings.json` → `appsettings.{Environment}.json` → variáveis de
ambiente `GESTORPDV_*`.

## Compilando

Em uma máquina Windows com o SDK instalado, `dotnet build GestorPDV.sln`
compila a solução inteira, WPF incluído. Fora do Windows, o `.sln` **não**
compila de uma vez (o `GestorPDV.Wpf.csproj` tem `net9.0-windows` como alvo,
e o SDK recusa isso fora do Windows) — compile os projetos individualmente,
o que cobre tudo menos a interface:

```bash
dotnet restore GestorPDV.sln

# Os 10 projetos que não são WPF + os testes (funciona em qualquer SO):
for p in Domain Application Infrastructure Data.Postgres Vendas Estoque \
         Financeiro Caixa Fiscal Relatorios Tests; do
  dotnet build "src/GestorPDV.$p/GestorPDV.$p.csproj" -c Release
done

# No Windows, com Visual Studio ou dotnet CLI, compila também a interface WPF:
dotnet build src/GestorPDV.Wpf/GestorPDV.Wpf.csproj -c Release
```

> **Nota sobre este ambiente de desenvolvimento**: a partir da Fase 8 este
> ambiente passou a ter o SDK do .NET disponível (antes não tinha —
> rede/instaladores bloqueados), então os 10 projetos acima e
> `GestorPDV.Tests` já são compilados e testados de verdade
> (`dotnet build`/`dotnet test`, 0 erros, todos os testes passando) a cada
> fase, e não apenas revisados manualmente. Só `GestorPDV.Wpf` continua sem
> poder ser compilado até o fim aqui: o código C# das telas compila
> normalmente, mas a etapa de XAML→BAML do WPF é Windows-only mesmo com
> `EnableWindowsTargeting=true` — uma limitação conhecida da própria
> ferramenta, não do código (detalhes em `docs/ARQUITETURA.md`, seção 9).
> A compilação final do `GestorPDV.Wpf` continua fazendo parte da
> **Fase 10 (testes)**, mas com muito mais confiança agora.

## Executando

```bash
dotnet run --project src/GestorPDV.Wpf/GestorPDV.Wpf.csproj
```

Ao iniciar, a aplicação valida a conexão com o PostgreSQL e cria
automaticamente qualquer tabela que estiver faltando. Com o banco OK, a
aplicação leva para a tela de login; pagamentos completos, relatórios e
impressão chegam nas próximas fases.

Se você rodou `database/seed/seed_inicial.sql`, o primeiro acesso é
`admin` / `admin123` — a aplicação exige a troca dessa senha no primeiro
login (`sec_usuario.exige_troca_senha`). O seed também cria um funcionário
vendedor vinculado ao `admin` e dois produtos de exemplo com estoque, para
a tela de Venda funcionar imediatamente após o setup.

## Testes

```bash
dotnet test src/GestorPDV.Tests/GestorPDV.Tests.csproj
```

## Publicando (Fase 11)

Instruções detalhadas de publicação (self-contained x framework-dependent,
gerar o instalador) serão adicionadas quando a Fase 11 for implementada.
Como referência inicial, o comando padrão do .NET para gerar um executável
autocontido no Windows é:

```bash
dotnet publish src/GestorPDV.Wpf/GestorPDV.Wpf.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## Estrutura da solução

```
GestorPDV.sln
src/
  GestorPDV.Domain           entidades e enums, sem dependências externas
  GestorPDV.Application      interfaces (portas) e DTOs
  GestorPDV.Infrastructure   configuração, segurança, inicialização de banco
  GestorPDV.Data.Postgres    repositórios Npgsql (SQL parametrizado)
  GestorPDV.Vendas           motor de vendas/orçamento/pedido (Fase 6)
  GestorPDV.Estoque          movimentação e saldos de estoque (Fase 6)
  GestorPDV.Financeiro       contas a receber/pagar, renegociação (Fase 7)
  GestorPDV.Caixa            abertura/fechamento de caixa (Fase 7)
  GestorPDV.Fiscal           motor tributário (Fases 6/8)
  GestorPDV.Relatorios       integração FastReport (Fase 8)
  GestorPDV.Wpf              interface (MVVM) — composition root
  GestorPDV.Tests            testes de regras de negócio (xUnit)
database/
  schema/                    DDL PostgreSQL, idempotente, banco gestordb
  seed/                      dados iniciais mínimos
docs/
  ARQUITETURA.md             Fase 1: análise e arquitetura
  ROADMAP.md                 status das fases e suposições registradas
```

Ver `docs/ARQUITETURA.md` para o detalhamento da arquitetura em camadas e o
mapeamento de cada módulo de negócio para seu projeto correspondente.
