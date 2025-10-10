using System.CommandLine;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ledger11.data;
using ledger11.model.Data;
using ledger11.service;
using System.Runtime.InteropServices;

namespace ledger11.cli;

public static class ApiKeyCommands
{

    public static RootCommand AddCreateApiKeyCommand(this RootCommand rootCommand)
    {
        var command = new Command("create-apikey", "Create API key");

        var logLevelOption = Tools.LogLevelOption;
        var dataOption = Tools.DataOption;

        var emailOption = new Option<string>("--email", description: "Email of the user") { IsRequired = true };

        command.AddGlobalOption(logLevelOption);
        command.AddGlobalOption(dataOption);
        command.AddOption(emailOption);

        command.SetHandler(async (data, logLevel, email) =>
        {
            await Tools.Catch(async () =>
            {
                var dataPath = Tools.DataPath(null, data);

                var host = Tools.CreateHost(logLevel, dataPath);
                using var scope = host.Services.CreateScope();
                var services = scope.ServiceProvider;

                var logger = services.GetRequiredService<ILogger<Program>>();

                await Tools.EnsureDatabaseMigratedAsync(services);
                var dbContext = services.GetRequiredService<AppDbContext>();
                var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

                var user = await userManager.FindByEmailAsync(email);
                if (user == null)
                    throw new Exception($"User {email} is not present.");

                logger.LogTrace($"Using data path {dataPath}");
                var dbPath = Path.Combine(dataPath, $"appdata.db");
                var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite($"Data Source={dbPath};Pooling=false");

                var context = new AppDbContext(optionsBuilder.Options);

                var apiKey = ApiKeyGenerator.GenerateApiKey();
                var apiKeyRecord = new ApiKey
                {
                    KeyHash = ApiKeyGenerator.ComputeHash(apiKey),
                    OwnerId = user.Id
                };

                context.ApiKeys.Add(apiKeyRecord);
                await context.SaveChangesAsync();

                Console.WriteLine(apiKey);

                Console.WriteLine("Done.");
            });

        }, dataOption, logLevelOption, emailOption);

        rootCommand.AddCommand(command);

        return rootCommand;
    }

    public static RootCommand AddRemoveApiKeyCommand(this RootCommand rootCommand)
    {
        var command = new Command("remove-apikey", "Remove all API keys for user");

        var logLevelOption = Tools.LogLevelOption;
        var dataOption = Tools.DataOption;

        var emailOption = new Option<string>("--email", description: "Email of the user") { IsRequired = true };

        command.AddGlobalOption(logLevelOption);
        command.AddGlobalOption(dataOption);
        command.AddOption(emailOption);

        command.SetHandler(async (data, logLevel, email) =>
        {
            await Tools.Catch(async () =>
            {
                var dataPath = Tools.DataPath(null, data);

                var host = Tools.CreateHost(logLevel, dataPath);
                using var scope = host.Services.CreateScope();
                var services = scope.ServiceProvider;

                var logger = services.GetRequiredService<ILogger<Program>>();

                await Tools.EnsureDatabaseMigratedAsync(services);
                var dbContext = services.GetRequiredService<AppDbContext>();
                var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

                var user = await userManager.FindByEmailAsync(email);
                if (user == null)
                    throw new Exception($"User {email} is not present.");

                logger.LogTrace($"Using data path {dataPath}");
                var dbPath = Path.Combine(dataPath, $"appdata.db");
                var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite($"Data Source={dbPath};Pooling=false");

                var context = new AppDbContext(optionsBuilder.Options);

                await context.ApiKeys
                    .Where(k => k.OwnerId == user.Id)
                    .ExecuteDeleteAsync();
 
                await context.SaveChangesAsync();

                Console.WriteLine("Done.");
            });

        }, dataOption, logLevelOption, emailOption);

        rootCommand.AddCommand(command);

        return rootCommand;
    }

}