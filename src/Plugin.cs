using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using JobTitles.Utils;
using JobTitles.Windows;

namespace JobTitles;

public sealed class Plugin : IDalamudPlugin
{
  [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
  [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
  [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
  [PluginService] internal static IClientState ClientState { get; private set; } = null!;
  [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
  [PluginService] internal static IPluginLog Log { get; private set; } = null!;
  [PluginService] internal static IChatGui Chat { get; private set; } = null!;

  public static Configuration Configuration { get; set; } = new Configuration();
  public readonly WindowSystem WindowSystem = new("JobTitles");
  public static ConfigWindow ConfigWindow { get; set; } = new ConfigWindow();
  private const string CommandName = "/jobtitles";

  public Plugin()
  {
    Configuration = Configuration.Load();
    Loc.SetLanguage(Configuration.Language);
    WindowSystem.AddWindow(ConfigWindow);
    CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
    {
      HelpMessage = "Open the JobTitles Configuration Window."
    });
    PluginInterface.UiBuilder.Draw += DrawUI;
    PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
    PluginInterface.UiBuilder.OpenMainUi += ToggleConfigUI;
    ClientState.ClassJobChanged += JobChanged;
    ClientState.EnterPvP += TitleUtils.OnEnterPvP;
  }

  public void Dispose()
  {
    WindowSystem.RemoveAllWindows();
    CommandManager.RemoveHandler(CommandName);
    ClientState.ClassJobChanged -= JobChanged;
    ClientState.EnterPvP -= TitleUtils.OnEnterPvP;
  }

  private void DrawUI() => WindowSystem.Draw();
  private void ToggleConfigUI() => ConfigWindow.Toggle();
  private void OnCommand(string command, string args) => ToggleConfigUI();
  private void JobChanged(uint jobId) => TitleUtils.SetTitle(jobId);
}
