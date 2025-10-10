using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ledger11.model.Data;
using ledger11.service;

namespace ledger11.web.Controllers;

[Authorize(AuthenticationSchemes = ApiKeyAuthenticationHandler.SchemeName)]
[ApiController]
[Route("api/[controller]")]
public class ReportController : ControllerBase
{
    private readonly ILogger<ReportController> _logger;
    private readonly ICurrentLedgerService _currentLedger;

    public ReportController(
        ILogger<ReportController> logger,
        ICurrentLedgerService currentLedger)
    {
        _logger = logger;
        _currentLedger = currentLedger;
    }

    // GET: api/report?start=0&limit=100
    [HttpGet]
    public async Task<IActionResult> Report(
        [FromQuery] ReportRequest filter
    )
    {
        _logger.LogTrace("Report request received with parameters: {@Filter}", filter);

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

    public class ReportRequest
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
