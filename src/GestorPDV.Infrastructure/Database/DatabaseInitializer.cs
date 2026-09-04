using GestorPDV.Application.Common;
using Npgsql;

namespace GestorPDV.Infrastructure.Database;

// Implementa o requisito do item 3 do escopo: valida a conexão com o
// PostgreSQL na inicialização, verifica se as tabelas necessárias existem
// e cria automaticamente as que estiverem faltando, a partir dos scripts em
// database/schema.
public class DatabaseInitializer : IDatabaseInitializer
{
    // Todas as tabelas criadas pelos scripts em database/schema/*.sql.
    private static readonly string[] TabelasEsperadas =
    {
        // Segurança
        "sec_perfil", "sec_permissao", "sec_perfil_permissao", "sec_usuario", "sec_usuario_permissao",
        // Cadastros
        "cad_filial", "cad_pessoa", "cad_cliente", "cad_fornecedor", "cad_funcionario",
        "cad_categoria_produto", "cad_produto", "cad_servico", "cad_forma_pagamento",
        "cad_condicao_pagamento", "cad_tabela_preco", "cad_tabela_preco_item",
        // Estoque
        "est_local_estoque", "est_produto_grade", "est_produto_lote", "est_produto_serial",
        "est_estoque", "est_movimentacao", "est_transferencia_estoque", "est_inventario",
        "est_inventario_item", "est_promocao", "est_promocao_produtos", "est_promocao_pagamento",
        // Vendas
        "mv_venda", "mv_venda_produto", "mv_venda_grade", "mv_venda_lote", "mv_venda_serial",
        "mv_venda_tecnico", "mv_venda_pagamento",
        // Orçamento e pedido
        "mv_orcamento_situacao", "mv_orcamento", "mv_orcamento_produto", "mv_orcamento_parcela",
        "mv_pedido_venda_situacao", "mv_pedido_venda", "mv_pedido_venda_produto",
        "mv_pedido_venda_produto_grade", "mv_pedido_venda_produto_lote",
        "mv_pedido_venda_produto_serial", "mv_pedido_venda_parcela",
        // Nota fiscal
        "mv_nota_saida", "mv_nota_saida_itens", "mv_nota_saida_grade", "mv_nota_saida_lote",
        "mv_nota_saida_serial", "mv_nota_saida_parcela", "fis_documento_controle", "fis_configuracao",
        // Financeiro
        "crb_documento", "fin_parcela", "crb_documento_vendas", "crb_documento_baixa",
        "crb_renegociacao", "crb_renegociacao_documento", "ctp_renegociacao", "ctp_renegociacao_parcelas",
        // Caixa
        "cx_caixa", "cx_movimento", "cx_conferencia", "ecf_caixa",
        // Fiscal (referência)
        "fis_cfop", "fis_csticms", "fis_csosn", "fis_cstpiscofins", "fis_cstipi", "fis_ncm",
        "fis_cest", "fis_aliquota_icms", "fis_classificacao_ibs_cbs", "fis_cst_ibs_cbs",
        // Comissão e DRE
        "com_comissao", "dre_grupo", "dre_lancamento"
    };

    private readonly NpgsqlConnectionFactory _connectionFactory;
    private readonly SchemaScriptRunner _schemaScriptRunner;

    public DatabaseInitializer(NpgsqlConnectionFactory connectionFactory, SchemaScriptRunner schemaScriptRunner)
    {
        _connectionFactory = connectionFactory;
        _schemaScriptRunner = schemaScriptRunner;
    }

    public async Task<DatabaseStatus> InicializarAsync(CancellationToken cancellationToken = default)
    {
        NpgsqlConnection connection;
        try
        {
            connection = await _connectionFactory.CriarAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            return new DatabaseStatus
            {
                ConexaoOk = false,
                SchemaOk = false,
                Mensagem = $"Não foi possível conectar ao PostgreSQL: {ex.Message}"
            };
        }

        await using (connection)
        {
            try
            {
                var tabelasExistentes = await ObterTabelasExistentesAsync(connection, cancellationToken);
                var tabelasFaltantes = TabelasEsperadas.Except(tabelasExistentes).ToList();
                var tabelasCriadas = new List<string>();

                if (tabelasFaltantes.Count > 0)
                {
                    await _schemaScriptRunner.ExecutarScriptsAsync(connection, cancellationToken);
                    tabelasExistentes = await ObterTabelasExistentesAsync(connection, cancellationToken);
                    tabelasCriadas = tabelasFaltantes.Intersect(tabelasExistentes).ToList();
                }

                var tabelasAindaFaltando = TabelasEsperadas.Except(tabelasExistentes).ToList();

                return new DatabaseStatus
                {
                    ConexaoOk = true,
                    SchemaOk = tabelasAindaFaltando.Count == 0,
                    TabelasCriadas = tabelasCriadas,
                    Mensagem = tabelasAindaFaltando.Count == 0
                        ? "Conexão validada e schema completo."
                        : $"Tabelas ainda ausentes após executar os scripts: {string.Join(", ", tabelasAindaFaltando)}"
                };
            }
            catch (Exception ex)
            {
                return new DatabaseStatus
                {
                    ConexaoOk = true,
                    SchemaOk = false,
                    Mensagem = $"Conexão estabelecida, mas falhou ao validar/criar o schema: {ex.Message}"
                };
            }
        }
    }

    private static async Task<HashSet<string>> ObterTabelasExistentesAsync(
        NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'";

        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var tabelas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (await reader.ReadAsync(cancellationToken))
        {
            tabelas.Add(reader.GetString(0));
        }

        return tabelas;
    }
}
