using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using JobTitles.Utils;
using Lumina.Data;
using Lumina.Excel.Sheets;
using System.IO;

namespace JobTitles.Windows;

public class ConfigWindow : Window, IDisposable
{
  private string _jobSearchTerm = string.Empty;
  private string _titleSearchTerm = string.Empty;
  private readonly Dictionary<uint, bool> _dropdownDrawState = new();
  
  public ConfigWindow(Plugin plugin) : base("JobTitles###JobTitles")
  {
    Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize;
    SizeCondition = ImGuiCond.Always;
  }

  public void Dispose()
  {
    _jobSearchTerm = string.Empty;
    _titleSearchTerm = string.Empty;
  }

  public override void Draw()
  {
    UpdateWindowTitle();

    if (Plugin.ClientState.LocalPlayer == null)
    {
      UpdateSizeContraints(0);
      DrawLoginPrompt();
      return;
    }

    if (!TitleUtils.TitleList.DataReceived)
    {
      UpdateSizeContraints(0);
      DrawTitleListRequest();
      return;
    }

    UpdateSizeContraints();
    if (DrawJobSearch()) return;

    using var tabBar = ImRaii.TabBar("###TabBar");
    if (!tabBar.Success)
    {
      Logger.Error("Failed to draw TabBar?");
      return;
    }

    DrawJobTab($"{Loc.Get(Loc.Phrase.Tanks)}###Tanks", JobUtils.Tanks);
    DrawJobTab($"{Loc.Get(Loc.Phrase.Healers)}###Healers", JobUtils.Healers);
    DrawJobTab($"{Loc.Get(Loc.Phrase.Melee)}###Melee", JobUtils.Melee);
    DrawJobTab($"{Loc.Get(Loc.Phrase.Ranged)}###Ranged", JobUtils.Ranged);
    DrawJobTab($"{Loc.Get(Loc.Phrase.Crafters)}###Crafters", JobUtils.Crafters);
    DrawJobTab($"{Loc.Get(Loc.Phrase.Gatherers)}###Gatherers", JobUtils.Gatherers);
    DrawOptionsTab();
  }

  private void UpdateSizeContraints(float? minWidth = null)
  {
    string[] tabLabels = { Loc.Get(Loc.Phrase.Tanks), Loc.Get(Loc.Phrase.Healers), Loc.Get(Loc.Phrase.Melee), Loc.Get(Loc.Phrase.Ranged), Loc.Get(Loc.Phrase.Crafters), Loc.Get(Loc.Phrase.Gatherers), Loc.Get(Loc.Phrase.Options) };

    var style = ImGui.GetStyle();
    float windowPadding = style.WindowPadding.X;
    float framePadding = style.FramePadding.X;
    float itemInnerSpacing = style.ItemInnerSpacing.X;

    float width = windowPadding * 2;

    for (int i = 0; i < tabLabels.Length; i++)
    {
      width += ImGui.CalcTextSize(tabLabels[i]).X + (framePadding * 2);

      if (i < tabLabels.Length - 1)
      {
        width += itemInnerSpacing;
      }
    }

    float totalWidth = (width / ImGuiHelpers.GlobalScale) + windowPadding;

    SizeConstraints = new WindowSizeConstraints
    {
      MinimumSize = new Vector2(minWidth ?? totalWidth, 0),
      MaximumSize = new Vector2(totalWidth, 400),
    };
  }

  private void UpdateWindowTitle()
  {
    string debugFlag = Plugin.Configuration.Debug ? " (Debug)" : string.Empty;

    var localPlayer = Plugin.ClientState.LocalPlayer;
    if (localPlayer == null)
      WindowName = $"Job Titles{debugFlag}###JobTitles";
    else
      WindowName = $"Job Titles ({localPlayer.Name}){debugFlag}###JobTitles";
  }

  private void DrawHorizontallyCenteredText(string text)
  {
    float textWidth = ImGui.CalcTextSize(text).X;
    float textX = (ImGui.GetContentRegionAvail().X - textWidth) * 0.5f;
    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + textX);
    ImGui.TextUnformatted(text);
  }

  private void DrawHorizontallyCenteredButton(string text, bool disabledCondition, System.Action onClick)
  {
    float availableWidth = ImGui.GetContentRegionAvail().X;
    float buttonWidth = ImGui.CalcTextSize(text).X + ImGui.GetStyle().FramePadding.X * 2;
    float buttonX = (availableWidth - buttonWidth) * 0.5f;
    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + buttonX);
    using (ImRaii.Disabled(disabledCondition))
      if (ImGui.Button(text) && !disabledCondition) onClick?.Invoke();
  }

  private void DrawLoginPrompt()
  {
    DrawHorizontallyCenteredText(Loc.Get(Loc.Phrase.PleaseLogIn));
  }

  private void DrawTitleListRequest()
  {
    DrawHorizontallyCenteredText(Loc.Get(Loc.Phrase.RequestTitleListDescription));
    DrawHorizontallyCenteredButton(
      Loc.Get(Loc.Phrase.RequestTitleList),
      TitleUtils.TitleList.DataPending,
      () =>
      {
        Logger.Debug("User requested the title list");
        TitleUtils.TitleList.RequestTitleList();
      }
    );
  }

  private bool DrawJobSearch()
  {
    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
    ImGui.InputTextWithHint("###JobSearch", Loc.Get(Loc.Phrase.Search), ref _jobSearchTerm, 256, ImGuiInputTextFlags.AutoSelectAll);

    if (_jobSearchTerm.Length > 0)
    {
      Func<ClassJob, bool> shouldRenderJobRow = jobRow => _jobSearchTerm.Length == 0 || jobRow.Name.ExtractText().IndexOf(_jobSearchTerm, StringComparison.OrdinalIgnoreCase) >= 0;
      if (DrawJobTitleSelectRows(shouldRenderJobRow) == 0)
      {
        DrawHorizontallyCenteredText(Loc.Get(Loc.Phrase.NoResults));
      }
      return true;
    }

    return false;
  }

  private void DrawJobTab(string name, JobUtils.Job[] jobIds)
  {
    using var tabItem = ImRaii.TabItem(name);
    if (!tabItem.Success) return;

    DrawJobTitleSelectRows(jobRow => Enum.IsDefined(typeof(JobUtils.Job), jobRow.RowId) && jobIds.Contains((JobUtils.Job)jobRow.RowId));
  }

  private void DrawOptionsTab()
  {
    using var tabItem = ImRaii.TabItem($"{Loc.Get(Loc.Phrase.Options)}###Options");
    if (!tabItem.Success) return;

    ImGui.TextUnformatted(Loc.Get(Loc.Phrase.Language));

    ImGui.SetNextItemWidth(200 * ImGuiHelpers.GlobalScale);
    using (var dropdown = ImRaii.Combo("###LanguageSelect", Loc.GetLanguageName(Plugin.Configuration.Language)))
    {
      if (dropdown)
      {
        DrawLanguageSelectable(Language.None);
        DrawLanguageSelectable(Language.English);
        DrawLanguageSelectable(Language.German);
      }
    }

    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 5);
    ImGui.TextUnformatted(Loc.Get(Loc.Phrase.ClassMode));
    ImGui.SameLine();
    uint iconId = 60407;
    if (!Plugin.TextureProvider.TryGetFromGameIcon(new GameIconLookup(iconId), out var infoIcon))
    {
      Logger.Error($"Unable to retrieve icon for id: {iconId}. Not drawing options tab.");
      return;
    }
    ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 5);
    ImGui.SetCursorPosX(ImGui.GetCursorPosX() - 10);
    ImGui.Image(infoIcon!.GetWrapOrEmpty().ImGuiHandle, new Vector2(40));
    if (ImGui.IsItemHovered())
      using (ImRaii.Tooltip())
        ImGui.TextUnformatted(Loc.Get(Loc.Phrase.ClassModeTooltip));

    if (ImGui.RadioButton(Loc.Get(Loc.Phrase.InheritJobTitles), Plugin.Configuration.ClassMode == Configuration.ClassModeOption.InheritJobTitles))
    {
      Plugin.Configuration.ClassMode = Configuration.ClassModeOption.InheritJobTitles;
      Plugin.Configuration.Save();
      TitleUtils.UpdateTitle();
    }

    if (ImGui.RadioButton(Loc.Get(Loc.Phrase.ShowClasses), Plugin.Configuration.ClassMode == Configuration.ClassModeOption.ShowClasses))
    {
      Plugin.Configuration.ClassMode = Configuration.ClassModeOption.ShowClasses;
      Plugin.Configuration.Save();
      TitleUtils.UpdateTitle();
    }

    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 10);
    var debug = Plugin.Configuration.Debug;
    if (ImGui.Checkbox(Loc.Get(Loc.Phrase.Debug), ref debug))
    {
      Plugin.Configuration.Debug = debug;
      Plugin.Configuration.Save();
    }

    ImGui.SameLine(ImGui.GetContentRegionAvail().X - (40 * ImGuiHelpers.GlobalScale));
    ImGui.SetCursorPosY(ImGui.GetCursorPosY() - (25 * ImGuiHelpers.GlobalScale));
    DrawGambler(new Vector2(50 * ImGuiHelpers.GlobalScale));
  }

  private void DrawGambler(Vector2 size)
  {
    CharacterConfig characterConfig = Configuration.GetCharacterConfig();
    if (!characterConfig.JobTitleMappings.TryGetValue((uint)JobUtils.Job.AST, out var titleId)) return;
    if (titleId != 195) return;

    var gamblerImagePath = Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "gambler.png");
    var gamblerImage = Plugin.TextureProvider.GetFromFile(gamblerImagePath).GetWrapOrDefault();
    if (gamblerImage == null) return;
    IntPtr textureID = gamblerImage.ImGuiHandle;
    ImGui.Image(gamblerImage.ImGuiHandle, size);
    if (ImGui.IsItemHovered())
      using (ImRaii.Tooltip())
        ImGui.TextUnformatted("Gambi is the Gambler");
  }

  private void DrawLanguageSelectable(Language language)
  {
    if (ImGui.Selectable(Loc.GetLanguageName(language), Plugin.Configuration.Language == language))
    {
      Loc.SetLanguage(language);
      Plugin.Configuration.Language = language;
      Plugin.Configuration.Save();
    }
  }

  private int DrawJobTitleSelectRows(Func<ClassJob, bool> shouldRenderJobRow)
  {
    int rows = 0;
    var characterConfig = Configuration.GetCharacterConfig();
    foreach (var jobId in JobUtils.OrderedJobs.Select(job => (uint)job))
    {
      if (JobUtils.IsClass(jobId) && Plugin.Configuration.ClassMode != Configuration.ClassModeOption.ShowClasses)
        continue;

      if (!Plugin.DataManager.Excel.GetSheet<ClassJob>(Loc.Language).TryGetRow(jobId, out var jobRow))
      {
        Logger.Error($"Unable to retrieve data for classjob row id: {jobId}. Not drawing title selection.");
        continue;
      }

      if (!shouldRenderJobRow(jobRow)) continue;

      // 62100 - job icons with background
      uint iconId = 62100 + jobId;
      if (!Plugin.TextureProvider.TryGetFromGameIcon(new GameIconLookup(iconId), out var jobIcon))
      {
        Logger.Error($"Unable to retrieve icon for id: {iconId}. Not drawing title selection.");
        continue;
      }

      int selectedTitleId = characterConfig.JobTitleMappings.GetValueOrDefault(jobId, TitleUtils.TitleIds.DoNotOverride);
      DrawJobTitleSelectRow(jobRow, jobIcon, jobId, selectedTitleId);
      rows++;
    }

    return rows;
  }

  private void DrawJobTitleSelectRow(ClassJob jobRow, ISharedImmediateTexture jobIcon, uint jobId, int selectedTitleId)
  {
    // Draw Job Icon with vertically centered Job Name
    var jobName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(jobRow.Name.ExtractText());
    var iconSize = new Vector2(24 * ImGuiHelpers.GlobalScale);
    var verticalOffset = (iconSize.Y - ImGui.CalcTextSize(jobName).Y) / 2.0f;
    ImGui.Image(jobIcon.GetWrapOrEmpty().ImGuiHandle, iconSize);
    ImGui.SameLine();
    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + verticalOffset);
    ImGui.Text(jobName);
    if (ImGui.IsItemHovered())
      using (ImRaii.Tooltip())
        ImGui.TextUnformatted(Loc.Get(Loc.Phrase.JobNameTooltip).Replace("%s", jobName));
    // ImGui.SetCursorPosY(ImGui.GetCursorPosY() - verticalOffset);

    // Set size and distance for the dropdown
    ImGui.SameLine(140 * ImGuiHelpers.GlobalScale);
    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);

    var selectedTitleName = TitleUtils.GetTitleName(selectedTitleId);
    using var dropdown = ImRaii.Combo($"###Title{jobRow.RowId}", selectedTitleName);

    if (dropdown)
    {
      DrawJobTitleDropdownContents(jobId, selectedTitleId);
    }
    else
    {
      _dropdownDrawState[jobId] = true;
    }
  }

  private void DrawJobTitleDropdownContents(uint jobId, int selectedTitleId)
  {
    if (_dropdownDrawState[jobId])
    {
      _titleSearchTerm = string.Empty;
      _dropdownDrawState[jobId] = false;
      ImGui.SetKeyboardFocusHere();
    }

    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - (ImGui.GetStyle().ScrollbarSize / ImGuiHelpers.GlobalScale));
    ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 2);
    ImGui.InputTextWithHint("###TitleSearch", Loc.Get(Loc.Phrase.Search), ref _titleSearchTerm, 256, ImGuiInputTextFlags.AutoSelectAll);

    DrawTitleSelectable(Loc.Get(Loc.Phrase.DoNotOverride), TitleUtils.TitleIds.DoNotOverride, jobId, selectedTitleId);
    DrawTitleSelectable(Loc.Get(Loc.Phrase.None), TitleUtils.TitleIds.None, jobId, selectedTitleId);

    foreach (var title in Plugin.DataManager.Excel.GetSheet<Title>(Loc.Language).Where(t => t.RowId != 0 && TitleUtils.IsTitleUnlocked(t.RowId)))
    {
      DrawTitleSelectable(TitleUtils.GetTitleName(title), (int)title.RowId, jobId, selectedTitleId);
    }
  }

  private void DrawTitleSelectable(string option, int titleId, uint jobId, int selectedTitleId)
  {
    if (_titleSearchTerm.Length == 0 || option.IndexOf(_titleSearchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
    {
      if (ImGui.Selectable(option, selectedTitleId == titleId))
      {
        TitleUtils.SaveTitle(jobId, titleId);
        TitleUtils.UpdateTitle();
      }
    }
  }
}
