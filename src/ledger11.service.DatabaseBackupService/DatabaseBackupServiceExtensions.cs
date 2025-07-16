using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ledger11.service.DatabaseBackupService;

public static class DatabaseBackupServiceExtensions
{
    public static IServiceCollection AddDatabaseBackupService(this IServiceCollection services, Action<DatabaseBackupServiceConfig> configure)
    {
        var config = new DatabaseBackupServiceConfig();
        configure(config);
        services.AddSingleton(config);

        switch (config.StorageType)
        {
            case StorageType.Sftp:
                services.AddSingleton<IRemoteStorageProvider, SftpStorageProvider>();
                break;
            case StorageType.File:
                services.AddSingleton<IRemoteStorageProvider, LocalStorageProvider>();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(config.StorageType), config.StorageType, null);
        }

        services.AddSingleton<IDatabaseBackupService, DatabaseBackupService>();
        services.AddHostedService(provider => (IHostedService)provider.GetRequiredService<IDatabaseBackupService>());
        return services;
    }
}
