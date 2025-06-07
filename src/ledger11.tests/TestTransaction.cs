using Microsoft.Extensions.DependencyInjection;
using Moq;
using ledger11.web.Controllers;
using Microsoft.AspNetCore.Mvc;
using ledger11.model.Data;
using FluentAssertions;
using ledger11.model.Api;
using ledger11.data;
using ledger11.service;

namespace ledger11.tests;

public class TestTransaction
{
    [Fact]
    public async Task TransactionController_Update()
    {
        // Arrange
        using var serviceProvider = await TestExtesions.MockLedgerServiceProviderAsync("xuser1");

        var categoriesController = ActivatorUtilities.CreateInstance<CategoryController>(serviceProvider);
        var transactionController = ActivatorUtilities.CreateInstance<TransactionController>(serviceProvider);

        // Get available categories
        var allCategoriesResult = await categoriesController.GetAll();
        var categories = Assert.IsType<List<Category>>(Assert.IsType<OkObjectResult>(allCategoriesResult).Value);

        Assert.True(categories.Count >= 2, "At least two categories are required for this test.");

        var initialCategory = categories[0];
        var updatedCategory = categories[1];

        // Act 1: Create a new transaction
        var newTransaction = new Transaction
        {
            Value = 50,
            Date = DateTime.UtcNow,
            CategoryId = initialCategory.Id,
            Notes = "note 1",
            TransactionDetails = new List<TransactionDetail>
            {
                new TransactionDetail
                {
                    Value = 50,
                    Quantity = 1,
                    Description = "Test Item",
                    CategoryId = initialCategory.Id
                }
            }
        };

        var createResult = await transactionController.Create(newTransaction);
        var created = Assert.IsType<CreatedAtActionResult>(createResult);
        var createdTransaction = Assert.IsType<Transaction>(created.Value);

        Assert.Equal(initialCategory.Id, createdTransaction.CategoryId);
        Assert.Equal("note 1", createdTransaction.Notes);

        // Act 2: Update the transaction with a new category
        createdTransaction.Category = null;
        createdTransaction.CategoryId = updatedCategory.Id;
        createdTransaction.Notes = "Note 2";

        var updateResult = await transactionController.Update(createdTransaction.Id, createdTransaction);
        var updated = Assert.IsType<OkObjectResult>(updateResult);
        var updatedTransaction = Assert.IsType<Transaction>(updated.Value);

        // Assert
        Assert.Equal(updatedCategory.Id, updatedTransaction.CategoryId);
        Assert.Equal("Note 2", updatedTransaction.Notes);
        Assert.Equal(createdTransaction.Id, updatedTransaction.Id);
    }
}
