using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ledger11.model.Data;
using ledger11.service;
using ledger11.model.Api;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using ledger11.data;
using ledger11.model.Config;
using ledger11.data.Extensions;

namespace ledger11.web.Controllers;

public class MergeSpaceRequestDto
{
    public Guid SourceSpaceId { get; set; }
    public Guid TargetSpaceId { get; set; }
    public int TargetCategoryId { get; set; }
}

public class UpdateCurrencyRequestDto
{
    public required Guid SpaceId { get; set; }
    public required string Currency { get; set; }
    public decimal ExchangeRate { get; set; }
}

[Authorize]
[ApiController]
[Route("api/space")]
public class SpaceController : ControllerBase
{
    private readonly ILogger<SpaceController> _logger;
    private readonly IUserSpaceService _userSpace;
    private readonly ICurrentLedgerService _currentLedgerService;
    private readonly AppDbContext _appDbContext;
    private readonly IExchangeRateService _exchangeRateService;

    public SpaceController(
        ILogger<SpaceController> logger,
        IUserSpaceService userSpace,
        ICurrentLedgerService currentLedgerService,
        AppDbContext appDbContext,
        IExchangeRateService exchangeRateService)
    {
        _logger = logger;
        _userSpace = userSpace;
        _currentLedgerService = currentLedgerService;
        _appDbContext = appDbContext;
        _exchangeRateService = exchangeRateService;
    }

    // GET: api/space
    [HttpGet]
    // Returns a list of user-accessible spaces with optional detailed statistics.
    public async Task<IActionResult> List()
    {
        var stopwatch = Stopwatch.StartNew();

        // Get the current space and all accessible spaces for the user
        var current = await _userSpace.GetUserSpaceAsync();
        var spaces = await _userSpace.GetAvailableSpacesAsync();

        // Convert to DTOs for API response
        var dto = new SpaceListResponseDto
        {
            Spaces = spaces
                .OrderByDescending(s => s.CreatedAt)
                .ToDtoList(),
        };

        // Add settings to each space
        foreach (var space in dto.Spaces)
        {
            await AddDetailsSpacesAsync(space);
        }

        dto.Current = dto.Spaces.FirstOrDefault(s => s.Id == current?.Id);

        stopwatch.Stop();
        _logger.LogInformation("Retrieved spaces in {ElapsedMilliseconds} ms", stopwatch.ElapsedMilliseconds);
        return Ok(dto);
    }

    // Adds detailed statistics and member info to all spaces
    private async Task AddDetailsSpacesAsync(SpaceDto space)
    {
        // Add transaction/category stats and settings
        await AddSpaceDetailsAsync(space);

        // Add member emails for the space
        await AddSpaceMembersAsync(space);
    }

    // Adds transaction count, total value, and category count to a space
    private async Task AddSpaceDetailsAsync(SpaceDto space)
    {
        try
        {
            // Get the ledger context for the space (ledger might not be initialized)
            var ledgerContext = await _userSpace.GetLedgerDbContextAsync(space.Id, false);
            if (ledgerContext == null)
                return; // No ledger context means no data to process

            // Group transactions to calculate total count and sum of value
            var result = await ledgerContext.Transactions
                .GroupBy(t => 1)
                .Select(g => new
                {
                    Count = g.Count(),
                    Sum = g.Sum(t => t.Value * (t.ExchangeRate ?? 1.0m)) // handle null exchange rate
                })
                .FirstOrDefaultAsync();

            // Populate stats in the space DTO
            space.TotalValue = result?.Sum;
            space.CountTransactions = result?.Count;
            space.CountCategories = await ledgerContext.Categories.CountAsync();

            // Read settings from the ledger context
            space.Settings = await ledgerContext
                .Settings
                .ToDictionaryAsync((s) => s.Key, (s) => s.Value);

        }
        catch (Exception ex)
        {
            // If ledger isn't set up yet, log and continue gracefully
            _logger.LogTrace(ex, "Error processing details info for space {SpaceId}", space.Id);
        }
    }

    // Adds the list of member emails to the space DTO
    private async Task AddSpaceMembersAsync(SpaceDto space)
    {
        var members = await _appDbContext.SpaceMembers
            .Include(m => m.User)
            .Where(m => m.SpaceId == space.Id)
            .Select(m => m.User.Email)
            .ToListAsync();

        if (members != null)
        {
            space.Members = members;
        }
    }

    // POST: api/space
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Space space)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _userSpace.CreateSpace(space);
        var currentSpace = await _userSpace.GetUserSpaceAsync();

        _logger.LogInformation("result.Id = {result}", result?.Id);
        _logger.LogInformation("currentSpace.Id = {currentSpace}", currentSpace?.Id);
        if (currentSpace != null && result != null && result.Id != currentSpace?.Id)
        {
            // copy the categories form the current space to the new space
            var currentLedger = await _currentLedgerService.GetLedgerDbContextAsync();
            var newLedger = await _userSpace.GetLedgerDbContextAsync(result.Id, true);
            _logger.LogInformation("Copying categories from current ledger to new ledger {NewLedgerId}", result.Id);
            await CopyCategoriesAsync(currentLedger, newLedger);
        }

        return CreatedAtAction(nameof(List), new { id = result!.Id }, result.ToDto());
    }

    // DELETE: api/space/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _userSpace.DeleteSpaceAsync(id);

        var current = await _userSpace.GetUserSpaceAsync();
        var spaces = await _userSpace.GetAvailableSpacesAsync();

        var dto = new SpaceListResponseDto
        {
            Current = current?.ToDto(),
            Spaces = spaces.ToDtoList()
        };

        return Ok(dto);
    }

    // PUT: api/space/5
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Dictionary<string, object> updatedFields)
    {
        var result = await _userSpace.UpdateSpace(id, updatedFields);

        var resultDto = result.ToDto();
        await AddDetailsSpacesAsync(resultDto);

        return Ok(resultDto);
    }

    // POST: api/space/currency
    [HttpPost("currency")]
    public async Task<IActionResult> UpdateCurrency([FromBody] UpdateCurrencyRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            await _currentLedgerService.UpdateDefaultCurrencyAsync(request.SpaceId, request.Currency, request.ExchangeRate);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update default currency.");
            return StatusCode(500, "An error occurred while updating the currency.");
        }
    }

    // POST: api/space/current
    [HttpPost("current")]
    public async Task<IActionResult> Current([FromBody] Guid id)
    {
        await _userSpace.SetCurrentSpaceAsync(id);
        return Ok();
    }

    // POST: api/space/share
    [HttpPost("share")]
    public async Task<IActionResult> Share([FromBody] ShareSpaceRequestDto request)
    {
        if (request == null || request.SpaceId == Guid.Empty || string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest("Invalid share request.");
        }

        try
        {
            await _userSpace.ShareSpaceWithAsync(request.SpaceId, request.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sharing space {SpaceId} with {Email}", request.SpaceId, request.Email);
            return BadRequest("The space could not be shared...");
        }

        return Ok("Space shared successfully.");
    }

    private async Task CopyCategoriesAsync(LedgerDbContext from, LedgerDbContext to)
    {
        if (from == null) throw new ArgumentNullException(nameof(from));
        if (to == null) throw new ArgumentNullException(nameof(to));

        // Begin transaction to ensure atomic operation
        await using var transaction = await to.Database.BeginTransactionAsync();

        try
        {
            // Step 1: Delete existing categories in target if they exist
            if (await to.Categories.AnyAsync())
            {
                _logger.LogInformation("Deleting existing categories in target ledger {TargetLedgerId}", to.Database.GetDbConnection().Database);
                to.Categories.RemoveRange(to.Categories);
                await to.SaveChangesAsync();
            }

            // Step 2: Retrieve all categories from source
            var categoriesToCopy = await from.Categories
                .AsNoTracking() // Important to avoid tracking issues
                .ToListAsync();

            if (categoriesToCopy.Count == 0)
            {
                _logger.LogInformation("No categories to copy from source ledger {SourceLedgerId}", from.Database.GetDbConnection().Database);
                // No data to copy
                await transaction.CommitAsync();
                return;
            }

            // Step 3: Add all categories to target
            _logger.LogInformation("Copying {CategoryCount} categories from source ledger {SourceLedgerId} to target ledger {TargetLedgerId}",
                categoriesToCopy.Count, from.Database.GetDbConnection().Database, to.Database.GetDbConnection().Database);
            await to.Categories.AddRangeAsync(categoriesToCopy);
            await to.SaveChangesAsync();

            // Commit transaction if all operations succeeded
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw; // Re-throw the exception after rollback
        }
    }

    // POST: api/space/merge
    [HttpPost("merge")]
    public async Task<IActionResult> Merge([FromBody] MergeSpaceRequestDto request)
    {
        if (request.SourceSpaceId == Guid.Empty || request.TargetSpaceId == Guid.Empty)
        {
            return BadRequest("Source and target space IDs must be provided.");
        }

        if (request.SourceSpaceId == request.TargetSpaceId)
        {
            return BadRequest("Source and target spaces cannot be the same.");
        }

        try
        {
            var spaces = await _userSpace.GetAvailableSpacesAsync();

            var sourceSpace = spaces.FirstOrDefault(s => s.Id == request.SourceSpaceId);
            if (sourceSpace == null)
                return Ok(); // Source space does not exist, nothing to merge

            var sourceLedger = await _userSpace.GetLedgerDbContextAsync(request.SourceSpaceId, false);
            if (sourceLedger == null)
                return Ok(); // Source space does not exist, nothing to merge

            var targetLedger = await _userSpace.GetLedgerDbContextAsync(request.TargetSpaceId, true);

            var sourceDto = sourceSpace.ToDto();
            await AddSpaceDetailsAsync(sourceDto); // read totals and settings from the source ledger

            var currency = sourceDto.Settings.FirstOrDefault((s) => s.Key == LedgerSettings.Currency).Value ?? "USD";
            var targetCurrency = (await targetLedger.Settings.FirstOrDefaultAsync(s => s.Key == LedgerSettings.Currency))?.Value ?? "USD";
            var exchangeRate = await _exchangeRateService.GetExchangeRateAsync(currency, targetCurrency);
            var targetCategory = await targetLedger.Categories.FirstOrDefaultAsync(c => c.Id == request.TargetCategoryId);
            targetLedger.Transactions.Add(new Transaction()
            {
                Value = sourceDto.TotalValue ?? 0,
                Currency = currency == targetCurrency ? null : targetCurrency,
                ExchangeRate = currency == targetCurrency ? null : exchangeRate,
                Date = DateTime.UtcNow,
                Notes = sourceDto.Name,
                CategoryId = targetCategory?.Id,
            });
            await targetLedger.SaveChangesAsync();

            // Mark the source space as closed
            _logger.LogInformation("Marking source space {SourceSpaceId} as closed", request.SourceSpaceId);
            await sourceLedger.SetSettingValue(LedgerSettings.Status, "Closed");
            await sourceLedger.SetSettingValue(LedgerSettings.ClosingBalanceTransferLedger, request.TargetSpaceId.ToString());

            return Ok("Spaces merged successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error merging spaces.");
            return StatusCode(500, "An error occurred while merging the spaces.");
        }
    }
}