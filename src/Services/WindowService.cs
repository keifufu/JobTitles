namespace JobTitles.Services;

public interface IWindowService : IHostedService;

public class WindowService(
  ILogger _logger,
  ConfigWindow _configWindow,
  PromptWindow _promptWindow,
  WindowSystem _windowSystem,
  IDalamudPluginInterface _pluginInterface) : IWindowService
{
  public Task StartAsync(CancellationToken cancellationToken)
  {
    _windowSystem.AddWindow(_configWindow);
    _windowSystem.AddWindow(_promptWindow);

    _pluginInterface.UiBuilder.Draw += UiBuilderOnDraw;
    _pluginInterface.UiBuilder.OpenMainUi += ToggleUi;
    _pluginInterface.UiBuilder.OpenConfigUi += ToggleUi;

    return _logger.ServiceLifecycle();
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    _pluginInterface.UiBuilder.OpenConfigUi -= ToggleUi;
    _pluginInterface.UiBuilder.OpenMainUi -= ToggleUi;
    _pluginInterface.UiBuilder.Draw -= UiBuilderOnDraw;

    _windowSystem.RemoveAllWindows();

    return _logger.ServiceLifecycle();
  }

  private void ToggleUi() => _configWindow.Toggle();

  private void UiBuilderOnDraw() => _windowSystem.Draw();
}
