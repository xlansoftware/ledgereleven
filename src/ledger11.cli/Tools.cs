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

    /// <summary>
    /// Determines the absolute path to the data directory.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostic messages.</param>
    /// <param name="path">
    /// An optional path that can be either:
    /// 1. The direct path to the data folder.
    /// 2. The path to the application's root folder (containing appsettings.json).
    /// If not provided, the method will try to locate the application root automatically.
    /// </param>
    /// <returns>The absolute path to the data directory.</returns>
    /// <exception cref="Exception">
    /// Thrown if the data path cannot be determined, for example, if appsettings.json is not found
    /// or if it's missing the required 'AppConfig:DataPath' setting.
    /// </exception>
    public static string DataPath(ILogger? logger, string path)
    {
        logger?.LogTrace($"--data parameter = {path}");

        string appRootPath = path;

        // 1. Determine the application root path if not explicitly provided.
        if (string.IsNullOrWhiteSpace(appRootPath))
        {
            logger?.LogTrace("Data path not provided. Attempting to discover appsettings.json...");
            logger?.LogTrace($"Searching in: {AppContext.BaseDirectory}");
            logger?.LogTrace($"Searching in: {Environment.CurrentDirectory}");

            if (File.Exists(Path.Combine(AppContext.BaseDirectory, "appsettings.json")))
            {
                appRootPath = AppContext.BaseDirectory;
            }
            else if (File.Exists(Path.Combine(Environment.CurrentDirectory, "appsettings.json")))
            {
                appRootPath = Environment.CurrentDirectory;
            }
            else
            {
                throw new Exception("Could not find appsettings.json in default locations. Please use the --data option to specify the path to the data folder or the application root.");
            }
            logger?.LogTrace($"Found application root at: {appRootPath}");
        }

        // 2. Convert to an absolute path to handle relative inputs.
        var absolutePath = Path.GetFullPath(appRootPath, Environment.CurrentDirectory);
        logger?.LogTrace($"Resolved path to: {absolutePath}");

        var configFile = Path.Combine(absolutePath, "appsettings.json");

        // 3. Check if the path is the application root (contains appsettings.json).
        if (File.Exists(configFile))
        {
            logger?.LogTrace($"Found configuration file: {configFile}");

            var config = new ConfigurationBuilder()
                .SetBasePath(absolutePath)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var dataPathFromConfig = config["AppConfig:DataPath"];

            if (string.IsNullOrWhiteSpace(dataPathFromConfig))
            {
                throw new Exception($"The configuration file at '{configFile}' is missing the required 'AppConfig:DataPath' setting.");
            }

            // The path from config can be relative to the app root. Resolve it.
            var finalPath = Path.GetFullPath(dataPathFromConfig, absolutePath);
            logger?.LogTrace($"Data path from config: '{dataPathFromConfig}', resolved to: '{finalPath}'");
            return finalPath;
        }
        else
        {
            // 4. If no appsettings.json, assume the provided path is the data directory itself.
            logger?.LogTrace($"No appsettings.json found at '{absolutePath}'. Assuming it is the data directory.");
            return absolutePath;
        }
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