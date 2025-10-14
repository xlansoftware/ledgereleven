using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ledger11.model.Data;
using ledger11.service;

namespace ledger11.web.Controllers;

[Authorize(AuthenticationSchemes = ApiKeyAuthenticationHandler.SchemeName)]
[ApiController]
[Route("api/v1")]
public class ExternalApiController : ControllerBase
{
    private readonly ILogger<ExternalApiController> _logger;
    private readonly ICurrentLedgerService _currentLedger;
    private readonly IBackupService _backupService;

    public ExternalApiController(
        ILogger<ExternalApiController> logger,
        ICurrentLedgerService currentLedger,
        IBackupService backupService)
    {
        _logger = logger;
        _currentLedger = currentLedger;
        _backupService = backupService;
    }

    // GET: api/v1/MonthlyReport?month=2000-01-01
    [HttpGet("monthlyreport")]
    public async Task<IActionResult> MonthlyReport(
        [FromQuery] ReportRequest filter
    )
    {
        _logger.LogTrace("MonthlyReport request received with parameters: {@Filter}", filter);

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

        var result = GenerateReport(monthDate.Month, transactions);

        return Ok(result);
    }

    // GET: api/v1/YearlyReport?year=2000
    [HttpGet("yearlyreport")]
    public async Task<IActionResult> YearlyReport(
        [FromQuery] YearlyReportRequest filter
    )
    {
        _logger.LogTrace("YearlyReport request received with parameters: {@Filter}", filter);

        using var db = await _currentLedger.GetLedgerDbContextAsync();

        var query = db.Transactions
            .Include(t => t.Category)
            .OrderByDescending(t => t.Date)
            .AsQueryable();

        // Apply filters
        var yearDate = filter.Year.HasValue
            ? new DateTime(filter.Year.Value, 1, 1)
            : DateTime.Now.Date;
        var startDate = new DateTime(yearDate.Year, 1, 1);
        var endDate = startDate.AddYears(1);
        query = query.Where(t => t.Date >= startDate && t.Date < endDate);

        var transactions = await query.ToListAsync();

        var transactionsPerMonth = transactions
            .GroupBy(t => t.Date!.Value.Month)
            .Select(g => GenerateReport(g.Key, g.ToList()))
            .ToArray();

        var result = new YearlyReportResult();
        result.Year = yearDate.Year;

        result.TotalExpense = transactionsPerMonth.Sum(m => m.ExpenseByCategory.Sum(r => r.Value));
        result.AverageExpenseByMonth = result.TotalExpense / transactionsPerMonth.Length;

        result.ExpenseByCategory = transactions
            .GroupBy(t => t.Category?.Name ?? "Uncategorized")
            .ToDictionary(
                g => g.Key,
                g => Math.Max(decimal.Zero, g.Sum(t => t.Value))
            );

        result.AverageByCategory = result.ExpenseByCategory
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value / transactionsPerMonth.Length // could have less than 12 months
            );

        return Ok(result);
    }

    // GET: api/v1/MonthlyReport?month=2000-01-01
    [HttpGet("backup")]
    public async Task<IActionResult> Backup(
        [FromQuery] BackupRequest options
    )
    {
        _logger.LogTrace("Backup request received with parameters: {@options}", options);

        // Get current UTC time in canonical format
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");

        // Set content type and file name based on format
        string contentType;
        string fileName;

        switch (options.Format)
        {
            case ExportFormat.Excel:
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                fileName = $"Backup-{timestamp}.xlsx";
                break;
            case ExportFormat.Csv:
                contentType = "text/csv";
                fileName = $"Backup-{timestamp}.csv";
                break;
            default:
                return BadRequest("Unsupported export format.");
        }

        var context = await _currentLedger.GetLedgerDbContextAsync();

        return new FileCallbackResult(contentType, async (stream, _) =>
        {
            await _backupService.ExportAsync(options.Format, context, stream);
        })
        {
            FileDownloadName = fileName
        };
    }

    public class FilterResponse
    {
        public List<Transaction> Transactions { get; set; } = new();
        public int TotalCount { get; set; }
    }

    public class BackupRequest
    {
        public ExportFormat Format { get; set; } = ExportFormat.Excel;
    }

    public class ReportRequest
    {
        public DateTime? Month { get; set; }
    }

    public class YearlyReportRequest
    {
        public int? Year { get; set; }
    }

    public class YearlyReportResult
    {
        public int Year { get; set; }

        public decimal TotalExpense { get; set; }
        public decimal AverageExpenseByMonth { get; set; }

        public decimal TotalIncome { get; set; }

        public Dictionary<string, decimal> ExpenseByCategory { get; set; } = new Dictionary<string, decimal>();
        public Dictionary<string, decimal> AverageByCategory { get; set; } = new Dictionary<string, decimal>();

    }

    /// <summary>
    /// Represents a monthly financial report summary.
    /// </summary>
    public class MonthlyReportResult
    {
        public MonthlyReportResult(int month)
        {
            var d = new DateTime(2000, month, 1);
            this.Month = month;
            this.MonthName = d.ToString("MMMM");
            this.MonthShort = d.ToString("MMM");
        }

        public int Month { get; set; }
        public string MonthName { get; set; } = String.Empty;
        public string MonthShort { get; set; } = String.Empty;

        /// <summary>
        /// Gets or sets the total expenses for the month.
        /// </summary>
        public decimal TotalExpense { get; set; }

        /// <summary>
        /// Gets or sets the total income for the month.
        /// </summary>
        public decimal TotalIncome { get; set; }

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
    private static MonthlyReportResult GenerateReport(int month, IEnumerable<Transaction> transactions)
    {
        var report = new MonthlyReportResult(month);

        // Filter for expense transactions (negative values) and calculate totals
        var expenseTransactions = transactions
            .ToList();

        // Calculate total expense and income
        report.TotalExpense = Math.Abs(expenseTransactions.Sum(t => Math.Max(decimal.Zero, t.Value)));
        report.TotalIncome = Math.Abs(expenseTransactions.Sum(t => Math.Min(decimal.Zero, t.Value)));

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
