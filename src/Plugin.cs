using Microsoft.Extensions.Logging;
using ILogger = JobTitles.Services.ILogger;

namespace JobTitles;

public sealed class Plugin : IAsyncDalamudPlugin
{
  private readonly IHost _host;

  public Plugin(
    IChatGui chatGui,
    IFramework framework,
    IPluginLog pluginLog,
    IClientState clientState,
    IDataManager dataManager,
    IObjectTable objectTable,
    IPlayerState playerState,
    ICommandManager commandManager,
    ITextureProvider textureProvider,
    IGameInteropProvider interopProvider,
    IDalamudPluginInterface pluginInterface)
  {
    _host = new HostBuilder()
      .UseContentRoot(pluginInterface.ConfigDirectory.FullName)
      .ConfigureLogging(lb =>
      {
        lb.ClearProviders();
        lb.SetMinimumLevel(LogLevel.Trace);
      })
      .ConfigureServices(collection =>
      {
        collection.AddSingleton(chatGui);
        collection.AddSingleton(framework);
        collection.AddSingleton(pluginLog);
        collection.AddSingleton(clientState);
        collection.AddSingleton(dataManager);
        collection.AddSingleton(objectTable);
        collection.AddSingleton(playerState);
        collection.AddSingleton(commandManager);
        collection.AddSingleton(textureProvider);
        collection.AddSingleton(interopProvider);
        collection.AddSingleton(pluginInterface);

        collection.AddSingleton<ConfigWindow>();
        collection.AddSingleton<PromptWindow>();

        collection.AddSingleton<ILoc, Loc>();
        collection.AddSingleton<ILogger, Logger>();
        collection.AddSingleton<IJobService, JobService>();
        collection.AddSingleton<IEventService, EventService>();
        collection.AddSingleton<ITitleService, TitleService>();
        collection.AddSingleton<IWindowService, WindowService>();
        collection.AddSingleton<ICommandService, CommandService>();
        collection.AddSingleton<IInteropService, InteropService>();

        collection.AddSingleton(InitializeConfiguration);
        collection.AddSingleton(new WindowSystem(pluginInterface.InternalName));

        collection.AddHostedService(sp => sp.GetRequiredService<IEventService>());
        collection.AddHostedService(sp => sp.GetRequiredService<IWindowService>());
        collection.AddHostedService(sp => sp.GetRequiredService<ICommandService>());
        collection.AddHostedService(sp => sp.GetRequiredService<IInteropService>());
        collection.AddHostedService(sp => sp.GetRequiredService<ConfigWindow>());
        collection.AddHostedService(sp => sp.GetRequiredService<PromptWindow>());
      }).Build();
  }

  private Configuration InitializeConfiguration(IServiceProvider s)
  {
    ILogger logger = s.GetRequiredService<ILogger>();
    IDalamudPluginInterface pluginInterface = s.GetRequiredService<IDalamudPluginInterface>();
    IPlayerState playerState = s.GetRequiredService<IPlayerState>();
    Configuration configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
    configuration.Initialize(logger, pluginInterface, playerState);
    return configuration;
  }

  public async Task LoadAsync(CancellationToken cancellationToken)
    => await _host.StartAsync(cancellationToken);

  public async ValueTask DisposeAsync()
  {
    await _host.StopAsync();
    _host.Dispose();
  }
}
