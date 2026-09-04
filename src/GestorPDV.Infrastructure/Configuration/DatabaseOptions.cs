using Npgsql;

namespace GestorPDV.Infrastructure.Configuration;

// Configuração de conexão com o PostgreSQL, lida de appsettings.json e/ou
// variáveis de ambiente (item 3 do escopo: "utilizar configuração externa
// para conexão"). Nunca deve conter a senha real em appsettings.json
// versionado — use variáveis de ambiente (GESTORPDV_DATABASE__PASSWORD) ou
// um appsettings.Local.json fora do controle de versão em produção.
public class DatabaseOptions
{
    public const string SectionName = "Database";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5432;
    public string Database { get; set; } = "gestordb";
    public string Username { get; set; } = "postgres";
    public string Password { get; set; } = string.Empty;
    public int TimeoutSegundos { get; set; } = 15;

    public string ConnectionString => new NpgsqlConnectionStringBuilder
    {
        Host = Host,
        Port = Port,
        Database = Database,
        Username = Username,
        Password = Password,
        Timeout = TimeoutSegundos
    }.ConnectionString;
}
