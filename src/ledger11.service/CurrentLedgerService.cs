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
        AppDbContext dbContext)
    {
        _logger = logger;
        _userSpace = userSpace;
        _dbContext = dbContext;
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
        _logger.LogInformation("UpdateDefaultCurrencyAsync started for SpaceId: {SpaceId}, NewCurrency: {NewCurrency}, ExchangeRate: {ExchangeRate}", spaceId, newCurrency, exchangeRate);

        var space = await _userSpace.GetUserSpaceByIdAsync(spaceId);
        if (space == null)
        {
            _logger.LogError("UpdateDefaultCurrencyAsync: No active user space found for SpaceId: {SpaceId}.", spaceId);
            throw new InvalidOperationException("No active user space found.");
        }

        _logger.LogDebug("UpdateDefaultCurrencyAsync: Space '{SpaceName}' (ID: {SpaceId}) found.", space.Name, spaceId);

        var ledgerDb = await _userSpace.GetLedgerDbContextAsync(space.Id, false);
        _logger.LogDebug("UpdateDefaultCurrencyAsync: Retrieved LedgerDbContext for SpaceId: {SpaceId}.", spaceId);

        var currencySetting = await ledgerDb.Settings
            .FirstOrDefaultAsync(s => s.Key == "Currency");

        var oldCurrency = "USD";
        if (currencySetting != null && currencySetting.Value != null)
        {
            oldCurrency = currencySetting.Value;
        }

        if (oldCurrency == newCurrency)
        {
            _logger.LogInformation("UpdateDefaultCurrencyAsync: Old currency '{OldCurrency}' is the same as new currency '{NewCurrency}'. No update needed.", oldCurrency, newCurrency);
            return;
        }

        _logger.LogInformation("UpdateDefaultCurrencyAsync: Changing currency from '{OldCurrency}' to '{NewCurrency}' for SpaceId: {SpaceId}.", oldCurrency, newCurrency, spaceId);

        // Handle transactions that were implicitly in the old default currency
        _logger.LogInformation("UpdateDefaultCurrencyAsync: Processing transactions implicitly in old currency ('{OldCurrency}').", oldCurrency);
        var nullCurrencyTransactions = await ledgerDb.Transactions
            .Where(t => t.Currency == null)
            .ToListAsync();

        _logger.LogDebug("UpdateDefaultCurrencyAsync: Found {Count} transactions with null currency.", nullCurrencyTransactions.Count);
        foreach (var transaction in nullCurrencyTransactions)
        {
            _logger.LogTrace("UpdateDefaultCurrencyAsync: Updating transaction {TransactionId} (Value: {Value}) from null currency to explicit '{OldCurrency}' with exchange rate {ExchangeRate}.", transaction.Id, transaction.Value, oldCurrency, exchangeRate);
            transaction.Currency = oldCurrency;
            transaction.ExchangeRate = exchangeRate;
        }

        // Handle transactions that had an explicit currency set
        _logger.LogInformation("UpdateDefaultCurrencyAsync: Processing transactions with explicit currencies.");
        var explicitCurrencyTransactions = await ledgerDb.Transactions
            .Where(t => t.Currency != null)
            .ToListAsync();

        _logger.LogDebug("UpdateDefaultCurrencyAsync: Found {Count} transactions with explicit currency.", explicitCurrencyTransactions.Count);
        foreach (var transaction in explicitCurrencyTransactions)
        {
            _logger.LogTrace("UpdateDefaultCurrencyAsync: Examining explicit transaction {TransactionId} (Currency: '{TransactionCurrency}', Value: {Value}).", transaction.Id, transaction.Currency, transaction.Value);
            // If the transaction's currency is the new default, the rate is 1. Represent this as null.
            if (transaction.Currency == newCurrency)
            {
                _logger.LogTrace("UpdateDefaultCurrencyAsync: Transaction {TransactionId} currency matches new default '{NewCurrency}'. Setting ExchangeRate to null.", transaction.Id, newCurrency);
                transaction.ExchangeRate = null;
                continue;
            }

            // Otherwise, update the exchangeRate of the transaction to reflect conversion to the new currency
            var newRate = transaction.ExchangeRate * exchangeRate;
            _logger.LogTrace("UpdateDefaultCurrencyAsync: {Value} ({TransactionCurrency} -> {OldCurrency}) {Rate} converted to {NewRate}. (Transaction {TransactionId})", 
                transaction.Value,
                transaction.Currency, 
                oldCurrency,
                transaction.ExchangeRate, newRate, transaction.Id);
            transaction.ExchangeRate = newRate;
        }

        _logger.LogInformation("UpdateDefaultCurrencyAsync: Updating space '{SpaceName}' (ID: {SpaceId}) default currency to '{NewCurrency}'.", space.Name, spaceId, newCurrency);
        if (currencySetting != null)
        {
            _logger.LogDebug("Updating existing setting '{SettingKey}' for space {SpaceId}.", "Currency", space.Id);
            currencySetting.Value = newCurrency;
        } 
        else
        {
            _logger.LogDebug("Adding new setting '{SettingKey}' for space {SpaceId}.", "Currency", space.Id);
            ledgerDb.Settings.Add(new Setting
            {
                Key = "Currency",
                Value = newCurrency
            });
        }

        await ledgerDb.SaveChangesAsync();
        _logger.LogDebug("UpdateDefaultCurrencyAsync: Saved changes to LedgerDbContext for SpaceId: {SpaceId}.", spaceId);

        await _dbContext.SaveChangesAsync(); 
        _logger.LogDebug("UpdateDefaultCurrencyAsync: Saved changes to AppDbContext for SpaceId: {SpaceId}.", spaceId);

        _logger.LogInformation("UpdateDefaultCurrencyAsync completed successfully for SpaceId: {SpaceId}.", spaceId);
    }
}
