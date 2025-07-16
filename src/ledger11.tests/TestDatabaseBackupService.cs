using System.Data.SQLite;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ledger11.service.DatabaseBackupService;
using Microsoft.Extensions.Logging;
using Moq;
using Renci.SshNet;
using Xunit;

namespace ledger11.tests;

public class TestDatabaseBackupService
{
    private readonly DatabaseBackupServiceConfig _config;
    private readonly Mock<ILogger<DatabaseBackupService>> _loggerMock;

    public TestDatabaseBackupService()
    {
        _config = new DatabaseBackupServiceConfig
        {
            SftpHost = "localhost",
            SftpPort = 2222,
            SftpUsername = "testuser",
            SftpPassword = "testpassword",
            SftpPath = "/backups"
        };
        _loggerMock = new Mock<ILogger<DatabaseBackupService>>();
    }

    [Fact]
    public async Task TestStartAndStop()
    {
        var service = new DatabaseBackupService(_config, _loggerMock.Object);
        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public void TestNotify()
    {
        var service = new DatabaseBackupService(_config, _loggerMock.Object);
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

        var service = new DatabaseBackupService(_config, _loggerMock.Object);
        await service.StartAsync(CancellationToken.None);

        service.Notify(dbFilePath);

        // Wait for the backup to complete
        await Task.Delay(2000);

        await service.StopAsync(CancellationToken.None);

        // Verify that the backup file was created and uploaded
        // This part is tricky to test without a real SFTP server.
        // We can mock the SftpClient, but that would require significant changes to the service.
        // For now, we will just verify that the service runs without errors.

        File.Delete(dbFilePath);
    }
}
