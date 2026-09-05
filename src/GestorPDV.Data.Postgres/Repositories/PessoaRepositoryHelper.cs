using GestorPDV.Domain.Cadastros;
using Npgsql;

namespace GestorPDV.Data.Postgres.Repositories;

// cad_cliente, cad_fornecedor e cad_funcionario compartilham o cadastro
// base de pessoa (cad_pessoa) — este helper concentra a leitura/escrita de
// Pessoa para evitar repetir a mesma SQL nos três repositórios.
internal static class PessoaRepositoryHelper
{
    public const string ColunasPessoa =
        "p.id, p.tipo_pessoa, p.nome, p.nome_fantasia, p.cpf_cnpj, p.rg_ie, p.email, p.telefone, " +
        "p.endereco, p.numero, p.bairro, p.municipio, p.uf, p.cep, p.ativo, p.criado_em";

    public static Pessoa MapPessoa(NpgsqlDataReader reader, int offset) => new()
    {
        Id = reader.GetInt64(offset),
        TipoPessoa = reader.GetString(offset + 1) == "F" ? TipoPessoa.Fisica : TipoPessoa.Juridica,
        Nome = reader.GetString(offset + 2),
        NomeFantasia = reader.IsDBNull(offset + 3) ? null : reader.GetString(offset + 3),
        CpfCnpj = reader.IsDBNull(offset + 4) ? null : reader.GetString(offset + 4),
        RgIe = reader.IsDBNull(offset + 5) ? null : reader.GetString(offset + 5),
        Email = reader.IsDBNull(offset + 6) ? null : reader.GetString(offset + 6),
        Telefone = reader.IsDBNull(offset + 7) ? null : reader.GetString(offset + 7),
        Endereco = reader.IsDBNull(offset + 8) ? null : reader.GetString(offset + 8),
        Numero = reader.IsDBNull(offset + 9) ? null : reader.GetString(offset + 9),
        Bairro = reader.IsDBNull(offset + 10) ? null : reader.GetString(offset + 10),
        Municipio = reader.IsDBNull(offset + 11) ? null : reader.GetString(offset + 11),
        Uf = reader.IsDBNull(offset + 12) ? null : reader.GetString(offset + 12),
        Cep = reader.IsDBNull(offset + 13) ? null : reader.GetString(offset + 13),
        Ativo = reader.GetBoolean(offset + 14),
        CriadoEm = reader.GetFieldValue<DateTimeOffset>(offset + 15)
    };

    public static async Task<long> InserirAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, Pessoa pessoa, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO cad_pessoa
                (tipo_pessoa, nome, nome_fantasia, cpf_cnpj, rg_ie, email, telefone,
                 endereco, numero, bairro, municipio, uf, cep, ativo)
            VALUES
                (@tipoPessoa, @nome, @nomeFantasia, @cpfCnpj, @rgIe, @email, @telefone,
                 @endereco, @numero, @bairro, @municipio, @uf, @cep, @ativo)
            RETURNING id
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        AdicionarParametros(command, pessoa);
        return (long)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    public static async Task AtualizarAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, Pessoa pessoa, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE cad_pessoa
            SET tipo_pessoa = @tipoPessoa, nome = @nome, nome_fantasia = @nomeFantasia,
                cpf_cnpj = @cpfCnpj, rg_ie = @rgIe, email = @email, telefone = @telefone,
                endereco = @endereco, numero = @numero, bairro = @bairro, municipio = @municipio,
                uf = @uf, cep = @cep, ativo = @ativo
            WHERE id = @id
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("id", pessoa.Id);
        AdicionarParametros(command, pessoa);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AdicionarParametros(NpgsqlCommand command, Pessoa pessoa)
    {
        command.Parameters.AddWithValue("tipoPessoa", pessoa.TipoPessoa == TipoPessoa.Fisica ? "F" : "J");
        command.Parameters.AddWithValue("nome", pessoa.Nome);
        command.Parameters.AddWithValue("nomeFantasia", (object?)pessoa.NomeFantasia ?? DBNull.Value);
        command.Parameters.AddWithValue("cpfCnpj", (object?)pessoa.CpfCnpj ?? DBNull.Value);
        command.Parameters.AddWithValue("rgIe", (object?)pessoa.RgIe ?? DBNull.Value);
        command.Parameters.AddWithValue("email", (object?)pessoa.Email ?? DBNull.Value);
        command.Parameters.AddWithValue("telefone", (object?)pessoa.Telefone ?? DBNull.Value);
        command.Parameters.AddWithValue("endereco", (object?)pessoa.Endereco ?? DBNull.Value);
        command.Parameters.AddWithValue("numero", (object?)pessoa.Numero ?? DBNull.Value);
        command.Parameters.AddWithValue("bairro", (object?)pessoa.Bairro ?? DBNull.Value);
        command.Parameters.AddWithValue("municipio", (object?)pessoa.Municipio ?? DBNull.Value);
        command.Parameters.AddWithValue("uf", (object?)pessoa.Uf ?? DBNull.Value);
        command.Parameters.AddWithValue("cep", (object?)pessoa.Cep ?? DBNull.Value);
        command.Parameters.AddWithValue("ativo", pessoa.Ativo);
    }
}
