# Banco de dados — GestorPDV

Scripts SQL do sistema (Fase 2). Banco alvo: PostgreSQL, base `gestordb`.

## Estrutura

- `schema/00_extensions.sql` … `schema/10_comissao_dre.sql` — DDL de todas as
  tabelas, na ordem correta de dependência. Todos os scripts usam
  `CREATE TABLE IF NOT EXISTS` / blocos `DO $$ ... EXCEPTION WHEN
  duplicate_object` para chaves estrangeiras adicionadas depois, então podem
  ser executados repetidamente sem erro — é o mesmo conjunto de scripts que
  `GestorPDV.Infrastructure` roda automaticamente na inicialização para criar
  o que estiver faltando (item 3 do escopo).
- `schema/run.sql` — executa todos os scripts acima, em ordem, via `psql`.
- `seed/seed_inicial.sql` — dados mínimos para o sistema funcionar após criar
  o schema: filial padrão, perfil/usuário administrador, formas e condições
  de pagamento, situações de orçamento/pedido e uma tabela fiscal de
  referência parcial (CFOP, CST ICMS, CSOSN, CST PIS/COFINS, CST IPI). Use
  como ponto de partida — as tabelas fiscais completas (NCM, CEST, alíquotas
  por UF) devem ser importadas via cadastro fiscal oficial na Fase 8+.

## Criando o banco

```bash
# 1. Criar o banco (uma vez)
createdb -h <host> -U <usuario> gestordb
# ou, via psql:
psql -h <host> -U <usuario> -d postgres -c "CREATE DATABASE gestordb;"

# 2. Criar o schema
psql -h <host> -U <usuario> -d gestordb -f database/schema/run.sql

# 3. (opcional, recomendado no primeiro setup) carregar dados iniciais
psql -h <host> -U <usuario> -d gestordb -f database/seed/seed_inicial.sql
```

O usuário administrador inicial é `admin` / `admin123` (o script já força
troca de senha no primeiro acesso, campo `exige_troca_senha`). Troque a senha
imediatamente em qualquer ambiente que não seja de desenvolvimento local.

## Convenção de nomes

Os prefixos de tabela preservam a nomenclatura identificada no sistema de
referência (`Especificacao_Regras_Negocio_GestorPDV`), para manter as regras
de negócio existentes sempre que possível (item 12 do escopo):

| Prefixo | Domínio |
|---------|---------|
| `sec_`  | Segurança (usuários, perfis, permissões) |
| `cad_`  | Cadastros (pessoas, produtos, serviços, formas/condições de pagamento, tabela de preço) |
| `est_`  | Estoque (saldo, movimentação, grade, lote, serial, transferência, promoção) |
| `mv_`   | Movimento de vendas (venda, orçamento, pedido, nota fiscal de saída) |
| `crb_` / `ctp_` / `fin_` | Financeiro (contas a receber/pagar, parcelas, renegociação) |
| `cx_` / `ecf_` | Caixa |
| `fis_`  | Fiscal (CFOP, CST, CSOSN, NCM, CEST, alíquotas, IBS/CBS) |
| `com_`  | Comissão |
| `dre_`  | Classificação financeira (DRE) |

## Alterando o schema

Como o `DatabaseInitializer` reexecuta estes scripts a cada inicialização,
qualquer alteração de estrutura deve ser feita criando um novo arquivo
numerado (ex.: `schema/11_ajuste_xxx.sql`) e adicionando-o a `run.sql`, em vez
de editar um script já aplicado em produção — scripts já aplicados só devem
ser editados enquanto o sistema ainda não tiver sido implantado em nenhum
ambiente real.
