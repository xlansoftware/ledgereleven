using System.CommandLine;
using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ledger11.data;
using ledger11.model.Data;

namespace ledger11.cli;

public static class Tools
{
    public static int ExitCode = 0;

    public static Option<string> DataOption => new Option<string>("--data", description: "Data folder") { IsRequired = false };
    public static Option<LogLevel> LogLevelOption => new Option<LogLevel>(
        name: "--log-level",
        description: "Set the logging level (e.g., Information, Debug, Warning)",
        getDefaultValue: () => LogLevel.Warning
    );

    public static IHost CreateHost(LogLevel logLevel, string dataFolder) =>
        Host.CreateDefaultBuilder()
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(logLevel);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlite($"Data Source={Path.Combine(dataFolder, "appdata.db")}"));

                services.
                    AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
                    // .AddIdentity<IdentityUser, IdentityRole>(options =>
                    {
                        options.SignIn.RequireConfirmedAccount = true;

                        // Customize password rules
                        options.Password.RequireDigit = false;
                        options.Password.RequiredLength = 2;
                        options.Password.RequireNonAlphanumeric = false;
                        options.Password.RequireUppercase = false;
                        options.Password.RequireLowercase = false;
                    })
                    .AddEntityFrameworkStores<AppDbContext>()
                    .AddDefaultTokenProviders();
            })
            .Build();

    public static async Task<Space> CreateSpaceAsync(this IServiceProvider services, ApplicationUser user, string name)
    {
        var _dbContext = services.GetRequiredService<AppDbContext>();
        var _userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // Create new space
        var newSpace = new Space
        {
            Name = name,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = user.Id,
            Members = new List<SpaceMember>()
        };

        _dbContext.Spaces.Add(newSpace);
        await _dbContext.SaveChangesAsync();

        // Add user as owner
        var spaceMember = new SpaceMember
        {
            SpaceId = newSpace.Id,
            UserId = user.Id,
            AccessLevel = AccessLevel.Owner
        };

        _dbContext.SpaceMembers.Add(spaceMember);

        // Assign new space as current space
        user.CurrentSpaceId = newSpace.Id;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            throw new Exception("Failed to update user with current space.");
        }

        await _dbContext.SaveChangesAsync();

        return newSpace;

    }

    public static string DataPath(ILogger? logger, string path)
    {
        logger?.LogTrace($"--data = {path}");
        logger?.LogTrace($"AppContext.BaseDirectory = {AppContext.BaseDirectory}");
        logger?.LogTrace($"Environment.CurrentDirectory = {Environment.CurrentDirectory}");

        if (string.IsNullOrWhiteSpace(path))
        {
            if (File.Exists(Path.Combine(AppContext.BaseDirectory, "appsettings.json")))
            {
                path = AppContext.BaseDirectory;
            }
            else if (File.Exists(Path.Combine(Environment.CurrentDirectory, "appsettings.json")))
            {
                path = Environment.CurrentDirectory;
            }
            else
            {
                throw new Exception("The default location has no appsettings.json. Use --data option to proveide path to the db files or appsettings.json file.");
            }

            logger?.LogTrace($"Using data path {path} ...");
        }

        var configFile = Path.Combine(path, "appsettings.json");

        if (!File.Exists(configFile))
        {
            return path;
        }

        logger?.LogTrace($"Found appsettings.json at {configFile} ...");

        var config = new ConfigurationBuilder()
            .SetBasePath(path)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        var result = config["AppConfig:DataPath"];
        if (result == null)
        {
            throw new Exception($"Path {path} has appsettings.json, but it has no setting for AppConfig:DataPath...");
        }

        return Path.GetFullPath(result, path);
    }

    public static ILogger CreateConsoleLogger(LogLevel level, string name)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddConsole()
                .SetMinimumLevel(level);
        });
        return loggerFactory.CreateLogger(name);
    }

    public static Task Catch(Func<Task> action)
    {
        try
        {
            return action();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            Tools.ExitCode = 1;
            return Task.CompletedTask;
        }
    }

    public static void Catch(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            Tools.ExitCode = 1;
        }
    }

}