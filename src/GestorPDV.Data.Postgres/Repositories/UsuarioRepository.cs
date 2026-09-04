using GestorPDV.Application.Seguranca;
using GestorPDV.Domain.Seguranca;
using GestorPDV.Infrastructure.Database;
using Npgsql;

namespace GestorPDV.Data.Postgres.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private const string ColunasUsuario =
        "id, login, senha_hash, nome, email, perfil_id, filial_id, ativo, bloqueado, " +
        "exige_troca_senha, ultimo_acesso_em, criado_em, atualizado_em";

    private readonly NpgsqlConnectionFactory _connectionFactory;

    public UsuarioRepository(NpgsqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Usuario?> ObterPorLoginAsync(string login, CancellationToken cancellationToken = default)
    {
        var sql = $"SELECT {ColunasUsuario} FROM sec_usuario WHERE login = @login";

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("login", login);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapUsuario(reader) : null;
    }

    public async Task<Usuario?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var sql = $"SELECT {ColunasUsuario} FROM sec_usuario WHERE id = @id";

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapUsuario(reader) : null;
    }

    // Códigos de permissão efetivos do usuário: os do perfil, sobrepostos
    // pelas permissões específicas do usuário (sec_usuario_permissao),
    // conforme RN-SEG-001 (bloqueio de ações/botões por usuário).
    public async Task<IReadOnlyList<string>> ObterCodigosPermissaoAsync(
        long usuarioId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            WITH permissoes AS (
                SELECT pp.permissao_id, pp.permitido, 0 AS prioridade
                FROM sec_usuario u
                JOIN sec_perfil_permissao pp ON pp.perfil_id = u.perfil_id
                WHERE u.id = @usuarioId
                UNION ALL
                SELECT up.permissao_id, up.permitido, 1 AS prioridade
                FROM sec_usuario_permissao up
                WHERE up.usuario_id = @usuarioId
            ),
            efetivas AS (
                SELECT DISTINCT ON (permissao_id) permissao_id, permitido
                FROM permissoes
                ORDER BY permissao_id, prioridade DESC
            )
            SELECT p.codigo
            FROM efetivas e
            JOIN sec_permissao p ON p.id = e.permissao_id
            WHERE e.permitido = TRUE
            ORDER BY p.codigo
            """;

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("usuarioId", usuarioId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var codigos = new List<string>();
        while (await reader.ReadAsync(cancellationToken))
        {
            codigos.Add(reader.GetString(0));
        }

        return codigos;
    }

    public async Task AtualizarUltimoAcessoAsync(
        long usuarioId, DateTimeOffset dataAcesso, CancellationToken cancellationToken = default)
    {
        const string sql =
            "UPDATE sec_usuario SET ultimo_acesso_em = @dataAcesso, atualizado_em = now() WHERE id = @id";

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("dataAcesso", dataAcesso);
        command.Parameters.AddWithValue("id", usuarioId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task AtualizarSenhaAsync(
        long usuarioId, string novaSenhaHash, bool exigeTrocaSenha, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE sec_usuario
            SET senha_hash = @senhaHash, exige_troca_senha = @exigeTrocaSenha, atualizado_em = now()
            WHERE id = @id
            """;

        await using var connection = await _connectionFactory.CriarAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("senhaHash", novaSenhaHash);
        command.Parameters.AddWithValue("exigeTrocaSenha", exigeTrocaSenha);
        command.Parameters.AddWithValue("id", usuarioId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static Usuario MapUsuario(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetInt64(0),
        Login = reader.GetString(1),
        SenhaHash = reader.GetString(2),
        Nome = reader.GetString(3),
        Email = reader.IsDBNull(4) ? null : reader.GetString(4),
        PerfilId = reader.IsDBNull(5) ? null : reader.GetInt64(5),
        FilialId = reader.IsDBNull(6) ? null : reader.GetInt64(6),
        Ativo = reader.GetBoolean(7),
        Bloqueado = reader.GetBoolean(8),
        ExigeTrocaSenha = reader.GetBoolean(9),
        UltimoAcessoEm = reader.IsDBNull(10) ? null : reader.GetFieldValue<DateTimeOffset>(10),
        CriadoEm = reader.GetFieldValue<DateTimeOffset>(11),
        AtualizadoEm = reader.GetFieldValue<DateTimeOffset>(12)
    };
}
