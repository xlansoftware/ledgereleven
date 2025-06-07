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

public class TestPurchases
{
    [Fact]
    public async Task PurchasesController_Parse()
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
                    ""total_paid"": ""2"",
                    ""category"": ""Dining Out""
                    }" });

            // Register mocked service
            services.AddSingleton<IChatGptService>(mockChatGpt.Object);

        });

        var controller = ActivatorUtilities.CreateInstance<PurchasesController>(serviceProvider);

        // Act
        var result = await controller.Parse(new PurchasesController.ParseArgs
        {
            Query = "dos tequilas por favor",
        });

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);

        (ok.Value as Receipt).Should().BeEquivalentTo(new Receipt
        {
            Category = "Dining Out",
            TotalPaid = "2",
            Items = [new Item { Name = "tequila", Quantity = "2", UnitPrice = "1", TotalPrice = "2" }]
        });
    }

}
