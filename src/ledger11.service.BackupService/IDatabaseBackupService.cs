namespace ledger11.service.BackupService;

public interface IDatabaseBackupService
{
    void Notify(string dbFilePath);
}