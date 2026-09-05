-- GestorPDV — Fase 7 (Pagamentos): a baixa (crb_documento_baixa) criada na
-- Fase 2 só referenciava o documento inteiro; para dar baixa por parcela
-- individual (RN-FIN-001), a baixa precisa apontar também para a parcela.
-- Idempotente (ADD COLUMN IF NOT EXISTS é nativamente idempotente).

ALTER TABLE crb_documento_baixa
    ADD COLUMN IF NOT EXISTS parcela_id BIGINT REFERENCES fin_parcela(id);
