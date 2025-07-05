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

    [Fact]
    public async Task Test_GetPerPeriodDataAsync()
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

        var today = new DateTime(2024, 1, 15);

        // Act 1: Create test data
        await transactionController.Create(new Transaction { Value = 10, Date = today, CategoryId = Groceries.Id });
        await transactionController.Create(new Transaction { Value = -70, Date = today, CategoryId = Groceries.Id }); // Income
        await transactionController.Create(new Transaction { Value = 20, Date = today.AddDays(-1), CategoryId = Education.Id }); // 2024-01-14
        await transactionController.Create(new Transaction { Value = 30, Date = today.AddDays(-7), CategoryId = Groceries.Id }); // 2024-01-08
        await transactionController.Create(new Transaction { Value = 40, Date = today.AddDays(-14), CategoryId = Education.Id }); // 2024-01-01
        await transactionController.Create(new Transaction { Value = 50, Date = new DateTime(2023, 12, 31), CategoryId = Groceries.Id });
        await transactionController.Create(new Transaction { Value = 60, Date = new DateTime(2023, 12, 25), CategoryId = Education.Id });
        

        // Act 2: Test daily period
        var dailyResult = await insightController.GetPerPeriodDataAsync("day", timeZoneId: "UTC");
        var dailyData = Assert.IsType<OkObjectResult>(dailyResult).Value as IEnumerable<PerPeriodData>;
        Assert.NotNull(dailyData);
        var dailyList = dailyData.ToList();

        // the default retun page size is 5
        Assert.Equal(5, dailyList.Count);
        Assert.Equal("2024-01-15", dailyList[0].Title);
        Assert.Equal(10, dailyList[0].Expense["Groceries"]);
        Assert.Equal(70, dailyList[0].Income["Groceries"]);
        Assert.Equal("2024-01-14", dailyList[1].Title);
        Assert.Equal(20, dailyList[1].Expense["Education"]);
        Assert.Equal("2024-01-08", dailyList[2].Title);
        Assert.Equal(30, dailyList[2].Expense["Groceries"]);
        Assert.Equal("2024-01-01", dailyList[3].Title);
        Assert.Equal(40, dailyList[3].Expense["Education"]);
        Assert.Equal("2023-12-31", dailyList[4].Title);
        Assert.Equal(50, dailyList[4].Expense["Groceries"]);
        // Assert.Equal("2023-12-25", dailyList[5].Title);
        // Assert.Equal(60, dailyList[5].Expense["Education"]);

        // Act 3: Test weekly period
        var weeklyResult = await insightController.GetPerPeriodDataAsync("week", timeZoneId: "UTC");
        var weeklyData = Assert.IsType<OkObjectResult>(weeklyResult).Value as IEnumerable<PerPeriodData>;
        Assert.NotNull(weeklyData);
        var weeklyList = weeklyData.ToList();

        Assert.Equal(4, weeklyList.Count);
        Assert.Equal("2024-W03", weeklyList[0].Title);
        Assert.Equal(10, weeklyList[0].Expense["Groceries"]);
        Assert.Equal(70, weeklyList[0].Income["Groceries"]);
        Assert.Equal("2024-W02", weeklyList[1].Title);
        Assert.Equal(30, weeklyList[1].Expense["Groceries"]);
        Assert.Equal(20, weeklyList[1].Expense["Education"]);
        Assert.Equal("2024-W01", weeklyList[2].Title);
        Assert.Equal(40, weeklyList[2].Expense["Education"]);
        Assert.Equal("2023-W53", weeklyList[3].Title);
        Assert.Equal(50, weeklyList[3].Expense["Groceries"]);
        Assert.Equal(60, weeklyList[3].Expense["Education"]);

        // Act 4: Test monthly period
        var monthlyResult = await insightController.GetPerPeriodDataAsync("month", timeZoneId: "UTC");
        var monthlyData = Assert.IsType<OkObjectResult>(monthlyResult).Value as IEnumerable<PerPeriodData>;
        Assert.NotNull(monthlyData);
        var monthlyList = monthlyData.ToList();
        
        Assert.Equal(2, monthlyList.Count);
        Assert.Equal("January 2024", monthlyList[0].Title);
        Assert.Equal(40, monthlyList[0].Expense["Groceries"]);
        Assert.Equal(60, monthlyList[0].Expense["Education"]);
        Assert.Equal(70, monthlyList[0].Income["Groceries"]);
        Assert.Equal("December 2023", monthlyList[1].Title);
        Assert.Equal(50, monthlyList[1].Expense["Groceries"]);
        Assert.Equal(60, monthlyList[1].Expense["Education"]);

        // Test pagination
        var dailyPagedResult = await insightController.GetPerPeriodDataAsync("day", start: 2, count: 2, timeZoneId: "UTC");
        var dailyPagedData = Assert.IsType<OkObjectResult>(dailyPagedResult).Value as IEnumerable<PerPeriodData>;
        Assert.NotNull(dailyPagedData);
        var dailyPagedList = dailyPagedData.ToList();

        Assert.Equal(2, dailyPagedList.Count);
        Assert.Equal("2024-01-08", dailyPagedList[0].Title);
        Assert.Equal("2024-01-01", dailyPagedList[1].Title);
    }
}
