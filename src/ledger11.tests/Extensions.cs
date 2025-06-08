using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using ledger11.data;
using ledger11.model.Config;
using ledger11.model.Data;
using ledger11.service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using System.Threading.Tasks;


public static class TestExtesions
{
    public static void DumpServices(this IServiceCollection services)
    {
        foreach (var s in services.OrderBy(s => s.Lifetime))
        {
            Console.WriteLine($"[{s.Lifetime}] {s.ServiceType.Name} => {s.ImplementationType?.Name ?? "Factory/Instance"}");
        }
    }

    private static IHttpContextAccessor MockHttpContextAcessor()
    {
        var claims = new[]
        {
            new Claim("sub", "abc123"),
            new Claim("email", "abc123@example.com"),
            new Claim("name", "abc123@example.com"),
            new Claim("iss", "https://myprovider.com"),
            new Claim("auth_scheme", "Google") // if you added this
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(ctx => ctx.User).Returns(principal);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(acc => acc.HttpContext).Returns(mockHttpContext.Object);

        return mockHttpContextAccessor.Object;
    }

    public static async Task<ServiceProvider> MockLedgerServiceProviderAsync(string userName, Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        var tempPath = new TempPath();
        services.AddSingleton(provider =>
        {
            // Adding to the service provider will cause it to be disposed
            return tempPath;
        });

        services.Configure<AppConfig>((options) =>
        {
            options.DataPath = tempPath.Path;
            options.Pooling = false;
        });

        services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Warning);
        });

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlite(new SqliteConnection($"Data Source={tempPath.Path}/{userName}-appdata.db;Pooling=false;"));
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        });

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddSingleton<IHttpContextAccessor>(MockHttpContextAcessor());
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IUserSpaceService, UserSpaceService>();
        services.AddScoped<ICurrentLedgerService, CurrentLedgerService>();

        configureServices?.Invoke(services);

        // services.DumpServices();
        var serviceProvider = services.BuildServiceProvider();

        using (var scope = serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync(); // Applies pending migrations
        }

        return serviceProvider;
    }


}