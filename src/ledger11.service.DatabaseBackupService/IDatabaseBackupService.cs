namespace ledger11.service.DatabaseBackupService;

public interface IDatabaseBackupService
{
    void Notify(string dbFilePath);
}