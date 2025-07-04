using Microsoft.Extensions.DependencyInjection;
using ledger11.web.Controllers;
using Microsoft.AspNetCore.Mvc;
using ledger11.model.Data;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;

namespace ledger11.tests;

public class TestInsightController
{
    [Fact]
    public async Task Test_TotalByPeriodByCategory()
    {
        // Arrange
        using var serviceProvider = await TestExtesions.MockLedgerServiceProviderAsync("xuser1");

        var categoriesController = ActivatorUtilities.CreateInstance<CategoryController>(serviceProvider);
        var transactionController = ActivatorUtilities.CreateInstance<TransactionController>(serviceProvider);
        var insightController = ActivatorUtilities.CreateInstance<InsightController>(serviceProvider);

        // Get available categories
        var allCategoriesResult = await categoriesController.GetAll();
        var categories = Assert.IsType<List<Category>>(Assert.IsType<OkObjectResult>(allCategoriesResult).Value);

        var Groceries = categories.FirstOrDefault(c => c.Name == "Groceries")!;
        var Education = categories.FirstOrDefault(c => c.Name == "Education")!;

        // Use DateTime.Now to avoid time-dependent test failures.
        var now = DateTime.Now;
        var today = now.Date;

        // Act 1: Create test data on today and yesterday to make test more robust
        var tx1 = new Transaction
        {
            Value = 50,
            Date = now.AddDays(-1),
            CategoryId = Groceries.Id,
            Notes = "Note abc",
        };
        var tx2 = new Transaction
        {
            Value = 150,
            Date = now.AddDays(-1),
            CategoryId = Education.Id,
            Notes = "Another note",
        };
        var tx3 = new Transaction
        {
            Value = 75,
            Date = now,
            CategoryId = Groceries.Id,
            Notes = "note XYZ",
        };
        var tx4 = new Transaction
        {
            Value = 200,
            Date = now,
            CategoryId = Education.Id,
            Notes = "Some other note",
        };

        // Create all test transactions
        await transactionController.Create(tx1);
        await transactionController.Create(tx2);
        await transactionController.Create(tx3);
        await transactionController.Create(tx4);

        // Act 2: Apply filters one by one and assert results
        var result1 = await insightController.TotalByPeriodByCategory();
        var response1 = Assert.IsType<TotalByPeriodByCategoryResult>(Assert.IsType<OkObjectResult>(result1).Value);
        
        Assert.Equal(75, response1.Expense["today"]["Groceries"]);
        Assert.Equal(200, response1.Expense["today"]["Education"]);
        Assert.Equal(50, response1.Expense["yesterday"]["Groceries"]);
        Assert.Equal(150, response1.Expense["yesterday"]["Education"]);

        // Handle month boundaries for "thisMonth" assertions
        var expectedGroceriesMonth = 75m;
        var expectedEducationMonth = 200m;

        if (today.Day > 1)
        {
            expectedGroceriesMonth += 50m;
            expectedEducationMonth += 150m;
        }
        
        Assert.Equal(expectedGroceriesMonth, response1.Expense["thisMonth"]["Groceries"]);
        Assert.Equal(expectedEducationMonth, response1.Expense["thisMonth"]["Education"]);
    }

    [Fact]
    public async Task Test_History()
    {
        // Arrange
        using var serviceProvider = await TestExtesions.MockLedgerServiceProviderAsync("xuser1");

        var categoriesController = ActivatorUtilities.CreateInstance<CategoryController>(serviceProvider);
        var transactionController = ActivatorUtilities.CreateInstance<TransactionController>(serviceProvider);
        var insightController = ActivatorUtilities.CreateInstance<InsightController>(serviceProvider);

        // Get available categories
        var allCategoriesResult = await categoriesController.GetAll();
        var categories = Assert.IsType<List<Category>>(Assert.IsType<OkObjectResult>(allCategoriesResult).Value);

        var Groceries = categories.FirstOrDefault(c => c.Name == "Groceries")!;
        var Education = categories.FirstOrDefault(c => c.Name == "Education")!;

        var now = DateTime.Now;

        // Act 1: Create test data
        var tx1 = new Transaction
        {
            Value = 75,
            Date = now,
            CategoryId = Groceries.Id,
            Notes = "note XYZ",
        };
        var tx2 = new Transaction
        {
            Value = 200,
            Date = now,
            CategoryId = Education.Id,
            Notes = "Some other note",
        };
        var tx3 = new Transaction
        {
            Value = 50,
            Date = now.AddDays(-1),
            CategoryId = Groceries.Id,
            Notes = "Note abc",
        };
        var tx4 = new Transaction
        {
            Value = 150,
            Date = now.AddDays(-1),
            CategoryId = Education.Id,
            Notes = "Another note",
        };

        // Create all test transactions
        await transactionController.Create(tx1);
        await transactionController.Create(tx2);
        await transactionController.Create(tx3);
        await transactionController.Create(tx4);

        // History
        var result1 = await insightController.History();
        var response1 = Assert.IsType<HistoryResult>(Assert.IsType<OkObjectResult>(result1).Value);

        // Assert dayly
        Assert.Collection(response1.Dayly,
            item =>
            {
                Assert.Equal(200.0m, item.Value);
                Assert.Equal(2, item.ByCategory.Count);
                Assert.Equal(150.0m, item.ByCategory["Education"]);
                Assert.Equal(50.0m, item.ByCategory["Groceries"]);
            },
            item =>
            {
                Assert.Equal(275.0m, item.Value);
                Assert.Equal(2, item.ByCategory.Count);
                Assert.Equal(200.0m, item.ByCategory["Education"]);
                Assert.Equal(75.0m, item.ByCategory["Groceries"]);
            });

        // Assert weekly
        var today = now.Date;
        var isMonday = today.DayOfWeek == DayOfWeek.Monday;
        if (isMonday)
        {
            Assert.Collection(response1.Weekly,
                item =>
                {
                    Assert.Equal(200.0m, item.Value);
                    Assert.Equal(2, item.ByCategory.Count);
                    Assert.Equal(150.0m, item.ByCategory["Education"]);
                    Assert.Equal(50.0m, item.ByCategory["Groceries"]);
                },
                item =>
                {
                    Assert.Equal(275.0m, item.Value);
                    Assert.Equal(2, item.ByCategory.Count);
                    Assert.Equal(200.0m, item.ByCategory["Education"]);
                    Assert.Equal(75.0m, item.ByCategory["Groceries"]);
                });
        }
        else
        {
            Assert.Collection(response1.Weekly,
                item =>
                {
                    Assert.Equal(475.0m, item.Value);
                    Assert.Equal(2, item.ByCategory.Count);
                    Assert.Equal(350.0m, item.ByCategory["Education"]);
                    Assert.Equal(125.0m, item.ByCategory["Groceries"]);
                });
        }

        // Assert monthly
        var isFirstDayOfMonth = today.Day == 1;
        if (isFirstDayOfMonth)
        {
            Assert.Collection(response1.Monthly,
                item =>
                {
                    Assert.Equal(200.0m, item.Value);
                    Assert.Equal(2, item.ByCategory.Count);
                    Assert.Equal(150.0m, item.ByCategory["Education"]);
                    Assert.Equal(50.0m, item.ByCategory["Groceries"]);
                },
                item =>
                {
                    Assert.Equal(275.0m, item.Value);
                    Assert.Equal(2, item.ByCategory.Count);
                    Assert.Equal(200.0m, item.ByCategory["Education"]);
                    Assert.Equal(75.0m, item.ByCategory["Groceries"]);
                });
        }
        else
        {
            Assert.Collection(response1.Monthly,
                item =>
                {
                    Assert.Equal(475.0m, item.Value);
                    Assert.Equal(2, item.ByCategory.Count);
                    Assert.Equal(350.0m, item.ByCategory["Education"]);
                    Assert.Equal(125.0m, item.ByCategory["Groceries"]);
                });
        }
    }

}
