namespace ledger11.service.BackupService;

public class DatabaseBackupServiceConfig
{
    public string SftpHost { get; set; } = string.Empty;
    public int SftpPort { get; set; }
    public string SftpUsername { get; set; } = string.Empty;
    public string SftpPassword { get; set; } = string.Empty;
    public string SftpPath { get; set; } = string.Empty;
}
