namespace ledger11.service.DatabaseBackupService;

/// <summary>
/// Defines the storage type for database backups.
/// </summary>
public enum StorageType
{
    /// <summary>
    /// No backup storage is configured.
    /// </summary>
    None,
    /// <summary>
    /// Backup to an SFTP server.
    /// </summary>
    Sftp,
    /// <summary>
    /// Backup to a local file path.
    /// </summary>
    File
}

/// <summary>
/// Configuration for the DatabaseBackupService.
/// </summary>
public class DatabaseBackupServiceConfig
{
    /// <summary>
    /// The type of storage to use for backups.
    /// </summary>
    public StorageType StorageType { get; set; }
    /// <summary>
    /// The hostname or IP address of the SFTP server.
    /// </summary>
    public string SftpHost { get; set; } = string.Empty;
    /// <summary>
    /// The port number for the SFTP server.
    /// </summary>
    public int SftpPort { get; set; }
    /// <summary>
    /// The username for the SFTP server.
    /// </summary>
    public string SftpUsername { get; set; } = string.Empty;
    /// <summary>
    /// The password for the SFTP server.
    /// </summary>
    public string SftpPassword { get; set; } = string.Empty;
    /// <summary>
    /// The remote path on the SFTP server or the local path for file storage.
    /// </summary>
    public string SftpPath { get; set; } = string.Empty;
}
