using Npgsql;

namespace GestorPDV.Infrastructure.Database;

// Executa os scripts de database/schema/*.sql, em ordem alfabética (os
// arquivos são numerados 00_, 01_, ... para garantir a ordem de
// dependência), dentro da conexão informada. Todos os scripts são
// idempotentes (CREATE TABLE IF NOT EXISTS / DO $$ ... EXCEPTION WHEN
// duplicate_object), então podem ser reexecutados sem erro a cada
// inicialização.
public class SchemaScriptRunner
{
    private readonly string _scriptsPath;

    public SchemaScriptRunner(string scriptsPath)
    {
        _scriptsPath = scriptsPath;
    }

    public async Task ExecutarScriptsAsync(NpgsqlConnection connection, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_scriptsPath))
        {
            throw new DirectoryNotFoundException(
                $"Diretório de scripts de banco não encontrado: {_scriptsPath}");
        }

        var arquivos = Directory.GetFiles(_scriptsPath, "*.sql")
            .Where(arquivo => !Path.GetFileName(arquivo).Equals("run.sql", StringComparison.OrdinalIgnoreCase))
            .OrderBy(arquivo => Path.GetFileName(arquivo), StringComparer.Ordinal)
            .ToList();

        foreach (var arquivo in arquivos)
        {
            var sql = await File.ReadAllTextAsync(arquivo, cancellationToken);
            if (string.IsNullOrWhiteSpace(sql))
            {
                continue;
            }

            await using var command = new NpgsqlCommand(sql, connection)
            {
                CommandTimeout = 120
            };
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
