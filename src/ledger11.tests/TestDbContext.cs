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
using Moq;

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
        var mockExchangeRateService = new Mock<IExchangeRateService>();
        decimal oldToNewExchangeRate = 0.92m; // USD to EUR
        decimal jpyToEurExchangeRate = 0.007m; // JPY to EUR

        // Setup mock exchange rate service to return specific rates
        mockExchangeRateService.Setup(s => s.GetExchangeRateAsync("JPY", "EUR")).ReturnsAsync(jpyToEurExchangeRate);
        mockExchangeRateService.Setup(s => s.GetExchangeRateAsync("USD", "EUR")).ReturnsAsync(oldToNewExchangeRate);

        using var serviceProvider = await TestExtesions
            .MockLedgerServiceProviderAsync("xuser1", services =>
            {
                // Override the default mock with our specific setup
                services.AddScoped<IExchangeRateService>(provider => mockExchangeRateService.Object);
            });

        var appDbContext = serviceProvider.GetRequiredService<AppDbContext>();
        var userSpaceService = serviceProvider.GetRequiredService<IUserSpaceService>();
        var currentLedgerService = serviceProvider.GetRequiredService<ICurrentLedgerService>();

        var space = await userSpaceService.GetUserSpaceAsync();
        Assert.NotNull(space);
        
        var ledgerDb = await currentLedgerService.GetLedgerDbContextAsync();
        Assert.NotNull(ledgerDb);

        var originalCurrency = "USD";
        var currencySetting = await ledgerDb.Settings
            .FirstOrDefaultAsync(s => s.Key == "Currency");
        Assert.NotNull(currencySetting); // it is EUR by default
        currencySetting.Value = originalCurrency;
        await ledgerDb.SaveChangesAsync();

        // 1. Create transaction in default currency (implicit)
        var defaultCurrencyTx = new Transaction { Value = 100, Notes = "Implicit USD" };
        // 2. Create transaction in JPY
        var jpyTx = new Transaction { Value = 10000, Currency = "JPY", Notes = "Explicit JPY" };
        // 3. Create transaction in EUR (the future default currency)
        var eurTx = new Transaction { Value = 50, Currency = "EUR", Notes = "Explicit EUR" };

        ledgerDb.Transactions.AddRange(defaultCurrencyTx, jpyTx, eurTx);
        await ledgerDb.SaveChangesAsync();

        // Act
        // The exchange rate provided is for converting the OLD default (USD) to the NEW default (EUR)
        await currentLedgerService.UpdateDefaultCurrencyAsync(space.Id, "EUR", oldToNewExchangeRate);

        // Assert
        // 1. Assert the space's default currency has changed
        var updatedSpace = await userSpaceService.GetUserSpaceAsync();
        Assert.NotNull(updatedSpace);
        var updatedCurrencySetting = await ledgerDb.Settings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == "Currency")
            ;
        Assert.NotNull(updatedCurrencySetting);
        updatedCurrencySetting.Value.Should().Be("EUR");

        // 2. Re-fetch transactions to check their updated state
        // Using AsNoTracking and then ToList to ensure fresh data and not re-use potentially cached instances
        var transactions = await ledgerDb.Transactions.AsNoTracking().ToListAsync();
        var updatedDefaultTx = transactions.FirstOrDefault(t => t.Id == defaultCurrencyTx.Id);
        var updatedJpyTx = transactions.FirstOrDefault(t => t.Id == jpyTx.Id);
        var updatedEurTx = transactions.FirstOrDefault(t => t.Id == eurTx.Id);

        // 3. Assert the implicitly-default-currency transaction is now explicit
        Assert.NotNull(updatedDefaultTx);
        updatedDefaultTx.Value.Should().Be(100);
        updatedDefaultTx.Currency.Should().Be(originalCurrency); // Explicitly set to old currency
        updatedDefaultTx.ExchangeRate.Should().Be(oldToNewExchangeRate); // Uses the rate provided in the method call

        // 4. Assert the JPY transaction has its exchange rate updated to convert JPY -> EUR
        Assert.NotNull(updatedJpyTx);
        updatedJpyTx.Value.Should().Be(10000);
        updatedJpyTx.Currency.Should().Be("JPY");
        updatedJpyTx.ExchangeRate.Should().Be(jpyToEurExchangeRate);

        // 5. Assert the EUR transaction now has a null exchange rate (since it matches the default)
        Assert.NotNull(updatedEurTx);
        updatedEurTx.Value.Should().Be(50);
        updatedEurTx.Currency.Should().Be("EUR");
        updatedEurTx.ExchangeRate.Should().BeNull(); // Rate is null when currency matches default
    }

}
