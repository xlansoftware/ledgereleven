using System.Security.Cryptography;
using ledger11.auth.Data;
using ledger11.auth.Models;
using ledger11.auth.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

public static class Extensions
{
    public static IServiceCollection AddAuthSupport(this IServiceCollection services, IConfiguration configuration)
    {
        var key = services.AddSecurityKey(configuration);
        
        services.AddSingleton<SecurityKey>(key);

        var authority = configuration["Issuer"];
        var requireHttpsMetadata = bool.TryParse(configuration["RequireHttpsMetadata"], out var https) && https;
        var clientId = configuration["Client:ClientId"] ?? Guid.NewGuid().ToString();

        services.AddAuthentication("Cookies")
            .AddCookie("Cookies")
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.RequireHttpsMetadata = requireHttpsMetadata;
                options.Authority = authority;
                options.Audience = clientId;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    IssuerSigningKey = key,
                    ValidateAudience = true,
                    ValidateIssuer = true,
                    ValidIssuer = authority,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true
                };
            });

        return services;
    }

    public static IServiceCollection AddEmailsSupport(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SmtpConfig>()
            .BindConfiguration("Smtp")
            .ValidateDataAnnotations()
            .Validate(config =>
            {
                if (string.IsNullOrEmpty(config.Host))
                    return false;
                // Add other validation rules
                return true;
            });

        services.Configure<EmailFeatureFlags>(configuration.GetSection("EmailFeatureFlags"));

        // Register the service with both interfaces
        services.AddSingleton<SmtpEmailSender>();

        // Register interfaces
        services.AddSingleton<IEmailSender>(provider => provider.GetRequiredService<SmtpEmailSender>());
        services.AddSingleton<IEmailTester>(provider => provider.GetRequiredService<SmtpEmailSender>());
        return services;
    }

    public static IServiceCollection AddAccountsSupport(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(connectionString));

        services.Configure<IdentityOptions>(options =>
        {
            options.SignIn.RequireConfirmedAccount = true;

            // Password settings
            options.Password.RequireDigit = false;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;

            // Lockout settings
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
            options.Lockout.MaxFailedAccessAttempts = 5;

            // User settings
            options.User.RequireUniqueEmail = true;
        });

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        return services;
    }
}