using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ledger11.data;
using ledger11.model.Data;
using ledger11.model.Config;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ledger11.service;

/// <summary>
/// Defines the interface for managing user spaces (ledgers).
/// </summary>
public interface IUserSpaceService
{
    /// <summary>
    /// Retrieves the current space for the authenticated user.
    /// </summary>
    /// <returns>The current Space object, or null if none is assigned.</returns>
    Task<Space?> GetUserSpaceAsync();

    /// <summary>
    /// Creates a new space for the authenticated user.
    /// </summary>
    /// <param name="space">The Space object containing details for the new space.</param>
    /// <returns>The newly created Space object.</returns>
    Task<Space> CreateSpace(Space space, Dictionary<string, string> settings);

    /// <summary>
    /// Updates an existing space with the provided fields.
    /// </summary>
    /// <param name="spaceId">The ID of the space to update.</param>
    /// <param name="updatedFields">A dictionary of fields to update (e.g., "Name", "Tint", "Currency").</param>
    /// <returns>The updated Space object.</returns>
    Task<Space> UpdateSpace(Guid spaceId, Dictionary<string, object> updatedFields);

    /// <summary>
    /// Sets the current active space for the authenticated user.
    /// </summary>
    /// <param name="id">The ID of the space to set as current.</param>
    Task SetCurrentSpaceAsync(Guid id);

    /// <summary>
    /// Retrieves a space by its ID.
    /// </summary>
    /// <param name="spaceId">The ID of the space to retrieve.</param>
    /// <returns>The Space object, or null if not found.</returns>
    Task<Space?> GetUserSpaceByIdAsync(Guid spaceId);

    /// <summary>
    /// Deletes a space. Only the owner of the space can delete it.
    /// </summary>
    /// <param name="id">The ID of the space to delete.</param>
    Task DeleteSpaceAsync(Guid id);

    /// <summary>
    /// Retrieves a list of all spaces available to the authenticated user.
    /// </summary>
    /// <returns>A list of Space objects.</returns>
    Task<List<Space>> GetAvailableSpacesAsync();

    /// <summary>
    /// Shares a space with another user by their email address.
    /// Only the owner of the space can share it.
    /// </summary>
    /// <param name="spaceId">The ID of the space to share.</param>
    /// <param name="targetUserEmail">The email of the user to share the space with.</param>
    Task ShareSpaceWithAsync(Guid spaceId, string targetUserEmail);

    /// <summary>
    /// Gets a LedgerDbContext instance for a specific space, optionally initializing its database.
    /// </summary>
    /// <param name="spaceId">The ID of the space for which to get the context.</param>
    /// <param name="initialize">If true, ensures the database for the space is migrated and seeded with default categories if empty.</param>
    /// <returns>A LedgerDbContext instance configured for the specified space.</returns>
    Task<LedgerDbContext> GetLedgerDbContextAsync(Guid spaceId, bool initialize);
}

/// <summary>
/// Service for managing user-specific spaces (ledgers) and their associated data.
/// Handles creation, retrieval, updating, deletion, and sharing of spaces.
/// </summary>
public class UserSpaceService : IUserSpaceService
{
    private readonly ILogger<UserSpaceService> _logger;
    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserService _currentUserService;
    private readonly AppConfig _appConfig;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserSpaceService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="dbContext">The application database context.</param>
    /// <param name="currentUserService">The current user service.</param>
    /// <param name="userManager">The user manager for handling application users.</param>
    /// <param name="appConfig">The application configuration options.</param>
    public UserSpaceService(
        ILogger<UserSpaceService> logger,
        AppDbContext dbContext,
        ICurrentUserService currentUserService,
        UserManager<ApplicationUser> userManager,
        IOptions<AppConfig> appConfig)
    {
        _logger = logger;
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _userManager = userManager;
        _appConfig = appConfig.Value;
    }

    /// <summary>
    /// Retrieves the current space for the authenticated user.
    /// If no current space is set, it attempts to assign a default space.
    /// </summary>
    /// <returns>The current Space object, or null if no space can be assigned.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no authenticated user is found.</exception>
    public async Task<Space?> GetUserSpaceAsync()
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        if (user == null)
            throw new InvalidOperationException("No authenticated user.");

        if (user.CurrentSpaceId == null)
        {
            return await AssignDefaultSpace(user);
        }

        var existingSpace = await _dbContext.Spaces
            .FirstOrDefaultAsync(s => s.Id == user.CurrentSpaceId);

        return existingSpace;
    }

    /// <summary>
    /// Assigns a default space to the user if no current space is set.
    /// Prioritizes spaces created by the user, then shared spaces, and finally creates a new default space.
    /// </summary>
    /// <param name="user">The application user.</param>
    /// <returns>The assigned or newly created Space object.</returns>
    private async Task<Space> AssignDefaultSpace(ApplicationUser user)
    {
        var userSpaces = await _dbContext.Spaces
            .Where(s => s.CreatedByUserId == user.Id)
            .ToListAsync();

        if (userSpaces.Count > 0)
        {
            // assign some of the other spaces the user has created
            var space = userSpaces[0];
            user.CurrentSpaceId = space.Id;
            await _dbContext.SaveChangesAsync();
            return space;
        }

        var sharedSpaces = await _dbContext.SpaceMembers
            .Include(s => s.Space)
            .Where(sm => sm.UserId == user.Id)
            .ToListAsync();

        if (sharedSpaces.Count > 0)
        {
            // assign some og the spaces shared with the user
            var space = sharedSpaces[0].Space;
            user.CurrentSpaceId = space.Id;
            await _dbContext.SaveChangesAsync();
            return space;
        }

        {
            // create new space and assign it
            return await CreateSpace(new Space()
            {
                Name = "Ledger",
            }, new Dictionary<string, string>
            {
                { "Tint", "#000000" },
                { "Currency", "USD" },
            });
        }

    }

    /// <summary>
    /// Creates a new space for the authenticated user and assigns them as the owner.
    /// </summary>
    /// <param name="space">The Space object containing details for the new space.</param>
    /// <returns>The newly created Space object.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no authenticated user is found.</exception>
    /// <exception cref="Exception">Thrown if the user ID is empty.</exception>
    public async Task<Space> CreateSpace(Space space, Dictionary<string, string> settings)
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        if (user == null)
            throw new InvalidOperationException("No authenticated user.");

        if (user.Id == Guid.Empty)
            throw new Exception("User id is empty...");

        _logger.LogInformation($"Creating new space for {user.UserName} ...");

        // Create new space
        var newSpace = new Space
        {
            Name = space.Name,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = user.Id,
            Members = new List<SpaceMember>()
        };
        
        _dbContext.Spaces.Add(newSpace);
        await _dbContext.SaveChangesAsync();

        await UpdateSpaceSettings(newSpace, settings);

        // Add user as owner
        var spaceMember = new SpaceMember
        {
            SpaceId = newSpace.Id,
            UserId = user.Id,
            AccessLevel = AccessLevel.Owner
        };

        _dbContext.SpaceMembers.Add(spaceMember);

        // Assign new space as current space
        if (user.CurrentSpaceId == null)
        {
            user.CurrentSpaceId = newSpace.Id;
        }

        await _dbContext.SaveChangesAsync();

        return newSpace;
    }

    /// <summary>
    /// Sets the current active space for the authenticated user.
    /// The user must be a member of the specified space.
    /// </summary>
    /// <param name="spaceId">The ID of the space to set as current.</param>
    /// <exception cref="InvalidOperationException">Thrown if no authenticated user is found.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown if the user is not a member of the space.</exception>
    public async Task SetCurrentSpaceAsync(Guid spaceId)
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        if (user == null)
            throw new InvalidOperationException("No authenticated user.");

        var isMember = await _dbContext.SpaceMembers
            .AnyAsync(sm => sm.UserId == user.Id && sm.SpaceId == spaceId);

        if (!isMember)
            throw new UnauthorizedAccessException("User is not a member of this space.");

        user.CurrentSpaceId = spaceId;
        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Deletes a space. Only the owner of the space can delete it.
    /// If the deleted space is the user's current space, another available space is assigned as current.
    /// </summary>
    /// <param name="spaceId">The ID of the space to delete.</param>
    /// <exception cref="InvalidOperationException">Thrown if no authenticated user is found.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown if the user is not the owner of the space.</exception>
    public async Task DeleteSpaceAsync(Guid spaceId)
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        if (user == null)
            throw new InvalidOperationException("No authenticated user.");

        // all spaces accessible to the user
        var spaces = await _dbContext.SpaceMembers
            .Where(sm => sm.UserId == user.Id)
            .Include(sm => sm.Space)
            .ToListAsync();

        var member = spaces
            .FirstOrDefault(sm => sm.SpaceId == spaceId
                && sm.AccessLevel == AccessLevel.Owner);

        if (member == null)
            throw new UnauthorizedAccessException("User is not the owner of this space.");

        // Assign another space as current if the user is the owner of the current space
        if (user.CurrentSpaceId == spaceId)
        {
            var otherSpace = spaces
                .FirstOrDefault(sm => sm.SpaceId != spaceId);

            user.CurrentSpaceId = otherSpace?.SpaceId;
            await _dbContext.SaveChangesAsync();
        }

        // Remove all members of the space
        var members = _dbContext.SpaceMembers.Where(sm => sm.SpaceId == spaceId);
        _dbContext.SpaceMembers.RemoveRange(members);

        // Remove the space itself
        _dbContext.Spaces.Remove(member.Space);

        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Retrieves a list of all spaces that the authenticated user is a member of.
    /// </summary>
    /// <returns>A list of Space objects available to the user.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no authenticated user is found.</exception>
    public async Task<List<Space>> GetAvailableSpacesAsync()
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        if (user == null)
            throw new InvalidOperationException("No authenticated user.");

        var spaceIds = await _dbContext.SpaceMembers
            .Where(sm => sm.UserId == user.Id)
            .Select(sm => sm.SpaceId)
            .ToListAsync();

        var spaces = await _dbContext.Spaces
            .Where(s => spaceIds.Contains(s.Id))
            .ToListAsync();

        return spaces;
    }

    /// <summary>
    /// Updates an existing space with the provided fields.
    /// Only the owner of the space can update it.
    /// </summary>
    /// <param name="spaceId">The ID of the space to update.</param>
    /// <param name="updatedFields">A dictionary of fields to update (e.g., "Name", "Tint", "Currency").
    /// Keys are case-insensitive.</param>
    /// <returns>The updated Space object.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no authenticated user is found.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown if the user is not the owner of the space.</exception>
    public async Task<Space> UpdateSpace(Guid spaceId, Dictionary<string, object> updatedFields)
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        if (user == null)
            throw new InvalidOperationException("No authenticated user.");

        var membership = await _dbContext.SpaceMembers
            .Include(sm => sm.Space)
            .FirstOrDefaultAsync(sm => sm.UserId == user.Id &&
                                       sm.SpaceId == spaceId &&
                                       sm.AccessLevel == AccessLevel.Owner);

        if (membership == null)
            throw new UnauthorizedAccessException("The user is not the owner of the space.");

        var insensitive = new Dictionary<string, object>(updatedFields.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in updatedFields)
        {
            insensitive[kvp.Key] = kvp.Value;
        }

        if (insensitive.ContainsKey("name"))
            membership.Space.Name = $"{insensitive["name"]}";

        if (insensitive.TryGetValue("settings", out var settingsValue))
        {
            Dictionary<string, string>? settings = settingsValue switch
            {
                Dictionary<string, string> s => s,
                JsonElement jsonElement when jsonElement.ValueKind == JsonValueKind.Object =>
                    JsonElementToDictionary(jsonElement),
                _ => null
            };

            if (settings != null && settings.Count > 0)
            {
                await UpdateSpaceSettings(membership.Space, settings);
            }
        }

        await _dbContext.SaveChangesAsync();
        return membership.Space;
    }

    // Helper method to convert JsonElement to Dictionary<string, string>
    private static Dictionary<string, string> JsonElementToDictionary(JsonElement element)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in element.EnumerateObject())
        {
            dict[prop.Name] = prop.Value.GetString() ?? string.Empty;
        }
        return dict;
    }

    private async Task UpdateSpaceSettings(Space space, Dictionary<string, string> settings)
    {
        if (space == null)
        {
            _logger.LogWarning("UpdateSpaceSettings called with null space.");
            return;
        }

        if (settings == null || settings.Count == 0)
        {
            _logger.LogInformation("No settings provided to update for space {SpaceId}.", space.Id);
            return;
        }

        try
        {
            _logger.LogDebug("Attempting to get ledger context for space {SpaceId}.", space.Id);
            var ledgerContext = await GetLedgerDbContextAsync(space.Id, true);

            if (ledgerContext == null)
            {
                _logger.LogWarning("Ledger context not found for space {SpaceId}.", space.Id);
                return;
            }

            _logger.LogInformation("Updating {SettingsCount} settings for space {SpaceId}.", settings.Count, space.Id);

            foreach (var setting in settings)
            {
                var existingSetting = await ledgerContext.Settings
                    .FirstOrDefaultAsync(s => s.Key == setting.Key);

                if (existingSetting != null)
                {
                    _logger.LogDebug("Updating existing setting '{SettingKey}' for space {SpaceId}.", setting.Key, space.Id);
                    existingSetting.Value = setting.Value;
                }
                else
                {
                    _logger.LogDebug("Adding new setting '{SettingKey}' for space {SpaceId}.", setting.Key, space.Id);
                    ledgerContext.Settings.Add(new Setting
                    {
                        Key = setting.Key,
                        Value = setting.Value
                    });
                }
            }

            await ledgerContext.SaveChangesAsync();
            _logger.LogInformation("Successfully updated settings for space {SpaceId}.", space.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating space settings for space {SpaceId}.", space.Id);
        }
    }


    // Calculate total value and transaction count

    /// <summary>
    /// Shares a space with another user by their email address.
    /// Only the owner of the space can share it.
    /// The target user will be added as an Editor.
    /// </summary>
    /// <param name="spaceId">The ID of the space to share.</param>
    /// <param name="targetUserEmail">The email of the user to share the space with.</param>
    /// <exception cref="InvalidOperationException">Thrown if no authenticated user is found, or if the space or target user is not found.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown if the current user is not the owner of the space.</exception>
    public async Task ShareSpaceWithAsync(Guid spaceId, string targetUserEmail)
    {
        var currentUser = await _currentUserService.GetCurrentUserAsync();
        if (currentUser == null)
            throw new InvalidOperationException("No authenticated user.");

        var space = await _dbContext.Spaces
            .Include(s => s.Members)
            .FirstOrDefaultAsync(s => s.Id == spaceId);

        if (space == null)
            throw new InvalidOperationException("Space not found.");

        var currentMembership = await _dbContext.SpaceMembers
            .FirstOrDefaultAsync(sm => sm.SpaceId == space.Id && sm.UserId == currentUser.Id);

        if (currentMembership?.AccessLevel != AccessLevel.Owner)
            throw new UnauthorizedAccessException("Only the owner can share the space.");

        var targetUser = await _userManager.FindByEmailAsync(targetUserEmail);
        if (targetUser == null)
            throw new InvalidOperationException("Target user not found.");

        var alreadyMember = await _dbContext.SpaceMembers
            .AnyAsync(sm => sm.SpaceId == space.Id && sm.UserId == targetUser.Id);

        if (!alreadyMember)
        {
            _dbContext.SpaceMembers.Add(new SpaceMember
            {
                SpaceId = space.Id,
                UserId = targetUser.Id,
                AccessLevel = AccessLevel.Editor
            });

            await _dbContext.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Gets a LedgerDbContext instance for a specific space, optionally initializing its database.
    /// The database path is determined by the AppConfig.DataPath and the space ID.
    /// </summary>
    /// <param name="spaceId">The ID of the space for which to get the context.</param>
    /// <param name="initialize">If true, ensures the database for the space is migrated and seeded with default categories if empty.</param>
    /// <returns>A LedgerDbContext instance configured for the specified space.</returns>
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

    /// <summary>
    /// A static list of default categories to be used when initializing a new ledger database.
    /// </summary>
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

    /// <summary>
    /// Initializes the LedgerDbContext by applying migrations and seeding default categories if the category table is empty.
    /// </summary>
    /// <param name="context">The LedgerDbContext instance to initialize.</param>
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

    /// <summary>
    /// Retrieves a space by its ID.
    /// </summary>
    /// <param name="spaceId">The ID of the space to retrieve.</param>
    /// <returns>The Space object, or null if not found.</returns>
    public async Task<Space?> GetUserSpaceByIdAsync(Guid spaceId)
    {
        //TODO: check if the user has access to the space before returning...
        return await _dbContext.Spaces
            .FirstOrDefaultAsync(s => s.Id == spaceId);
    }

    /// <summary>
    /// Sanitizes a string to be used as a valid file name by replacing invalid characters with underscores.
    /// </summary>
    /// <param name="fileName">The original file name string.</param>
    /// <returns>The sanitized file name string.</returns>
    public static string SanitizeFileName(string fileName)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(c, '_');
        }
        return fileName;
    }


}
