using FFXIVClientStructs.FFXIV.Client.UI;

namespace JobTitles.Windows;

public class PromptWindow(
  ILoc _loc,
  ILogger _logger,
  ITitleService _titleService) : Window("###JobTitlesPrompt"), IHostedService
{
  private TitleId _promptTitleId = TitleService.TitleIds.None;

  public Task StartAsync(CancellationToken cancellationToken)
  {
    Flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize;
    SizeCondition = ImGuiCond.Always;
    PositionCondition = ImGuiCond.Always;
    RespectCloseHotkey = false;
    DisableWindowSounds = true;
    ForceMainWindow = true;
    return _logger.ServiceLifecycle();
  }

  public Task StopAsync(CancellationToken cancellationToken)
    => _logger.ServiceLifecycle();

  public void Open(TitleId titleId)
  {
    if (IsOpen && _promptTitleId == titleId) return;
    _logger.Debug($"Opening prompt window for titleId::{titleId} titleName::{_titleService.GetTitleName(titleId)}");
    UIGlobals.PlayChatSoundEffect(3);
    _promptTitleId = titleId;
    IsOpen = true;
    BringToFront();
  }

  public unsafe void Close()
  {
    if (!IsOpen) return;
    _logger.Debug("Closing prompt window");
    UIGlobals.PlaySoundEffect(28);
    _promptTitleId = TitleService.TitleIds.None;
    IsOpen = false;
  }

  public override void Draw()
  {
    Vector2 screenSize = ImGuiHelpers.MainViewport.WorkSize;
    Vector2 windowSize = ImGui.GetWindowSize();
    Position = (screenSize - windowSize) / 2.0f;

    string text = string.Format(
      CultureInfo.CurrentCulture,
      _loc.Get(Loc.Phrase.SetTitleToX),
      _titleService.GetTitleName(_promptTitleId)
    );
    float textLength = ImGui.CalcTextSize(text).X;

    ImGui.Text(text);

    Vector2 buttonSize = new(textLength / 2.0f, 0.0f);
    if (ImGui.Button(_loc.Get(Loc.Phrase.Yes), buttonSize))
    {
      _logger.Debug("PromptButton::Yes");
      _titleService.UpdateTitle();
      Close();
    }

    ImGui.SameLine();
    if (ImGui.Button(_loc.Get(Loc.Phrase.No), buttonSize))
    {
      _logger.Debug("PromptButton::No");
      Close();
    }
  }
}
