using GestorPDV.Application.Cadastros;
using GestorPDV.Domain.Cadastros;
using GestorPDV.Infrastructure.Database;
using Npgsql;

namespace GestorPDV.Data.Postgres.Repositories;

public class FilialRepository : IFilialRepository
{
    private const string ColunasFilial =
        "id, codigo, razao_social, nome_fantasia, cnpj, inscricao_estadual, endereco, " +
        "numero, bairro, municipio, uf, cep, telefone, ativo";

    private readonly NpgsqlConnectionFactory _connectionFactory;

    public FilialRepository(NpgsqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Filial>> ListarAsync(CancellationToken cancellationToken = default)
    {
        var sql = $"SELECT {ColunasFilial} FROM cad_filial ORDER BY codigo";

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var filiais = new List<Filial>();
        while (await reader.ReadAsync(cancellationToken))
        {
            filiais.Add(MapFilial(reader));
        }

        return filiais;
    }

    public async Task<Filial?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var sql = $"SELECT {ColunasFilial} FROM cad_filial WHERE id = @id";

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapFilial(reader) : null;
    }

    public async Task<long> InserirAsync(Filial filial, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO cad_filial
                (codigo, razao_social, nome_fantasia, cnpj, inscricao_estadual, endereco,
                 numero, bairro, municipio, uf, cep, telefone, ativo)
            VALUES
                (@codigo, @razaoSocial, @nomeFantasia, @cnpj, @inscricaoEstadual, @endereco,
                 @numero, @bairro, @municipio, @uf, @cep, @telefone, @ativo)
            RETURNING id
            """;

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        AdicionarParametros(command, filial);
        return (long)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    public async Task AtualizarAsync(Filial filial, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE cad_filial SET
                codigo = @codigo, razao_social = @razaoSocial, nome_fantasia = @nomeFantasia,
                cnpj = @cnpj, inscricao_estadual = @inscricaoEstadual, endereco = @endereco,
                numero = @numero, bairro = @bairro, municipio = @municipio, uf = @uf,
                cep = @cep, telefone = @telefone, ativo = @ativo
            WHERE id = @id
            """;

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", filial.Id);
        AdicionarParametros(command, filial);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AdicionarParametros(NpgsqlCommand command, Filial filial)
    {
        command.Parameters.AddWithValue("codigo", filial.Codigo);
        command.Parameters.AddWithValue("razaoSocial", filial.RazaoSocial);
        command.Parameters.AddWithValue("nomeFantasia", (object?)filial.NomeFantasia ?? DBNull.Value);
        command.Parameters.AddWithValue("cnpj", (object?)filial.Cnpj ?? DBNull.Value);
        command.Parameters.AddWithValue("inscricaoEstadual", (object?)filial.InscricaoEstadual ?? DBNull.Value);
        command.Parameters.AddWithValue("endereco", (object?)filial.Endereco ?? DBNull.Value);
        command.Parameters.AddWithValue("numero", (object?)filial.Numero ?? DBNull.Value);
        command.Parameters.AddWithValue("bairro", (object?)filial.Bairro ?? DBNull.Value);
        command.Parameters.AddWithValue("municipio", (object?)filial.Municipio ?? DBNull.Value);
        command.Parameters.AddWithValue("uf", (object?)filial.Uf ?? DBNull.Value);
        command.Parameters.AddWithValue("cep", (object?)filial.Cep ?? DBNull.Value);
        command.Parameters.AddWithValue("telefone", (object?)filial.Telefone ?? DBNull.Value);
        command.Parameters.AddWithValue("ativo", filial.Ativo);
    }

    private static Filial MapFilial(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetInt64(0),
        Codigo = reader.GetString(1),
        RazaoSocial = reader.GetString(2),
        NomeFantasia = reader.IsDBNull(3) ? null : reader.GetString(3),
        Cnpj = reader.IsDBNull(4) ? null : reader.GetString(4),
        InscricaoEstadual = reader.IsDBNull(5) ? null : reader.GetString(5),
        Endereco = reader.IsDBNull(6) ? null : reader.GetString(6),
        Numero = reader.IsDBNull(7) ? null : reader.GetString(7),
        Bairro = reader.IsDBNull(8) ? null : reader.GetString(8),
        Municipio = reader.IsDBNull(9) ? null : reader.GetString(9),
        Uf = reader.IsDBNull(10) ? null : reader.GetString(10),
        Cep = reader.IsDBNull(11) ? null : reader.GetString(11),
        Telefone = reader.IsDBNull(12) ? null : reader.GetString(12),
        Ativo = reader.GetBoolean(13)
    };
}
