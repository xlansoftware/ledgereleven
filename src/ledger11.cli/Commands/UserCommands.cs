using System.CommandLine;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ledger11.model.Data;

namespace ledger11.cli;

public static class UserCommands
{

    public static RootCommand AddListUsersComand(this RootCommand rootCommand)
    {
        var listUsersCommand = new Command("list-users", "Show the users");

        var logLevelOption = Tools.LogLevelOption;
        var dataOption = Tools.DataOption;

        listUsersCommand.AddGlobalOption(logLevelOption);
        listUsersCommand.AddGlobalOption(dataOption);

        listUsersCommand.SetHandler(async (data, logLevel) =>
        {
            var host = Tools.CreateHost(logLevel, data);
            using var scope = host.Services.CreateScope();
            var services = scope.ServiceProvider;

            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            var users = await userManager.Users.ToListAsync();
            foreach (var user in users)
            {
                Console.WriteLine($"{user.Email}");
            }

        }, dataOption, logLevelOption);

        rootCommand.AddCommand(listUsersCommand);

        return rootCommand;
    }

    public static RootCommand AddCreateUserComand(this RootCommand rootCommand)
    {
        var createUserCommand = new Command("create-user", "Create a new identity user");

        var logLevelOption = Tools.LogLevelOption;
        var dataOption = Tools.DataOption;

        var emailOption = new Option<string>("--email", description: "Email of the user") { IsRequired = true };
        var passwordOption = new Option<string>("--password", description: "Password for the user") { IsRequired = true };

        createUserCommand.AddGlobalOption(logLevelOption);
        createUserCommand.AddGlobalOption(dataOption);
        createUserCommand.AddOption(emailOption);
        createUserCommand.AddOption(passwordOption);

        createUserCommand.SetHandler(async (logLevel, data, email, password) =>
        {
            await Tools.Catch(async () =>
            {
                var host = Tools.CreateHost(logLevel, data);
                using var scope = host.Services.CreateScope();
                var services = scope.ServiceProvider;

                var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

                var user = await userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    var newUser = new ApplicationUser { Email = email, UserName = email, EmailConfirmed = true };
                    var result = await userManager.CreateAsync(newUser, password);

                    if (result.Succeeded)
                    {
                        Console.WriteLine("User created successfully.");

                        var space = await services.CreateSpaceAsync(newUser, "Ledger");
                        Console.WriteLine($"User space {space.Name} for user {newUser.Email} created successfully.");
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                            Console.WriteLine($"Error: {error.Description}");
                    }
                }
                else
                {
                    Console.WriteLine("User already exists.");
                }
            });

        }, logLevelOption, dataOption, emailOption, passwordOption);

        rootCommand.AddCommand(createUserCommand);

        return rootCommand;
    }

}