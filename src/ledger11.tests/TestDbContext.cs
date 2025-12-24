using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using FluentAssertions;
using ledger11.model.Data;
using ledger11.model.Api;
using ledger11.web.Controllers;
using Microsoft.AspNetCore.Identity;
using ledger11.data;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using ledger11.service;
using System.Security.Claims;

namespace ledger11.tests;

public class TestDbContext
{
    [Fact]
    public async Task Test_UserManager()
    {
        // Arrange
        using var serviceProvider = await TestExtesions
            .MockLedgerServiceProviderAsync("xuser1");

        var appDbContext = serviceProvider.GetRequiredService<AppDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var currentUserService = serviceProvider.GetRequiredService<ICurrentUserService>();
        var userSpaceService = serviceProvider.GetRequiredService<IUserSpaceService>();

        var user = new ApplicationUser()
        {
            UserName = "User1",
            Email = "emal@koko.loco"
        };

        var result = await userManager.CreateAsync(user);
        Assert.True(result.Succeeded);

        var u2 = await userManager.FindByEmailAsync("emal@koko.loco");
        Assert.NotNull(u2);
        Assert.NotEqual(u2.Id, Guid.Empty);

        var u3 = await appDbContext.Users.FindAsync(u2.Id);
        Assert.NotNull(u3);
        Assert.Equal("emal@koko.loco", u3.Email);

        var u4 = await appDbContext.Users.FirstOrDefaultAsync((e) => e.Email == "emal@koko.loco");
        Assert.NotNull(u4);
        Assert.Equal(u2.Id, u4.Id);

        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "User1"),
            new Claim(ClaimTypes.Email, "emal@koko.loco"),
        }, "Scheme.Name"));

        var u5 = await currentUserService.EnsureUser(principal);
        Assert.NotNull(u5);
        Assert.Equal(u2.Id, u5.Id);

        var principal2 = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "User2"),
            new Claim(ClaimTypes.Email, "emal2@koko.loco"),
        }, "Scheme.Name"));

        var u6 = await currentUserService.EnsureUser(principal2);
        Assert.NotNull(u6);
        Assert.NotEqual(u2.Id, u6.Id);

        var u7 = await userManager.FindByEmailAsync("emal2@koko.loco");
        Assert.NotNull(u7);
        Assert.Equal(u7.Id, u6.Id);

    }

    [Fact]
    public async Task Test_CurrentLedgerService_UpdateDefaultCurrencyAsync()
    {
        // Arrange
        using var serviceProvider = await TestExtesions
            .MockLedgerServiceProviderAsync("xuser1");

        var appDbContext = serviceProvider.GetRequiredService<AppDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var currentUserService = serviceProvider.GetRequiredService<ICurrentUserService>();
        var userSpaceService = serviceProvider.GetRequiredService<IUserSpaceService>();

        var currentLedgerService = serviceProvider.GetRequiredService<ICurrentLedgerService>();

        var ledger = await currentLedgerService.GetLedgerDbContextAsync();
        Assert.NotNull(ledger);

        // The default currency (if not especified) is USD
        // 1. Create transaction in default currency
        // 2. Create transaction in EUR
        // 3. Create transaction in some other currency

        await currentLedgerService.UpdateDefaultCurrencyAsync("EUR", 1.95m);

        // Assert the values are converted

    }

}
