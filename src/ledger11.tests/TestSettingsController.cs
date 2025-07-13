using Microsoft.Extensions.DependencyInjection;
using ledger11.web.Controllers;
using Microsoft.AspNetCore.Mvc;
using ledger11.model.Data;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ledger11.tests;

public class TestSettingsController
{
    [Fact]
    public async Task Test_Settings_CRUD()
    {
        // Arrange
        using var serviceProvider = await TestExtesions.MockLedgerServiceProviderAsync("xuser_settings");
        var controller = ActivatorUtilities.CreateInstance<SettingsController>(serviceProvider);

        // Act & Assert: Get all settings (should be empty initially)
        var getAllResult = await controller.GetAllSettings();
        var okResult = Assert.IsType<OkObjectResult>(getAllResult);
        var settings = Assert.IsType<Dictionary<string, string?>>(okResult.Value);
        Assert.Empty(settings);

        // Act & Assert: Get a non-existent setting
        var getResult = await controller.GetSetting("test_key");
        Assert.IsType<NotFoundResult>(getResult);

        // Act & Assert: Create a new setting
        var upsertResult1 = await controller.UpsertSetting("test_key", new SettingsController.UpdateSettingRequest { Value = "test_value" });
        var okUpsert1 = Assert.IsType<OkObjectResult>(upsertResult1);
        var setting1 = Assert.IsType<Setting>(okUpsert1.Value);
        Assert.Equal("test_key", setting1.Key);
        Assert.Equal("test_value", setting1.Value);

        // Act & Assert: Get the created setting
        var getResult2 = await controller.GetSetting("test_key");
        var okResult2 = Assert.IsType<OkObjectResult>(getResult2);
        var setting2 = Assert.IsType<Setting>(okResult2.Value);
        Assert.Equal("test_key", setting2.Key);
        Assert.Equal("test_value", setting2.Value);

        // Act & Assert: Update the setting
        var upsertResult2 = await controller.UpsertSetting("test_key", new SettingsController.UpdateSettingRequest { Value = "updated_value" });
        var okUpsert2 = Assert.IsType<OkObjectResult>(upsertResult2);
        var setting3 = Assert.IsType<Setting>(okUpsert2.Value);
        Assert.Equal("updated_value", setting3.Value);

        // Act & Assert: Get all settings (should have one setting)
        var getAllResult2 = await controller.GetAllSettings();
        var okResult3 = Assert.IsType<OkObjectResult>(getAllResult2);
        var settings2 = Assert.IsType<Dictionary<string, string?>>(okResult3.Value);
        Assert.Single(settings2);
        Assert.Equal("updated_value", settings2["test_key"]);
    }
}
