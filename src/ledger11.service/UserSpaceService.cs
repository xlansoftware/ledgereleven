using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ledger11.data;
using ledger11.model.Data;
using ledger11.model.Config;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace ledger11.service;

public interface IUserSpaceService
{
    Task<Space?> GetUserSpaceAsync();
    Task<Space> CreateSpace(Space space);
    Task<Space> UpdateSpace(Guid spaceId, Dictionary<string, object> updatedFields);

    Task SetCurrentSpaceAsync(Guid id);
    Task DeleteSpaceAsync(Guid id);
    Task<List<Space>> GetAvailableSpacesAsync();
    Task ShareSpaceWithAsync(string targetUserEmail);
}

public class UserSpaceService : IUserSpaceService
{
    private readonly ILogger<UserSpaceService> _logger;
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    public UserSpaceService(
        ILogger<UserSpaceService> logger,
        AppDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<Space?> GetUserSpaceAsync()
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        if (user == null)
            throw new InvalidOperationException("No authenticated user.");

        if (user.CurrentSpaceId == null)
        {
            return await AssingDefaultSpace(user);
        }

        var existingSpace = await _dbContext.Spaces
            .FirstOrDefaultAsync(s => s.Id == user.CurrentSpaceId);

        return existingSpace;
    }

    private async Task<Space> AssingDefaultSpace(ApplicationUser user)
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
                Tint = "#000000",
                Currency = "EUR"
            });
        }

    }

    public async Task<Space> CreateSpace(Space space)
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        if (user == null)
            throw new InvalidOperationException("No authenticated user.");

        _logger.LogInformation($"Creating new space for {user.UserName} ...");

        // Create new space
        var newSpace = new Space
        {
            Name = space.Name,
            Tint = space.Tint,
            Currency = space.Currency,
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
        if (user.CurrentSpaceId == null)
        {
            user.CurrentSpaceId = newSpace.Id;
        }

        await _dbContext.SaveChangesAsync();

        return newSpace;
    }

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
        if (insensitive.ContainsKey("tint"))
            membership.Space.Tint = $"{insensitive["tint"]}";
        if (insensitive.ContainsKey("currency"))
            membership.Space.Currency = $"{insensitive["currency"]}";

        await _dbContext.SaveChangesAsync();
        return membership.Space;
    }

    public async Task ShareSpaceWithAsync(string targetUserEmail)
    {
        var currentUser = await _currentUserService.GetCurrentUserAsync();
        if (currentUser == null)
            throw new InvalidOperationException("No authenticated user.");

        if (currentUser.CurrentSpaceId == null)
            throw new InvalidOperationException("Current user has no active space.");

        var space = await _dbContext.Spaces
            .Include(s => s.Members)
            .FirstOrDefaultAsync(s => s.Id == currentUser.CurrentSpaceId);

        if (space == null)
            throw new InvalidOperationException("Space not found.");

        var currentMembership = await _dbContext.SpaceMembers
            .FirstOrDefaultAsync(sm => sm.SpaceId == space.Id && sm.UserId == currentUser.Id);

        if (currentMembership?.AccessLevel != AccessLevel.Owner)
            throw new UnauthorizedAccessException("Only the owner can share the space.");

        targetUserEmail = targetUserEmail.ToLower();
        var targetUser = await _dbContext.Users.FirstOrDefaultAsync((user) => user.NormalizedEmail == targetUserEmail);
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
}
