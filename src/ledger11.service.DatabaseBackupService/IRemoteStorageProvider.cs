using System.IO;
using System.Threading.Tasks;

namespace ledger11.service.DatabaseBackupService;

/// <summary>
/// Defines the interface for a remote storage provider that can store a backup stream.
/// </summary>
public interface IRemoteStorageProvider
{
    /// <summary>
    /// Stores the provided backup stream to a remote location.
    /// </summary>
    /// <param name="backupStream">The stream containing the backup data.</param>
    /// <param name="destinationFileName">The name of the destination file.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StoreAsync(Stream backupStream, string destinationFileName);
}
