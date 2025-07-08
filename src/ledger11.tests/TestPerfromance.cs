using Microsoft.Extensions.DependencyInjection;
using ledger11.web.Controllers;
using Microsoft.AspNetCore.Mvc;
using ledger11.model.Data;
using Xunit;
using System.Diagnostics;
using ledger11.model.Api;

namespace ledger11.tests;

//TODO: Move the performance tests in a dedicated assembly ledger11.performance
public class TestPerformance
{
    // [Fact]
    public async Task Test_Create1000Transactions()
    {
        // Arrange
        using var serviceProvider = await TestExtesions.MockLedgerServiceProviderAsync("xuser1");

        // Get categories
        var categoriesController = ActivatorUtilities.CreateInstance<CategoryController>(serviceProvider);
        var allCategoriesResult = await categoriesController.GetAll();
        var categories = Assert.IsType<List<Category>>(Assert.IsType<OkObjectResult>(allCategoriesResult).Value);
        Assert.True(categories.Count >= 1, "At least one category is required for this test.");
        var initialCategory = categories[0];

        const int transactionCount = 1000;
        var createdTransactions = new Transaction[transactionCount];
        var tasks = new List<Task>();
        var stopwatch = Stopwatch.StartNew();

        // Act: Create transactions in parallel
        for (int i = 0; i < transactionCount; i++)
        {
            int index = i;
            tasks.Add(Task.Run(async () =>
            {
                using var scope = serviceProvider.CreateScope();
                var scopedTransactionController = ActivatorUtilities.CreateInstance<TransactionController>(scope.ServiceProvider);

                var transaction = new Transaction
                {
                    Value = 50 + (index % 100), // Vary value between 50 and 149
                    Date = DateTime.UtcNow.AddSeconds(-index), // Offset time
                    CategoryId = initialCategory.Id,
                };

                var createResult = await scopedTransactionController.Create(transaction);
                var created = Assert.IsType<CreatedAtActionResult>(createResult);
                var createdTransaction = Assert.IsType<Transaction>(created.Value);

                createdTransactions[index] = createdTransaction;
            }));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        Assert.Equal(transactionCount, createdTransactions.Count(t => t != null));
        Console.WriteLine($"Time taken to create {transactionCount} transactions: {stopwatch.Elapsed.TotalSeconds:F2} seconds");
    }

    // [Fact]
    public async Task Test_SpaceList()
    {
        // Arrange
        using var serviceProvider = await TestExtesions.MockLedgerServiceProviderAsync("xuser1");

        // Get categories
        var categoriesController = ActivatorUtilities.CreateInstance<CategoryController>(serviceProvider);
        var allCategoriesResult = await categoriesController.GetAll();
        var categories = Assert.IsType<List<Category>>(Assert.IsType<OkObjectResult>(allCategoriesResult).Value);
        Assert.True(categories.Count >= 1, "At least one category is required for this test.");
        var initialCategory = categories[0];

        var transactionsController = ActivatorUtilities.CreateInstance<TransactionController>(serviceProvider);
        var spaceController = ActivatorUtilities.CreateInstance<SpaceController>(serviceProvider);

        const int transactionCount = 1000;
        var stopwatch = Stopwatch.StartNew();

        // Act: Create transactions
        for (int i = 0; i < transactionCount; i++)
        {
            var transaction = new Transaction
            {
                Value = 50 + (i % 100), // Vary value between 50 and 149
                Date = DateTime.UtcNow.AddSeconds(-i), // Offset time
                CategoryId = initialCategory.Id,
            };

            var createResult = await transactionsController.Create(transaction);
            var created = Assert.IsType<CreatedAtActionResult>(createResult);
            var createdTransaction = Assert.IsType<Transaction>(created.Value);
        }

        stopwatch.Stop();
        Console.WriteLine($"Time taken to create {transactionCount} transactions: {stopwatch.ElapsedMilliseconds:F2} ms");

        // Act: List spaces
        stopwatch = Stopwatch.StartNew();
        var listResult = await spaceController.List();
        var dto = Assert.IsType<SpaceListResponseDto>(Assert.IsType<OkObjectResult>(listResult).Value);
        stopwatch.Stop();
        Console.WriteLine($"Time taken to list spaces: {stopwatch.ElapsedMilliseconds:F2} ms");
        Assert.True(stopwatch.ElapsedMilliseconds < 100, "Listing spaces should take less than 100 ms.");

        // Assert
        Assert.NotNull(dto);
        Assert.NotNull(dto.Current);
        Assert.NotEmpty(dto.Spaces);
        Assert.Equal(1000, dto.Spaces[0].CountTransactions);
        Assert.Equal(17, dto.Spaces[0].CountCategories);
        Assert.Equal(99500, dto.Spaces[0].TotalValue);
    }

}
