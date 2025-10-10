using System.CommandLine;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ledger11.model.Data;

namespace ledger11.cli;

public static class InfoCommands
{

    public static RootCommand AddInfoCommand(this RootCommand rootCommand)
    {
        var command = new Command("info", "Show info");

        var logLevelOption = Tools.LogLevelOption;
        var dataOption = Tools.DataOption;

        command.AddGlobalOption(logLevelOption);
        command.AddGlobalOption(dataOption);

        command.SetHandler((data, logLevel) => 
        {
            Tools.Catch(() =>
            { 
                var consoleLogger = Tools.CreateConsoleLogger(logLevel, "InfoCommand");
                var dataPath = Tools.DataPath(consoleLogger, data);

                var host = Tools.CreateHost(logLevel, dataPath);
                using var scope = host.Services.CreateScope();
                var services = scope.ServiceProvider;

                var logger = services.GetRequiredService<ILogger<Program>>();

                Console.WriteLine($"Data path = {dataPath}");
                Console.WriteLine("Info command executed.");
            });
            
        }, dataOption, logLevelOption);

        rootCommand.AddCommand(command);

        return rootCommand;
    }

}