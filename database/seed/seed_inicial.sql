-- GestorPDV — dados iniciais mínimos para o sistema ficar operável após a
-- criação do schema: filial, perfil/usuário administrador, formas e
-- condições de pagamento, situações de orçamento/pedido e tabelas fiscais
-- de referência mais comuns.
-- Idempotente (usa ON CONFLICT DO NOTHING). Requer database/schema/run.sql
-- já executado.
-- Uso: psql -h <host> -U <usuario> -d gestordb -f database/seed/seed_inicial.sql

INSERT INTO cad_filial (codigo, razao_social, nome_fantasia, uf, ativo)
VALUES ('1', 'Empresa Matriz', 'Matriz', 'SP', TRUE)
ON CONFLICT (codigo) DO NOTHING;

INSERT INTO sec_perfil (nome, descricao)
VALUES ('Administrador', 'Acesso completo a todos os módulos')
ON CONFLICT (nome) DO NOTHING;

INSERT INTO sec_permissao (codigo, descricao, modulo) VALUES
    ('VENDA_INCLUIR',        'Incluir venda',                    'vendas'),
    ('VENDA_CANCELAR',       'Cancelar venda',                   'vendas'),
    ('VENDA_AUTORIZAR_DESCONTO', 'Autorizar desconto acima do limite', 'vendas'),
    ('ESTOQUE_AJUSTAR',      'Ajustar estoque manualmente',      'estoque'),
    ('FINANCEIRO_BAIXAR',    'Dar baixa em documento financeiro','financeiro'),
    ('FINANCEIRO_RENEGOCIAR','Renegociar dívida de cliente',     'financeiro'),
    ('CAIXA_ABRIR',          'Abrir caixa',                      'caixa'),
    ('CAIXA_FECHAR',         'Fechar caixa',                     'caixa'),
    ('CAIXA_SANGRIA',        'Realizar sangria/suprimento',      'caixa'),
    ('CADASTRO_GERENCIAR',   'Gerenciar cadastros',              'cadastros'),
    ('RELATORIO_VISUALIZAR', 'Visualizar relatórios',            'relatorios'),
    ('SEGURANCA_GERENCIAR',  'Gerenciar usuários e permissões',  'seguranca')
ON CONFLICT (codigo) DO NOTHING;

INSERT INTO sec_perfil_permissao (perfil_id, permissao_id, permitido)
SELECT p.id, perm.id, TRUE
FROM sec_perfil p
CROSS JOIN sec_permissao perm
WHERE p.nome = 'Administrador'
ON CONFLICT DO NOTHING;

-- Usuário administrador inicial. Senha padrão: "admin123" (hash bcrypt real,
-- gerado pela própria extensão pgcrypto). TROQUE a senha no primeiro acesso.
INSERT INTO sec_usuario (login, senha_hash, nome, perfil_id, filial_id, exige_troca_senha)
SELECT 'admin', crypt('admin123', gen_salt('bf')), 'Administrador', p.id, f.id, TRUE
FROM sec_perfil p, cad_filial f
WHERE p.nome = 'Administrador' AND f.codigo = '1'
ON CONFLICT (login) DO NOTHING;

INSERT INTO est_local_estoque (filial_id, descricao)
SELECT f.id, 'Estoque Principal' FROM cad_filial f WHERE f.codigo = '1'
ON CONFLICT DO NOTHING;

INSERT INTO cad_forma_pagamento (codigo, descricao, tipo, permite_parcelamento, gera_financeiro, movimenta_caixa) VALUES
    ('DIN',  'Dinheiro',           'dinheiro',       FALSE, FALSE, TRUE),
    ('CRED', 'Cartão de Crédito',  'cartao_credito', TRUE,  TRUE,  TRUE),
    ('DEB',  'Cartão de Débito',   'cartao_debito',  FALSE, FALSE, TRUE),
    ('PIX',  'Pix',                'pix',            FALSE, FALSE, TRUE),
    ('BOL',  'Boleto',             'boleto',         TRUE,  TRUE,  FALSE),
    ('CHQ',  'Cheque',             'cheque',         TRUE,  TRUE,  FALSE),
    ('CRED_LOJA', 'Crediário Loja','crediario',      TRUE,  TRUE,  FALSE)
ON CONFLICT (codigo) DO NOTHING;

INSERT INTO cad_condicao_pagamento (descricao, numero_parcelas, intervalo_dias, entrada_pct) VALUES
    ('À vista',        1, 0,  0),
    ('30 dias',        1, 30, 0),
    ('30/60 dias',     2, 30, 0),
    ('30/60/90 dias',  3, 30, 0)
ON CONFLICT DO NOTHING;

INSERT INTO mv_orcamento_situacao (codigo, descricao) VALUES
    ('ABERTO',     'Em aberto'),
    ('APROVADO',   'Aprovado'),
    ('CONVERTIDO', 'Convertido em venda'),
    ('CANCELADO',  'Cancelado'),
    ('EXPIRADO',   'Expirado')
ON CONFLICT (codigo) DO NOTHING;

INSERT INTO mv_pedido_venda_situacao (codigo, descricao) VALUES
    ('ABERTO',      'Em aberto'),
    ('FATURADO',    'Faturado (convertido em venda)'),
    ('CANCELADO',   'Cancelado')
ON CONFLICT (codigo) DO NOTHING;

-- CFOP mais usados em operações de venda de mercadoria (referência parcial;
-- a tabela completa de CFOP deve ser carregada via cadastro na Fase 8+).
INSERT INTO fis_cfop (codigo, descricao, tipo_operacao, devolucao) VALUES
    ('5101', 'Venda de produção do estabelecimento',                'saida', FALSE),
    ('5102', 'Venda de mercadoria adquirida ou recebida de terceiros', 'saida', FALSE),
    ('5405', 'Venda de mercadoria sujeita a ICMS-ST',                'saida', FALSE),
    ('5202', 'Devolução de compra para comercialização',             'saida', TRUE),
    ('6101', 'Venda de produção do estabelecimento (outro estado)',  'saida', FALSE),
    ('6102', 'Venda de mercadoria de terceiros (outro estado)',      'saida', FALSE)
ON CONFLICT (codigo) DO NOTHING;

INSERT INTO fis_csticms (codigo, descricao) VALUES
    ('00', 'Tributada integralmente'),
    ('10', 'Tributada e com cobrança do ICMS por ST'),
    ('20', 'Com redução de base de cálculo'),
    ('40', 'Isenta'),
    ('41', 'Não tributada'),
    ('60', 'ICMS cobrado anteriormente por ST'),
    ('90', 'Outras')
ON CONFLICT (codigo) DO NOTHING;

INSERT INTO fis_csosn (codigo, descricao) VALUES
    ('101', 'Tributada pelo Simples Nacional com permissão de crédito'),
    ('102', 'Tributada pelo Simples Nacional sem permissão de crédito'),
    ('500', 'ICMS cobrado anteriormente por ST ou por antecipação'),
    ('900', 'Outros')
ON CONFLICT (codigo) DO NOTHING;

INSERT INTO fis_cstpiscofins (codigo, descricao) VALUES
    ('01', 'Operação tributável com alíquota básica'),
    ('04', 'Operação tributável monofásica — revenda a alíquota zero'),
    ('06', 'Operação tributável a alíquota zero'),
    ('07', 'Operação isenta'),
    ('08', 'Operação sem incidência'),
    ('49', 'Outras operações de saída')
ON CONFLICT (codigo) DO NOTHING;

INSERT INTO fis_cstipi (codigo, descricao) VALUES
    ('50', 'Saída tributada'),
    ('51', 'Saída isenta'),
    ('53', 'Saída não tributada'),
    ('99', 'Outras saídas')
ON CONFLICT (codigo) DO NOTHING;

INSERT INTO dre_grupo (codigo, descricao, tipo) VALUES
    ('RECEITA_VENDA', 'Receita de vendas',        'receita'),
    ('CUSTO_MERC',    'Custo da mercadoria vendida','custo'),
    ('DESPESA_OP',    'Despesas operacionais',    'despesa'),
    ('IMPOSTO_VENDA', 'Impostos sobre vendas',    'imposto')
ON CONFLICT (codigo) DO NOTHING;
