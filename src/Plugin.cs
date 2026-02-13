using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JobTitles;

public sealed class Plugin : IDalamudPlugin
{
  private readonly IHost _host;

  public Plugin(
    IDalamudPluginInterface pluginInterface,
    IChatGui chatGui,
    IClientState clientState,
    ICommandManager commandManager,
    IDataManager dataManager,
    IFramework framework,
    IGameInteropProvider interopProvider,
    IPluginLog pluginLog,
    IObjectTable objectTable,
    IPlayerState playerState,
    ITextureProvider textureProvider,
    IToastGui toastGui)
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
        collection.AddSingleton(pluginInterface);
        collection.AddSingleton(chatGui);
        collection.AddSingleton(clientState);
        collection.AddSingleton(commandManager);
        collection.AddSingleton(dataManager);
        collection.AddSingleton(framework);
        collection.AddSingleton(interopProvider);
        collection.AddSingleton(pluginLog);
        collection.AddSingleton(objectTable);
        collection.AddSingleton(playerState);
        collection.AddSingleton(textureProvider);
        collection.AddSingleton(toastGui);

        collection.AddSingleton<WindowService>();
        collection.AddSingleton<CommandService>();
        collection.AddSingleton<ConfigWindow>();
        collection.AddSingleton<PromptWindow>();

        collection.AddSingleton<Loc>();
        collection.AddSingleton<Logger>();
        collection.AddSingleton<JobService>();
        collection.AddSingleton<TitleService>();
        collection.AddSingleton<EventService>();
        collection.AddSingleton<InteropService>();

        collection.AddSingleton(InitializeConfiguration);
        collection.AddSingleton(new WindowSystem(pluginInterface.InternalName));

        collection.AddHostedService<WindowService>();
        collection.AddHostedService<CommandService>();
        collection.AddHostedService<EventService>();
        collection.AddHostedService<InteropService>();
      }).Build();

    _host.StartAsync();
  }

  private Configuration InitializeConfiguration(IServiceProvider s)
  {
    Logger logger = s.GetRequiredService<Logger>();
    IDalamudPluginInterface pluginInterface = s.GetRequiredService<IDalamudPluginInterface>();
    IPlayerState playerState = s.GetRequiredService<IPlayerState>();
    Configuration configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
    configuration.Initialize(logger, pluginInterface, playerState);
    return configuration;
  }

  public void Dispose()
  {
    _host.StopAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    _host.Dispose();
  }
}
