using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ledger11.service.BackupService;

public static class BackupServiceExtensions
{
    public static IServiceCollection AddBackupService(this IServiceCollection services, Action<BackupServiceConfig> configure)
    {
        var config = new BackupServiceConfig();
        configure(config);
        services.AddSingleton(config);
        services.AddSingleton<IBackupService, BackupService>();
        services.AddHostedService(provider => (IHostedService)provider.GetRequiredService<IBackupService>());
        return services;
    }
}
