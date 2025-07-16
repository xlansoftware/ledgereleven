using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ledger11.service.BackupService;

public static class DatabaseBackupServiceExtensions
{
    public static IServiceCollection AddDatabaseBackupService(this IServiceCollection services, Action<DatabaseBackupServiceConfig> configure)
    {
        var config = new DatabaseBackupServiceConfig();
        configure(config);
        services.AddSingleton(config);
        services.AddSingleton<IDatabaseBackupService, DatabaseBackupService>();
        services.AddHostedService(provider => (IHostedService)provider.GetRequiredService<IDatabaseBackupService>());
        return services;
    }
}
