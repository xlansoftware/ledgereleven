namespace ledger11.model.Config;

/// <summary>
/// Provides centralized, compile-time safe constants for ledger setting keys.
/// Using these constants prevents the use of "magic strings" throughout the application.
/// </summary>
public static class LedgerSettings
{
    
    /// <summary>
    /// A boolean setting ("true" or "false") indicating whether the ledger is closed to new entries.
    /// </summary>
    public const string IsClosed = "IsClosed";

    /// <summary>
    /// A string setting containing the UTC date (ISO 8601 format) when the ledger was closed.
    /// </summary>
    public const string ClosingDate = "ClosingDate";

    /// <summary>
    /// A string setting containing the ID of the ledger to which the closing balance was transferred.
    /// </summary>
    public const string ClosingBalanceTransferLedger = "ClosingBalanceTransferLedger";

    /// <summary>
    /// A string setting that specifies the default language for the ledger (e.g., "English", "Bulgarian").
    /// This is used for operations like receipt translation. Defaults to "English" if not set.
    /// </summary>
    public const string Language = "Language";

    /// <summary>
    /// A string setting for the ISO 4217 currency code (e.g., "USD", "EUR") that the ledger uses for reporting.
    /// Defaults to "USD" if not set.
    /// </summary>
    public const string Currency = "Currency";

    /// <summary>
    /// A string setting for an ARGB color (e.g., "#FF00FF00") used for UI elements related to the ledger.
    /// </summary>
    public const string Tint = "Tint";
}
