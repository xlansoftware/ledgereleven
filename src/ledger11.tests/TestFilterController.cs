using Microsoft.Extensions.DependencyInjection;
using ledger11.web.Controllers;
using Microsoft.AspNetCore.Mvc;
using ledger11.model.Data;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ledger11.tests;

public class TestFilterController
{
    [Fact]
    public async Task Test_Filter()
    {
        // Arrange
        using var serviceProvider = await TestExtesions.MockLedgerServiceProviderAsync("xuser1");

        var categoriesController = ActivatorUtilities.CreateInstance<CategoryController>(serviceProvider);
        var transactionController = ActivatorUtilities.CreateInstance<TransactionController>(serviceProvider);
        var filterController = ActivatorUtilities.CreateInstance<FilterController>(serviceProvider);

        // Get available categories
        var allCategoriesResult = await categoriesController.GetAll();
        var categories = Assert.IsType<List<Category>>(Assert.IsType<OkObjectResult>(allCategoriesResult).Value);

        var cat1 = categories[0];
        var cat2 = categories[1];

        var now = DateTime.UtcNow;
        var today = now.Date;
        var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
        var startOfMonth = new DateTime(today.Year, today.Month, 1);
        var startOfYear = new DateTime(today.Year, 1, 1);

        // Act 1: Create test data
        var tx1 = new Transaction
        {
            Value = 50,
            Date = now.AddDays(-1),
            CategoryId = cat1.Id,
            Notes = "Note abc",
        };
        var tx2 = new Transaction
        {
            Value = 150,
            Date = now.AddDays(-5),
            CategoryId = cat2.Id,
            Notes = "Another note",
        };
        var tx3 = new Transaction
        {
            Value = 75,
            Date = now,
            CategoryId = cat1.Id,
            Notes = "note XYZ",
        };
        var tx4 = new Transaction
        {
            Value = 200,
            Date = now.AddDays(-3),
            CategoryId = cat2.Id,
            Notes = "Some other note",
        };

        // Create all test transactions
        await transactionController.Create(tx1);
        await transactionController.Create(tx2);
        await transactionController.Create(tx3);
        await transactionController.Create(tx4);

        // Act 2: Apply filters one by one and assert results

        // Filter by Note (case-insensitive)
        var result1 = await filterController.Filter(new FilterController.FilterRequest
        {
            Note = "ABC"
        }, 0, 10);
        var response1 = Assert.IsType<FilterController.FilterResponse>(Assert.IsType<OkObjectResult>(result1).Value);
        Assert.Single(response1.Transactions);
        Assert.Contains("abc", response1.Transactions[0].Notes!, StringComparison.OrdinalIgnoreCase);

        // Filter by StartDate and EndDate
        var result2 = await filterController.Filter(new FilterController.FilterRequest
        {
            StartDate = now.AddDays(-4),
            EndDate = now.AddDays(-2)
        }, 0, 10);
        var response2 = Assert.IsType<FilterController.FilterResponse>(Assert.IsType<OkObjectResult>(result2).Value);
        Assert.Single(response2.Transactions);
        Assert.Equal(tx4.Value, response2.Transactions[0].Value);

        // Filter by MinValue and MaxValue
        var result3 = await filterController.Filter(new FilterController.FilterRequest
        {
            MinValue = 70,
            MaxValue = 160
        }, 0, 10);
        var response3 = Assert.IsType<FilterController.FilterResponse>(Assert.IsType<OkObjectResult>(result3).Value);
        Assert.Equal(2, response3.TotalCount); // tx2 and tx3

        // Filter by Category
        var result5 = await filterController.Filter(new FilterController.FilterRequest
        {
            Category = new[] { cat1.Id }
        }, 0, 10);
        var response5 = Assert.IsType<FilterController.FilterResponse>(Assert.IsType<OkObjectResult>(result5).Value);
        Assert.Equal(2, response5.TotalCount); // tx1 and tx3

        // Period: today
        var result6 = await filterController.Filter(new FilterController.FilterRequest
        {
            Period = "today"
        }, 0, 10);
        var response6 = Assert.IsType<FilterController.FilterResponse>(Assert.IsType<OkObjectResult>(result6).Value);
        Assert.All(response6.Transactions, t => Assert.Equal(today, t.Date?.Date));

        // Period: thisweek
        //TODO: Fix this
        // var result7 = await filterController.Filter(new FilterController.FilterRequest
        // {
        //     Period = "thisweek"
        // }, 0, 10);
        // var response7 = Assert.IsType<FilterController.FilterResponse>(Assert.IsType<OkObjectResult>(result7).Value);
        // Assert.All(response7.Transactions, t => Assert.True(t.Date >= startOfWeek));

        // Period: thismonth
        var result8 = await filterController.Filter(new FilterController.FilterRequest
        {
            Period = "thismonth"
        }, 0, 10);
        var response8 = Assert.IsType<FilterController.FilterResponse>(Assert.IsType<OkObjectResult>(result8).Value);
        Assert.All(response8.Transactions, t => Assert.True(t.Date >= startOfMonth));

        // Period: thisyear
        var result9 = await filterController.Filter(new FilterController.FilterRequest
        {
            Period = "thisyear"
        }, 0, 10);
        var response9 = Assert.IsType<FilterController.FilterResponse>(Assert.IsType<OkObjectResult>(result9).Value);
        Assert.All(response9.Transactions, t => Assert.True(t.Date >= startOfYear));
    }
}
