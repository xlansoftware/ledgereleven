using System.IO;
using System.Threading.Tasks;
using Renci.SshNet;

namespace ledger11.service.DatabaseBackupService;

/// <summary>
/// An implementation of IRemoteStorageProvider that uploads backups to an SFTP server.
/// </summary>
public class SftpStorageProvider : IRemoteStorageProvider
{
    private readonly DatabaseBackupServiceConfig _config;

    public SftpStorageProvider(DatabaseBackupServiceConfig config)
    {
        _config = config;
    }

    public Task StoreAsync(Stream backupStream, string destinationFileName)
    {
        using (var sftp = new SftpClient(_config.SftpHost, _config.SftpPort, _config.SftpUsername, _config.SftpPassword))
        {
            sftp.Connect();
            sftp.UploadFile(backupStream, Path.Combine(_config.SftpPath, destinationFileName));
            sftp.Disconnect();
        }
        return Task.CompletedTask;
    }
}
