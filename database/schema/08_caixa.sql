-- GestorPDV — Caixa: abertura, movimentação, sangria, suprimento,
-- conferência e fechamento (RN-CAI-001).
-- Idempotente: pode ser executado múltiplas vezes sem erro.

CREATE TABLE IF NOT EXISTS cx_caixa (
    id                          BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    filial_id                   BIGINT NOT NULL REFERENCES cad_filial(id),
    usuario_abertura_id         BIGINT NOT NULL REFERENCES sec_usuario(id),
    data_abertura               TIMESTAMPTZ NOT NULL DEFAULT now(),
    valor_abertura               NUMERIC(14,2) NOT NULL DEFAULT 0,
    usuario_fechamento_id       BIGINT REFERENCES sec_usuario(id),
    data_fechamento             TIMESTAMPTZ,
    valor_fechamento_informado  NUMERIC(14,2),
    valor_fechamento_calculado  NUMERIC(14,2),
    diferenca                   NUMERIC(14,2),
    status                      VARCHAR(10) NOT NULL DEFAULT 'aberto'
        CHECK (status IN ('aberto','fechado'))
);

CREATE INDEX IF NOT EXISTS ix_cx_caixa_filial_status ON cx_caixa(filial_id, status);

CREATE TABLE IF NOT EXISTS cx_movimento (
    id                          BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    caixa_id                    BIGINT NOT NULL REFERENCES cx_caixa(id) ON DELETE CASCADE,
    tipo                        VARCHAR(15) NOT NULL
        CHECK (tipo IN ('venda','sangria','suprimento','recebimento','pagamento','estorno')),
    forma_pagamento_id          BIGINT REFERENCES cad_forma_pagamento(id),
    valor                       NUMERIC(14,2) NOT NULL,
    data_movimento              TIMESTAMPTZ NOT NULL DEFAULT now(),
    usuario_id                  BIGINT NOT NULL REFERENCES sec_usuario(id),
    documento_referencia_tipo   VARCHAR(30),
    documento_referencia_id     BIGINT,
    observacao                  VARCHAR(300),
    estornado                   BOOLEAN NOT NULL DEFAULT FALSE,
    data_estorno                TIMESTAMPTZ
);

CREATE INDEX IF NOT EXISTS ix_cx_movimento_caixa ON cx_movimento(caixa_id, tipo);

CREATE TABLE IF NOT EXISTS cx_conferencia (
    id                  BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    caixa_id            BIGINT NOT NULL REFERENCES cx_caixa(id) ON DELETE CASCADE,
    forma_pagamento_id  BIGINT NOT NULL REFERENCES cad_forma_pagamento(id),
    valor_sistema       NUMERIC(14,2) NOT NULL,
    valor_conferido     NUMERIC(14,2) NOT NULL,
    diferenca           NUMERIC(14,2) GENERATED ALWAYS AS (valor_conferido - valor_sistema) STORED,
    usuario_id          BIGINT NOT NULL REFERENCES sec_usuario(id),
    data_conferencia    TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Integração com impressora fiscal/ECF, quando existir (equipamento legado).
CREATE TABLE IF NOT EXISTS ecf_caixa (
    id              BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    filial_id       BIGINT NOT NULL REFERENCES cad_filial(id),
    numero_ecf      VARCHAR(20) NOT NULL,
    modelo          VARCHAR(60),
    numero_serie    VARCHAR(30),
    ativo           BOOLEAN NOT NULL DEFAULT TRUE
);
