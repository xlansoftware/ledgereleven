using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ledger11.model.Data;
using ledger11.service;
using ledger11.model.Api;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using ledger11.data;

namespace ledger11.web.Controllers;

[Authorize]
[ApiController]
[Route("api/space")]
public class SpaceController : ControllerBase
{
    private readonly ILogger<SpaceController> _logger;
    private readonly IUserSpaceService _userSpace;
    private readonly ICurrentLedgerService _currentLedgerService;
    private readonly AppDbContext _appDbContext;

    public SpaceController(
        ILogger<SpaceController> logger,
        IUserSpaceService userSpace,
        ICurrentLedgerService currentLedgerService,
        AppDbContext appDbContext)
    {
        _logger = logger;
        _userSpace = userSpace;
        _currentLedgerService = currentLedgerService;
        _appDbContext = appDbContext;
    }

    // GET: api/space
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool includeDetails = true)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        var current = await _userSpace.GetUserSpaceAsync();
        var spaces = await _userSpace.GetAvailableSpacesAsync();

        var dto = new SpaceListResponseDto
        {
            Current = current?.ToDto(),
            Spaces = spaces.ToDtoList()
        };

        if (includeDetails)
        {
            foreach (var space in dto.Spaces)
            {
                try
                {
                    var ledgerContext = await _currentLedgerService.GetLedgerDbContextAsync(space.Id, false);
                    var result = await ledgerContext.Transactions
                        .GroupBy(t => 1)
                        .Select(g => new
                        {
                            Count = g.Count(),
                            Sum = g.Sum(t => t.Value * (t.ExchangeRate ?? 1.0m))
                        })
                        .FirstOrDefaultAsync();

                    space.TotalValue = result?.Sum;
                    space.CountTransactions = result?.Count;
                    space.CountCategories = await ledgerContext.Categories.CountAsync();

                    if (dto.Current?.Id == space.Id)
                    {
                        dto.Current.TotalValue = space.TotalValue;
                        dto.Current.CountTransactions = space.CountTransactions;
                        dto.Current.CountCategories = space.CountCategories;
                    }
                }
                catch (Exception ex)
                {
                    // if the space is not yet initialized, then the
                    // number of transactions and categories will be null
                    _logger.LogTrace(ex, "Error processing details info for space {SpaceId}", space.Id);
                }

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
        }
        else
        {
            // count the transactions in current only
            if (dto.Current != null)
            {
                try
                {
                    var ledgerContext = await _currentLedgerService.GetLedgerDbContextAsync(dto.Current.Id, false);
                    dto.Current.CountTransactions = await ledgerContext.Transactions.CountAsync();
                }
                catch
                {
                    // the ledger does not exists yet, so 0 transactions
                }
            }
        }

        stopwatch.Stop();
        _logger.LogInformation("Retrieved spaces in {ElapsedMilliseconds} ms", stopwatch.ElapsedMilliseconds);
        return Ok(dto);
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
            var newLedger = await _currentLedgerService.GetLedgerDbContextAsync(result.Id, true);
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
        return Ok(result.ToDto());
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
}