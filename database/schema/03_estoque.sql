-- GestorPDV — Estoque: saldo, movimentação, grade, lote, serial, local,
-- transferência, inventário e promoção (RN-EST-*).
-- Idempotente: pode ser executado múltiplas vezes sem erro.

CREATE TABLE IF NOT EXISTS est_local_estoque (
    id              BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    filial_id       BIGINT NOT NULL REFERENCES cad_filial(id),
    descricao       VARCHAR(100) NOT NULL,
    ativo           BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS est_produto_grade (
    id              BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    produto_id      BIGINT NOT NULL REFERENCES cad_produto(id) ON DELETE CASCADE,
    cor             VARCHAR(40),
    tamanho         VARCHAR(20),
    codigo_barras   VARCHAR(20),
    ativo           BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS est_produto_lote (
    id              BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    produto_id      BIGINT NOT NULL REFERENCES cad_produto(id) ON DELETE CASCADE,
    numero_lote     VARCHAR(40) NOT NULL,
    data_fabricacao DATE,
    data_validade   DATE,
    UNIQUE (produto_id, numero_lote)
);

CREATE TABLE IF NOT EXISTS est_produto_serial (
    id              BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    produto_id      BIGINT NOT NULL REFERENCES cad_produto(id) ON DELETE CASCADE,
    numero_serial   VARCHAR(60) NOT NULL,
    status          VARCHAR(20) NOT NULL DEFAULT 'disponivel'
        CHECK (status IN ('disponivel','reservado','vendido','devolvido')),
    UNIQUE (produto_id, numero_serial)
);

CREATE TABLE IF NOT EXISTS est_estoque (
    id                  BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    produto_id          BIGINT NOT NULL REFERENCES cad_produto(id),
    produto_grade_id    BIGINT REFERENCES est_produto_grade(id),
    produto_lote_id     BIGINT REFERENCES est_produto_lote(id),
    local_estoque_id    BIGINT NOT NULL REFERENCES est_local_estoque(id),
    quantidade          NUMERIC(14,3) NOT NULL DEFAULT 0,
    quantidade_reservada NUMERIC(14,3) NOT NULL DEFAULT 0,
    atualizado_em       TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (produto_id, local_estoque_id, produto_grade_id, produto_lote_id)
);

CREATE TABLE IF NOT EXISTS est_movimentacao (
    id                  BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    produto_id          BIGINT NOT NULL REFERENCES cad_produto(id),
    produto_grade_id    BIGINT REFERENCES est_produto_grade(id),
    produto_lote_id     BIGINT REFERENCES est_produto_lote(id),
    produto_serial_id   BIGINT REFERENCES est_produto_serial(id),
    local_estoque_id    BIGINT NOT NULL REFERENCES est_local_estoque(id),
    tipo                VARCHAR(20) NOT NULL
        CHECK (tipo IN ('entrada','saida','transferencia','perda','inventario','estorno')),
    origem              VARCHAR(20) NOT NULL
        CHECK (origem IN ('venda','compra','ajuste','devolucao','producao','transferencia','inventario')),
    documento_tipo      VARCHAR(30),
    documento_id        BIGINT,
    quantidade          NUMERIC(14,3) NOT NULL,
    quantidade_anterior NUMERIC(14,3) NOT NULL,
    quantidade_atual    NUMERIC(14,3) NOT NULL,
    custo_unitario      NUMERIC(14,4),
    usuario_id          BIGINT NOT NULL REFERENCES sec_usuario(id),
    data_movimento      TIMESTAMPTZ NOT NULL DEFAULT now(),
    estornado           BOOLEAN NOT NULL DEFAULT FALSE,
    movimentacao_estorno_id BIGINT REFERENCES est_movimentacao(id),
    observacao          VARCHAR(300)
);

CREATE INDEX IF NOT EXISTS ix_est_movimentacao_produto ON est_movimentacao(produto_id, data_movimento);
CREATE INDEX IF NOT EXISTS ix_est_movimentacao_documento ON est_movimentacao(documento_tipo, documento_id);

CREATE TABLE IF NOT EXISTS est_transferencia_estoque (
    id                  BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    produto_id          BIGINT NOT NULL REFERENCES cad_produto(id),
    produto_grade_id    BIGINT REFERENCES est_produto_grade(id),
    produto_lote_id     BIGINT REFERENCES est_produto_lote(id),
    produto_serial_id   BIGINT REFERENCES est_produto_serial(id),
    local_origem_id     BIGINT NOT NULL REFERENCES est_local_estoque(id),
    local_destino_id    BIGINT NOT NULL REFERENCES est_local_estoque(id),
    quantidade          NUMERIC(14,3) NOT NULL,
    usuario_id          BIGINT NOT NULL REFERENCES sec_usuario(id),
    data_transferencia  TIMESTAMPTZ NOT NULL DEFAULT now(),
    status              VARCHAR(20) NOT NULL DEFAULT 'concluida'
        CHECK (status IN ('pendente','concluida','estornada'))
);

CREATE TABLE IF NOT EXISTS est_inventario (
    id              BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    filial_id       BIGINT NOT NULL REFERENCES cad_filial(id),
    local_estoque_id BIGINT REFERENCES est_local_estoque(id),
    data_inventario TIMESTAMPTZ NOT NULL DEFAULT now(),
    status          VARCHAR(20) NOT NULL DEFAULT 'em_andamento'
        CHECK (status IN ('em_andamento','concluido','cancelado')),
    usuario_id      BIGINT NOT NULL REFERENCES sec_usuario(id)
);

CREATE TABLE IF NOT EXISTS est_inventario_item (
    id                  BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    inventario_id       BIGINT NOT NULL REFERENCES est_inventario(id) ON DELETE CASCADE,
    produto_id          BIGINT NOT NULL REFERENCES cad_produto(id),
    produto_grade_id    BIGINT REFERENCES est_produto_grade(id),
    produto_lote_id     BIGINT REFERENCES est_produto_lote(id),
    quantidade_sistema  NUMERIC(14,3) NOT NULL,
    quantidade_contada  NUMERIC(14,3) NOT NULL,
    diferenca           NUMERIC(14,3) GENERATED ALWAYS AS (quantidade_contada - quantidade_sistema) STORED
);

CREATE TABLE IF NOT EXISTS est_promocao (
    id                  BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    descricao           VARCHAR(150) NOT NULL,
    filial_id           BIGINT REFERENCES cad_filial(id),
    data_inicio         DATE NOT NULL,
    data_fim            DATE NOT NULL,
    tipo_desconto       VARCHAR(12) NOT NULL CHECK (tipo_desconto IN ('percentual','valor')),
    valor_desconto      NUMERIC(14,4) NOT NULL,
    combo               BOOLEAN NOT NULL DEFAULT FALSE,
    ativo               BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS est_promocao_produtos (
    promocao_id     BIGINT NOT NULL REFERENCES est_promocao(id) ON DELETE CASCADE,
    produto_id      BIGINT NOT NULL REFERENCES cad_produto(id) ON DELETE CASCADE,
    PRIMARY KEY (promocao_id, produto_id)
);

CREATE TABLE IF NOT EXISTS est_promocao_pagamento (
    promocao_id         BIGINT NOT NULL REFERENCES est_promocao(id) ON DELETE CASCADE,
    forma_pagamento_id  BIGINT NOT NULL REFERENCES cad_forma_pagamento(id) ON DELETE CASCADE,
    PRIMARY KEY (promocao_id, forma_pagamento_id)
);
