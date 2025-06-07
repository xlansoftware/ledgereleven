using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using FluentAssertions;
using ledger11.model.Data;
using ledger11.model.Api;
using ledger11.web.Controllers;
using Xunit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ledger11.tests;

public class TestApiSpace
{
    [Fact]
    public async Task Test_Create_Space()
    {
        // Arrange
        using var serviceProvider = await TestExtesions.MockLedgerServiceProviderAsync("xuser1");

        var controller = ActivatorUtilities.CreateInstance<ApiSpaceController>(serviceProvider);

        // Act
        var response = await controller.Create(new Space
        {
            Name = "Test Space",
            Tint = "#FF0000",
            Currency = "USD"
        });

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(response);
        var result = Assert.IsType<SpaceDto>(createdResult.Value);
        result.Name.Should().Be("Test Space");
        result.Tint.Should().Be("#FF0000");
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public async Task Test_List_Spaces()
    {
        // Arrange
        using var serviceProvider = await TestExtesions.MockLedgerServiceProviderAsync("xuser1");
        var controller = ActivatorUtilities.CreateInstance<ApiSpaceController>(serviceProvider);

        // Act
        var result = await controller.List();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var data = Assert.IsType<SpaceListResponseDto>(okResult.Value);
        data.Current.Should().NotBeNull();
        data.Spaces.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Test_Delete_Space()
    {
        // Arrange
        using var serviceProvider = await TestExtesions.MockLedgerServiceProviderAsync("xuser1");
        var controller = ActivatorUtilities.CreateInstance<ApiSpaceController>(serviceProvider);

        var createResult = await controller.Create(new Space
        {
            Name = "ToDelete",
            Tint = "#111",
            Currency = "EUR"
        });
        var created = Assert.IsType<CreatedAtActionResult>(createResult);
        var space = Assert.IsType<SpaceDto>(created.Value);

        // Act
        var deleteResult = await controller.Delete(space.Id);

        // Assert
        var okDelete = Assert.IsType<OkObjectResult>(deleteResult);
        var list = Assert.IsType<SpaceListResponseDto>(okDelete.Value);
        list.Current.Should().NotBeNull();
        list.Current?.Id.Should().NotBe(space.Id);
        list.Spaces.Should().NotContain(s => s.Id == space.Id);

        // assert that after the delete, the other space is set
        var listResult = await controller.List();
        var okList = Assert.IsType<OkObjectResult>(deleteResult);
        var listData = Assert.IsType<SpaceListResponseDto>(okList.Value);
        listData.Current.Should().NotBeNull();
        listData.Current?.Id.Should().NotBe(space.Id);
        listData.Spaces.Should().NotContain(s => s.Id == space.Id);

        // delete the last space
        var deleteLastResult = await controller.Delete(list.Current!.Id);
        var okLastDelete = Assert.IsType<OkObjectResult>(deleteLastResult);
        var lastList = Assert.IsType<SpaceListResponseDto>(okLastDelete.Value);
        
        // if the last space is deleted, a new one is automatically created and assigned
        lastList.Current.Should().NotBeNull();
        lastList.Spaces.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task Test_Update_Space()
    {
        // Arrange
        using var serviceProvider = await TestExtesions.MockLedgerServiceProviderAsync("xuser1");
        var controller = ActivatorUtilities.CreateInstance<ApiSpaceController>(serviceProvider);

        var createResult = await controller.Create(new Space
        {
            Name = "ToUpdate",
            Tint = "#222",
            Currency = "GBP"
        });
        var created = Assert.IsType<CreatedAtActionResult>(createResult);
        var space = Assert.IsType<SpaceDto>(created.Value);

        var updates = new Dictionary<string, object>
        {
            ["Name"] = "Updated Name",
            ["Tint"] = "#333"
        };

        // Act
        var updateResult = await controller.Update(space.Id, updates);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(updateResult);
        var updated = Assert.IsType<SpaceDto>(okResult.Value);
        updated.Name.Should().Be("Updated Name");
        updated.Tint.Should().Be("#333");
        updated.Currency.Should().Be("GBP"); // unchanged
    }

    [Fact]
    public async Task Test_Set_Current_Space()
    {
        // Arrange
        using var serviceProvider = await TestExtesions.MockLedgerServiceProviderAsync("xuser1");
        var controller = ActivatorUtilities.CreateInstance<ApiSpaceController>(serviceProvider);

        var createResult = await controller.Create(new Space
        {
            Name = "Main",
            Tint = "#ABC",
            Currency = "JPY"
        });
        var created = Assert.IsType<CreatedAtActionResult>(createResult);
        var space = Assert.IsType<SpaceDto>(created.Value);

        // Act
        var result = await controller.Current(space.Id);

        // Assert
        Assert.IsType<OkResult>(result);

        // Optional: confirm it is current
        var listResult = await controller.List();
        var okList = Assert.IsType<OkObjectResult>(listResult);
        var listData = Assert.IsType<SpaceListResponseDto>(okList.Value);
        listData.Current.Should().NotBeNull();
        listData.Current?.Id.Should().Be(space.Id);
    }
}
