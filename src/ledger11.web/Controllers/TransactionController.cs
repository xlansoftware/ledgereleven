using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ledger11.model.Data;
using ledger11.service;

namespace ledger11.web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TransactionController : ControllerBase
{
    private readonly ICurrentLedgerService _currentLedger;

    public TransactionController(
        ICurrentLedgerService currentLedger)
    {
        _currentLedger = currentLedger;
    }

    // GET: api/transaction?start=0&limit=100
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? start = 0,
        [FromQuery] int? limit = 100)
    {
        using var db = await _currentLedger.GetLedgerDbContextAsync();

        var query = db.Transactions
            .Include(t => t.TransactionDetails)
            .OrderByDescending(t => t.Date)
            .AsQueryable();

        if (start.HasValue)
            query = query.Skip(start.Value);

        if (limit.HasValue)
            query = query.Take(limit.Value);

        var transactions = await query.ToListAsync();

        return Ok(transactions);
    }

    // GET: api/transaction/5
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        using var db = await _currentLedger.GetLedgerDbContextAsync();
        var transaction = await db.Transactions
            .Include(t => t.TransactionDetails)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (transaction == null)
            return NotFound();

        return Ok(transaction);
    }

    // POST: api/transaction
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Transaction transaction)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        using var db = await _currentLedger.GetLedgerDbContextAsync();
        if (transaction.Date == default)
        {
            transaction.Date = DateTime.UtcNow;
        }
        transaction.User = User?.Identity?.Name;
        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();

        var result = await db.Transactions.FirstOrDefaultAsync(t => t.Id == transaction.Id);

        return CreatedAtAction(nameof(Get), new { id = transaction.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Transaction transaction)
    {
        if (id != transaction.Id)
            return BadRequest("ID mismatch");

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        using var db = await _currentLedger.GetLedgerDbContextAsync();

        var existingTransaction = await db.Transactions
            .Include(t => t.TransactionDetails)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (existingTransaction == null)
            return NotFound();

        // Update scalar properties
        existingTransaction.Value = transaction.Value;
        existingTransaction.ExchangeRate = transaction.ExchangeRate;
        existingTransaction.Currency = transaction.Currency;
        existingTransaction.Date = transaction.Date;
        existingTransaction.Notes = transaction.Notes;
        existingTransaction.CategoryId = transaction.CategoryId;

        // Sync TransactionDetails
        // 1. Remove deleted
        var incomingIds = transaction.TransactionDetails?.Select(d => d.Id).ToHashSet() ?? new HashSet<int>();
        var toRemove = existingTransaction.TransactionDetails
            .Where(d => !incomingIds.Contains(d.Id))
            .ToList();

        foreach (var detail in toRemove)
            db.TransactionDetail.Remove(detail);

        // 2. Add or update remaining
        foreach (var detail in transaction.TransactionDetails ?? Enumerable.Empty<TransactionDetail>())
        {
            var existingDetail = existingTransaction.TransactionDetails
                .FirstOrDefault(d => d.Id == detail.Id);

            if (existingDetail != null)
            {
                // Update
                existingDetail.Value = detail.Value;
                existingDetail.Description = detail.Description;
                existingDetail.Quantity = detail.Quantity;
                existingDetail.CategoryId = detail.CategoryId;
            }
            else
            {
                // Add new
                detail.TransactionId = transaction.Id;
                db.TransactionDetail.Add(detail);
            }
        }

        await db.SaveChangesAsync();

        return Ok(existingTransaction);
    }


    // DELETE: api/transaction/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        using var db = await _currentLedger.GetLedgerDbContextAsync();
        var transaction = db.Transactions.Find(id);
        if (transaction == null)
            return NotFound();

        db.Transactions.Remove(transaction);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // POST: api/transaction/clear
    [HttpPost("clear")]
    public async Task<IActionResult> ClearAllTransactions()
    {
        using var db = await _currentLedger.GetLedgerDbContextAsync();
        await db.Transactions.ExecuteDeleteAsync();
        await db.SaveChangesAsync();

        return NoContent();   
    }
}
