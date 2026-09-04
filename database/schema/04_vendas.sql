-- GestorPDV — Vendas: venda, itens, pagamentos, grade/lote/serial/técnico
-- (RN-VEN-*, RN-PAG-*, RN-CAN-001).
-- Idempotente: pode ser executado múltiplas vezes sem erro.

CREATE TABLE IF NOT EXISTS mv_venda (
    id                      BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    numero                  BIGINT NOT NULL,
    filial_id               BIGINT NOT NULL REFERENCES cad_filial(id),
    cliente_id              BIGINT REFERENCES cad_cliente(id),
    vendedor_id             BIGINT NOT NULL REFERENCES cad_funcionario(id),
    tipo                    VARCHAR(12) NOT NULL DEFAULT 'venda'
        CHECK (tipo IN ('venda','pre_venda')),
    status                  VARCHAR(15) NOT NULL DEFAULT 'aberta'
        CHECK (status IN ('aberta','finalizada','cancelada')),
    tabela_preco_id         BIGINT REFERENCES cad_tabela_preco(id),
    condicao_pagamento_id   BIGINT REFERENCES cad_condicao_pagamento(id),
    subtotal                NUMERIC(14,2) NOT NULL DEFAULT 0,
    desconto                NUMERIC(14,2) NOT NULL DEFAULT 0,
    acrescimo               NUMERIC(14,2) NOT NULL DEFAULT 0,
    total                   NUMERIC(14,2) NOT NULL DEFAULT 0,
    data_venda              TIMESTAMPTZ NOT NULL DEFAULT now(),
    data_cancelamento       TIMESTAMPTZ,
    motivo_cancelamento     VARCHAR(300),
    usuario_abertura_id     BIGINT NOT NULL REFERENCES sec_usuario(id),
    usuario_cancelamento_id BIGINT REFERENCES sec_usuario(id),
    UNIQUE (filial_id, numero)
);

CREATE TABLE IF NOT EXISTS mv_venda_produto (
    id                  BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    venda_id            BIGINT NOT NULL REFERENCES mv_venda(id) ON DELETE CASCADE,
    item_numero         INTEGER NOT NULL,
    produto_id          BIGINT REFERENCES cad_produto(id),
    servico_id          BIGINT REFERENCES cad_servico(id),
    quantidade          NUMERIC(14,3) NOT NULL,
    valor_unitario      NUMERIC(14,4) NOT NULL,
    valor_unitario_final NUMERIC(14,4) NOT NULL,
    desconto            NUMERIC(14,2) NOT NULL DEFAULT 0,
    acrescimo           NUMERIC(14,2) NOT NULL DEFAULT 0,
    subtotal            NUMERIC(14,2) NOT NULL,
    total               NUMERIC(14,2) NOT NULL,
    cfop_id             BIGINT,
    base_icms           NUMERIC(14,2) NOT NULL DEFAULT 0,
    valor_icms          NUMERIC(14,2) NOT NULL DEFAULT 0,
    base_icms_st        NUMERIC(14,2) NOT NULL DEFAULT 0,
    valor_icms_st       NUMERIC(14,2) NOT NULL DEFAULT 0,
    valor_ipi           NUMERIC(14,2) NOT NULL DEFAULT 0,
    valor_pis           NUMERIC(14,2) NOT NULL DEFAULT 0,
    valor_cofins        NUMERIC(14,2) NOT NULL DEFAULT 0,
    valor_iss           NUMERIC(14,2) NOT NULL DEFAULT 0,
    cancelado           BOOLEAN NOT NULL DEFAULT FALSE,
    CHECK (produto_id IS NOT NULL OR servico_id IS NOT NULL),
    UNIQUE (venda_id, item_numero)
);

CREATE TABLE IF NOT EXISTS mv_venda_grade (
    id                  BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    venda_produto_id    BIGINT NOT NULL REFERENCES mv_venda_produto(id) ON DELETE CASCADE,
    produto_grade_id    BIGINT NOT NULL REFERENCES est_produto_grade(id),
    quantidade          NUMERIC(14,3) NOT NULL
);

CREATE TABLE IF NOT EXISTS mv_venda_lote (
    id                  BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    venda_produto_id    BIGINT NOT NULL REFERENCES mv_venda_produto(id) ON DELETE CASCADE,
    produto_lote_id     BIGINT NOT NULL REFERENCES est_produto_lote(id),
    quantidade          NUMERIC(14,3) NOT NULL
);

CREATE TABLE IF NOT EXISTS mv_venda_serial (
    id                  BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    venda_produto_id    BIGINT NOT NULL REFERENCES mv_venda_produto(id) ON DELETE CASCADE,
    produto_serial_id   BIGINT NOT NULL REFERENCES est_produto_serial(id)
);

-- Técnico responsável por um item de serviço, quando diferente do vendedor
-- da venda (base para comissão de serviço em RN-COM-001).
CREATE TABLE IF NOT EXISTS mv_venda_tecnico (
    id                  BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    venda_produto_id    BIGINT NOT NULL REFERENCES mv_venda_produto(id) ON DELETE CASCADE,
    funcionario_id      BIGINT NOT NULL REFERENCES cad_funcionario(id),
    comissao_pct        NUMERIC(5,2) NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS mv_venda_pagamento (
    id                      BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    venda_id                BIGINT NOT NULL REFERENCES mv_venda(id) ON DELETE CASCADE,
    forma_pagamento_id      BIGINT NOT NULL REFERENCES cad_forma_pagamento(id),
    condicao_pagamento_id   BIGINT REFERENCES cad_condicao_pagamento(id),
    valor                   NUMERIC(14,2) NOT NULL,
    parcelas                INTEGER NOT NULL DEFAULT 1,
    cnpj_credenciadora      VARCHAR(14),
    nsu                     VARCHAR(30),
    nsu_pos                 VARCHAR(30),
    rede                    VARCHAR(40),
    pix_e2e                 VARCHAR(60),
    pix_txid                VARCHAR(60),
    status                  VARCHAR(15) NOT NULL DEFAULT 'confirmado'
        CHECK (status IN ('confirmado','cancelado','estornado'))
);

CREATE INDEX IF NOT EXISTS ix_mv_venda_cliente ON mv_venda(cliente_id);
CREATE INDEX IF NOT EXISTS ix_mv_venda_data ON mv_venda(data_venda);
