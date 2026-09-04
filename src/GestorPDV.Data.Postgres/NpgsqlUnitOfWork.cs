using GestorPDV.Application.Common;
using GestorPDV.Infrastructure.Database;
using Npgsql;

namespace GestorPDV.Data.Postgres;

// Unidade de trabalho transacional usada nas operações críticas (finalizar
// venda, baixar estoque, movimentar caixa, baixar parcela — item 3 do
// escopo). Repositórios que participam da mesma transação devem receber a
// Connection/Transaction expostas aqui em vez de abrir uma nova conexão.
public class NpgsqlUnitOfWork : IUnitOfWork
{
    private readonly NpgsqlConnectionFactory _connectionFactory;

    public NpgsqlConnection? Connection { get; private set; }
    public NpgsqlTransaction? Transaction { get; private set; }

    public NpgsqlUnitOfWork(NpgsqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task BeginAsync(CancellationToken cancellationToken = default)
    {
        Connection = await _connectionFactory.CriarAsync(cancellationToken);
        Transaction = await Connection.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (Transaction is null)
        {
            throw new InvalidOperationException("Transação não iniciada. Chame BeginAsync antes de CommitAsync.");
        }

        await Transaction.CommitAsync(cancellationToken);
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (Transaction is null)
        {
            return;
        }

        await Transaction.RollbackAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (Transaction is not null)
        {
            await Transaction.DisposeAsync();
        }

        if (Connection is not null)
        {
            await Connection.DisposeAsync();
        }
    }
}
