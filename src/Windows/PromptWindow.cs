using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace JobTitles.Windows;

public class PromptWindow : Window, IDisposable
{
  private TitleId _promptTitleId = TitleService.TitleIds.None;

  private readonly Loc Loc;
  private readonly Logger Logger;
  private readonly TitleService TitleService;

  public PromptWindow(Loc loc, Logger logger, TitleService titleService) : base("JobTitlesPrompt###JobTitlesPrompt")
  {
    Loc = loc;
    Logger = logger;
    TitleService = titleService;

    Flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize;
    SizeCondition = ImGuiCond.Always;
    PositionCondition = ImGuiCond.Always;
    RespectCloseHotkey = false;
    DisableWindowSounds = true;
    ForceMainWindow = true;
  }

  public void Dispose() { }

  public void Open(TitleId titleId)
  {
    if (IsOpen && _promptTitleId == titleId)
      return;

    Logger.Debug($"Opening prompt window for titleId::{titleId} titleName::{TitleService.GetTitleName(titleId)}");
    UIGlobals.PlayChatSoundEffect(3);
    _promptTitleId = titleId;
    IsOpen = true;
    BringToFront();
  }

  public void Close()
  {
    if (!IsOpen) return;
    Logger.Debug("Closing prompt window");
    UIGlobals.PlaySoundEffect(28);
    _promptTitleId = TitleService.TitleIds.None;
    IsOpen = false;
  }

  public override void Draw()
  {
    Vector2 screenSize = ImGuiHelpers.MainViewport.WorkSize;
    Vector2 windowSize = ImGui.GetWindowSize();
    Position = (screenSize - windowSize) / 2;

    string text = string.Format(Loc.Get(Loc.Phrase.SetTitleToX), TitleService.GetTitleName(_promptTitleId));
    float textLength = ImGui.CalcTextSize(text).X;

    ImGui.TextUnformatted(text);

    Vector2 buttonSize = new Vector2(textLength / 2, 0);
    if (ImGui.Button(Loc.Get(Loc.Phrase.Yes), buttonSize))
    {
      Logger.Debug("PromptButton::Yes");
      TitleService.UpdateTitle();
      Close();
    }

    ImGui.SameLine();
    if (ImGui.Button(Loc.Get(Loc.Phrase.No), buttonSize))
    {
      Logger.Debug("PromptButton::No");
      Close();
    }
  }
}
