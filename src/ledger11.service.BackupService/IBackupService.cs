namespace ledger11.service.BackupService;

public interface IBackupService
{
    void Notify(string dbFilePath);
}
