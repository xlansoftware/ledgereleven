using System.Runtime.CompilerServices;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ledger11.data;
using ledger11.model.Config;
using ledger11.model.Data;

namespace ledger11.service;

public interface ICurrentLedgerService
{
    Task<LedgerDbContext> GetLedgerDbContextAsync();
    Task<LedgerDbContext> GetLedgerDbContextAsync(Guid spaceId, bool initialize);
}

public class CurrentLedgerService : ICurrentLedgerService
{
    private readonly AppConfig _appConfig;
    private readonly IUserSpaceService _userSpace;
    private readonly ILogger<CurrentLedgerService> _logger;

    public CurrentLedgerService(
        ILogger<CurrentLedgerService> logger,
        IOptions<AppConfig> appConfig,
        IUserSpaceService userSpace)
    {
        _logger = logger;
        _appConfig = appConfig.Value;
        _userSpace = userSpace;
    }

    public async Task<LedgerDbContext> GetLedgerDbContextAsync()
    {
        var space = await _userSpace.GetUserSpaceAsync();
        if (space == null)
            throw new Exception("User has not selected default space");

        return await GetLedgerDbContextAsync(space.Id, true);
    }

    public async Task<LedgerDbContext> GetLedgerDbContextAsync(Guid spaceId, bool initialize)
    {

        var memory = string.Compare(_appConfig.DataPath, "memory", StringComparison.OrdinalIgnoreCase) == 0;
        var dbPath = memory
            ? ":memory:"
            : Path.Combine(_appConfig.DataPath, $"space-{SanitizeFileName(spaceId.ToString())}.db");

        _logger.LogTrace($"Creating LedgerDbContext: ${dbPath}");

        var optionsBuilder = new DbContextOptionsBuilder<LedgerDbContext>()
            .UseSqlite($"Data Source={dbPath};Pooling={_appConfig.Pooling}");

        var options = optionsBuilder.Options;

        var context = new LedgerDbContext(options);

        if (memory)
        {
            // For in-memory SQLite, you MUST open the connection manually and keep it open
            await context.Database.OpenConnectionAsync();
        }

        if (initialize)
        {
            await InitializeDbAsync(context);
        }

        return context;
    }

    private static List<Category> defaultCategories = [
        new Category { Name = "Groceries", Color = "#fde68a", Icon = "shopping-cart" },
        new Category { Name = "Entertainment", Color = "#bae6fd", Icon = "film" },
        new Category { Name = "Education", Color = "#fef9c3", Icon = "book" },
        new Category { Name = "Sport", Color = "#bae6fd", Icon = "dumbbell" },
        new Category { Name = "Health / Medical", Color = "#fecaca", Icon = "heart" },
        new Category { Name = "Personal Care", Color = "#ddd6fe", Icon = "smile" },
        new Category { Name = "Transportation", Color = "#bbf7d0", Icon = "car" },
        new Category { Name = "Dining Out", Color = "#fbcfe8", Icon = "utensils"},
        new Category { Name = "Clothing", Color = "#e0f2fe", Icon = "shirt"},
        new Category { Name = "Gifts", Color = "#d9f99d", Icon = "gift" },
        new Category { Name = "Travel", Color = "#a7f3d0", Icon = "plane" },
        new Category { Name = "Savings", Color = "#f0abfc", Icon = "piggy-bank" },
        new Category { Name = "Utilities", Color = "#a5b4fc", Icon = "plug" },
        new Category { Name = "Subscriptions", Color = "#fde2e4", Icon = "credit-card" },
        new Category { Name = "Insurance", Color = "#fcd34d", Icon = "shield" },
        new Category { Name = "Rent / Mortgage", Color = "#fca5a5", Icon = "home" },
        new Category { Name = "Miscellaneous", Color = "#f5f5f4", Icon = "dots-horizontal" }
    ];

    public static async Task InitializeDbAsync(LedgerDbContext context)
    {
        await context.Database.MigrateAsync();

        // if Categoty is empty, add default categories
        if (!await context.Categories.AnyAsync())
        {
            context.Categories.AddRange(
                defaultCategories
                    .Select((c, index) => new Category
                    {
                        Name = c.Name,
                        Color = c.Color,
                        Icon = c.Icon,
                        DisplayOrder = index + 1,
                    })
            );
            await context.SaveChangesAsync();
        }

    }

    public static string SanitizeFileName(string fileName)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(c, '_');
        }
        return fileName;
    }


}
