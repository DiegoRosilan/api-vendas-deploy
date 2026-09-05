using GestorPDV.Application.Common;
using GestorPDV.Infrastructure.Database;

namespace GestorPDV.Data.Postgres;

public class NpgsqlUnitOfWorkFactory : IUnitOfWorkFactory
{
    private readonly NpgsqlConnectionFactory _connectionFactory;

    public NpgsqlUnitOfWorkFactory(NpgsqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public IUnitOfWork Criar() => new NpgsqlUnitOfWork(_connectionFactory);
}
