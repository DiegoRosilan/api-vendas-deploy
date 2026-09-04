-- GestorPDV — Financeiro: contas a receber/pagar, parcelas, baixas, juros,
-- multas e renegociação (RN-FIN-*, RN-CLI-001).
-- Idempotente: pode ser executado múltiplas vezes sem erro.

CREATE TABLE IF NOT EXISTS crb_documento (
    id                  BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    tipo                VARCHAR(10) NOT NULL CHECK (tipo IN ('receber','pagar')),
    pessoa_id           BIGINT NOT NULL REFERENCES cad_pessoa(id),
    filial_id           BIGINT NOT NULL REFERENCES cad_filial(id),
    numero_documento    VARCHAR(30) NOT NULL,
    valor_original      NUMERIC(14,2) NOT NULL,
    data_emissao        DATE NOT NULL DEFAULT CURRENT_DATE,
    data_vencimento     DATE NOT NULL,
    situacao            VARCHAR(15) NOT NULL DEFAULT 'aberto'
        CHECK (situacao IN ('aberto','parcial','baixado','cancelado','renegociado')),
    origem              VARCHAR(15) NOT NULL DEFAULT 'manual'
        CHECK (origem IN ('venda','manual','renegociacao')),
    venda_id            BIGINT REFERENCES mv_venda(id),
    observacao          VARCHAR(300)
);

CREATE INDEX IF NOT EXISTS ix_crb_documento_pessoa ON crb_documento(pessoa_id, situacao);
CREATE INDEX IF NOT EXISTS ix_crb_documento_vencimento ON crb_documento(data_vencimento);

CREATE TABLE IF NOT EXISTS fin_parcela (
    id              BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    documento_id    BIGINT NOT NULL REFERENCES crb_documento(id) ON DELETE CASCADE,
    numero_parcela  INTEGER NOT NULL,
    valor           NUMERIC(14,2) NOT NULL,
    vencimento      DATE NOT NULL,
    situacao        VARCHAR(15) NOT NULL DEFAULT 'aberto'
        CHECK (situacao IN ('aberto','parcial','baixado','cancelado')),
    UNIQUE (documento_id, numero_parcela)
);

-- Liga documentos financeiros à(s) venda(s) que os originaram (uma venda
-- parcelada pode gerar mais de um documento).
CREATE TABLE IF NOT EXISTS crb_documento_vendas (
    documento_id    BIGINT NOT NULL REFERENCES crb_documento(id) ON DELETE CASCADE,
    venda_id        BIGINT NOT NULL REFERENCES mv_venda(id) ON DELETE CASCADE,
    PRIMARY KEY (documento_id, venda_id)
);

CREATE TABLE IF NOT EXISTS crb_documento_baixa (
    id                  BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    documento_id        BIGINT NOT NULL REFERENCES crb_documento(id),
    valor_baixa         NUMERIC(14,2) NOT NULL,
    valor_juros         NUMERIC(14,2) NOT NULL DEFAULT 0,
    valor_multa         NUMERIC(14,2) NOT NULL DEFAULT 0,
    valor_desconto      NUMERIC(14,2) NOT NULL DEFAULT 0,
    data_baixa          TIMESTAMPTZ NOT NULL DEFAULT now(),
    forma_pagamento_id  BIGINT NOT NULL REFERENCES cad_forma_pagamento(id),
    usuario_id          BIGINT NOT NULL REFERENCES sec_usuario(id),
    estornado           BOOLEAN NOT NULL DEFAULT FALSE,
    data_estorno        TIMESTAMPTZ
);

CREATE TABLE IF NOT EXISTS crb_renegociacao (
    id                      BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    pessoa_id               BIGINT NOT NULL REFERENCES cad_pessoa(id),
    filial_id               BIGINT NOT NULL REFERENCES cad_filial(id),
    data_renegociacao       TIMESTAMPTZ NOT NULL DEFAULT now(),
    valor_original_total    NUMERIC(14,2) NOT NULL,
    valor_juros             NUMERIC(14,2) NOT NULL DEFAULT 0,
    valor_multa             NUMERIC(14,2) NOT NULL DEFAULT 0,
    valor_desconto          NUMERIC(14,2) NOT NULL DEFAULT 0,
    valor_final             NUMERIC(14,2) NOT NULL,
    usuario_id              BIGINT NOT NULL REFERENCES sec_usuario(id),
    status                  VARCHAR(15) NOT NULL DEFAULT 'ativa'
        CHECK (status IN ('ativa','estornada')),
    data_estorno            TIMESTAMPTZ
);

-- Documentos originais consolidados em uma renegociação.
CREATE TABLE IF NOT EXISTS crb_renegociacao_documento (
    renegociacao_id BIGINT NOT NULL REFERENCES crb_renegociacao(id) ON DELETE CASCADE,
    documento_id    BIGINT NOT NULL REFERENCES crb_documento(id),
    PRIMARY KEY (renegociacao_id, documento_id)
);

-- Novo contrato de parcelamento gerado a partir de uma renegociação.
CREATE TABLE IF NOT EXISTS ctp_renegociacao (
    id                  BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    renegociacao_id     BIGINT NOT NULL REFERENCES crb_renegociacao(id) ON DELETE CASCADE,
    numero_contrato     VARCHAR(30) NOT NULL,
    data_contrato       DATE NOT NULL DEFAULT CURRENT_DATE,
    valor_total         NUMERIC(14,2) NOT NULL,
    numero_parcelas     INTEGER NOT NULL,
    condicao_pagamento_id BIGINT REFERENCES cad_condicao_pagamento(id)
);

CREATE TABLE IF NOT EXISTS ctp_renegociacao_parcelas (
    id                      BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    ctp_renegociacao_id     BIGINT NOT NULL REFERENCES ctp_renegociacao(id) ON DELETE CASCADE,
    numero_parcela          INTEGER NOT NULL,
    valor                   NUMERIC(14,2) NOT NULL,
    vencimento              DATE NOT NULL,
    documento_id            BIGINT REFERENCES crb_documento(id),
    UNIQUE (ctp_renegociacao_id, numero_parcela)
);
