-- GestorPDV — Segurança: usuários, perfis e permissões (RN-SEG-001).
-- Idempotente: pode ser executado múltiplas vezes sem erro.

CREATE TABLE IF NOT EXISTS sec_perfil (
    id              BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nome            VARCHAR(60)  NOT NULL UNIQUE,
    descricao       VARCHAR(200),
    ativo           BOOLEAN      NOT NULL DEFAULT TRUE,
    criado_em       TIMESTAMPTZ  NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS sec_permissao (
    id              BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    codigo          VARCHAR(80)  NOT NULL UNIQUE,
    descricao       VARCHAR(200) NOT NULL,
    modulo          VARCHAR(40)  NOT NULL
);

CREATE TABLE IF NOT EXISTS sec_perfil_permissao (
    perfil_id       BIGINT NOT NULL REFERENCES sec_perfil(id) ON DELETE CASCADE,
    permissao_id    BIGINT NOT NULL REFERENCES sec_permissao(id) ON DELETE CASCADE,
    permitido       BOOLEAN NOT NULL DEFAULT TRUE,
    PRIMARY KEY (perfil_id, permissao_id)
);

CREATE TABLE IF NOT EXISTS sec_usuario (
    id              BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    login           VARCHAR(60)  NOT NULL UNIQUE,
    senha_hash      VARCHAR(200) NOT NULL,
    nome            VARCHAR(150) NOT NULL,
    email           VARCHAR(150),
    perfil_id       BIGINT REFERENCES sec_perfil(id),
    filial_id       BIGINT,
    ativo           BOOLEAN      NOT NULL DEFAULT TRUE,
    bloqueado       BOOLEAN      NOT NULL DEFAULT FALSE,
    exige_troca_senha BOOLEAN    NOT NULL DEFAULT FALSE,
    ultimo_acesso_em TIMESTAMPTZ,
    criado_em       TIMESTAMPTZ  NOT NULL DEFAULT now(),
    atualizado_em   TIMESTAMPTZ  NOT NULL DEFAULT now()
);

-- Permissões específicas do usuário, sobrepõem o perfil (bloqueio de ações/botões).
CREATE TABLE IF NOT EXISTS sec_usuario_permissao (
    usuario_id      BIGINT NOT NULL REFERENCES sec_usuario(id) ON DELETE CASCADE,
    permissao_id    BIGINT NOT NULL REFERENCES sec_permissao(id) ON DELETE CASCADE,
    permitido       BOOLEAN NOT NULL,
    PRIMARY KEY (usuario_id, permissao_id)
);

CREATE INDEX IF NOT EXISTS ix_sec_usuario_perfil ON sec_usuario(perfil_id);
