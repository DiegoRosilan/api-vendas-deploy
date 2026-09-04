-- GestorPDV — Cadastros: filiais, pessoas (clientes/fornecedores/funcionários),
-- produtos, serviços, formas/condições de pagamento e tabelas de preço.
-- Idempotente: pode ser executado múltiplas vezes sem erro.

CREATE TABLE IF NOT EXISTS cad_filial (
    id              BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    codigo          VARCHAR(20)  NOT NULL UNIQUE,
    razao_social    VARCHAR(150) NOT NULL,
    nome_fantasia   VARCHAR(150),
    cnpj            VARCHAR(14),
    inscricao_estadual VARCHAR(20),
    endereco        VARCHAR(200),
    numero          VARCHAR(20),
    bairro          VARCHAR(100),
    municipio       VARCHAR(100),
    uf              CHAR(2),
    cep             VARCHAR(8),
    telefone        VARCHAR(20),
    ativo           BOOLEAN      NOT NULL DEFAULT TRUE
);

DO $$ BEGIN
    ALTER TABLE sec_usuario
        ADD CONSTRAINT fk_sec_usuario_filial FOREIGN KEY (filial_id)
            REFERENCES cad_filial(id);
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

-- Cadastro base de pessoa física/jurídica, compartilhado por clientes,
-- fornecedores e funcionários (item 5 do escopo: "Pessoas / Clientes").
CREATE TABLE IF NOT EXISTS cad_pessoa (
    id              BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    tipo_pessoa     CHAR(1)      NOT NULL CHECK (tipo_pessoa IN ('F', 'J')),
    nome            VARCHAR(150) NOT NULL,
    nome_fantasia   VARCHAR(150),
    cpf_cnpj        VARCHAR(14)  UNIQUE,
    rg_ie           VARCHAR(20),
    email           VARCHAR(150),
    telefone        VARCHAR(20),
    endereco        VARCHAR(200),
    numero          VARCHAR(20),
    bairro          VARCHAR(100),
    municipio       VARCHAR(100),
    uf              CHAR(2),
    cep             VARCHAR(8),
    ativo           BOOLEAN      NOT NULL DEFAULT TRUE,
    criado_em       TIMESTAMPTZ  NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS cad_cliente (
    id                          BIGINT PRIMARY KEY REFERENCES cad_pessoa(id) ON DELETE CASCADE,
    limite_credito              NUMERIC(14,2) NOT NULL DEFAULT 0,
    bloquear_venda_dias_vencido INTEGER,
    tabela_preco_id             BIGINT,
    observacao                  VARCHAR(500)
);

CREATE TABLE IF NOT EXISTS cad_fornecedor (
    id              BIGINT PRIMARY KEY REFERENCES cad_pessoa(id) ON DELETE CASCADE,
    banco           VARCHAR(60),
    agencia         VARCHAR(20),
    conta           VARCHAR(20),
    observacao      VARCHAR(500)
);

CREATE TABLE IF NOT EXISTS cad_funcionario (
    id                  BIGINT PRIMARY KEY REFERENCES cad_pessoa(id) ON DELETE CASCADE,
    filial_id           BIGINT REFERENCES cad_filial(id),
    usuario_id          BIGINT REFERENCES sec_usuario(id),
    cargo               VARCHAR(80),
    comissao_padrao_pct NUMERIC(5,2) NOT NULL DEFAULT 0,
    eh_gerente          BOOLEAN NOT NULL DEFAULT FALSE,
    data_admissao       DATE,
    data_demissao       DATE
);

CREATE TABLE IF NOT EXISTS cad_categoria_produto (
    id              BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    descricao       VARCHAR(100) NOT NULL,
    categoria_pai_id BIGINT REFERENCES cad_categoria_produto(id)
);

CREATE TABLE IF NOT EXISTS cad_produto (
    id                  BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    codigo              VARCHAR(30)  NOT NULL UNIQUE,
    codigo_barras       VARCHAR(20),
    descricao           VARCHAR(200) NOT NULL,
    categoria_id        BIGINT REFERENCES cad_categoria_produto(id),
    unidade             VARCHAR(6)   NOT NULL DEFAULT 'UN',
    ncm                 VARCHAR(8),
    cest                VARCHAR(7),
    preco_custo         NUMERIC(14,4) NOT NULL DEFAULT 0,
    preco_custo_medio   NUMERIC(14,4) NOT NULL DEFAULT 0,
    preco_venda         NUMERIC(14,4) NOT NULL DEFAULT 0,
    preco_minimo        NUMERIC(14,4),
    preco_promocional   NUMERIC(14,4),
    markup_pct          NUMERIC(9,4),
    margem_contribuicao_pct NUMERIC(9,4),
    estoque_minimo      NUMERIC(14,3) NOT NULL DEFAULT 0,
    estoque_maximo      NUMERIC(14,3),
    localizacao         VARCHAR(60),
    controla_estoque    BOOLEAN NOT NULL DEFAULT TRUE,
    controla_grade      BOOLEAN NOT NULL DEFAULT FALSE,
    controla_lote       BOOLEAN NOT NULL DEFAULT FALSE,
    controla_serial     BOOLEAN NOT NULL DEFAULT FALSE,
    desconto_maximo_pct NUMERIC(5,2) NOT NULL DEFAULT 0,
    bloquear_desconto   BOOLEAN NOT NULL DEFAULT FALSE,
    ativo               BOOLEAN NOT NULL DEFAULT TRUE,
    criado_em           TIMESTAMPTZ NOT NULL DEFAULT now(),
    atualizado_em       TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_cad_produto_codigo_barras ON cad_produto(codigo_barras);
CREATE INDEX IF NOT EXISTS ix_cad_produto_descricao ON cad_produto(descricao);

CREATE TABLE IF NOT EXISTS cad_servico (
    id              BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    codigo          VARCHAR(30)  NOT NULL UNIQUE,
    descricao       VARCHAR(200) NOT NULL,
    preco           NUMERIC(14,4) NOT NULL DEFAULT 0,
    aliquota_iss_pct NUMERIC(5,2) NOT NULL DEFAULT 0,
    ativo           BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS cad_forma_pagamento (
    id                  BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    codigo              VARCHAR(20)  NOT NULL UNIQUE,
    descricao           VARCHAR(60)  NOT NULL,
    tipo                VARCHAR(20)  NOT NULL
        CHECK (tipo IN ('dinheiro','cartao_credito','cartao_debito','pix','boleto','cheque','crediario','transferencia')),
    permite_parcelamento BOOLEAN NOT NULL DEFAULT FALSE,
    gera_financeiro     BOOLEAN NOT NULL DEFAULT TRUE,
    movimenta_caixa     BOOLEAN NOT NULL DEFAULT TRUE,
    ativo               BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS cad_condicao_pagamento (
    id              BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    descricao       VARCHAR(100) NOT NULL,
    numero_parcelas INTEGER NOT NULL DEFAULT 1,
    intervalo_dias  INTEGER NOT NULL DEFAULT 30,
    entrada_pct     NUMERIC(5,2) NOT NULL DEFAULT 0,
    ativo           BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS cad_tabela_preco (
    id              BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    descricao       VARCHAR(100) NOT NULL,
    filial_id       BIGINT REFERENCES cad_filial(id),
    vigencia_inicio DATE,
    vigencia_fim    DATE,
    ativo           BOOLEAN NOT NULL DEFAULT TRUE
);

DO $$ BEGIN
    ALTER TABLE cad_cliente
        ADD CONSTRAINT fk_cad_cliente_tabela_preco FOREIGN KEY (tabela_preco_id)
            REFERENCES cad_tabela_preco(id);
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

CREATE TABLE IF NOT EXISTS cad_tabela_preco_item (
    id              BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    tabela_preco_id BIGINT NOT NULL REFERENCES cad_tabela_preco(id) ON DELETE CASCADE,
    produto_id      BIGINT NOT NULL REFERENCES cad_produto(id) ON DELETE CASCADE,
    preco           NUMERIC(14,4) NOT NULL,
    UNIQUE (tabela_preco_id, produto_id)
);
