-- GestorPDV — Tabelas fiscais de referência: CFOP, CST/CSOSN, NCM, CEST,
-- alíquotas de ICMS e classificação IBS/CBS (RN-FIS-*).
-- Idempotente: pode ser executado múltiplas vezes sem erro.

CREATE TABLE IF NOT EXISTS fis_cfop (
    id              BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    codigo          VARCHAR(4) NOT NULL UNIQUE,
    descricao       VARCHAR(200) NOT NULL,
    tipo_operacao   VARCHAR(8) NOT NULL CHECK (tipo_operacao IN ('entrada','saida')),
    devolucao       BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE TABLE IF NOT EXISTS fis_csticms (
    id              BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    codigo          VARCHAR(3) NOT NULL UNIQUE,
    descricao       VARCHAR(200) NOT NULL
);

CREATE TABLE IF NOT EXISTS fis_csosn (
    id              BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    codigo          VARCHAR(3) NOT NULL UNIQUE,
    descricao       VARCHAR(200) NOT NULL
);

CREATE TABLE IF NOT EXISTS fis_cstpiscofins (
    id              BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    codigo          VARCHAR(2) NOT NULL UNIQUE,
    descricao       VARCHAR(200) NOT NULL
);

CREATE TABLE IF NOT EXISTS fis_cstipi (
    id              BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    codigo          VARCHAR(2) NOT NULL UNIQUE,
    descricao       VARCHAR(200) NOT NULL
);

CREATE TABLE IF NOT EXISTS fis_ncm (
    id              BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    codigo          VARCHAR(8) NOT NULL UNIQUE,
    descricao       VARCHAR(300) NOT NULL
);

CREATE TABLE IF NOT EXISTS fis_cest (
    id              BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    codigo          VARCHAR(7) NOT NULL UNIQUE,
    descricao       VARCHAR(300) NOT NULL,
    ncm_id          BIGINT REFERENCES fis_ncm(id)
);

CREATE TABLE IF NOT EXISTS fis_aliquota_icms (
    id              BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    uf_origem       CHAR(2) NOT NULL,
    uf_destino      CHAR(2) NOT NULL,
    aliquota_pct    NUMERIC(5,2) NOT NULL,
    fcp_aliquota_pct NUMERIC(5,2) NOT NULL DEFAULT 0,
    vigencia_inicio DATE NOT NULL,
    vigencia_fim    DATE,
    UNIQUE (uf_origem, uf_destino, vigencia_inicio)
);

CREATE TABLE IF NOT EXISTS fis_classificacao_ibs_cbs (
    id              BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    codigo          VARCHAR(10) NOT NULL UNIQUE,
    descricao       VARCHAR(200) NOT NULL
);

CREATE TABLE IF NOT EXISTS fis_cst_ibs_cbs (
    id              BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    codigo          VARCHAR(3) NOT NULL UNIQUE,
    descricao       VARCHAR(200) NOT NULL
);

-- Agora que as tabelas fiscais existem, liga as colunas de referência
-- criadas antecipadamente em vendas e notas de saída.
DO $$ BEGIN
    ALTER TABLE mv_venda_produto
        ADD CONSTRAINT fk_mv_venda_produto_cfop FOREIGN KEY (cfop_id) REFERENCES fis_cfop(id);
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

DO $$ BEGIN
    ALTER TABLE mv_nota_saida_itens
        ADD CONSTRAINT fk_mv_nota_saida_itens_cfop FOREIGN KEY (cfop_id) REFERENCES fis_cfop(id);
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

DO $$ BEGIN
    ALTER TABLE mv_nota_saida_itens
        ADD CONSTRAINT fk_mv_nota_saida_itens_cst_icms FOREIGN KEY (cst_icms_id) REFERENCES fis_csticms(id);
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;
