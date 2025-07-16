using System.Collections.Concurrent;
using System.Data.SQLite;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Renci.SshNet;

namespace ledger11.service.DatabaseBackupService;

public class DatabaseBackupService : IHostedService, IDisposable, IDatabaseBackupService
{
    private readonly DatabaseBackupServiceConfig _config;
    private readonly ILogger<DatabaseBackupService> _logger;
    private readonly ConcurrentQueue<string> _queue = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public DatabaseBackupService(DatabaseBackupServiceConfig config, ILogger<DatabaseBackupService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public void Notify(string dbFilePath)
    {
        _logger.LogInformation("Queuing file for backup: {FilePath}", dbFilePath);
        _queue.Enqueue(dbFilePath);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("DatabaseBackupService is starting.");
        Task.Run(() => ProcessQueue(), _cancellationTokenSource.Token);
        return Task.CompletedTask;
    }

    private void ProcessQueue()
    {
        _logger.LogInformation("DatabaseBackupService queue processing started.");
        while (!_cancellationTokenSource.IsCancellationRequested)
        {
            if (_queue.TryDequeue(out var dbFilePath))
            {
                try
                {
                    Backup(dbFilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred during backup of {FilePath}", dbFilePath);
                }
            }
            else
            {
                Thread.Sleep(1000);
            }
        }
        _logger.LogInformation("DatabaseBackupService queue processing stopped.");
    }

    private void Backup(string dbFilePath)
    {
        _logger.LogInformation("Backing up {FilePath}", dbFilePath);
        var backupFileName = $"{Path.GetFileNameWithoutExtension(dbFilePath)}-{DateTime.UtcNow:yyyyMMddHHmmss}.db.bak";
        var backupFilePath = Path.Combine(Path.GetTempPath(), backupFileName);

        _logger.LogDebug("Creating local backup at {BackupFilePath}", backupFilePath);
        using (var source = new SQLiteConnection($"Data Source={dbFilePath}; Version=3;"))
        using (var destination = new SQLiteConnection($"Data Source={backupFilePath}; Version=3;"))
        {
            source.Open();
            destination.Open();
            source.BackupDatabase(destination, "main", "main", -1, null, 0);
        }
        _logger.LogDebug("Local backup created successfully.");

        _logger.LogDebug("Uploading {BackupFileName} to SFTP host {SftpHost}", backupFileName, _config.SftpHost);
        using (var sftp = new SftpClient(_config.SftpHost, _config.SftpPort, _config.SftpUsername, _config.SftpPassword))
        {
            sftp.Connect();
            sftp.UploadFile(File.OpenRead(backupFilePath), Path.Combine(_config.SftpPath, backupFileName));
            sftp.Disconnect();
        }
        _logger.LogInformation("Successfully uploaded backup to {SftpHost}", _config.SftpHost);

        _logger.LogDebug("Deleting local backup file {BackupFilePath}", backupFilePath);
        File.Delete(backupFilePath);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("DatabaseBackupService is stopping.");
        _cancellationTokenSource.Cancel();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _cancellationTokenSource.Dispose();
    }
}
