-- GestorPDV — Documentos fiscais de saída: NFC-e/NF-e (RN-FIS-009, RN-FIS-010).
-- Idempotente: pode ser executado múltiplas vezes sem erro.

CREATE TABLE IF NOT EXISTS mv_nota_saida (
    id                      BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    numero                  BIGINT NOT NULL,
    serie                   VARCHAR(3) NOT NULL,
    modelo                  VARCHAR(2) NOT NULL CHECK (modelo IN ('55','65')),
    chave_acesso            VARCHAR(44),
    venda_id                BIGINT REFERENCES mv_venda(id),
    filial_id               BIGINT NOT NULL REFERENCES cad_filial(id),
    cliente_id              BIGINT REFERENCES cad_cliente(id),
    natureza_operacao       VARCHAR(60) NOT NULL,
    ambiente                VARCHAR(12) NOT NULL DEFAULT 'homologacao'
        CHECK (ambiente IN ('homologacao','producao')),
    status                  VARCHAR(20) NOT NULL DEFAULT 'pendente'
        CHECK (status IN ('pendente','autorizada','rejeitada','cancelada','inutilizada','contingencia')),
    protocolo_autorizacao   VARCHAR(30),
    data_emissao            TIMESTAMPTZ NOT NULL DEFAULT now(),
    data_autorizacao        TIMESTAMPTZ,
    data_cancelamento       TIMESTAMPTZ,
    motivo_cancelamento     VARCHAR(300),
    valor_total             NUMERIC(14,2) NOT NULL DEFAULT 0,
    valor_icms              NUMERIC(14,2) NOT NULL DEFAULT 0,
    valor_icms_st           NUMERIC(14,2) NOT NULL DEFAULT 0,
    valor_ipi               NUMERIC(14,2) NOT NULL DEFAULT 0,
    valor_pis               NUMERIC(14,2) NOT NULL DEFAULT 0,
    valor_cofins            NUMERIC(14,2) NOT NULL DEFAULT 0,
    valor_iss               NUMERIC(14,2) NOT NULL DEFAULT 0,
    xml_path                VARCHAR(300),
    UNIQUE (filial_id, modelo, serie, numero)
);

CREATE TABLE IF NOT EXISTS mv_nota_saida_itens (
    id              BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nota_saida_id   BIGINT NOT NULL REFERENCES mv_nota_saida(id) ON DELETE CASCADE,
    produto_id      BIGINT NOT NULL REFERENCES cad_produto(id),
    cfop_id         BIGINT,
    ncm             VARCHAR(8),
    cest            VARCHAR(7),
    quantidade      NUMERIC(14,3) NOT NULL,
    valor_unitario  NUMERIC(14,4) NOT NULL,
    valor_total     NUMERIC(14,2) NOT NULL,
    cst_icms_id     BIGINT,
    base_icms       NUMERIC(14,2) NOT NULL DEFAULT 0,
    valor_icms      NUMERIC(14,2) NOT NULL DEFAULT 0,
    base_icms_st    NUMERIC(14,2) NOT NULL DEFAULT 0,
    valor_icms_st   NUMERIC(14,2) NOT NULL DEFAULT 0,
    valor_ipi       NUMERIC(14,2) NOT NULL DEFAULT 0,
    valor_pis       NUMERIC(14,2) NOT NULL DEFAULT 0,
    valor_cofins    NUMERIC(14,2) NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS mv_nota_saida_grade (
    id                  BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nota_saida_item_id  BIGINT NOT NULL REFERENCES mv_nota_saida_itens(id) ON DELETE CASCADE,
    produto_grade_id    BIGINT NOT NULL REFERENCES est_produto_grade(id),
    quantidade          NUMERIC(14,3) NOT NULL
);

CREATE TABLE IF NOT EXISTS mv_nota_saida_lote (
    id                  BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nota_saida_item_id  BIGINT NOT NULL REFERENCES mv_nota_saida_itens(id) ON DELETE CASCADE,
    produto_lote_id     BIGINT NOT NULL REFERENCES est_produto_lote(id),
    quantidade          NUMERIC(14,3) NOT NULL
);

CREATE TABLE IF NOT EXISTS mv_nota_saida_serial (
    id                  BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nota_saida_item_id  BIGINT NOT NULL REFERENCES mv_nota_saida_itens(id) ON DELETE CASCADE,
    produto_serial_id   BIGINT NOT NULL REFERENCES est_produto_serial(id)
);

CREATE TABLE IF NOT EXISTS mv_nota_saida_parcela (
    id                  BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nota_saida_id       BIGINT NOT NULL REFERENCES mv_nota_saida(id) ON DELETE CASCADE,
    numero_parcela      INTEGER NOT NULL,
    valor               NUMERIC(14,2) NOT NULL,
    vencimento          DATE NOT NULL,
    forma_pagamento_id  BIGINT REFERENCES cad_forma_pagamento(id)
);

-- Controle de numeração/documentos fiscais utilizados, cancelados e
-- inutilizados, para detectar lacunas de sequência (RN-FIS-010).
CREATE TABLE IF NOT EXISTS fis_documento_controle (
    id              BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    filial_id       BIGINT NOT NULL REFERENCES cad_filial(id),
    modelo          VARCHAR(2) NOT NULL CHECK (modelo IN ('55','65')),
    serie           VARCHAR(3) NOT NULL,
    numero          BIGINT NOT NULL,
    status          VARCHAR(15) NOT NULL CHECK (status IN ('utilizado','cancelado','inutilizado')),
    data_registro   TIMESTAMPTZ NOT NULL DEFAULT now(),
    justificativa   VARCHAR(300),
    UNIQUE (filial_id, modelo, serie, numero)
);

-- Parâmetros de emissão fiscal por filial (ambiente, série, tentativas,
-- intervalo, timeout — RN-FIS-009).
CREATE TABLE IF NOT EXISTS fis_configuracao (
    id                          BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    filial_id                   BIGINT NOT NULL UNIQUE REFERENCES cad_filial(id),
    ambiente                    VARCHAR(12) NOT NULL DEFAULT 'homologacao'
        CHECK (ambiente IN ('homologacao','producao')),
    serie_nfce                  VARCHAR(3) NOT NULL DEFAULT '1',
    serie_nfe                   VARCHAR(3) NOT NULL DEFAULT '1',
    numero_atual_nfce           BIGINT NOT NULL DEFAULT 0,
    numero_atual_nfe            BIGINT NOT NULL DEFAULT 0,
    tentativas_envio            INTEGER NOT NULL DEFAULT 3,
    intervalo_tentativas_seg    INTEGER NOT NULL DEFAULT 5,
    timeout_seg                 INTEGER NOT NULL DEFAULT 30,
    certificado_path            VARCHAR(300),
    certificado_senha_criptografada VARCHAR(300)
);
