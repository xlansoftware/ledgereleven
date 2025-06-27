using ledger11.service.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ledger11.service;


public static class SmtpEmailSenderExtensions
{
    public static IServiceCollection AddEmailsSupport(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

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