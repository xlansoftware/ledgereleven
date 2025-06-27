namespace ledger11.service.Models;

public class SmtpConfig
{
    public bool Enable { get; set; } = false;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;

    /// <summary>
    /// Threshold for circuit breaker.
    /// If the number of consecutive failures exceeds this value, the circuit will open.
    /// The circuit will remain open for the specified duration before allowing attempts again.
    /// </summary>
    public int CircuitBreakThreshold { get; set; } = 3;

    /// <summary>
    /// Duration in minutes for which the circuit will remain open after it has been tripped.
    /// </summary>
    public int CircuitBreakDurationMinutes { get; set; } = 1;

    /// <summary>
    /// Maximum number of retries for sending an email before giving up.
    /// If the email fails to send after this many attempts, an exception will be thrown.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Base delay in milliseconds between retries.
    /// This delay will be used in conjunction with an exponential backoff strategy.
    /// The actual delay will increase exponentially with each retry attempt.
    /// </summary>
    public int BaseRetryDelayMs { get; set; } = 1000;
}