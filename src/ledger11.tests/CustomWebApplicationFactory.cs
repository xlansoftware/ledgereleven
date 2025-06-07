using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using ledger11.model.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;

public class CustomWebApplicationFactory : WebApplicationFactory<ledger11.web.Program>
{
    private TempPath? _tempPath;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");

        _tempPath = new TempPath();

        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            var config = new Dictionary<string, string?>
            {
                ["Logging:LogLevel:Default"] = "Warning",
                ["Logging:LogLevel:Microsoft.AspNetCore"] = "Warning",
                ["AppConfig:DataPath"] = _tempPath.Path,
                ["AppConfig:Pooling"] = "false"
            };

            configBuilder.AddInMemoryCollection(config);
        });

        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IAntiforgery, NoOpAntiforgery>();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        // Clean up TempPath when the factory is disposed
        if (disposing)
        {
            _tempPath?.Dispose();
        }
    }
}

public class NoOpAntiforgery : IAntiforgery
{
    public AntiforgeryTokenSet GetAndStoreTokens(HttpContext context)
    {
        return new AntiforgeryTokenSet("__dummy__", "__dummy__", "__dummy__", "__dummy__");
    }

    public AntiforgeryTokenSet GetTokens(HttpContext context)
    {
        return new AntiforgeryTokenSet("__dummy__", "__dummy__", "__dummy__", "__dummy__");
    }

    public Task<bool> IsRequestValidAsync(HttpContext context)
    {
        return Task.FromResult(true);
    }

    public void SetCookieTokenAndHeader(HttpContext httpContext)
    {
        // throw new NotImplementedException();
    }

    public Task ValidateRequestAsync(HttpContext context)
    {
        return Task.CompletedTask;
    }
}