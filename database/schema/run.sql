-- GestorPDV — executa todos os scripts de schema, em ordem, dentro do banco
-- gestordb. Todos os scripts são idempotentes (podem ser reexecutados).
-- Uso: psql -h <host> -U <usuario> -d gestordb -f database/schema/run.sql

\i 00_extensions.sql
\i 01_seguranca.sql
\i 02_cadastros.sql
\i 03_estoque.sql
\i 04_vendas.sql
\i 05_orcamento_pedido.sql
\i 06_nota_fiscal.sql
\i 07_financeiro.sql
\i 08_caixa.sql
\i 09_fiscal.sql
\i 10_comissao_dre.sql
