using GestorPDV.Application.Cadastros;
using GestorPDV.Domain.Cadastros;
using GestorPDV.Infrastructure.Database;
using Npgsql;

namespace GestorPDV.Data.Postgres.Repositories;

public class FuncionarioRepository : IFuncionarioRepository
{
    private const string ColunasFuncionario =
        PessoaRepositoryHelper.ColunasPessoa +
        ", fu.filial_id, fu.usuario_id, fu.cargo, fu.comissao_padrao_pct, fu.eh_gerente, " +
        "fu.data_admissao, fu.data_demissao";

    private const string BaseSelect =
        $"SELECT {ColunasFuncionario} FROM cad_funcionario fu JOIN cad_pessoa p ON p.id = fu.id";

    private readonly NpgsqlConnectionFactory _connectionFactory;

    public FuncionarioRepository(NpgsqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Funcionario>> ListarAsync(string? filtro, CancellationToken cancellationToken = default)
    {
        var sql = $"{BaseSelect} WHERE (@filtro::text IS NULL OR p.nome ILIKE @filtro) ORDER BY p.nome";

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue(
            "filtro", (object?)(string.IsNullOrWhiteSpace(filtro) ? null : $"%{filtro}%") ?? DBNull.Value);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var funcionarios = new List<Funcionario>();
        while (await reader.ReadAsync(cancellationToken))
        {
            funcionarios.Add(MapFuncionario(reader));
        }

        return funcionarios;
    }

    public async Task<Funcionario?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var sql = $"{BaseSelect} WHERE fu.id = @id";

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapFuncionario(reader) : null;
    }

    public async Task<long> InserirAsync(Funcionario funcionario, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(funcionario.Pessoa);

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var pessoaId = await PessoaRepositoryHelper.InserirAsync(connection, transaction, funcionario.Pessoa, cancellationToken);

        const string sql = """
            INSERT INTO cad_funcionario
                (id, filial_id, usuario_id, cargo, comissao_padrao_pct, eh_gerente, data_admissao, data_demissao)
            VALUES
                (@id, @filialId, @usuarioId, @cargo, @comissaoPadraoPct, @ehGerente, @dataAdmissao, @dataDemissao)
            """;

        await using (var command = new NpgsqlCommand(sql, connection, transaction))
        {
            command.Parameters.AddWithValue("id", pessoaId);
            AdicionarParametrosFuncionario(command, funcionario);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return pessoaId;
    }

    public async Task AtualizarAsync(Funcionario funcionario, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(funcionario.Pessoa);

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await PessoaRepositoryHelper.AtualizarAsync(connection, transaction, funcionario.Pessoa, cancellationToken);

        const string sql = """
            UPDATE cad_funcionario
            SET filial_id = @filialId, usuario_id = @usuarioId, cargo = @cargo,
                comissao_padrao_pct = @comissaoPadraoPct, eh_gerente = @ehGerente,
                data_admissao = @dataAdmissao, data_demissao = @dataDemissao
            WHERE id = @id
            """;

        await using (var command = new NpgsqlCommand(sql, connection, transaction))
        {
            command.Parameters.AddWithValue("id", funcionario.Id);
            AdicionarParametrosFuncionario(command, funcionario);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private static void AdicionarParametrosFuncionario(NpgsqlCommand command, Funcionario funcionario)
    {
        command.Parameters.AddWithValue("filialId", (object?)funcionario.FilialId ?? DBNull.Value);
        command.Parameters.AddWithValue("usuarioId", (object?)funcionario.UsuarioId ?? DBNull.Value);
        command.Parameters.AddWithValue("cargo", (object?)funcionario.Cargo ?? DBNull.Value);
        command.Parameters.AddWithValue("comissaoPadraoPct", funcionario.ComissaoPadraoPct);
        command.Parameters.AddWithValue("ehGerente", funcionario.EhGerente);
        command.Parameters.AddWithValue("dataAdmissao", (object?)funcionario.DataAdmissao ?? DBNull.Value);
        command.Parameters.AddWithValue("dataDemissao", (object?)funcionario.DataDemissao ?? DBNull.Value);
    }

    private static Funcionario MapFuncionario(NpgsqlDataReader reader)
    {
        var pessoa = PessoaRepositoryHelper.MapPessoa(reader, 0);
        return new Funcionario
        {
            Id = pessoa.Id,
            Pessoa = pessoa,
            FilialId = reader.IsDBNull(16) ? null : reader.GetInt64(16),
            UsuarioId = reader.IsDBNull(17) ? null : reader.GetInt64(17),
            Cargo = reader.IsDBNull(18) ? null : reader.GetString(18),
            ComissaoPadraoPct = reader.GetDecimal(19),
            EhGerente = reader.GetBoolean(20),
            DataAdmissao = reader.IsDBNull(21) ? null : reader.GetFieldValue<DateOnly>(21),
            DataDemissao = reader.IsDBNull(22) ? null : reader.GetFieldValue<DateOnly>(22)
        };
    }
}
