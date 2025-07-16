using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ledger11.service.DatabaseBackupService;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ledger11.tests;

public class TestDatabaseBackupService
{
    private readonly DatabaseBackupServiceConfig _config;
    private readonly Mock<ILogger<DatabaseBackupService>> _loggerMock;
    private readonly string _backupPath;

    public TestDatabaseBackupService()
    {
        _backupPath = Path.Combine(Path.GetTempPath(), "backups");
        _config = new DatabaseBackupServiceConfig
        {
            StorageType = StorageType.File,
            SftpPath = _backupPath
        };
        _loggerMock = new Mock<ILogger<DatabaseBackupService>>();
    }

    [Fact]
    public async Task TestStartAndStop()
    {
        var storageProvider = new LocalStorageProvider(_config);
        var service = new DatabaseBackupService(storageProvider, _loggerMock.Object);
        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public void TestNotify()
    {
        var storageProvider = new LocalStorageProvider(_config);
        var service = new DatabaseBackupService(storageProvider, _loggerMock.Object);
        service.Notify("test.db");
    }

    [Fact]
    public async Task TestBackup()
    {
        // Create a dummy database file
        var dbFilePath = Path.Combine(Path.GetTempPath(), "test.db");
        using (var connection = new SQLiteConnection($"Data Source={dbFilePath}; Version=3;"))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE Test (Id INT)";
            command.ExecuteNonQuery();
        }

        var storageProvider = new LocalStorageProvider(_config);
        var service = new DatabaseBackupService(storageProvider, _loggerMock.Object);
        await service.StartAsync(CancellationToken.None);

        service.Notify(dbFilePath);

        // Wait for the backup to complete
        await Task.Delay(2000);

        await service.StopAsync(CancellationToken.None);

        // Verify that the backup file was created
        Assert.True(Directory.Exists(_backupPath));
        Assert.Single(Directory.GetFiles(_backupPath));

        // Cleanup
        File.Delete(dbFilePath);
        Directory.Delete(_backupPath, true);
    }
}
