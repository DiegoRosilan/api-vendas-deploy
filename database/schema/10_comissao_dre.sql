-- GestorPDV — Comissão de vendedor/gerente e classificação financeira (DRE)
-- (RN-COM-001, RN-PRE-001, RN-DRE-001).
-- Idempotente: pode ser executado múltiplas vezes sem erro.

CREATE TABLE IF NOT EXISTS com_comissao (
    id              BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    venda_id        BIGINT NOT NULL REFERENCES mv_venda(id) ON DELETE CASCADE,
    funcionario_id  BIGINT NOT NULL REFERENCES cad_funcionario(id),
    tipo            VARCHAR(10) NOT NULL CHECK (tipo IN ('vendedor','gerente')),
    percentual      NUMERIC(5,2) NOT NULL,
    valor_base      NUMERIC(14,2) NOT NULL,
    valor_comissao  NUMERIC(14,2) NOT NULL,
    data_referencia DATE NOT NULL DEFAULT CURRENT_DATE,
    status          VARCHAR(10) NOT NULL DEFAULT 'pendente'
        CHECK (status IN ('pendente','pago','cancelado')),
    data_pagamento  DATE
);

CREATE INDEX IF NOT EXISTS ix_com_comissao_funcionario ON com_comissao(funcionario_id, status);

CREATE TABLE IF NOT EXISTS dre_grupo (
    id              BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    codigo          VARCHAR(20) NOT NULL UNIQUE,
    descricao       VARCHAR(150) NOT NULL,
    tipo            VARCHAR(10) NOT NULL CHECK (tipo IN ('receita','despesa','imposto','custo')),
    grupo_pai_id    BIGINT REFERENCES dre_grupo(id)
);

CREATE TABLE IF NOT EXISTS dre_lancamento (
    id              BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    grupo_id        BIGINT NOT NULL REFERENCES dre_grupo(id),
    filial_id       BIGINT NOT NULL REFERENCES cad_filial(id),
    origem_tipo     VARCHAR(30) NOT NULL,
    origem_id       BIGINT NOT NULL,
    valor           NUMERIC(14,2) NOT NULL,
    data_referencia DATE NOT NULL DEFAULT CURRENT_DATE
);

CREATE INDEX IF NOT EXISTS ix_dre_lancamento_grupo ON dre_lancamento(grupo_id, data_referencia);
