using System.CommandLine;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ledger11.model.Data;

namespace ledger11.cli;

public static class SyncUsersCommand
{

    public static RootCommand AddSyncUsersCommand(this RootCommand rootCommand)
    {
        var command = new Command("sync-users", "Sync the AspNetUsers table.");

        var logLevelOption = Tools.LogLevelOption;
        
        var sourceDbOption = new Option<string>("--source-db", "The source database file.") { IsRequired = true };
        var targetDbOption = new Option<string>("--target-db", "The target database file.") { IsRequired = true };

        command.AddGlobalOption(logLevelOption);
        command.AddGlobalOption(sourceDbOption);
        command.AddGlobalOption(targetDbOption);

        command.SetHandler((logLevel, sourceDb, targetDb) => 
        {
            Tools.Catch(() =>
            { 
                var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(logLevel));
                var logger = loggerFactory.CreateLogger<Program>();
                
                UserSyncService.SyncAspNetUsers(sourceDb, targetDb);

                Console.WriteLine("sync-users command executed.");
            });
            
        }, logLevelOption, sourceDbOption, targetDbOption);

        rootCommand.AddCommand(command);

        return rootCommand;
    }

}