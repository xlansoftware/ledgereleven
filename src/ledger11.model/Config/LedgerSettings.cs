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
}
