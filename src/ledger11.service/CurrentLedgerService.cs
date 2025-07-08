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
}

public class CurrentLedgerService : ICurrentLedgerService
{
    private readonly IUserSpaceService _userSpace;
    private readonly ILogger<CurrentLedgerService> _logger;

    public CurrentLedgerService(
        ILogger<CurrentLedgerService> logger,
        IOptions<AppConfig> appConfig,
        IUserSpaceService userSpace)
    {
        _logger = logger;
        _userSpace = userSpace;
    }

    public async Task<LedgerDbContext> GetLedgerDbContextAsync()
    {
        var space = await _userSpace.GetUserSpaceAsync();
        if (space == null)
            throw new Exception("User has not selected default space");

        return await _userSpace.GetLedgerDbContextAsync(space.Id, true);
    }



}
