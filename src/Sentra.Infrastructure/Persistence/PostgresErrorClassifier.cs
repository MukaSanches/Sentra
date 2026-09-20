using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Sentra.Infrastructure.Persistence;

public static class PostgresErrorClassifier
{
    public static bool IsUniqueViolation(DbUpdateException exception)
        => exception.InnerException is PostgresException postgresException
           && postgresException.SqlState == PostgresErrorCodes.UniqueViolation;
}
