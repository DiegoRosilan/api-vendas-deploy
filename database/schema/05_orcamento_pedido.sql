-- GestorPDV — Orçamento e Pedido de Venda (RN-ORC-001, RN-PED-001).
-- Idempotente: pode ser executado múltiplas vezes sem erro.

CREATE TABLE IF NOT EXISTS mv_orcamento_situacao (
    id              BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    codigo          VARCHAR(20) NOT NULL UNIQUE,
    descricao       VARCHAR(60) NOT NULL
);

CREATE TABLE IF NOT EXISTS mv_orcamento (
    id                      BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    numero                  BIGINT NOT NULL,
    filial_id               BIGINT NOT NULL REFERENCES cad_filial(id),
    cliente_id              BIGINT REFERENCES cad_cliente(id),
    vendedor_id             BIGINT NOT NULL REFERENCES cad_funcionario(id),
    situacao_id             BIGINT NOT NULL REFERENCES mv_orcamento_situacao(id),
    condicao_pagamento_id   BIGINT REFERENCES cad_condicao_pagamento(id),
    data_orcamento          TIMESTAMPTZ NOT NULL DEFAULT now(),
    validade                DATE,
    subtotal                NUMERIC(14,2) NOT NULL DEFAULT 0,
    desconto                NUMERIC(14,2) NOT NULL DEFAULT 0,
    acrescimo               NUMERIC(14,2) NOT NULL DEFAULT 0,
    total                   NUMERIC(14,2) NOT NULL DEFAULT 0,
    venda_id                BIGINT REFERENCES mv_venda(id),
    UNIQUE (filial_id, numero)
);

CREATE TABLE IF NOT EXISTS mv_orcamento_produto (
    id                  BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    orcamento_id        BIGINT NOT NULL REFERENCES mv_orcamento(id) ON DELETE CASCADE,
    produto_id          BIGINT REFERENCES cad_produto(id),
    servico_id          BIGINT REFERENCES cad_servico(id),
    produto_grade_id    BIGINT REFERENCES est_produto_grade(id),
    quantidade          NUMERIC(14,3) NOT NULL,
    valor_unitario      NUMERIC(14,4) NOT NULL,
    desconto            NUMERIC(14,2) NOT NULL DEFAULT 0,
    acrescimo           NUMERIC(14,2) NOT NULL DEFAULT 0,
    total               NUMERIC(14,2) NOT NULL,
    CHECK (produto_id IS NOT NULL OR servico_id IS NOT NULL)
);

CREATE TABLE IF NOT EXISTS mv_orcamento_parcela (
    id              BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    orcamento_id    BIGINT NOT NULL REFERENCES mv_orcamento(id) ON DELETE CASCADE,
    numero_parcela  INTEGER NOT NULL,
    valor           NUMERIC(14,2) NOT NULL,
    vencimento      DATE NOT NULL
);

CREATE TABLE IF NOT EXISTS mv_pedido_venda_situacao (
    id              BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    codigo          VARCHAR(20) NOT NULL UNIQUE,
    descricao       VARCHAR(60) NOT NULL
);

CREATE TABLE IF NOT EXISTS mv_pedido_venda (
    id                      BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    numero                  BIGINT NOT NULL,
    filial_id               BIGINT NOT NULL REFERENCES cad_filial(id),
    cliente_id              BIGINT REFERENCES cad_cliente(id),
    vendedor_id             BIGINT NOT NULL REFERENCES cad_funcionario(id),
    situacao_id             BIGINT NOT NULL REFERENCES mv_pedido_venda_situacao(id),
    condicao_pagamento_id   BIGINT REFERENCES cad_condicao_pagamento(id),
    data_pedido             TIMESTAMPTZ NOT NULL DEFAULT now(),
    previsao_entrega        DATE,
    subtotal                NUMERIC(14,2) NOT NULL DEFAULT 0,
    desconto                NUMERIC(14,2) NOT NULL DEFAULT 0,
    acrescimo               NUMERIC(14,2) NOT NULL DEFAULT 0,
    total                   NUMERIC(14,2) NOT NULL DEFAULT 0,
    venda_id                BIGINT REFERENCES mv_venda(id),
    UNIQUE (filial_id, numero)
);

CREATE TABLE IF NOT EXISTS mv_pedido_venda_produto (
    id                  BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    pedido_id           BIGINT NOT NULL REFERENCES mv_pedido_venda(id) ON DELETE CASCADE,
    produto_id          BIGINT REFERENCES cad_produto(id),
    servico_id          BIGINT REFERENCES cad_servico(id),
    quantidade          NUMERIC(14,3) NOT NULL,
    valor_unitario      NUMERIC(14,4) NOT NULL,
    desconto            NUMERIC(14,2) NOT NULL DEFAULT 0,
    acrescimo           NUMERIC(14,2) NOT NULL DEFAULT 0,
    total               NUMERIC(14,2) NOT NULL,
    CHECK (produto_id IS NOT NULL OR servico_id IS NOT NULL)
);

CREATE TABLE IF NOT EXISTS mv_pedido_venda_produto_grade (
    id                          BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    pedido_venda_produto_id     BIGINT NOT NULL REFERENCES mv_pedido_venda_produto(id) ON DELETE CASCADE,
    produto_grade_id            BIGINT NOT NULL REFERENCES est_produto_grade(id),
    quantidade                  NUMERIC(14,3) NOT NULL
);

CREATE TABLE IF NOT EXISTS mv_pedido_venda_produto_lote (
    id                          BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    pedido_venda_produto_id     BIGINT NOT NULL REFERENCES mv_pedido_venda_produto(id) ON DELETE CASCADE,
    produto_lote_id             BIGINT NOT NULL REFERENCES est_produto_lote(id),
    quantidade                  NUMERIC(14,3) NOT NULL
);

CREATE TABLE IF NOT EXISTS mv_pedido_venda_produto_serial (
    id                          BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    pedido_venda_produto_id     BIGINT NOT NULL REFERENCES mv_pedido_venda_produto(id) ON DELETE CASCADE,
    produto_serial_id           BIGINT NOT NULL REFERENCES est_produto_serial(id)
);

CREATE TABLE IF NOT EXISTS mv_pedido_venda_parcela (
    id              BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    pedido_id       BIGINT NOT NULL REFERENCES mv_pedido_venda(id) ON DELETE CASCADE,
    numero_parcela  INTEGER NOT NULL,
    valor           NUMERIC(14,2) NOT NULL,
    vencimento      DATE NOT NULL
);
