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

public class TestCategory
{
    [Fact]
    public async Task Test_NewUserHasCategories()
    {
        using var serviceProvider = await TestExtesions.MockLedgerServiceProviderAsync("xuser1", (services) =>
        {
            // Mock IChatGptService
            var mockChatGpt = new Mock<IChatGptService>();
            mockChatGpt
                .Setup(s => s.SendTextToChatGptAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new AIResponse { result = @"{
                    ""items"": [{
                        ""name"": ""tequila"",
                        ""quantity"": ""2"",
                        ""unit_price"": ""1"",
                        ""total_price"": ""2""
                    }],
                    ""total_paid"": ""2""
                    }" });

            // Register mocked service
            services.AddSingleton<IChatGptService>(mockChatGpt.Object);

        });
        var controller = ActivatorUtilities.CreateInstance<CategoryController>(serviceProvider);

        // Act
        var result = await controller.GetAll();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsType<List<Category>>(ok.Value);
        Assert.NotEmpty(list);
    }

    [Fact]
    public async Task Test_Create()
    {
        using var serviceProvider = await TestExtesions.MockLedgerServiceProviderAsync("xuser1");

        // Set up fake login in the same scope
        var controller = ActivatorUtilities.CreateInstance<CategoryController>(serviceProvider);

        // Act
        var result = await controller.Create(new Category
        {
            Name = "Test Category",
            Color = "red",
            Icon = "test-icon"
        });

        // Assert
        var ok = Assert.IsType<CreatedAtActionResult>(result);
        var item = Assert.IsType<Category>(ok.Value);
        Assert.Equal("Test Category", item.Name);

        // read it back
        var getResult = await controller.Get(item.Id);
        var getOk = Assert.IsType<OkObjectResult>(getResult);
        var getItem = Assert.IsType<Category>(getOk.Value);
        Assert.Equal("Test Category", getItem.Name);
        Assert.Equal(item.Id, getItem.Id);
    }

    [Fact]
    public async Task Test_Reorder()
    {
        using var serviceProvider = await TestExtesions.MockLedgerServiceProviderAsync("xuser1");

        var controller = ActivatorUtilities.CreateInstance<CategoryController>(serviceProvider);

        // delete existing categories
        var allCategories = await controller.GetAll();
        var toDelete = Assert.IsType<List<Category>>(Assert.IsType<OkObjectResult>(allCategories).Value);
        // Create 3 categories
        var result1 = await controller.Create(new Category { Name = "Category A" });
        var result2 = await controller.Create(new Category { Name = "Category B" });
        var result3 = await controller.Create(new Category { Name = "Category C" });

        var cat1 = Assert.IsType<Category>(Assert.IsType<CreatedAtActionResult>(result1).Value);
        var cat2 = Assert.IsType<Category>(Assert.IsType<CreatedAtActionResult>(result2).Value);
        var cat3 = Assert.IsType<Category>(Assert.IsType<CreatedAtActionResult>(result3).Value);


        // if we delete the before the create of the new categories
        // a default values will be re-created...
        foreach (var category in toDelete)
        {
            await controller.Delete(category.Id, null);
        }

        // New order: [cat3, cat1, cat2]
        var newOrder = new[] { cat3.Id, cat1.Id, cat2.Id };
        var reorderResult = await controller.Reorder(newOrder);
        Assert.IsType<OkResult>(reorderResult);

        // Get all categories and validate new order
        var getAllResult = await controller.GetAll();
        var ok = Assert.IsType<OkObjectResult>(getAllResult);
        var categories = Assert.IsAssignableFrom<List<Category>>(ok.Value);

        Assert.Equal(3, categories.Count);
        Assert.Equal(newOrder[0], categories[0].Id);
        Assert.Equal(newOrder[1], categories[1].Id);
        Assert.Equal(newOrder[2], categories[2].Id);

        Assert.Equal(0 + 1, categories[0].DisplayOrder);
        Assert.Equal(1 + 1, categories[1].DisplayOrder);
        Assert.Equal(2 + 1, categories[2].DisplayOrder);
    }
}
