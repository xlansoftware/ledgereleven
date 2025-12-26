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
using Microsoft.AspNetCore.Identity;
using ledger11.data;
using Microsoft.EntityFrameworkCore;
using ledger11.service;

namespace ledger11.tests;

public class TestSpaceController
{
    [Fact]
    public async Task Test_Create_Space()
    {
        // Arrange
        using var serviceProvider = await TestExtesions.MockLedgerServiceProviderAsync("xuser1");

        var controller = ActivatorUtilities.CreateInstance<SpaceController>(serviceProvider);

        // Act
        var response = await controller.Create(new CreateSpaceDto
        {
            Name = "Test Space",
        });

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(response);
        var result = Assert.IsType<SpaceDto>(createdResult.Value);
        result.Name.Should().Be("Test Space");
    }

    [Fact]
    public async Task Test_List_Spaces()
    {
        // Arrange
        using var serviceProvider = await TestExtesions.MockLedgerServiceProviderAsync("xuser1");
        var controller = ActivatorUtilities.CreateInstance<SpaceController>(serviceProvider);

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
        var controller = ActivatorUtilities.CreateInstance<SpaceController>(serviceProvider);

        var createResult = await controller.Create(new CreateSpaceDto
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
        var controller = ActivatorUtilities.CreateInstance<SpaceController>(serviceProvider);

        var createResult = await controller.Create(new CreateSpaceDto
        {
            Name = "ToUpdate",
        });
        var created = Assert.IsType<CreatedAtActionResult>(createResult);
        var space = Assert.IsType<SpaceDto>(created.Value);

        var updates = new Dictionary<string, object>
        {
            ["Name"] = "Updated Name",
            ["Settings"] = new Dictionary<string, string>
            {
                ["Tint"] = "#333", // updated
                ["Currency"] = "GBP", // unchanged
            },

        };

        // Act
        var updateResult = await controller.Update(space.Id, updates);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(updateResult);
        var updated = Assert.IsType<SpaceDto>(okResult.Value);
        updated.Name.Should().Be("Updated Name");
        updated.Settings["Tint"].Should().Be("#333");
        updated.Settings["Currency"].Should().Be("GBP"); // unchanged

        // Assert trough list
        var listResult = await controller.List();
        var okList = Assert.IsType<OkObjectResult>(listResult);
        var listData = Assert.IsType<SpaceListResponseDto>(okList.Value);
        listData.Current.Should().NotBeNull();
        listData.Current?.Id.Should().Be(space.Id);
        listData.Spaces.Should().ContainSingle(s => s.Id == space.Id);
        var updatedSpace = listData.Spaces.Single(s => s.Id == space.Id);
        updatedSpace.Name.Should().Be("Updated Name");
        updatedSpace.Settings["Tint"].Should().Be("#333");
        updatedSpace.Settings["Currency"].Should().Be("GBP"); // unchanged
    }

    [Fact]
    public async Task Test_Set_Current_Space()
    {
        // Arrange
        using var serviceProvider = await TestExtesions.MockLedgerServiceProviderAsync("xuser1");
        var controller = ActivatorUtilities.CreateInstance<SpaceController>(serviceProvider);

        var createResult = await controller.Create(new CreateSpaceDto
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

    [Fact]
    public async Task Test_Share()
    {
        // Arrange
        using var serviceProvider = await TestExtesions.MockLedgerServiceProviderAsync("xuser1");
        var dbContext = serviceProvider.GetRequiredService<AppDbContext>();

        // Create another user
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var otherUser = new ApplicationUser
        {
            UserName = "otheremail@example.com",
            Email = "otheremail@example.com"
        };
        var createUserResult = await userManager.CreateAsync(otherUser);
        Assert.True(createUserResult.Succeeded, "Failed to create other user");
        var otherUserId = otherUser.Id;

        Console.WriteLine($"Created other user: {otherUser.UserName} ({otherUserId})");
        foreach (var user in dbContext.Users)
        {
            Console.WriteLine($"User: {user.UserName} ({user.Id})");
        }

        var controller = ActivatorUtilities.CreateInstance<SpaceController>(serviceProvider);

        // Get the id of the current space
        var list = await controller.List();
        var okList = Assert.IsType<OkObjectResult>(list);
        var listData = Assert.IsType<SpaceListResponseDto>(okList.Value);
        var currentSpaceId = listData.Current?.Id ?? Guid.Empty;
        Assert.NotEqual(Guid.Empty, currentSpaceId);
        Assert.NotEmpty(listData.Spaces);

        // Share the space with the other user
        var shareRequest = new ShareSpaceRequestDto
        {
            SpaceId = currentSpaceId,
            Email = otherUser.Email
        };

        var shareResult = await controller.Share(shareRequest);
        Assert.IsType<OkObjectResult>(shareResult);

        // Verify the other user has access to the space
        var sharedSpace = await dbContext.Spaces.FindAsync(currentSpaceId);
        Assert.NotNull(sharedSpace);
        Assert.Contains(sharedSpace.Members, m => m.UserId == otherUserId && m.AccessLevel == AccessLevel.Editor);
        Console.WriteLine($"Space '{sharedSpace.Name}' shared with {otherUser.UserName} ({otherUserId})");
        var sharedSpaces = await dbContext.SpaceMembers
            .Where(m => m.UserId == otherUserId)
            .Select(m => m.Space)
            .ToListAsync();
        Assert.Contains(sharedSpaces, s => s.Id == currentSpaceId);
        Console.WriteLine($"Other user has access to shared space: {sharedSpaces.Count} spaces found.");
        foreach (var space in sharedSpaces)
        {
            Console.WriteLine($"Shared Space: {space.Name} ({space.Id})");
        }
        Assert.NotEmpty(sharedSpaces);

        // Check via list
        var list2 = await controller.List();
        var okList2 = Assert.IsType<OkObjectResult>(list2);
        var listData2 = Assert.IsType<SpaceListResponseDto>(okList2.Value);
        Assert.NotEmpty(listData.Spaces);
        // assert listData.Spaces[0].Members has member otherUser.Email 
        Assert.NotEmpty(listData2.Spaces[0].Members);
        Assert.Contains(listData2.Spaces[0].Members, m => m == otherUser.Email);

    }

    [Fact]
    public async Task Test_Merge()
    {
        // Arrange
        using var serviceProvider = await TestExtesions.MockLedgerServiceProviderAsync("xuser_merge");
        var controller = ActivatorUtilities.CreateInstance<SpaceController>(serviceProvider);
        var userSpaceService = serviceProvider.GetRequiredService<IUserSpaceService>();

        // 1. Create source space and add a transaction to it
        var sourceSpaceResponse = await controller.Create(new CreateSpaceDto { Name = "Source Space", Currency = "USD" });
        var sourceSpace = Assert.IsType<SpaceDto>(((CreatedAtActionResult)sourceSpaceResponse).Value);

        var sourceLedger = await userSpaceService.GetLedgerDbContextAsync(sourceSpace.Id, true);
        sourceLedger.Transactions.Add(new Transaction { Value = 100, Date = DateTime.UtcNow, Notes = "Source Tx 1" });
        sourceLedger.Transactions.Add(new Transaction { Value = 50, Date = DateTime.UtcNow, Notes = "Source Tx 2" });
        await sourceLedger.SaveChangesAsync();

        // 2. Create target space
        var targetSpaceResponse = await controller.Create(new CreateSpaceDto { Name = "Target Space", Currency = "USD" });
        var targetSpace = Assert.IsType<SpaceDto>(((CreatedAtActionResult)targetSpaceResponse).Value);
        var targetLedger = await userSpaceService.GetLedgerDbContextAsync(targetSpace.Id, true);
        var targetCategory = await targetLedger.Categories.FirstAsync();

        // 3. Prepare merge request
        var mergeRequest = new MergeSpaceRequestDto
        {
            SourceSpaceId = sourceSpace.Id,
            TargetSpaceId = targetSpace.Id,
            TargetCategoryId = targetCategory.Id
        };

        // Act
        var mergeResult = await controller.Merge(mergeRequest);

        // Assert
        Assert.IsType<OkObjectResult>(mergeResult);

        var targetTransactions = await targetLedger.Transactions.ToListAsync();
        targetTransactions.Should().HaveCount(1);
        var mergedTransaction = targetTransactions.First();
        mergedTransaction.Value.Should().Be(150); // 100 + 50
        mergedTransaction.Notes.Should().Be("Source Space");
        mergedTransaction.CategoryId.Should().Be(targetCategory.Id);

        var sourceSettings = await sourceLedger.Settings.ToListAsync();
        sourceSettings.Should().Contain(s => s.Key == "Status" && s.Value == "Closed");
        sourceSettings.Should().Contain(s => s.Key == "ClosingBalanceTransferLedger" && s.Value == targetSpace.Id.ToString());
    }
}
