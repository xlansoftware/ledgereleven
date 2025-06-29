using System.Net;
using System.Net.Mail;
using ledger11.service.Models;
using Microsoft.AspNetCore.Identity.UI.Services;


using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ledger11.service;

public class SmtpEmailSenderService : IEmailSender
{
    private readonly SmtpConfig _config;
    private readonly ILogger<SmtpEmailSenderService> _logger;
    private const int DefaultMaxRetries = 3;

    public SmtpEmailSenderService(
        IOptions<SmtpConfig> config,
        ILogger<SmtpEmailSenderService> logger)
    {
        _config = config.Value;
        _logger = logger;

        _logger.LogInformation("SmtpEmailSender initialized for {Host}:{Port}",
            _config.Host, _config.Port);
    }

    private string mask(string str) => str[..2] + "***" + str[^2..];

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        try
        {
            _logger.LogDebug("Preparing to send email to {Email} with subject {Subject}", mask(email), subject);

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_config.FromEmail, _config.FromName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };

            mailMessage.To.Add(email);

            using var client = new SmtpClient(_config.Host, _config.Port)
            {
                EnableSsl = _config.EnableSsl,
                Credentials = new NetworkCredential(_config.UserName, _config.Password),
                Timeout = 1000 // 30 seconds timeout
            };

            await client.SendMailAsync(mailMessage);

            _logger.LogInformation("Email sent successfully to {Email}", mask(email));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}. Error: {ErrorMessage}",
                mask(email), ex.ToString());
            throw;
        }
    }
}
