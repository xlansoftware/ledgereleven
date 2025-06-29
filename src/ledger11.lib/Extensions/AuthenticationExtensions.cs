using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Authentication.Twitter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.OAuth;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;

public class AuthProviderSettings
{
    public bool Enable { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}

public class TwitterAuthSettings : AuthProviderSettings
{
    public string ConsumerAPIKey => ClientId;  // Alias for Twitter-specific naming
    public string ConsumerSecret => ClientSecret;
    public bool RetrieveUserDetails { get; set; } = true;
}

public static class AuthenticationExtensions
{
    public static AuthenticationBuilder AddMultiProviderAuthentication(
        this AuthenticationBuilder builder,
        IConfiguration config,
        ILogger? logger = null)
    {
        // Google
        var googleSettings = config.GetSection("Authentication:Google").Get<AuthProviderSettings>();
        if (googleSettings?.Enable == true && ValidateSettings(googleSettings, "Google", logger))
        {
            builder.AddGoogle(options =>
            {
                options.ClientId = googleSettings.ClientId;
                options.ClientSecret = googleSettings.ClientSecret;
                options.SignInScheme = IdentityConstants.ExternalScheme;
            });
            logger?.LogInformation("Google authentication configured");
        }

        // Facebook
        var fbSettings = config.GetSection("Authentication:Facebook").Get<AuthProviderSettings>();
        if (fbSettings?.Enable == true && ValidateSettings(fbSettings, "Facebook", logger))
        {
            builder.AddFacebook(options =>
            {
                options.ClientId = fbSettings.ClientId;
                options.ClientSecret = fbSettings.ClientSecret;
                options.SignInScheme = IdentityConstants.ExternalScheme;
            });
            logger?.LogInformation("Facebook authentication configured");
        }

        // Microsoft
        var microsoftSettings = config.GetSection("Authentication:Microsoft").Get<AuthProviderSettings>();
        if (microsoftSettings?.Enable == true && ValidateSettings(microsoftSettings, "Microsoft", logger))
        {
            builder.AddMicrosoftAccount(options =>
            {
                options.ClientId = microsoftSettings.ClientId;
                options.ClientSecret = microsoftSettings.ClientSecret;
                options.SignInScheme = IdentityConstants.ExternalScheme;

                // Uncomment the following lines if you want to debug the claims during ticket creation

                // options.Events.OnCreatingTicket = context =>
                // {
                //     var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                //         .CreateLogger("ledger11.MicrosoftAuth");

                //     foreach (var claim in context.Identity!.Claims)
                //     {
                //         logger.LogInformation("Claim: {Type} = {Value}", claim.Type, claim.Value);
                //     }

                //     return Task.CompletedTask;
                // };
            });
            logger?.LogInformation("Microsoft authentication configured");
        }

        // Twitter (uses extended settings)
        var twitterSettings = config.GetSection("Authentication:Twitter").Get<TwitterAuthSettings>();
        if (twitterSettings?.Enable == true && ValidateSettings(twitterSettings, "Twitter", logger))
        {
            builder.AddTwitter(options =>
            {
                options.ConsumerKey = twitterSettings.ConsumerAPIKey;
                options.ConsumerSecret = twitterSettings.ConsumerSecret;
                options.RetrieveUserDetails = twitterSettings.RetrieveUserDetails;
                options.SignInScheme = IdentityConstants.ExternalScheme;
            });
            logger?.LogInformation("Twitter authentication configured");
        }

        // GitHub OAuth
        var githubSettings = config.GetSection("Authentication:GitHub").Get<AuthProviderSettings>();
        if (githubSettings?.Enable == true && ValidateSettings(githubSettings, "GitHub", logger))
        {
            AddGithubProviderAuthentication(builder, githubSettings, logger);
        }

        return builder;
    }

    private static void AddGithubProviderAuthentication(
        AuthenticationBuilder builder,
        AuthProviderSettings settings,
        ILogger? logger)
    {
        builder.AddOAuth("GitHub", "GitHub", options =>
        {
            options.ClientId = settings.ClientId;
            options.ClientSecret = settings.ClientSecret;

            options.SignInScheme = IdentityConstants.ExternalScheme;

            options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
            options.TokenEndpoint = "https://github.com/login/oauth/access_token";
            options.UserInformationEndpoint = "https://api.github.com/user";

            options.CallbackPath = new PathString("/signin-github");

            options.Scope.Add("user:email"); // Request email scope

            options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
            options.ClaimActions.MapJsonKey(ClaimTypes.Name, "login");
            options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
            options.ClaimActions.MapJsonKey("urn:github:name", "name");
            options.ClaimActions.MapJsonKey("urn:github:avatar", "avatar_url");

            options.SaveTokens = true;

            options.Events = new OAuthEvents
            {
                OnCreatingTicket = async context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("ledger11.OAuth.GitHub");

                    logger.LogInformation("Fetching GitHub user profile...");

                    var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
                    request.Headers.UserAgent.ParseAdd("LedgerEleven");

                    var response = await context.Backchannel.SendAsync(
                        request, HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);
                    response.EnsureSuccessStatusCode();

                    using var user = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                    context.RunClaimActions(user.RootElement);

                    logger.LogInformation("GitHub user profile loaded. Now requesting email...");

                    var emailRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user/emails");
                    emailRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
                    emailRequest.Headers.UserAgent.ParseAdd("LedgerEleven");

                    var emailResponse = await context.Backchannel.SendAsync(
                        emailRequest, HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);
                    emailResponse.EnsureSuccessStatusCode();

                    var emailsText = await emailResponse.Content.ReadAsStringAsync();
                    logger.LogDebug("GitHub email JSON: {EmailJson}", emailsText);

                    using var emails = JsonDocument.Parse(emailsText);

                    var email = emails.RootElement
                        .EnumerateArray()
                        .FirstOrDefault(e =>
                            e.GetProperty("primary").GetBoolean() &&
                            e.GetProperty("verified").GetBoolean())
                        .GetProperty("email")
                        .GetString();

                    if (!string.IsNullOrEmpty(email))
                    {
                        context.Identity!.AddClaim(new Claim(ClaimTypes.Email, email));
                        logger.LogInformation("Primary verified email added to claims");
                    }
                    else
                    {
                        logger.LogWarning("No primary verified email found in GitHub response.");
                    }
                },

                OnRemoteFailure = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("ledger11.OAuth.GitHub");
                    logger.LogError("GitHub OAuth failure: {Message}", context.Failure?.Message);
                    return Task.CompletedTask;
                }
            };
        });
        logger?.LogInformation("GitHub authentication configured");
    }

    private static bool ValidateSettings(AuthProviderSettings settings, string providerName, ILogger? logger)
    {
        if (string.IsNullOrEmpty(settings.ClientId))
        {
            logger?.LogWarning($"Missing ClientId for {providerName} authentication");
            return false;
        }

        if (string.IsNullOrEmpty(settings.ClientSecret))
        {
            logger?.LogWarning($"Missing ClientSecret for {providerName} authentication");
            return false;
        }

        return true;
    }
}