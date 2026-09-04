namespace GestorPDV.Application.Common;

public class DatabaseStatus
{
    public bool ConexaoOk { get; init; }
    public bool SchemaOk { get; init; }
    public IReadOnlyList<string> TabelasCriadas { get; init; } = Array.Empty<string>();
    public string? Mensagem { get; init; }
}

// Implementado em GestorPDV.Infrastructure.Database: valida a conexão com o
// PostgreSQL na inicialização, verifica se as tabelas existem e cria as que
// estiverem faltando a partir dos scripts em database/schema (item 3 do
// escopo).
public interface IDatabaseInitializer
{
    Task<DatabaseStatus> InicializarAsync(CancellationToken cancellationToken = default);
}
