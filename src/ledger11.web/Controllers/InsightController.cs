using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ledger11.model.Data;
using ledger11.service;
using TimeZoneConverter;
using Microsoft.Extensions.Options;
using ledger11.data;
using System.Globalization;

namespace ledger11.web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class InsightController : ControllerBase
{
    private readonly ILogger<InsightController> _logger;
    private readonly ICurrentLedgerService _currentLedger;

    public InsightController(
        ILogger<InsightController> logger,
        ICurrentLedgerService currentLedger)
    {
        _logger = logger;
        _currentLedger = currentLedger;
    }

    [HttpGet("{timeZoneId?}")]
    public async Task<ActionResult> TotalByPeriodByCategory(string timeZoneId = "Europe/Paris")
    {
        using var db = await _currentLedger.GetLedgerDbContextAsync();

        TimeZoneInfo timeZone;
        try
        {
            // Converts IANA or Windows time zone ID to platform-compatible ID
            timeZone = TZConvert.GetTimeZoneInfo(timeZoneId);
        }
        catch
        {
            // Default fallback
            timeZone = TZConvert.GetTimeZoneInfo("Europe/Paris");
        }

        // Convert UTC now to local time
        var nowUtc = DateTime.UtcNow;
        var now = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, timeZone);

        // Calculate local time periods
        var today = now.Date;
        var startOfWeek = now.Date.AddDays(-(int)now.DayOfWeek + (now.DayOfWeek == DayOfWeek.Sunday ? -6 : 1));
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var startOfYear = new DateTime(now.Year, 1, 1);

        var newSet = () => new Dictionary<string, Dictionary<string, decimal>>
        {
            ["today"] = new(),
            ["yesterday"] = new(),
            ["thisWeek"] = new(),
            ["lastWeek"] = new(),
            ["thisMonth"] = new(),
            ["lastMonth"] = new(),
            ["thisYear"] = new(),
            ["lastYear"] = new(),
            ["total"] = new(),
        };

        var expense = newSet();
        var income = newSet();

        // Scan all transactions and categorize them by period and categor
        await Scan(db, timeZone, (value, category, localDate) =>
        {
            var buckets = GetRelevantBuckets(localDate, today, startOfWeek, startOfMonth, startOfYear);

            foreach (var bucket in buckets)
            {
                if (IsExpense(value))
                {
                    AddToBucket(expense[bucket], category, Math.Abs(value));
                }
                else
                {
                    AddToBucket(income[bucket], category, Math.Abs(value));
                }
            }
        });

        return Ok(new TotalByPeriodByCategoryResult()
        {
            Income = income,
            Expense = expense
        });
    }

    private bool IsExpense(decimal value)
    {
        //TODO: This should be configurable
        return value >= decimal.Zero;
    }

    [HttpGet("history/{timeZoneId?}")]
    public async Task<IActionResult> History(string timeZoneId = "Europe/Paris")
    {
        using var db = await _currentLedger.GetLedgerDbContextAsync();

        TimeZoneInfo timeZone;
        try
        {
            // Converts IANA or Windows time zone ID to platform-compatible ID
            timeZone = TZConvert.GetTimeZoneInfo(timeZoneId);
        }
        catch
        {
            // Default fallback
            timeZone = TZConvert.GetTimeZoneInfo("Europe/Paris");
        }

        var result = new HistoryResult();

        await Scan(db, timeZone, (value, category, localDate) =>
        {
            var date = localDate.Date;

            // Ensure we have a dictionary for each day
            var dateKey = date.ToString("yyyy-MM-dd");
            AddToHistoryRecord(result.Dayly, dateKey, value, category);

            // Handle week
            var weekKey = $"{date.Year}-W{CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(date, CalendarWeekRule.FirstDay, DayOfWeek.Monday):D2}";
            AddToHistoryRecord(result.Weekly, weekKey, value, category);
            
            // Handle month
            var monthKey = $"{date.Year}-{date.Month:D2}";
            AddToHistoryRecord(result.Monthly, monthKey, value, category);
        });

        return Ok(result);
    }

    private static List<string> GetRelevantBuckets(
        DateTime date,
        DateTime today,
        DateTime startOfWeek,
        DateTime startOfMonth,
        DateTime startOfYear)
    {
        var buckets = new List<string> { "total" };

        var yesterday = today.AddDays(-1);

        var lastWeekStart = startOfWeek.AddDays(-7);
        var lastWeekEnd = startOfWeek.AddDays(-1);

        var lastMonthStart = startOfMonth.AddMonths(-1);
        var lastMonthEnd = startOfMonth.AddDays(-1);

        var lastYearStart = new DateTime(startOfYear.Year - 1, 1, 1);
        var lastYearEnd = startOfYear.AddDays(-1);

        if (date >= startOfYear) buckets.Add("thisYear");
        else if (date >= lastYearStart && date <= lastYearEnd) buckets.Add("lastYear");

        if (date >= startOfMonth) buckets.Add("thisMonth");
        else if (date >= lastMonthStart && date <= lastMonthEnd) buckets.Add("lastMonth");

        if (date >= startOfWeek) buckets.Add("thisWeek");
        else if (date >= lastWeekStart && date <= lastWeekEnd) buckets.Add("lastWeek");

        if (date.Date == today) buckets.Add("today");
        else if (date.Date == yesterday) buckets.Add("yesterday");

        return buckets;
    }

    private static void AddToBucket(Dictionary<string, decimal> bucket, string category, decimal value)
    {
        if (bucket.ContainsKey(category))
            bucket[category] += value;
        else
            bucket[category] = value;
    }

    private static void AddToHistoryRecord(
        List<HistoryRecord> records,
        string date,
        decimal value,
        string category)
    {
        if (records.Count == 0 || records.Last().Date != date)
        {
            records.Add(new HistoryRecord
            {
                Date = date,
                Value = 0,
                ByCategory = new Dictionary<string, decimal>()
            });
        }

        var record = records.Last();
        record.Value += value;

        if (record.ByCategory.ContainsKey(category))
            record.ByCategory[category] += value;
        else
            record.ByCategory[category] = value;
    }

    // Scans all transactions in the database, converting their UTC date to local time
    // and applying the provided action to each transaction's value and category.
    // This allows for processing transactions in a time zone-aware manner.
    // The action receives the value, category name, and local date of the transaction.
    private async Task Scan(LedgerDbContext db, TimeZoneInfo timeZone, Action<decimal, string, DateTime> action)
    {
        await foreach (var transaction in db.Transactions
            .Include(t => t.Category)
            .Include(t => t.TransactionDetails)
            .ThenInclude(td => td.Category)
            .OrderBy(t => t.Date)
            .AsAsyncEnumerable())
        {
            var utcDate = transaction.Date;
            if (utcDate == null) continue;

            // Convert each UTC transaction time to local time
            var localDate = TimeZoneInfo.ConvertTimeFromUtc(utcDate.Value, timeZone);

            if (transaction.TransactionDetails != null && transaction.TransactionDetails.Any())
            {
                foreach (var detail in transaction.TransactionDetails)
                {
                    var value = detail.Value;
                    var category = detail.Category?.Name ?? "Uncategorized";

                    action(value, category, localDate);
                }
            }
            else
            {
                var value = transaction.Value;
                var category = transaction.Category?.Name ?? "Uncategorized";

                action(value, category, localDate);
            }
        }

    }

    [HttpGet("per-month")]
    public async Task<IActionResult> GetPerMonthDataAsync(string timeZoneId = "Europe/Paris")
    {
        using var db = await _currentLedger.GetLedgerDbContextAsync();

        TimeZoneInfo timeZone;
        try
        {
            timeZone = TZConvert.GetTimeZoneInfo(timeZoneId);
        }
        catch
        {
            timeZone = TZConvert.GetTimeZoneInfo("Europe/Paris");
        }

        var monthlyData = new Dictionary<string, PerMonthData>();

        await Scan(db, timeZone, (value, category, localDate) =>
        {
            var monthKey = localDate.ToString("MMMM yyyy");

            if (!monthlyData.ContainsKey(monthKey))
            {
                monthlyData[monthKey] = new PerMonthData { Title = monthKey };
            }

            var data = monthlyData[monthKey];
            var dictionary = IsExpense(value) ? data.Expense : data.Income;

            if (dictionary.ContainsKey(category))
            {
                dictionary[category] += Math.Abs(value);
            }
            else
            {
                dictionary[category] = Math.Abs(value);
            }
        });

        return Ok(monthlyData.Values.OrderByDescending(m => DateTime.Parse(m.Title)));
    }


}

public class TotalByPeriodByCategoryResult
{
    public Dictionary<string, Dictionary<string, decimal>> Expense { get; set; } = new();
    public Dictionary<string, Dictionary<string, decimal>> Income { get; set; } = new();
}

public class HistoryRecord
{
    public string Date { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public Dictionary<string, decimal> ByCategory { get; set; } = new();
}

public class HistoryResult
{
    public List<HistoryRecord> Monthly { get; set; } = new();
    public List<HistoryRecord> Weekly { get; set; } = new();
    public List<HistoryRecord> Dayly { get; set; } = new();
}

public class PerMonthData
{
    public string Title { get; set; } = string.Empty;
    public Dictionary<string, decimal> Expense { get; set; } = new();
    public Dictionary<string, decimal> Income { get; set; } = new();
}