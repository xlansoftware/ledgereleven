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

/// <summary>
/// Defines the interface for retrieving the current user's ledger database context.
/// This service acts as a convenient shortcut for common operations that only require the current ledger context.
/// Instead of injecting `IUserSpaceService` and calling `GetUserSpaceAsync()` followed by `GetLedgerDbContextAsync(space.Id, true)`,
/// this service directly provides the `LedgerDbContext` for the current user's space, streamlining access to ledger data.
/// </summary>
public interface ICurrentLedgerService
{
    /// <summary>
    /// Asynchronously retrieves the LedgerDbContext for the currently active user space.
    /// </summary>
    /// <returns>A Task that represents the asynchronous operation. The Task result contains the LedgerDbContext instance.</returns>
    Task<LedgerDbContext> GetLedgerDbContextAsync();
}

/// <summary>
/// Provides services for accessing the LedgerDbContext associated with the current user's active space.
/// </summary>
public class CurrentLedgerService : ICurrentLedgerService
{
    private readonly IUserSpaceService _userSpace;
    private readonly ILogger<CurrentLedgerService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CurrentLedgerService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="appConfig">The application configuration options.</param>
    /// <param name="userSpace">The user space service.</param>
    public CurrentLedgerService(
        ILogger<CurrentLedgerService> logger,
        IOptions<AppConfig> appConfig,
        IUserSpaceService userSpace)
    {
        _logger = logger;
        _userSpace = userSpace;
    }

    /// <summary>
    /// Asynchronously retrieves the LedgerDbContext for the currently active user space.
    /// It relies on the `IUserSpaceService` to determine the current space and retrieve the appropriate database context.
    /// </summary>
    /// <returns>A Task that represents the asynchronous operation. The Task result contains the LedgerDbContext instance.</returns>
    /// <exception cref="Exception">Thrown if the user has not selected a default space.</exception>
    public async Task<LedgerDbContext> GetLedgerDbContextAsync()
    {
        var space = await _userSpace.GetUserSpaceAsync();
        if (space == null)
            throw new Exception("User has not selected default space");

        return await _userSpace.GetLedgerDbContextAsync(space.Id, true);
    }



}
