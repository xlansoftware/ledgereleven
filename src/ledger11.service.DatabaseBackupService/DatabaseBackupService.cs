using System.Collections.Concurrent;
using System.Data.SQLite;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ledger11.service.DatabaseBackupService;

/// <summary>
/// A background service that monitors a queue of database files to be backed up.
/// When a file is added to the queue, the service creates a local backup,
/// then uploads it to a remote storage provider.
/// </summary>
public class DatabaseBackupService : IHostedService, IDisposable, IDatabaseBackupService
{
    private readonly IRemoteStorageProvider _storageProvider;
    private readonly ILogger<DatabaseBackupService> _logger;
    private readonly ConcurrentQueue<string> _queue = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public DatabaseBackupService(IRemoteStorageProvider storageProvider, ILogger<DatabaseBackupService> logger)
    {
        _storageProvider = storageProvider;
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

    private async Task ProcessQueue()
    {
        _logger.LogInformation("DatabaseBackupService queue processing started.");
        while (!_cancellationTokenSource.IsCancellationRequested)
        {
            if (_queue.TryDequeue(out var dbFilePath))
            {
                try
                {
                    await Backup(dbFilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred during backup of {FilePath}", dbFilePath);
                }
            }
            else
            {
                await Task.Delay(1000, _cancellationTokenSource.Token);
            }
        }
        _logger.LogInformation("DatabaseBackupService queue processing stopped.");
    }

    private async Task Backup(string dbFilePath)
    {
        _logger.LogInformation("Backing up {FilePath}", dbFilePath);
        var backupFileName = $"{Path.GetFileNameWithoutExtension(dbFilePath)}-{DateTime.UtcNow:yyyyMMddHHmmss}.db.bak";
        var backupFilePath = Path.Combine(Path.GetTempPath(), backupFileName);

        _logger.LogDebug("Creating local backup at {BackupFilePath}", backupFilePath);
        using (var source = new SQLiteConnection($"Data Source={dbFilePath}; Version=3;"))
        using (var destination = new SQLiteConnection($"Data Source={backupFilePath}; Version=3;"))
        {
            await source.OpenAsync(_cancellationTokenSource.Token);
            if (_cancellationTokenSource.IsCancellationRequested) return;

            await destination.OpenAsync(_cancellationTokenSource.Token);
            if (_cancellationTokenSource.IsCancellationRequested) return;
            source.BackupDatabase(destination, "main", "main", -1, null, 0);
        }
        _logger.LogDebug("Local backup created successfully.");

        _logger.LogDebug("Uploading {BackupFileName}", backupFileName);
        using (var backupStream = File.OpenRead(backupFilePath))
        {
            await _storageProvider.StoreAsync(backupStream, backupFileName);
        }
        _logger.LogInformation("Successfully uploaded backup.");

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
