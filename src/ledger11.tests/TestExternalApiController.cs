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

public class TestReportController
{
    [Fact]
    public async Task Test_MonthlyReport()
    {
        // Arrange
        using var serviceProvider = await TestExtesions.MockLedgerServiceProviderAsync("xuser1");

        var categoriesController = ActivatorUtilities.CreateInstance<CategoryController>(serviceProvider);
        var transactionController = ActivatorUtilities.CreateInstance<TransactionController>(serviceProvider);
        var reportController = ActivatorUtilities.CreateInstance<ExternalApiController>(serviceProvider);

        // Get available categories
        var allCategoriesResult = await categoriesController.GetAll();
        var categories = Assert.IsType<List<Category>>(Assert.IsType<OkObjectResult>(allCategoriesResult).Value);

        var cat1 = categories[0];
        var cat2 = categories[1];

        var now = DateTime.UtcNow;
        var today = now.Date;
        var startOfMonth = new DateTime(today.Year, today.Month, 1);

        // Act 1: Create test data
        var tx1 = new Transaction
        {
            Value = 50,
            Date = startOfMonth.AddDays(-11), // Previous month - should NOT be included
            CategoryId = cat1.Id,
            Notes = "Note abc",
        };
        var tx2 = new Transaction
        {
            Value = 150,
            Date = startOfMonth.AddDays(5), // Current month - should be included
            CategoryId = cat2.Id,
            Notes = "Another note",
        };
        var tx3 = new Transaction
        {
            Value = 75,
            Date = startOfMonth, // First day of current month - should be included
            CategoryId = cat1.Id,
            Notes = "note XYZ",
        };
        var tx4 = new Transaction
        {
            Value = 200,
            Date = startOfMonth.AddDays(3), // Current month - should be included
            CategoryId = cat2.Id,
            Notes = "Some other note",
        };

        // Create all test transactions
        await transactionController.Create(tx1);
        await transactionController.Create(tx2);
        await transactionController.Create(tx3);
        await transactionController.Create(tx4);

        // Act 2: Apply filters and get monthly report
        var result1 = await reportController.MonthlyReport(new ExternalApiController.ReportRequest
        {
            Month = startOfMonth
        });
        var response1 = Assert.IsType<ExternalApiController.MonthlyReportResult>(Assert.IsType<OkObjectResult>(result1).Value);

        // Assert
        // Only tx2, tx3, and tx4 should be included (tx1 is from previous month)
        var expectedTotal = 150 + 75 + 200; // 425

        Assert.Equal(expectedTotal, response1.TotalExpense);

        // Verify expense by category
        Assert.Equal(2, response1.ExpenseByCategory.Count);
        Assert.True(response1.ExpenseByCategory.ContainsKey(cat1.Name));
        Assert.True(response1.ExpenseByCategory.ContainsKey(cat2.Name));

        Assert.Equal(75, response1.ExpenseByCategory[cat1.Name]);   // tx3 only
        Assert.Equal(350, response1.ExpenseByCategory[cat2.Name]);  // tx2 + tx4 = 150 + 200

        // Additional verification: tx1 should not be included
        Assert.DoesNotContain(response1.ExpenseByCategory.Values, v => v == 50);
    }

    [Fact]
    public async Task Test_YearlyReport()
    {
        // Arrange
        using var serviceProvider = await TestExtesions.MockLedgerServiceProviderAsync("xuser1");

        var categoriesController = ActivatorUtilities.CreateInstance<CategoryController>(serviceProvider);
        var transactionController = ActivatorUtilities.CreateInstance<TransactionController>(serviceProvider);
        var reportController = ActivatorUtilities.CreateInstance<ExternalApiController>(serviceProvider);

        // Get available categories
        var allCategoriesResult = await categoriesController.GetAll();
        var categories = Assert.IsType<List<Category>>(Assert.IsType<OkObjectResult>(allCategoriesResult).Value);

        var cat1 = categories[0];
        var cat2 = categories[1];

        var now = DateTime.UtcNow;
        var today = now.Date;
        var startOfYear = new DateTime(today.Year, 1, 1);

        // Act 1: Create test data across different months of the same year
        var tx1 = new Transaction
        {
            Value = 100,
            Date = startOfYear.AddMonths(0).AddDays(5), // January
            CategoryId = cat1.Id,
            Notes = "Jan tx cat1",
        };
        var tx2 = new Transaction
        {
            Value = 200,
            Date = startOfYear.AddMonths(0).AddDays(15), // January
            CategoryId = cat2.Id,
            Notes = "Jan tx cat2",
        };
        var tx3 = new Transaction
        {
            Value = 150,
            Date = startOfYear.AddMonths(5).AddDays(10), // June
            CategoryId = cat1.Id,
            Notes = "June tx cat1",
        };
        var tx4 = new Transaction
        {
            Value = 250,
            Date = startOfYear.AddMonths(11).AddDays(20), // December
            CategoryId = cat2.Id,
            Notes = "Dec tx cat2",
        };
        var tx5 = new Transaction
        {
            Value = 300,
            Date = startOfYear.AddYears(-1).AddDays(10), // Previous year - should NOT be included
            CategoryId = cat1.Id,
            Notes = "Last year tx",
        };

        // Create all test transactions
        await transactionController.Create(tx1);
        await transactionController.Create(tx2);
        await transactionController.Create(tx3);
        await transactionController.Create(tx4);
        await transactionController.Create(tx5);

        // Act 2: Get yearly report
        var result = await reportController.YearlyReport(new ExternalApiController.YearlyReportRequest
        {
            Year = today.Year
        });
        var response = Assert.IsType<ExternalApiController.YearlyReportResult>(Assert.IsType<OkObjectResult>(result).Value);

        // Assert
        // Only tx1, tx2, tx3, tx4 should be included (tx5 is from previous year)
        var expectedTotalExpense = 100 + 200 + 150 + 250; // 700

        Assert.Equal(today.Year, response.Year);
        Assert.Equal(expectedTotalExpense, response.TotalExpense);

        // Verify expense by category
        Assert.Equal(2, response.ExpenseByCategory.Count);
        Assert.True(response.ExpenseByCategory.ContainsKey(cat1.Name));
        Assert.True(response.ExpenseByCategory.ContainsKey(cat2.Name));

        var cat1Total = 100 + 150; // tx1 + tx3
        var cat2Total = 200 + 250; // tx2 + tx4

        Assert.Equal(cat1Total, response.ExpenseByCategory[cat1.Name]);
        Assert.Equal(cat2Total, response.ExpenseByCategory[cat2.Name]);

        var monthsCount = 3m;
        // Verify average by category (total / 12)
        Assert.Equal(2, response.AverageByCategory.Count);
        Assert.Equal(cat1Total / monthsCount, response.AverageByCategory[cat1.Name]);
        Assert.Equal(cat2Total / monthsCount, response.AverageByCategory[cat2.Name]);

        // Verify average expense per month
        var expectedAvgExpensePerMonth = expectedTotalExpense / monthsCount;
        Assert.Equal(expectedAvgExpensePerMonth, response.AverageExpenseByMonth);
    }
}