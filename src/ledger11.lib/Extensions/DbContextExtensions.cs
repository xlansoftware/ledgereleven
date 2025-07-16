using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ledger11.lib.Extensions;

/// <summary>
/// Provides extension methods for the DbContext class.
/// </summary>
public static class DbContextExtensions
{
    /// <summary>
    /// Gets the database file path from the DbContext's connection string.
    /// This method is designed for SQLite databases where the DataSource property of the connection holds the file path.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <returns>The file path of the database, or an empty string if the context is null or the data source cannot be determined.</returns>
    public static string GetDbFilePath(this DbContext context)
    {
        if (context == null)
        {
            return string.Empty;
        }

        var connection = context.Database.GetDbConnection();
        if (connection != null)
        {
            return connection.DataSource;
        }

        return string.Empty;
    }
}
