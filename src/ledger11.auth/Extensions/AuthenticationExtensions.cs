using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Authentication.Twitter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace ledger11.auth.Extensions;

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
        if (googleSettings?.Enable == true && ValidateSettings(googleSettings, "Google"))
        {
            builder.AddGoogle(options =>
            {
                options.ClientId = googleSettings.ClientId;
                options.ClientSecret = googleSettings.ClientSecret;
            });
            logger?.LogInformation("Google authentication configured");
        }

        // Facebook
        var fbSettings = config.GetSection("Authentication:Facebook").Get<AuthProviderSettings>();
        if (fbSettings?.Enable == true && ValidateSettings(fbSettings, "Facebook"))
        {
            builder.AddFacebook(options =>
            {
                options.ClientId = fbSettings.ClientId;
                options.ClientSecret = fbSettings.ClientSecret;
            });
            logger?.LogInformation("Facebook authentication configured");
        }

        // Microsoft
        var microsoftSettings = config.GetSection("Authentication:Microsoft").Get<AuthProviderSettings>();
        if (microsoftSettings?.Enable == true && ValidateSettings(microsoftSettings, "Microsoft"))
        {
            builder.AddMicrosoftAccount(options =>
            {
                options.ClientId = microsoftSettings.ClientId;
                options.ClientSecret = microsoftSettings.ClientSecret;
            });
            logger?.LogInformation("Microsoft authentication configured");
        }

        // Twitter (uses extended settings)
        var twitterSettings = config.GetSection("Authentication:Twitter").Get<TwitterAuthSettings>();
        if (twitterSettings?.Enable == true && ValidateSettings(twitterSettings, "Twitter"))
        {
            builder.AddTwitter(options =>
            {
                options.ConsumerKey = twitterSettings.ConsumerAPIKey;
                options.ConsumerSecret = twitterSettings.ConsumerSecret;
                options.RetrieveUserDetails = twitterSettings.RetrieveUserDetails;
            });
            logger?.LogInformation("Twitter authentication configured");
        }

        // GitHub OAuth
        var githubSettings = config.GetSection("Authentication:GitHub").Get<AuthProviderSettings>();
        if (githubSettings?.Enable == true && ValidateSettings(githubSettings, "GitHub"))
        {
            builder.AddOAuth("GitHub", options =>
            {
                options.ClientId = githubSettings.ClientId;
                options.ClientSecret = githubSettings.ClientSecret;

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
            });
            logger?.LogInformation("GitHub authentication configured");
        }
        return builder;

        bool ValidateSettings(AuthProviderSettings settings, string providerName)
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
}