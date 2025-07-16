using System.IO;
using System.Threading.Tasks;

namespace ledger11.service.DatabaseBackupService;

public interface IRemoteStorageProvider
{
    Task StoreAsync(Stream backupStream, string destinationFileName);
}
