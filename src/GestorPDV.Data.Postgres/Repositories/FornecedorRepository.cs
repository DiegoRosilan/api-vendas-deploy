using GestorPDV.Application.Cadastros;
using GestorPDV.Domain.Cadastros;
using GestorPDV.Infrastructure.Database;
using Npgsql;

namespace GestorPDV.Data.Postgres.Repositories;

public class FornecedorRepository : IFornecedorRepository
{
    private const string ColunasFornecedor =
        PessoaRepositoryHelper.ColunasPessoa + ", f.banco, f.agencia, f.conta, f.observacao";

    private const string BaseSelect =
        $"SELECT {ColunasFornecedor} FROM cad_fornecedor f JOIN cad_pessoa p ON p.id = f.id";

    private readonly NpgsqlConnectionFactory _connectionFactory;

    public FornecedorRepository(NpgsqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Fornecedor>> ListarAsync(string? filtro, CancellationToken cancellationToken = default)
    {
        var sql = $"{BaseSelect} WHERE (@filtro::text IS NULL OR p.nome ILIKE @filtro OR p.cpf_cnpj ILIKE @filtro) ORDER BY p.nome";

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue(
            "filtro", (object?)(string.IsNullOrWhiteSpace(filtro) ? null : $"%{filtro}%") ?? DBNull.Value);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var fornecedores = new List<Fornecedor>();
        while (await reader.ReadAsync(cancellationToken))
        {
            fornecedores.Add(MapFornecedor(reader));
        }

        return fornecedores;
    }

    public async Task<Fornecedor?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var sql = $"{BaseSelect} WHERE f.id = @id";

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapFornecedor(reader) : null;
    }

    public async Task<long> InserirAsync(Fornecedor fornecedor, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fornecedor.Pessoa);

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var pessoaId = await PessoaRepositoryHelper.InserirAsync(connection, transaction, fornecedor.Pessoa, cancellationToken);

        const string sql = """
            INSERT INTO cad_fornecedor (id, banco, agencia, conta, observacao)
            VALUES (@id, @banco, @agencia, @conta, @observacao)
            """;

        await using (var command = new NpgsqlCommand(sql, connection, transaction))
        {
            command.Parameters.AddWithValue("id", pessoaId);
            AdicionarParametrosFornecedor(command, fornecedor);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return pessoaId;
    }

    public async Task AtualizarAsync(Fornecedor fornecedor, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fornecedor.Pessoa);

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await PessoaRepositoryHelper.AtualizarAsync(connection, transaction, fornecedor.Pessoa, cancellationToken);

        const string sql = """
            UPDATE cad_fornecedor SET banco = @banco, agencia = @agencia, conta = @conta, observacao = @observacao
            WHERE id = @id
            """;

        await using (var command = new NpgsqlCommand(sql, connection, transaction))
        {
            command.Parameters.AddWithValue("id", fornecedor.Id);
            AdicionarParametrosFornecedor(command, fornecedor);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private static void AdicionarParametrosFornecedor(NpgsqlCommand command, Fornecedor fornecedor)
    {
        command.Parameters.AddWithValue("banco", (object?)fornecedor.Banco ?? DBNull.Value);
        command.Parameters.AddWithValue("agencia", (object?)fornecedor.Agencia ?? DBNull.Value);
        command.Parameters.AddWithValue("conta", (object?)fornecedor.Conta ?? DBNull.Value);
        command.Parameters.AddWithValue("observacao", (object?)fornecedor.Observacao ?? DBNull.Value);
    }

    private static Fornecedor MapFornecedor(NpgsqlDataReader reader)
    {
        var pessoa = PessoaRepositoryHelper.MapPessoa(reader, 0);
        return new Fornecedor
        {
            Id = pessoa.Id,
            Pessoa = pessoa,
            Banco = reader.IsDBNull(16) ? null : reader.GetString(16),
            Agencia = reader.IsDBNull(17) ? null : reader.GetString(17),
            Conta = reader.IsDBNull(18) ? null : reader.GetString(18),
            Observacao = reader.IsDBNull(19) ? null : reader.GetString(19)
        };
    }
}
