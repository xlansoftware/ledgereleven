using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ledger11.model.Data;
using ledger11.service;

namespace ledger11.web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class FilterController : ControllerBase
{
    private readonly ILogger<FilterController> _logger;
    private readonly ICurrentLedgerService _currentLedger;

    public FilterController(
        ILogger<FilterController> logger,
        ICurrentLedgerService currentLedger)
    {
        _logger = logger;
        _currentLedger = currentLedger;
    }

    // GET: api/filter/arguments
    [HttpGet("arguments")]
    public async Task<IActionResult> GetArguments()
    {
        _logger.LogTrace("GetArguments request received");

        using var db = await _currentLedger.GetLedgerDbContextAsync();

        // Get distinct user names (excluding null or empty)
        var users = await db.Transactions
            .Where(t => !string.IsNullOrEmpty(t.User))
            .Select(t => t.User!)
            .Distinct()
            .OrderBy(u => u)
            .ToListAsync();

        // Get distinct category IDs (excluding null)
        var categories = await db.Transactions
            .Where(t => t.CategoryId.HasValue)
            .Select(t => t.CategoryId!.Value)
            .Distinct()
            .OrderBy(id => id)
            .ToListAsync();

        return Ok(new
        {
            Categories = categories,
            Users = users
        });
    }

    // GET: api/filter?start=0&limit=100
    [HttpGet]
    public async Task<IActionResult> Filter(
        [FromQuery] FilterRequest filter,
        [FromQuery] int? start = 0,
        [FromQuery] int? limit = 100
    )
    {
        _logger.LogTrace("Filter request received with parameters: {@Filter}, start: {Start}, limit: {Limit}", filter, start, limit);

        using var db = await _currentLedger.GetLedgerDbContextAsync();

        var query = db.Transactions
            .Include(t => t.TransactionDetails)
            .OrderByDescending(t => t.Date)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(filter.Period))
        {
            var period = filter.Period.ToLower();
            if (period == "today")
            {
                query = query.Where(t => t.Date.HasValue && t.Date.Value.Date == DateTime.UtcNow.Date);
            }
            else if (period == "thisweek")
            {
                var startOfWeek = DateTime.UtcNow.StartOfWeek();
                query = query.Where(t => t.Date >= startOfWeek);
            }
            else if (period == "thismonth")
            {
                var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                query = query.Where(t => t.Date >= startOfMonth);
            }
            else if (period == "thisyear")
            {
                var startOfYear = new DateTime(DateTime.UtcNow.Year, 1, 1);
                query = query.Where(t => t.Date >= startOfYear);
            }
        }

        if (filter.StartDate.HasValue)
        {
            query = query.Where(t => t.Date >= filter.StartDate.Value);
        }

        if (filter.EndDate.HasValue)
        {
            query = query.Where(t => t.Date <= filter.EndDate.Value);
        }

        if (filter.Category != null && filter.Category.Any())
        {
            query = query.Where(t => t.CategoryId.HasValue && filter.Category.Contains(t.CategoryId.Value));
        }

        if (!string.IsNullOrWhiteSpace(filter.Note))
        {
            var loweredNote = filter.Note.ToLower();
            query = query.Where(t => t.Notes != null && EF.Functions.Like(t.Notes.ToLower(), $"%{loweredNote}%"));
        }

        if (filter.User != null && filter.User.Any())
        {
            var loweredUsers = filter.User.Select(u => u.ToLower()).ToList();
            query = query.Where(t => t.User != null && loweredUsers.Contains(t.User.ToLower()));
        }

        if (filter.MinValue.HasValue)
        {
            query = query.Where(t => t.Value * (t.ExchangeRate ?? 1.0m) >= filter.MinValue.Value);
        }

        if (filter.MaxValue.HasValue)
        {
            query = query.Where(t => t.Value * (t.ExchangeRate ?? 1.0m) <= filter.MaxValue.Value);
        }

        var totalCount = await query.CountAsync();

        // Apply pagination
        if (start.HasValue)
            query = query.Skip(start.Value);

        if (limit.HasValue && limit.Value > -1)
            query = query.Take(limit.Value);

        var transactions = await query.ToListAsync();

        return Ok(new FilterResponse
        {
            Transactions = transactions,
            TotalCount = totalCount
        });
    }


    public class FilterResponse
    {
        public List<Transaction> Transactions { get; set; } = new();
        public int TotalCount { get; set; }
    }

    public class FilterRequest
    {
        public string? Period { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int[]? Category { get; set; }
        public string? Note { get; set; }
        public string[]? User { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }

        override public string ToString()
        {
            return $"Period: {Period}, StartDate: {StartDate}, EndDate: {EndDate}, Category: {string.Join(",", Category ?? Array.Empty<int>())}, Note: {Note}, User: {string.Join(",", User ?? Array.Empty<string>())}, MinValue: {MinValue}, MaxValue: {MaxValue}";
        }
    }

}
