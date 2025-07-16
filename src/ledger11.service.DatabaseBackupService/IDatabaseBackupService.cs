
namespace ledger11.service.DatabaseBackupService;

/// <summary>
/// Defines the interface for a service that can be notified to back up a database file.
/// </summary>
public interface IDatabaseBackupService
{
    /// <summary>
    /// Notifies the service to back up the specified database file.
    /// </summary>
    /// <param name="dbFilePath">The path to the database file to be backed up.</param>
    void Notify(string dbFilePath);
}
