using System.CommandLine;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ledger11.model.Data;
using ledger11.data;

namespace ledger11.cli;

public static class TestUpgradeCommands
{

    public static RootCommand AddTestUpgradeCommand(this RootCommand rootCommand)
    {
        var command = new Command("test-upgrade", "Test the migration on the DB files.");

        var logLevelOption = Tools.LogLevelOption;
        var dataOption = Tools.DataOption;

        command.AddGlobalOption(logLevelOption);
        command.AddGlobalOption(dataOption);

        command.SetHandler(async (data, logLevel) =>
        {
            await Tools.Catch(async () =>
            {
                var consoleLogger = Tools.CreateConsoleLogger(logLevel, "InfoCommand");
                var dataPath = Tools.DataPath(consoleLogger, data);

                var host = Tools.CreateHost(logLevel, dataPath);
                using var scope = host.Services.CreateScope();
                var services = scope.ServiceProvider;

                var logger = services.GetRequiredService<ILogger<Program>>();

                var errors = await TestUpgradeDatabases<LedgerDbContext>(logger, dataPath, "space-*.db");
                if (errors > 0) Tools.ExitCode = 1;

                var appdataErrors = await TestUpgradeDatabases<AppDbContext>(logger, dataPath, "appdata.db");
                if (appdataErrors > 0) Tools.ExitCode = 1;

                Console.WriteLine("Test upgrade completed.");
            });

        }, dataOption, logLevelOption);

        rootCommand.AddCommand(command);

        return rootCommand;
    }
    private static async Task<int> TestUpgradeDatabases<TContext>(
        ILogger logger,
        string path,
        string filePattern
    ) where TContext : DbContext
    {
        var dbFiles = Directory.GetFiles(path, filePattern);
        int errorCount = 0;

        foreach (var file in dbFiles)
        {
            string fileName = Path.GetFileName(file);
            string tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_{fileName}");

            try
            {
                logger.LogTrace("Copying {File} to temp: {TempPath}", file, tempPath);
                File.Copy(file, tempPath, overwrite: true);

                var optionsBuilder = new DbContextOptionsBuilder<TContext>();
                optionsBuilder.UseSqlite($"Data Source={tempPath};Pooling=false;");

                using var context = (TContext)Activator.CreateInstance(typeof(TContext), optionsBuilder.Options)!;

                logger.LogTrace("Migrating temp DB: {TempPath}", tempPath);
                await context.Database.MigrateAsync();

                logger.LogInformation("Migration successful for: {File}", file);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error migrating {File}. Error: {Error}", file, ex);
                errorCount++;
            }
            finally
            {
                try
                {
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                        logger.LogTrace("Deleted temp file: {TempPath}", tempPath);
                    }
                }
                catch (Exception deleteEx)
                {
                    logger.LogWarning(deleteEx, "Could not delete temp file: {TempPath}", tempPath);
                }
            }
        }

        return errorCount;
    }

}