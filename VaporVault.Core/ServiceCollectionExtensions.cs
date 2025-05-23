using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using VaporVault.Core.SteamCmd;
using VaporVault.Core.SteamCmd.Infrastructure;

namespace VaporVault.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSteamServices(
        this IServiceCollection services,
        Action<SteamCmdDownloadOptions>? configureOptions = null)
    {
        // Configure options
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
            // Register options as singleton for direct injection
            services.TryAddSingleton(sp =>
                sp.GetRequiredService<IOptions<SteamCmdDownloadOptions>>().Value);
        }
        else
        {
            // Register default options
            services.TryAddSingleton(new SteamCmdDownloadOptions());
        }

        // Register infrastructure services
        services.AddSteamInfrastructure();

        // Register steam services
        services.AddSteamCore();

        return services;
    }

    private static IServiceCollection AddSteamInfrastructure(this IServiceCollection services)
    {
        services.TryAddSingleton<IFileSystem, SteamCmd.Infrastructure.FileSystem>();
        services.TryAddSingleton<IPlatformService, PlatformService>();
        services.TryAddSingleton<IHttpDownloadService, HttpDownloadService>();

        return services;
    }

    private static IServiceCollection AddSteamCore(this IServiceCollection services)
    {
        services.TryAddSingleton<ISteamCmdLocator, SteamCmdLocator>();

        return services;
    }
}