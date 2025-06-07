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

    private async Task<T> Retry<T>(int maxRetries, Func<Task<T>> action, int delayMs = 100)
    {
        int retries = 0;
        while (true)
        {
            try
            {
                return await action();
            }
            catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.SqliteErrorCode == 5) // database locked
            {
                retries++;
                if (retries > maxRetries)
                    throw;

                await Task.Delay(delayMs);
            }
        }
    }

    public async Task<ApplicationUser> EnsureUser(ClaimsPrincipal claimsPrincipal)
    {
        var idd = Guid.NewGuid().ToString("N");
        // Do your user/login creation logic here
        var externalUserId = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? claimsPrincipal.FindFirst("sub")?.Value;
        if (externalUserId == null)
        {
            DumpClaims(claimsPrincipal);
            throw new Exception("'sub' claim is required");
        }

        var provider = claimsPrincipal.FindFirst("auth_scheme")?.Value;
        if (provider == null)
        {
            DumpClaims(claimsPrincipal);
            throw new Exception("'auth_scheme' claim should be added by the handler");
        }

        var user = await _userManager.FindByLoginAsync(provider, externalUserId);
        if (user != null) return user;

        // Create new user
        var email = claimsPrincipal.FindFirst(ClaimTypes.Email)?.Value;
        var name = claimsPrincipal.FindFirst(ClaimTypes.Name)?.Value;
        user = new ApplicationUser { UserName = name ?? email, Email = email ?? name };
        await _userManager.CreateAsync(user);
        await _userManager.AddLoginAsync(user, new UserLoginInfo(provider, externalUserId, provider));

        _logger.LogInformation($"Ensured user with id = {user.Id}");

        return user;
    }
}
