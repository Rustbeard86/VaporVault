using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VaporVault.CLI;
using VaporVault.CLI.Commands;
using VaporVault.Core.SteamCmd;
using VaporVault.Core.SteamCmd.Infrastructure;
using VaporVault.Core.SteamCmd.Validation;
using VaporVault.Core.SteamCmd.Validation.Events;

await Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddTransient<App>();

        // Register commands
        services.AddTransient<ICommand, SteamCmdCommands>();

        // Core services
        services.AddSingleton<ISteamCmdLocator, SteamCmdLocator>();
        services.AddSingleton<SteamCmdDownloadOptions>();

        // Infrastructure services
        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<IPlatformService, PlatformService>();
        services.AddHttpClient();
        services.AddSingleton<IHttpDownloadService, HttpDownloadService>();

        // Validation services
        services.AddSingleton<ISteamCmdValidator, SteamCmdValidator>();
        services.AddSingleton<IValidationEventHandler>(sp =>
            new LoggingValidationEventHandler(
                sp.GetRequiredService<ILogger<SteamCmdValidator>>()
            ));
    })
    .Build()
    .Services
    .GetRequiredService<App>()
    .RunAsync(args);