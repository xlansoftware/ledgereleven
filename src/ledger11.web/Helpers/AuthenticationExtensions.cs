using ledger11.model.Data;
using ledger11.service;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

public class OpenIdConnectSettings
{
    [Required]
    public string Authority { get; set; } = string.Empty;

    [Required]
    public string ClientId { get; set; } = string.Empty;

    public string? ClientSecret { get; set; }

    [Required]
    public string ResponseType { get; set; } = "code";

    [Required]
    public string Scope { get; set; } = "openid profile email";

    [Required]
    public string PostLogoutRedirectUri { get; set; } = "/";

    [Required]
    public string SignedOutCallbackPath { get; set; } = "/signedout-callback";
}

public static class AuthenticationExtensions
{
    public static IServiceCollection AddAuthentication(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        var config = builder.Configuration;
        var env = builder.Environment;

        var logger = services.BuildServiceProvider().GetRequiredService<ILoggerFactory>().CreateLogger("ledger11.Authentication");

        var section = config.GetSection("Authentication:oidc");
        var settings = section.Get<OpenIdConnectSettings>();

        if (settings == null || string.IsNullOrWhiteSpace(settings.Authority) || string.IsNullOrWhiteSpace(settings.ClientId))
        {
            logger.LogInformation("OpenIdConnect settings are missing. Falling back to SingleUser authentication.");

            // Fall back to single user mode
            services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = "SingleUser";
                    options.DefaultAuthenticateScheme = "SingleUser";
                    options.DefaultChallengeScheme = "SingleUser";
                })
                .AddScheme<AuthenticationSchemeOptions, SingleUserAuthHandler>("SingleUser", _ => { });

            return services;
        }

        // Validate settings
        var context = new ValidationContext(settings);
        Validator.ValidateObject(settings, context, validateAllProperties: true);

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = "Cookies";
            options.DefaultChallengeScheme = "oidc";
        })
        .AddCookie("Cookies", options =>
        {
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.HttpOnly = true;
            options.SlidingExpiration = true;
        })
        .AddOpenIdConnect("oidc", options =>
        {
            options.Authority = settings.Authority;
            options.ClientId = settings.ClientId;
            options.ClientSecret = settings.ClientSecret;
            options.ResponseType = settings.ResponseType;
            options.SaveTokens = true;
            options.UseTokenLifetime = false;
            options.RequireHttpsMetadata = !env.IsDevelopment();
            options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

            options.Scope.Clear();
            foreach (var scope in (settings.Scope ?? "openid profile email").Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                options.Scope.Add(scope);
            }

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = settings.Authority,
                ValidateAudience = true,
                ValidAudience = settings.ClientId,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true
            };

            options.SignedOutCallbackPath = settings.SignedOutCallbackPath;
            options.SignedOutRedirectUri = settings.PostLogoutRedirectUri;

            options.Events.OnRedirectToIdentityProviderForSignOut = context =>
            {
                var idToken = context.Properties.GetTokenValue("id_token");
                context.ProtocolMessage.IdTokenHint = idToken;
                context.ProtocolMessage.PostLogoutRedirectUri = context.Properties.RedirectUri;
                context.ProtocolMessage.State = Guid.NewGuid().ToString();
                return Task.CompletedTask;
            };

            options.Events.OnTokenValidated = async context =>
            {
                var currentUserService = context.HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();
                var signInManager = context.HttpContext.RequestServices.GetRequiredService<SignInManager<ApplicationUser>>();

                var claimsPrincipal = context.Principal;
                if (claimsPrincipal == null)
                {
                    throw new Exception("Token contains no principal...");
                }

                var user = await currentUserService.EnsureUser(claimsPrincipal);
                await signInManager.SignInAsync(user, isPersistent: true);

                logger.LogInformation($"Signed in user {user.Email} with scheme {context.Scheme.Name}");
            };
        });

        return services;
    }
}