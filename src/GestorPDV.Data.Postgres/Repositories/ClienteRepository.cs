using GestorPDV.Application.Cadastros;
using GestorPDV.Domain.Cadastros;
using GestorPDV.Infrastructure.Database;
using Npgsql;

namespace GestorPDV.Data.Postgres.Repositories;

public class ClienteRepository : IClienteRepository
{
    private const string ColunasCliente =
        PessoaRepositoryHelper.ColunasPessoa +
        ", c.limite_credito, c.bloquear_venda_dias_vencido, c.tabela_preco_id, c.observacao";

    private const string BaseSelect =
        $"SELECT {ColunasCliente} FROM cad_cliente c JOIN cad_pessoa p ON p.id = c.id";

    private readonly NpgsqlConnectionFactory _connectionFactory;

    public ClienteRepository(NpgsqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Cliente>> ListarAsync(string? filtro, CancellationToken cancellationToken = default)
    {
        var sql = $"{BaseSelect} WHERE (@filtro IS NULL OR p.nome ILIKE @filtro OR p.cpf_cnpj ILIKE @filtro) ORDER BY p.nome";

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("filtro", (object?)(string.IsNullOrWhiteSpace(filtro) ? null : $"%{filtro}%") ?? DBNull.Value);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var clientes = new List<Cliente>();
        while (await reader.ReadAsync(cancellationToken))
        {
            clientes.Add(MapCliente(reader));
        }

        return clientes;
    }

    public async Task<Cliente?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var sql = $"{BaseSelect} WHERE c.id = @id";

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapCliente(reader) : null;
    }

    public async Task<long> InserirAsync(Cliente cliente, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cliente.Pessoa);

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var pessoaId = await PessoaRepositoryHelper.InserirAsync(connection, transaction, cliente.Pessoa, cancellationToken);

        const string sql = """
            INSERT INTO cad_cliente (id, limite_credito, bloquear_venda_dias_vencido, tabela_preco_id, observacao)
            VALUES (@id, @limiteCredito, @bloquearVendaDiasVencido, @tabelaPrecoId, @observacao)
            """;

        await using (var command = new NpgsqlCommand(sql, connection, transaction))
        {
            command.Parameters.AddWithValue("id", pessoaId);
            AdicionarParametrosCliente(command, cliente);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return pessoaId;
    }

    public async Task AtualizarAsync(Cliente cliente, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cliente.Pessoa);

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await PessoaRepositoryHelper.AtualizarAsync(connection, transaction, cliente.Pessoa, cancellationToken);

        const string sql = """
            UPDATE cad_cliente
            SET limite_credito = @limiteCredito, bloquear_venda_dias_vencido = @bloquearVendaDiasVencido,
                tabela_preco_id = @tabelaPrecoId, observacao = @observacao
            WHERE id = @id
            """;

        await using (var command = new NpgsqlCommand(sql, connection, transaction))
        {
            command.Parameters.AddWithValue("id", cliente.Id);
            AdicionarParametrosCliente(command, cliente);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private static void AdicionarParametrosCliente(NpgsqlCommand command, Cliente cliente)
    {
        command.Parameters.AddWithValue("limiteCredito", cliente.LimiteCredito);
        command.Parameters.AddWithValue("bloquearVendaDiasVencido", (object?)cliente.BloquearVendaDiasVencido ?? DBNull.Value);
        command.Parameters.AddWithValue("tabelaPrecoId", (object?)cliente.TabelaPrecoId ?? DBNull.Value);
        command.Parameters.AddWithValue("observacao", (object?)cliente.Observacao ?? DBNull.Value);
    }

    private static Cliente MapCliente(NpgsqlDataReader reader)
    {
        var pessoa = PessoaRepositoryHelper.MapPessoa(reader, 0);
        return new Cliente
        {
            Id = pessoa.Id,
            Pessoa = pessoa,
            LimiteCredito = reader.GetDecimal(16),
            BloquearVendaDiasVencido = reader.IsDBNull(17) ? null : reader.GetInt32(17),
            TabelaPrecoId = reader.IsDBNull(18) ? null : reader.GetInt64(18),
            Observacao = reader.IsDBNull(19) ? null : reader.GetString(19)
        };
    }
}
