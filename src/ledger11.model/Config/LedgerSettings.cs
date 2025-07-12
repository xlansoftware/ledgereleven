namespace ledger11.model.Config;

/// <summary>
/// Provides centralized, compile-time safe constants for ledger setting keys.
/// Using these constants prevents the use of "magic strings" throughout the application.
/// </summary>
public static class LedgerSettings
{
    
    /// <summary>
    /// The key for the ledger's status setting, which indicates whether the ledger is open or closed.
    /// This is used to manage the state of the ledger, such as preventing further transactions when closed.
    /// </summary>
    public const string Status = "Status";

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
