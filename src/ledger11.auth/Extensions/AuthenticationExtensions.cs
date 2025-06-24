using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Authentication.Twitter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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