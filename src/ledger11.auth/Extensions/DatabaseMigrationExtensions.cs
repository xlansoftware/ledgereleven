using ledger11.auth.Data;
using Microsoft.EntityFrameworkCore;

public static class DatabaseMigrationExtensions
{
    public static async Task MigrateDatabaseAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
                            .CreateLogger("DatabaseMigration");
        logger.LogInformation("Database migrated successfully.");
    }
}
