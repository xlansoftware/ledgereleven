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
    public async Task<IActionResult> MonthlyReport(
        [FromQuery] ReportRequest filter
    )
    {
        _logger.LogTrace("Report request received with parameters: {@Filter}", filter);

        using var db = await _currentLedger.GetLedgerDbContextAsync();

        var query = db.Transactions
            .Include(t => t.Category)
            .OrderByDescending(t => t.Date)
            .AsQueryable();

        // Apply filters
        var monthDate = filter.Month.HasValue
            ? filter.Month.Value.Date // Strip time components
            : DateTime.Now.Date;
        var startDate = new DateTime(monthDate.Year, monthDate.Month, 1);
        var endDate = startDate.AddMonths(1);
        query = query.Where(t => t.Date >= startDate && t.Date < endDate);

        var transactions = await query.ToListAsync();

        var result = GenerateMonthlyReport(transactions);

        return Ok(result);
    }


    public class FilterResponse
    {
        public List<Transaction> Transactions { get; set; } = new();
        public int TotalCount { get; set; }
    }

    public class ReportRequest
    {
        public DateTime? Month { get; set; }
    }

    /// <summary>
    /// Represents a monthly financial report summary.
    /// </summary>
    public class MonthlyReportResult
    {
        /// <summary>
        /// Gets or sets the total expenses for the month (negative values only).
        /// </summary>
        public decimal TotalExpense { get; set; }

        /// <summary>
        /// Gets or sets the total expenses grouped by category name.
        /// </summary>
        public Dictionary<string, decimal> ExpenseByCategory { get; set; } = new Dictionary<string, decimal>();
    }

    /// <summary>
    /// Generates a monthly financial report from a collection of transactions.
    /// Expenses are considered as transactions with negative values.
    /// </summary>
    /// <param name="transactions">The collection of transactions to analyze.</param>
    /// <returns>A MonthlyReportResult containing expense totals and breakdown by category.</returns>
    public static MonthlyReportResult GenerateMonthlyReport(IEnumerable<Transaction> transactions)
    {
        var report = new MonthlyReportResult();

        // Filter for expense transactions (negative values) and calculate totals
        var expenseTransactions = transactions
            .ToList();

        // Calculate total expense (sum of negative values, stored as positive number)
        report.TotalExpense = Math.Abs(expenseTransactions.Sum(t => t.Value));

        // Group expenses by category and calculate totals
        report.ExpenseByCategory = expenseTransactions
            .GroupBy(t => t.Category?.Name ?? "Uncategorized")
            .ToDictionary(
                g => g.Key,
                g => Math.Abs(g.Sum(t => t.Value))
            );

        return report;
    }
}
