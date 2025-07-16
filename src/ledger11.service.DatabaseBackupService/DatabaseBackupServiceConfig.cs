namespace ledger11.service.DatabaseBackupService;

public enum StorageType
{
    Sftp,
    File
}

public class DatabaseBackupServiceConfig
{
    public StorageType StorageType { get; set; }
    public string SftpHost { get; set; } = string.Empty;
    public int SftpPort { get; set; }
    public string SftpUsername { get; set; } = string.Empty;
    public string SftpPassword { get; set; } = string.Empty;
    public string SftpPath { get; set; } = string.Empty;
}
