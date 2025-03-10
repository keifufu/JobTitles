namespace JobTitles.Services;

public class WindowService : IHostedService
{
  private readonly Logger Logger;
  private readonly IDalamudPluginInterface PluginInterface;
  private readonly WindowSystem WindowSystem;
  private readonly ConfigWindow ConfigWindow;
  private readonly PromptWindow PromptWindow;

  public WindowService(Logger logger, IDalamudPluginInterface pluginInterface, WindowSystem windowSystem, ConfigWindow configWindow, PromptWindow promptWindow)
  {
    Logger = logger;
    PluginInterface = pluginInterface;
    WindowSystem = windowSystem;
    ConfigWindow = configWindow;
    PromptWindow = promptWindow;
  }

  public Task StartAsync(CancellationToken cancellationToken)
  {
    WindowSystem.AddWindow(ConfigWindow);
    WindowSystem.AddWindow(PromptWindow);

    PluginInterface.UiBuilder.Draw += UiBuilderOnDraw;
    PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;

    Logger.Debug("WindowService started");
    return Task.CompletedTask;
  }

  private void ToggleConfigUi()
  {
    ConfigWindow.Toggle();
  }

  private void UiBuilderOnDraw()
  {
    WindowSystem.Draw();
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
    PluginInterface.UiBuilder.Draw -= UiBuilderOnDraw;

    WindowSystem.RemoveAllWindows();

    Logger.Debug("WindowService stopped");
    return Task.CompletedTask;
  }
}
