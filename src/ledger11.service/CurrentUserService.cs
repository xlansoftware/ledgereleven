using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using ledger11.model.Data;
using ledger11.data;
using Microsoft.EntityFrameworkCore;
using DocumentFormat.OpenXml.ExtendedProperties;
using Microsoft.Extensions.Logging;

namespace ledger11.service;

public interface ICurrentUserService
{
    string? GetCurrentUserName();
    Task<Guid?> GetCurrentUserIdAsync();
    Task<ApplicationUser?> GetCurrentUserAsync();
    Task<ApplicationUser> EnsureUser(ClaimsPrincipal claimsPrincipal);
}

public class CurrentUserService : ICurrentUserService
{
    private readonly ILogger<CurrentUserService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AppDbContext _appDbContext;

    private readonly UserManager<ApplicationUser> _userManager;

    public CurrentUserService(
        ILogger<CurrentUserService> logger,
        UserManager<ApplicationUser> userManager,
        AppDbContext appDbContext,
        IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
        _appDbContext = appDbContext;
    }

    public string? GetCurrentUserName()
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        return principal?.Identity?.Name;
    }

    public string? GetCurrentUserEmail()
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        if (principal == null) return null;

        var email = principal.FindFirst("email")?.Value ??
            principal.FindFirst(ClaimTypes.Email)?.Value;

        return email;
    }

    public async Task<Guid?> GetCurrentUserIdAsync()
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        if (principal == null) return null;

        var user = await EnsureUser(principal);
        return user.Id;
    }

    public async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        if (principal == null) return null;

        var user = await EnsureUser(principal);
        return user;
    }

    private void DumpClaims(ClaimsPrincipal claimsPrincipal)
    {
        _logger.LogInformation($"Claims received:");
        foreach (var claim in claimsPrincipal.Claims)
        {
            _logger.LogInformation($"{claim.Type}  = {claim.Value}");
        }
    }

    public async Task<ApplicationUser> EnsureUser(ClaimsPrincipal claimsPrincipal)
    {
        var email = claimsPrincipal.FindFirst(ClaimTypes.Email)?.Value
            ?? claimsPrincipal.FindFirst("email")?.Value;
        if (string.IsNullOrWhiteSpace(email))
        {
            DumpClaims(claimsPrincipal);
            throw new Exception("'email' claim is required");
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user != null) return user;

        // Create new user
        var name = claimsPrincipal.FindFirst(ClaimTypes.Name)?.Value
            ?? claimsPrincipal.FindFirst("name")?.Value;
        user = new ApplicationUser { UserName = name ?? email, Email = email };
        var result = await _userManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            // Check for unique constraint failure (DB-specific error message or code)
            var isUniqueConstraintError = result.Errors.Any(e =>
                e.Description.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase) ||
                e.Code == "DuplicateUserName" || e.Code == "DuplicateEmail"
            );

            if (isUniqueConstraintError)
            {
                // User was likely created by another process between Find and Create
                user = await _userManager.FindByEmailAsync(email);
                if (user != null) return user;
            }

            throw new Exception($"Create user failed: {string.Join("; ", result.Errors.Select(e => e.Description))}");
        }

        _logger.LogInformation($"User created for {email}, id = {user.Id}");

        return user;
    }
}
