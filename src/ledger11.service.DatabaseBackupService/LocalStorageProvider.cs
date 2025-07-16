using System.IO;
using System.Threading.Tasks;

namespace ledger11.service.DatabaseBackupService;

public class LocalStorageProvider : IRemoteStorageProvider
{
    private readonly DatabaseBackupServiceConfig _config;

    public LocalStorageProvider(DatabaseBackupServiceConfig config)
    {
        _config = config;
    }

    public async Task StoreAsync(Stream backupStream, string destinationFileName)
    {
        if (string.IsNullOrWhiteSpace(_config.SftpPath)) return;

        var destinationPath = Path.Combine(_config.SftpPath, destinationFileName);
        var path = Path.GetDirectoryName(destinationPath);
        if (path == null) return;
        
        Directory.CreateDirectory(path);
        using (var fileStream = new FileStream(destinationPath, FileMode.Create))
        {
            await backupStream.CopyToAsync(fileStream);
        }
    }
}
