using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ledger11.model.Config;
using ledger11.service;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.FeatureManagement;
using ledger11.data;
using ledger11.model.Data;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.HttpOverrides;
using ledger11.service.Models;
using System.Text.Json.Serialization;

namespace ledger11.web;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.Configure<AppConfig>(builder.Configuration.GetSection("AppConfig"));

        builder.Services.AddFeatureManagement();

        builder.Services.AddDbContext<AppDbContext>((provider, options) =>
        {
            var config = provider.GetRequiredService<IOptions<AppConfig>>().Value;
            // Console.WriteLine($"2:DataPath: {config.DataPath}");
            options.UseSqlite($"Data Source={Path.Combine(config.DataPath, "appdata.db")};Pooling={config.Pooling}");
            options.EnableSensitiveDataLogging();
        });

        builder.Services
            .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                var smtpConfig = builder.Configuration.GetSection("Smtp").Get<SmtpConfig>();
                options.SignIn.RequireConfirmedEmail = smtpConfig?.Enable ?? false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders()
            .AddDefaultUI();

        // Create logger instance
        using var loggerFactory = LoggerFactory.Create(loggingBuilder =>
        {
            loggingBuilder
                .AddConsole()
                .AddDebug()
                .SetMinimumLevel(LogLevel.Information);
        });

        var logger = loggerFactory.CreateLogger("Authentication");

        builder.Services.AddAuthentication()
            .AddMultiProviderAuthentication(builder.Configuration, logger);

        builder.Services.Configure<SecurityStampValidatorOptions>(options =>
        {
            options.ValidationInterval = TimeSpan.Zero; // Always validate
        });

        builder.AddEmailsSupport();

        // Ledger Logic
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
        builder.Services.AddScoped<IUserSpaceService, UserSpaceService>();
        builder.Services.AddScoped<ICurrentLedgerService, CurrentLedgerService>();
        builder.Services.AddScoped<IBackupService, BackupService>();
        builder.Services.AddScoped<IExchangeRateService, ExchangeRateService>();

        builder.Services.AddHttpClient<IChatGptService, ChatGptService>();

        builder.AddOpenTelemetry();

        // Add services to the container.
        builder.Services.AddControllersWithViews()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });

        builder.Services.AddRazorPages();

        // Configure CORS
        if (builder.Environment.IsDevelopment())
        {
            Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("ReactApp", policy =>
                {
                    policy.WithOrigins("http://localhost:5173") // Vite dev server
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials(); // Required for cookies/auth
                });
            });
        }

        // Add HttpClient service
        builder.Services.AddHttpClient();
        builder.Services.AddMemoryCache();

        var app = builder.Build();

        app.UseDefaultForwardedHeaders();

        var appConfig = app.Services.GetRequiredService<IOptions<AppConfig>>().Value;
        Console.WriteLine($"Effective DataPath: {appConfig.DataPath}");

        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;

            var appContext = services.GetRequiredService<AppDbContext>();
            await appContext.Database.MigrateAsync();
        }

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        // app.UseHttpsRedirection();
        app.UseRouting();

        app.UseOpenTelemetryPrometheusScrapingEndpoint();

        app.UseCors("ReactApp");

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapStaticAssets();
        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                var headers = ctx.Context.Response.Headers;

                var path = ctx.File.PhysicalPath;

                if (path != null && path.Contains("index.html"))
                {
                    // No cache for index.html
                    headers.CacheControl = "no-cache, no-store, must-revalidate";
                    headers.Pragma = "no-cache";
                    headers.Expires = "-1";
                }
                else
                {
                    // Allow long cache for hashed static files (recommended)
                    headers.CacheControl = "public,max-age=31536000,immutable";
                }
            }
        });

        app.MapFallbackToFile("/app/{*path}", "app/index.html");

        app.Use(async (context, next) =>
        {
            var featureManager = context.RequestServices.GetRequiredService<IFeatureManager>();

            if (await featureManager.IsEnabledAsync("DisableRegister") &&
                context.Request.Path.Equals("/Identity/Account/Register", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.Redirect("/Home/Register");
                return;
            }

            // Console.WriteLine($"Path: {context.Request.Path}");
            await next();
        });

        app.MapRazorPages();

        app.MapGet("/", async (HttpContext context, IWebHostEnvironment env) =>
        {
            context.Response.ContentType = "text/html";
            await context.Response.SendFileAsync(Path.Combine(env.WebRootPath, "video", "index.html"));
        });

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}")
            .WithStaticAssets();

        app.Run();

    }
}