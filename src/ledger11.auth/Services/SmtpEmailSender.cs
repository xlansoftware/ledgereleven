using System.Net;
using System.Net.Mail;
using ledger11.auth.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;

namespace ledger11.auth.Services;

public interface IEmailTester
{
    /// <summary>
    /// Gets all captured emails in test mode
    /// </summary>
    IReadOnlyList<CapturedEmail> GetCapturedEmails();

    /// <summary>
    /// Clears all captured emails
    /// </summary>
    void ClearCapturedEmails();

    public bool IsTestModeEnabled { get; }
}

public class SmtpEmailSender : IEmailSender, IEmailTester, IDisposable
{
    private readonly SmtpClient? _client;
    private readonly SmtpConfig _config;
    private readonly ILogger<SmtpEmailSender> _logger;
    private readonly AsyncCircuitBreakerPolicy? _circuitBreaker;
    private const int DefaultMaxRetries = 3;

    // Test mode fields
    private readonly bool _testModeEnabled;
    private readonly List<CapturedEmail> _capturedEmails = new();
    private readonly object _capturedEmailsLock = new();

    public SmtpEmailSender(
        IOptions<SmtpConfig> config,
        ILogger<SmtpEmailSender> logger,
        IOptions<EmailFeatureFlags> featureFlags)
    {
        _config = config.Value;
        _logger = logger;
        _testModeEnabled = featureFlags?.Value?.EnableEmailTestMode ?? false;

        if (!_testModeEnabled)
        {
            _client = new SmtpClient(_config.Host, _config.Port)
            {
                EnableSsl = _config.EnableSsl,
                Credentials = new NetworkCredential(_config.UserName, _config.Password),
                Timeout = 1000 // 30 seconds timeout
            };

            // Configure Circuit Breaker only in production mode
            _circuitBreaker = Policy.Handle<SmtpException>()
                .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking: _config.CircuitBreakThreshold,
                    durationOfBreak: TimeSpan.FromMinutes(_config.CircuitBreakDurationMinutes),
                    onBreak: (ex, breakDelay) =>
                    {
                        _logger.LogWarning(ex, "Circuit broken! Will not attempt for {BreakDelay}ms", breakDelay.TotalMilliseconds);
                    },
                    onReset: () =>
                    {
                        _logger.LogInformation("Circuit reset - requests will flow normally");
                    },
                    onHalfOpen: () =>
                    {
                        _logger.LogInformation("Circuit half-open - testing connection");
                    });
        }

        _logger.LogInformation("SmtpEmailSender initialized for {Host}:{Port} (Test Mode: {TestMode})",
            _config.Host, _config.Port, _testModeEnabled);
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        try
        {
            _logger.LogDebug("Preparing to send email to {Email} with subject {Subject}", email, subject);

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_config.FromEmail, _config.FromName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };

            mailMessage.To.Add(email);

            if (_testModeEnabled)
            {
                CaptureEmail(mailMessage);
                _logger.LogInformation("Email captured in test mode (not actually sent) to {Email}", email);
                return;
            }

            // Combine circuit breaker with retry policy
            var policy = Policy.WrapAsync(
                GetRetryPolicy(),
                _circuitBreaker);

            await policy.ExecuteAsync(async () =>
            {
                await _client!.SendMailAsync(mailMessage);
            });

            _logger.LogInformation("Email sent successfully to {Email}", email);
        }
        catch (BrokenCircuitException bce)
        {
            _logger.LogError(bce, "Failed to send email to {Email} - Circuit is open", email);
            throw new Exception("Email service is temporarily unavailable. Please try again later.", bce);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}. Error: {ErrorMessage}",
                email, ex.ToString());
            throw; // Re-throw to let the caller handle it
        }
    }

    private void CaptureEmail(MailMessage mailMessage)
    {
        var capturedEmail = new CapturedEmail(
            To: mailMessage.To.Select(x => x.Address).ToList(),
            From: mailMessage.From?.Address ?? string.Empty,
            Subject: mailMessage.Subject,
            Body: mailMessage.Body,
            IsBodyHtml: mailMessage.IsBodyHtml,
            SentAt: DateTime.UtcNow);

        lock (_capturedEmailsLock)
        {
            _capturedEmails.Add(capturedEmail);
        }
    }

    private AsyncPolicy GetRetryPolicy()
    {
        return Policy
            .Handle<SmtpException>()
            .WaitAndRetryAsync(
                retryCount: _config.MaxRetries > 0 ? _config.MaxRetries : DefaultMaxRetries,
                sleepDurationProvider: attempt =>
                {
                    var delay = CalculateExponentialBackoff(attempt);
                    return TimeSpan.FromMilliseconds(delay);
                },
                onRetry: (exception, delay, attempt, context) =>
                {
                    _logger.LogWarning(exception,
                        "Attempt {Attempt} failed. Retrying in {Delay}ms...",
                        attempt, delay.TotalMilliseconds);
                });
    }

    private int CalculateExponentialBackoff(int attempt)
    {
        // Exponential backoff with jitter to avoid thundering herd problem
        var jitter = new Random().Next(500); // Add up to 0.5s jitter
        return (int)(Math.Pow(2, attempt) * _config.BaseRetryDelayMs) + jitter;
    }

    public void Dispose()
    {
        if (!_testModeEnabled)
        {
            _client?.Dispose();
        }
        GC.SuppressFinalize(this);
    }

    public string GetCircuitState() => _testModeEnabled
        ? "Test mode - no circuit breaker"
        : _circuitBreaker!.CircuitState switch
        {
            CircuitState.Closed => "Normal operation",
            CircuitState.Open => "Service unavailable",
            CircuitState.HalfOpen => "Testing connection",
            CircuitState.Isolated => "Manually isolated",
            _ => "Unknown state"
        };

    // IEmailTester implementation
    public IReadOnlyList<CapturedEmail> GetCapturedEmails()
    {
        lock (_capturedEmailsLock)
        {
            return _capturedEmails.ToList().AsReadOnly();
        }
    }

    public void ClearCapturedEmails()
    {
        lock (_capturedEmailsLock)
        {
            _capturedEmails.Clear();
        }
    }

    // Feature flag accessor
    public bool IsTestModeEnabled => _testModeEnabled;
}

public record CapturedEmail(
    List<string> To,
    string From,
    string Subject,
    string Body,
    bool IsBodyHtml,
    DateTime SentAt);

public class EmailFeatureFlags
{
    public bool EnableEmailTestMode { get; set; } = false;
}