-- GestorPDV — executa todos os scripts de schema, em ordem, dentro do banco
-- gestordb. Todos os scripts são idempotentes (podem ser reexecutados).
-- Uso: psql -h <host> -U <usuario> -d gestordb -f database/schema/run.sql
--
-- Usa \ir (include relativo ao arquivo, não ao diretório de trabalho do
-- psql) em vez de \i: rodar o comando acima a partir da raiz do repositório
-- (como documentado no README) resolveria "00_extensions.sql" contra o
-- diretório onde o psql foi chamado, não onde este arquivo está, e falharia
-- com "No such file or directory".

\ir 00_extensions.sql
\ir 01_seguranca.sql
\ir 02_cadastros.sql
\ir 03_estoque.sql
\ir 04_vendas.sql
\ir 05_orcamento_pedido.sql
\ir 06_nota_fiscal.sql
\ir 07_financeiro.sql
\ir 08_caixa.sql
\ir 09_fiscal.sql
\ir 10_comissao_dre.sql
\ir 11_ajuste_financeiro.sql
