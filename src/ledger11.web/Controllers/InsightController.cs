using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ledger11.model.Data;
using ledger11.service;
using TimeZoneConverter;
using Microsoft.Extensions.Options;

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

        await foreach (var transaction in db.Transactions
            .Include(t => t.Category)
            .Include(t => t.TransactionDetails)
            .ThenInclude(td => td.Category)
            .AsAsyncEnumerable())
        {
            var utcDate = transaction.Date;
            if (utcDate == null) continue;

            // _logger.LogInformation($"Processing transaction ID: {transaction.Id}, Date: {transaction.Date}, Value: {transaction.Value}");

            // Convert each UTC transaction time to local time
            var localDate = TimeZoneInfo.ConvertTimeFromUtc(utcDate.Value, timeZone);
            var buckets = GetRelevantBuckets(localDate, today, startOfWeek, startOfMonth, startOfYear);

            if (transaction.TransactionDetails != null && transaction.TransactionDetails.Any())
            {
                foreach (var detail in transaction.TransactionDetails)
                {
                    var value = detail.Value;
                    var category = detail.Category?.Name ?? "Uncategorized";

                    foreach (var bucket in buckets)
                    {
                        AddToBucket((value < decimal.Zero ? income : expense)[bucket], category, Math.Abs(value));
                    }
                }
            }
            else
            {
                var value = transaction.Value;

                var category = transaction.Category?.Name ?? "Uncategorized";

                foreach (var bucket in buckets)
                {
                    AddToBucket((value < decimal.Zero ? income : expense)[bucket], category, Math.Abs(value));
                }
            }
        }

        return Ok(new TotalByPeriodByCategoryResult()
        {
            Income = income,
            Expense = expense
        });
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

}

public class TotalByPeriodByCategoryResult
{
    public Dictionary<string, Dictionary<string, decimal>> Expense { get; set; } = new();
    public Dictionary<string, Dictionary<string, decimal>> Income { get; set; } = new();
}