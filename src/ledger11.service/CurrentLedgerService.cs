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

    /// <summary>
    /// Updates the default currency for the ledger with id spaceId.
    /// This operation finds all transactions using the old default currency (where Currency is null),
    /// explicitly sets their currency to the old default, and applies the provided exchange rate.
    /// It then updates the ledger's default currency to the new one.
    /// </summary>
    /// <param name="spaceId">The id of the ledger</param>
    /// <param name="newCurrency">The new ISO 4217 currency code to set as the default.</param>
    /// <param name="exchangeRate">The exchange rate to convert from the old default currency to the new one.</param>
    Task UpdateDefaultCurrencyAsync(Guid spaceId, string newCurrency, decimal exchangeRate);
}

/// <summary>
/// Provides services for accessing the LedgerDbContext associated with the current user's active space.
/// </summary>
public class CurrentLedgerService : ICurrentLedgerService
{
    private readonly IUserSpaceService _userSpace;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<CurrentLedgerService> _logger;
    private readonly IExchangeRateService _exchangeRateService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CurrentLedgerService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="appConfig">The application configuration options.</param>
    /// <param name="userSpace">The user space service.</param>
    /// <param name="dbContext">The application database context.</param>
    /// <param name="exchangeRateService">The exchange rate service.</param>
    public CurrentLedgerService(
        ILogger<CurrentLedgerService> logger,
        IOptions<AppConfig> appConfig,
        IUserSpaceService userSpace,
        AppDbContext dbContext,
        IExchangeRateService exchangeRateService)
    {
        _logger = logger;
        _userSpace = userSpace;
        _dbContext = dbContext;
        _exchangeRateService = exchangeRateService;
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

    public async Task UpdateDefaultCurrencyAsync(Guid spaceId, string newCurrency, decimal exchangeRate)
    {
        var space = await _userSpace.GetUserSpaceByIdAsync(spaceId);
        if (space == null)
            throw new InvalidOperationException("No active user space found.");

        var oldCurrency = space.Currency ?? "USD";
        if (oldCurrency == newCurrency)
        {
            return;
        }

        var ledgerDb = await _userSpace.GetLedgerDbContextAsync(space.Id, false);

        // Handle transactions that were implicitly in the old default currency
        var nullCurrencyTransactions = await ledgerDb.Transactions
            .Where(t => t.Currency == null)
            .ToListAsync();

        foreach (var transaction in nullCurrencyTransactions)
        {
            transaction.Currency = oldCurrency;
            transaction.ExchangeRate = exchangeRate;
        }

        // Handle transactions that had an explicit currency set
        var explicitCurrencyTransactions = await ledgerDb.Transactions
            .Where(t => t.Currency != null)
            .ToListAsync();

        foreach (var transaction in explicitCurrencyTransactions)
        {
            // If the transaction's currency is the new default, the rate is 1. Represent this as null.
            if (transaction.Currency == newCurrency)
            {
                transaction.ExchangeRate = null;
                continue;
            }

            // Otherwise, fetch the new rate to convert from its currency to the new default currency
            var newRate = await _exchangeRateService.GetExchangeRateAsync(transaction.Currency!, newCurrency);
            if (newRate == null)
            {
                throw new InvalidOperationException($"Could not retrieve new exchange rate from {transaction.Currency} to {newCurrency}. Aborting update.");
            }
            transaction.ExchangeRate = newRate;
        }

        space.Currency = newCurrency;

        await ledgerDb.SaveChangesAsync();
        await _dbContext.SaveChangesAsync(); 
    }
}
