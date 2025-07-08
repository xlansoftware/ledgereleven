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
        using var serviceProvider = await TestExtesions.MockLedgerServiceProviderAsync("xuser1");

        var categoriesController = ActivatorUtilities.CreateInstance<CategoryController>(serviceProvider);
        var transactionController = ActivatorUtilities.CreateInstance<TransactionController>(serviceProvider);
        var insightController = ActivatorUtilities.CreateInstance<InsightController>(serviceProvider);

        var allCategoriesResult = await categoriesController.GetAll();
        var categories = Assert.IsType<List<Category>>(Assert.IsType<OkObjectResult>(allCategoriesResult).Value);

        var Groceries = categories.First(c => c.Name == "Groceries");
        var Education = categories.First(c => c.Name == "Education");

        var now = DateTime.Now;
        var today = now.Date;

        var tx1 = new Transaction { Value = 50, Date = now.AddDays(-1), CategoryId = Groceries.Id, Notes = "Note abc" };
        var tx2 = new Transaction { Value = 150, Date = now.AddDays(-1), CategoryId = Education.Id, Notes = "Another note" };
        var tx3 = new Transaction { Value = 75, Date = now, CategoryId = Groceries.Id, Notes = "note XYZ" };
        var tx4 = new Transaction { Value = 200, Date = now, CategoryId = Education.Id, Notes = "Some other note" };

        // New: foreign currency transactions
        var tx5 = new Transaction
        {
            Value = 100,
            Currency = "EUR",
            ExchangeRate = 1.1m,
            Date = now,
            CategoryId = Groceries.Id,
            Notes = "Groceries EUR"
        };
        var tx6 = new Transaction
        {
            Value = 10000,
            Currency = "JPY",
            ExchangeRate = 0.009m,
            Date = now,
            CategoryId = Education.Id,
            Notes = "Education JPY"
        };

        await transactionController.Create(tx1);
        await transactionController.Create(tx2);
        await transactionController.Create(tx3);
        await transactionController.Create(tx4);
        await transactionController.Create(tx5);
        await transactionController.Create(tx6);

        var result1 = await insightController.TotalByPeriodByCategory();
        var response1 = Assert.IsType<TotalByPeriodByCategoryResult>(Assert.IsType<OkObjectResult>(result1).Value);

        var eur = 100 * 1.1m;
        var jpy = 10000 * 0.009m;

        Assert.Equal(75 + eur, response1.Expense["today"]["Groceries"]);
        Assert.Equal(200 + jpy, response1.Expense["today"]["Education"]);
        Assert.Equal(50, response1.Expense["yesterday"]["Groceries"]);
        Assert.Equal(150, response1.Expense["yesterday"]["Education"]);

        var expectedGroceriesMonth = 75 + eur;
        var expectedEducationMonth = 200 + jpy;

        if (today.Day > 1)
        {
            expectedGroceriesMonth += 50;
            expectedEducationMonth += 150;
        }

        Assert.Equal(expectedGroceriesMonth, response1.Expense["thisMonth"]["Groceries"]);
        Assert.Equal(expectedEducationMonth, response1.Expense["thisMonth"]["Education"]);
    }

    [Fact]
    public async Task Test_History()
    {
        using var serviceProvider = await TestExtesions.MockLedgerServiceProviderAsync("xuser1");

        var categoriesController = ActivatorUtilities.CreateInstance<CategoryController>(serviceProvider);
        var transactionController = ActivatorUtilities.CreateInstance<TransactionController>(serviceProvider);
        var insightController = ActivatorUtilities.CreateInstance<InsightController>(serviceProvider);

        var allCategoriesResult = await categoriesController.GetAll();
        var categories = Assert.IsType<List<Category>>(Assert.IsType<OkObjectResult>(allCategoriesResult).Value);

        var Groceries = categories.First(c => c.Name == "Groceries");
        var Education = categories.First(c => c.Name == "Education");

        var now = DateTime.Now;

        var tx1 = new Transaction { Value = 75, Date = now, CategoryId = Groceries.Id };
        var tx2 = new Transaction { Value = 200, Date = now, CategoryId = Education.Id };
        var tx3 = new Transaction { Value = 50, Date = now.AddDays(-1), CategoryId = Groceries.Id };
        var tx4 = new Transaction { Value = 150, Date = now.AddDays(-1), CategoryId = Education.Id };

        // New: foreign currency
        var tx5 = new Transaction
        {
            Value = 100,
            Currency = "EUR",
            ExchangeRate = 1.1m,
            Date = now,
            CategoryId = Groceries.Id
        };
        var tx6 = new Transaction
        {
            Value = 10000,
            Currency = "JPY",
            ExchangeRate = 0.009m,
            Date = now,
            CategoryId = Education.Id
        };

        await transactionController.Create(tx1);
        await transactionController.Create(tx2);
        await transactionController.Create(tx3);
        await transactionController.Create(tx4);
        await transactionController.Create(tx5);
        await transactionController.Create(tx6);

        var result1 = await insightController.History();
        var response1 = Assert.IsType<HistoryResult>(Assert.IsType<OkObjectResult>(result1).Value);

        var eur = 100 * 1.1m;
        var jpy = 10000 * 0.009m;

        Assert.Collection(response1.Dayly,
            item =>
            {
                Assert.Equal(150, item.ByCategory["Education"]);
                Assert.Equal(50, item.ByCategory["Groceries"]);
            },
            item =>
            {
                Assert.Equal(200 + jpy, item.ByCategory["Education"]);
                Assert.Equal(75 + eur, item.ByCategory["Groceries"]);
            }
        );

        var today = now.Date;
        if (today.DayOfWeek == DayOfWeek.Monday)
        {
            Assert.Collection(response1.Weekly,
                item =>
                {
                    Assert.Equal(150, item.ByCategory["Education"]);
                    Assert.Equal(50, item.ByCategory["Groceries"]);
                },
                item =>
                {
                    Assert.Equal(200 + jpy, item.ByCategory["Education"]);
                    Assert.Equal(75 + eur, item.ByCategory["Groceries"]);
                });
        }
        else
        {
            Assert.Collection(response1.Weekly,
                item =>
                {
                    Assert.Equal(350 + jpy, item.ByCategory["Education"]);
                    Assert.Equal(125 + eur, item.ByCategory["Groceries"]);
                });
        }

        if (today.Day == 1)
        {
            Assert.Collection(response1.Monthly,
                item =>
                {
                    Assert.Equal(150, item.ByCategory["Education"]);
                    Assert.Equal(50, item.ByCategory["Groceries"]);
                },
                item =>
                {
                    Assert.Equal(200 + jpy, item.ByCategory["Education"]);
                    Assert.Equal(75 + eur, item.ByCategory["Groceries"]);
                });
        }
        else
        {
            Assert.Collection(response1.Monthly,
                item =>
                {
                    Assert.Equal(350 + jpy, item.ByCategory["Education"]);
                    Assert.Equal(125 + eur, item.ByCategory["Groceries"]);
                });
        }
    }

    [Fact]
    public async Task Test_GetPerPeriodDataAsync()
    {
        using var serviceProvider = await TestExtesions.MockLedgerServiceProviderAsync("xuser1");

        var categoriesController = ActivatorUtilities.CreateInstance<CategoryController>(serviceProvider);
        var transactionController = ActivatorUtilities.CreateInstance<TransactionController>(serviceProvider);
        var insightController = ActivatorUtilities.CreateInstance<InsightController>(serviceProvider);

        var allCategoriesResult = await categoriesController.GetAll();
        var categories = Assert.IsType<List<Category>>(Assert.IsType<OkObjectResult>(allCategoriesResult).Value);

        var Groceries = categories.First(c => c.Name == "Groceries");
        var Education = categories.First(c => c.Name == "Education");

        var today = new DateTime(2024, 1, 15);

        await transactionController.Create(new Transaction { Value = 10, Date = today, CategoryId = Groceries.Id });
        await transactionController.Create(new Transaction { Value = -70, Date = today, CategoryId = Groceries.Id });
        await transactionController.Create(new Transaction { Value = 20, Date = today.AddDays(-1), CategoryId = Education.Id });
        await transactionController.Create(new Transaction { Value = 30, Date = today.AddDays(-7), CategoryId = Groceries.Id });
        await transactionController.Create(new Transaction { Value = 40, Date = today.AddDays(-14), CategoryId = Education.Id });
        await transactionController.Create(new Transaction { Value = 50, Date = new DateTime(2023, 12, 31), CategoryId = Groceries.Id });
        await transactionController.Create(new Transaction { Value = 60, Date = new DateTime(2023, 12, 25), CategoryId = Education.Id });

        // New: Foreign currency transactions
        await transactionController.Create(new Transaction
        {
            Value = 80,
            Currency = "EUR",
            ExchangeRate = 1.1m,
            Date = today,
            CategoryId = Groceries.Id
        });

        await transactionController.Create(new Transaction
        {
            Value = 5000,
            Currency = "JPY",
            ExchangeRate = 0.009m,
            Date = today,
            CategoryId = Education.Id
        });

        var eur = 80 * 1.1m;
        var jpy = 5000 * 0.009m;

        var dailyResult = await insightController.GetPerPeriodDataAsync("day", timeZoneId: "UTC");
        var dailyData = Assert.IsType<OkObjectResult>(dailyResult).Value as IEnumerable<PerPeriodData>;
        var dailyList = dailyData!.ToList();

        Assert.Equal("2024-01-15", dailyList[0].Title);
        Assert.Equal(10 + eur, dailyList[0].Expense["Groceries"]);
        Assert.Equal(70, dailyList[0].Income["Groceries"]);
        Assert.Equal(jpy, dailyList[0].Expense["Education"]);
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
        var weeklyList = weeklyData!.ToList();

        Assert.Equal("2024-W03", weeklyList[0].Title);
        Assert.Equal(10 + eur, weeklyList[0].Expense["Groceries"]);
        Assert.Equal(70, weeklyList[0].Income["Groceries"]);
        Assert.Equal(jpy, weeklyList[0].Expense["Education"]);
        Assert.Equal("2024-W02", weeklyList[1].Title);
        Assert.Equal(30, weeklyList[1].Expense["Groceries"]);
        Assert.Equal(20, weeklyList[1].Expense["Education"]);
        Assert.Equal("2024-W01", weeklyList[2].Title);
        Assert.Equal(40, weeklyList[2].Expense["Education"]);
        Assert.Equal("2023-W53", weeklyList[3].Title);
        Assert.Equal(50, weeklyList[3].Expense["Groceries"]);
        Assert.Equal(60, weeklyList[3].Expense["Education"]);


        var monthlyResult = await insightController.GetPerPeriodDataAsync("month", timeZoneId: "UTC");
        var monthlyList = (Assert.IsType<OkObjectResult>(monthlyResult).Value as IEnumerable<PerPeriodData>)!.ToList();

        Assert.Equal("January 2024", monthlyList[0].Title);
        Assert.Equal(10 + 30 + eur, monthlyList[0].Expense["Groceries"]);
        Assert.Equal(20 + 40 + jpy, monthlyList[0].Expense["Education"]);
        Assert.Equal(70, monthlyList[0].Income["Groceries"]);
        Assert.Equal("December 2023", monthlyList[1].Title);
        Assert.Equal(50, monthlyList[1].Expense["Groceries"]);
        Assert.Equal(60, monthlyList[1].Expense["Education"]);

        var dailyPagedResult = await insightController.GetPerPeriodDataAsync("day", start: 2, count: 2, timeZoneId: "UTC");
        var dailyPagedList = (Assert.IsType<OkObjectResult>(dailyPagedResult).Value as IEnumerable<PerPeriodData>)!.ToList();

        Assert.Equal("2024-01-08", dailyPagedList[0].Title);
        Assert.Equal("2024-01-01", dailyPagedList[1].Title);
    }
}
