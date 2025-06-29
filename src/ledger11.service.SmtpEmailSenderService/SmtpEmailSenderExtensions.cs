using ledger11.service.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ledger11.service;


public static class SmtpEmailSenderExtensions
{
    public static IServiceCollection AddEmailsSupport(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        var logger = services
            .BuildServiceProvider()
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("ledger11.SmtpEmailSender");

        var smtpSettings = configuration.GetSection("Smtp").Get<SmtpConfig>();
        if (!(smtpSettings?.Enable ?? false))
        {
            // the default is NopEmailSender which does nothing
            logger.LogInformation("Emails are disabled");
            return services;
        }

        logger.LogInformation("Emails are enabled for {Host}:{Port}", smtpSettings.Host, smtpSettings.Port);

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

        services.AddTransient<IEmailSender, SmtpEmailSenderService>();

        return services;
    }

}