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
        var reportController = ActivatorUtilities.CreateInstance<ReportController>(serviceProvider);

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
        var result1 = await reportController.MonthlyReport(new ReportController.ReportRequest
        {
            Month = startOfMonth
        });
        var response1 = Assert.IsType<ReportController.MonthlyReportResult>(Assert.IsType<OkObjectResult>(result1).Value);

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
}