using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;

using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Services;
using Dalamud.Configuration;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Game;
using Dalamud.IoC;

using Lumina.Excel.Sheets;
using ImGuiNET;

using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Numerics;
using System.Linq;
using System;

namespace JobTitles;

public sealed class Plugin : IDalamudPlugin
{
  [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
  [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
  [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
  [PluginService] internal static IClientState ClientState { get; private set; } = null!;
  [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
  [PluginService] internal static IPluginLog Logger { get; private set; } = null!;

  public static unsafe TitleController TitleController => UIState.Instance()->TitleController;
  public static unsafe TitleList TitleList => UIState.Instance()->TitleList;
  public static Configuration Configuration { get; set; } = new Configuration();

  public readonly WindowSystem WindowSystem = new("JobTitles");
  private ConfigWindow ConfigWindow { get; init; }
  private const string CommandName = "/jobtitles";

  public Plugin()
  {
    Configuration = Configuration.Load();
    ConfigWindow = new ConfigWindow(this);
    WindowSystem.AddWindow(ConfigWindow);
    CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
    {
      HelpMessage = "Open the JobTitles Configuration Window."
    });
    PluginInterface.UiBuilder.Draw += DrawUI;
    PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
    PluginInterface.UiBuilder.OpenMainUi += ToggleConfigUI;
    ClientState.ClassJobChanged += JobChanged;
  }

  public void Dispose()
  {
    WindowSystem.RemoveAllWindows();
    ConfigWindow.Dispose();
    CommandManager.RemoveHandler(CommandName);
    ClientState.ClassJobChanged -= JobChanged;
  }

  private void JobChanged(uint jobId)
  {
    CharacterConfig config = GetCharacterConfig();
    if (config.JobTitleMappings.TryGetValue(jobId, out int titleId))
      SetTitle(titleId);
  }

  private void DrawUI() => WindowSystem.Draw();
  private void ToggleConfigUI() => ConfigWindow.Toggle();
  private void OnCommand(string command, string args) => ToggleConfigUI();

  public static bool IsTitleUnlocked(uint titleId)
  {
    if (!TitleList.DataReceived)
    {
      Logger.Debug("Function was called before title data was received.");
      return false;
    }

    return TitleList.IsTitleUnlocked((ushort)titleId);
  }

  public static string GetTitleName(Title titleRow) =>
    (ClientState.LocalPlayer?.Customize[(int)CustomizeIndex.Gender] == 1
      ? titleRow.Feminine
      : titleRow.Masculine).ExtractText();

  public static CharacterConfig GetCharacterConfig()
  {
    ulong localContentId = ClientState.LocalContentId;
    if (localContentId == 0)
    {
      Logger.Debug("Function was called before character was logged in, returning temporary config.");
      return new CharacterConfig();
    }

    if (!Configuration.CharacterConfigs.TryGetValue(localContentId, out CharacterConfig? config))
    {
      Logger.Debug($"No configuration was found for {localContentId}, creating one.");
      config = new CharacterConfig();
      Configuration.CharacterConfigs[localContentId] = config;
      Configuration.Save();
    }

    return config;
  }

  public static void SaveTitle(uint jobId, int titleId)
  {
    ulong localContentId = ClientState.LocalContentId;
    if (localContentId == 0)
    {
      Logger.Debug("Function was called before character was logged in.");
      return;
    }

    CharacterConfig config = GetCharacterConfig();
    config.JobTitleMappings[jobId] = titleId;
    Configuration.CharacterConfigs[localContentId] = config;
    Configuration.Save();

    if (ClientState.LocalPlayer?.ClassJob.RowId == jobId)
    {
      SetTitle(titleId);
    }
  }

  public static void SetTitle(int titleId)
  {
    if (titleId == TitleIds.DoNotOverride) return;

    if (DataManager.Excel.GetSheet<Title>().TryGetRow((uint)titleId, out var _))
    {
      TitleController.SendTitleIdUpdate((ushort)titleId);
    }
    else
    {
      Logger.Error($"Unable to retrieve data for title row id: {titleId}. Not updating title.");
    }
  }
}

public class ConfigWindow : Window, IDisposable
{
  private string _searchTerm = string.Empty;
  private readonly Dictionary<uint, bool> _dropdownDrawState = new();

  public ConfigWindow(Plugin plugin) : base("JobTitles###JobTitles")
  {
    Flags = ImGuiWindowFlags.NoResize;
    Size = new Vector2(360, 400);
    SizeCondition = ImGuiCond.Always;
  }

  public void Dispose() => _searchTerm = string.Empty;

  public override void Draw()
  {
    var language = Plugin.ClientState.ClientLanguage;
    var localPlayer = Plugin.ClientState.LocalPlayer;

    UpdateWindowTitle(localPlayer);

    if (localPlayer == null)
    {
      DrawLoginPrompt(language);
      return;
    }

    if (!Plugin.TitleList.DataReceived)
    {
      DrawTitleListRequest(language);
      return;
    }

    Size = new Vector2(360, 400);
    DrawJobTitleSelect(language);
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

  private void DrawLoginPrompt(ClientLanguage language)
  {
    Size = new Vector2(360, 50);

    DrawHorizontallyCenteredText(Loc.Get(language, Loc.Phrase.PleaseLogIn));
  }

  private void UpdateWindowTitle(IPlayerCharacter? localPlayer)
  {
    if (localPlayer == null)
    {
      WindowName = "Job Titles###JobTitles";
      return;
    }

    string debugFlag = Plugin.Configuration.DebugMode ? " (Debug)" : string.Empty;
    WindowName = $"Job Titles ({localPlayer.Name}){debugFlag}###JobTitles";
  }

  private void DrawTitleListRequest(ClientLanguage language)
  {
    Size = new Vector2(360, 75);

    DrawHorizontallyCenteredText(Loc.Get(language, Loc.Phrase.RequestTitleListDescription));
    DrawHorizontallyCenteredButton(
      Loc.Get(language, Loc.Phrase.RequestTitleList),
      Plugin.TitleList.DataPending,
      () =>
      {
        Logger.Debug("User requested the title list");
        Plugin.TitleList.RequestTitleList();
      }
    );
  }

  private void DrawJobTitleSelect(ClientLanguage language)
  {
    var config = Plugin.GetCharacterConfig();
    foreach (var jobId in GetOrderedJobs())
    {
      if (!Plugin.DataManager.Excel.GetSheet<ClassJob>().TryGetRow(jobId, out var jobRow))
      {
        Logger.Error($"Unable to retrieve data for classjob row id: {jobId}. Not drawing title selection.");
        continue;
      }

      // 62100 - job icons with background
      uint iconId = 62100 + jobId;
      if (!Plugin.TextureProvider.TryGetFromGameIcon(new GameIconLookup(iconId), out var jobIcon))
      {
        Logger.Error($"Unable to retrieve icon for id: {iconId}. Not drawing title selection.");
        continue;
      }

      int selectedTitleId = config.JobTitleMappings.GetValueOrDefault(jobId, TitleIds.DoNotOverride);
      DrawJobTitleDropdown(language, jobRow, jobIcon, jobId, selectedTitleId);
    }
  }

  private void DrawJobTitleDropdown(ClientLanguage language, ClassJob jobRow, ISharedImmediateTexture jobIcon, uint jobId, int selectedTitleId)
  {
    // Draw Job Icon with vertically centered Job Name
    var jobName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(jobRow.Name.ExtractText());
    var iconSize = new Vector2(24 * ImGuiHelpers.GlobalScale);
    var verticalOffset = (iconSize.Y - ImGui.CalcTextSize(jobName).Y) / 2.0f;
    ImGui.Image(jobIcon.GetWrapOrEmpty().ImGuiHandle, iconSize);
    if (jobId == 18 && ImGui.IsItemClicked()) // Debug Button for FSH
    {
      Plugin.Configuration.DebugMode = !Plugin.Configuration.DebugMode;
      Plugin.Configuration.Save();
    }
    ImGui.SameLine();
    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + verticalOffset);
    ImGui.Text(jobName);
    if (ImGui.IsItemHovered())
      ImGui.SetTooltip(Loc.Get(language, Loc.Phrase.JobNameTooltip).Replace("%s", jobName));
    ImGui.SetCursorPosY(ImGui.GetCursorPosY() - verticalOffset);

    // Set size and distance for the dropdown
    ImGui.SameLine(140 * ImGuiHelpers.GlobalScale);
    ImGui.SetNextItemWidth(200 * ImGuiHelpers.GlobalScale);

    var selectedTitleName = GetTitleName(language, selectedTitleId);
    using var dropdown = ImRaii.Combo($"###Title{jobRow.RowId}", selectedTitleName);

    if (dropdown)
    {
      DrawDropdownContents(language, jobId, selectedTitleId);
    }
    else
    {
      _dropdownDrawState[jobId] = true;
    }
  }

  private void DrawDropdownContents(ClientLanguage language, uint jobId, int selectedTitleId)
  {
    if (_dropdownDrawState[jobId])
    {
      _searchTerm = string.Empty;
      _dropdownDrawState[jobId] = false;
      ImGui.SetKeyboardFocusHere();
    }

    ImGui.InputTextWithHint("###TextSearch", Loc.Get(language, Loc.Phrase.Search), ref _searchTerm, 256, ImGuiInputTextFlags.AutoSelectAll);

    DrawSelectable(Loc.Get(language, Loc.Phrase.DoNotOverride), TitleIds.DoNotOverride, jobId, selectedTitleId);
    DrawSelectable(Loc.Get(language, Loc.Phrase.None), TitleIds.None, jobId, selectedTitleId);

    foreach (var title in Plugin.DataManager.Excel.GetSheet<Title>().Where(t => t.RowId != 0 && Plugin.IsTitleUnlocked(t.RowId)))
    {
      DrawSelectable(Plugin.GetTitleName(title), (int)title.RowId, jobId, selectedTitleId);
    }
  }

  private void DrawSelectable(string option, int titleId, uint jobId, int selectedTitleId)
  {
    if (_searchTerm.Length == 0 || option.IndexOf(_searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
    {
      if (ImGui.Selectable(option, selectedTitleId == titleId))
      {
        Plugin.SaveTitle(jobId, titleId);
      }
    }
  }

  // Job ordering following the in-game character screen order.
  // Tanks -> Healers -> Melee -> Phys. DPS -> Magical DPS -> Crafters -> Gatherers
  // with jobs within those sections being ordered like in-game as well.
  private static uint[] GetOrderedJobs() => new uint[]
  {
    19, 21, 32, 37, 24, 28, 33, 40, 20, 22, 30, 34, 39, 41, 23, 31, 38,
    25, 27, 35, 42, 36, 08, 09, 10, 11, 12, 13, 14, 15, 16, 17, 18,
  };

  private string GetTitleName(ClientLanguage language, int titleId)
  {
    if (titleId == TitleIds.DoNotOverride)
      return Loc.Get(language, Loc.Phrase.DoNotOverride);

    if (titleId == TitleIds.None)
      return Loc.Get(language, Loc.Phrase.None);

    if (!Plugin.DataManager.Excel.GetSheet<Title>().TryGetRow((uint)titleId, out var titleRow))
    {
      Logger.Error($"Unable to retrieve data for title row id: {titleId}.");
      return "Failed to retrieve title";
    }

    return Plugin.GetTitleName(titleRow);
  }
}

[Serializable]
public class CharacterConfig
{
  public Dictionary<uint, int> JobTitleMappings { get; set; } = new();
}

[Serializable]
public class Configuration : IPluginConfiguration
{
  public int Version { get; set; } = 0;
  public Dictionary<ulong, CharacterConfig> CharacterConfigs { get; set; } = new();
  public bool DebugMode { get; set; } = false;

  public void Save() => Plugin.PluginInterface.SavePluginConfig(this);

  public static Configuration Load() =>
    Plugin.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
}

public static class TitleIds
{
  public const int DoNotOverride = -1;
  public const int None = 0;
}

public class Loc
{
  public enum Phrase
  {
    PleaseLogIn,
    RequestTitleList,
    RequestTitleListDescription,
    JobNameTooltip,
    None,
    DoNotOverride,
    Search,
  }

  private static readonly Dictionary<ClientLanguage, Dictionary<Phrase, string>> Translations = new()
  {
    { ClientLanguage.English, new Dictionary<Phrase, string>
      {
        { Phrase.PleaseLogIn, "Please log in to start configuring JobTitles." },
        { Phrase.RequestTitleList, "Request Title List" },
        { Phrase.RequestTitleListDescription, "Click the button below to load your unlocked titles." },
        { Phrase.JobNameTooltip, $"Configure title used for %s.\nDo not override - does not update title when you switch to this job\nNone - Clears your title" },
        { Phrase.None, "None" },
        { Phrase.DoNotOverride, "Do not override" },
        { Phrase.Search, "Search" },
      }
    },
    { ClientLanguage.German, new Dictionary<Phrase, string>
      {
        { Phrase.PleaseLogIn, "Bitte logge dich ein um JobTitles zu konfigurieren." },
        { Phrase.RequestTitleList, "Titelliste anfordern" },
        { Phrase.RequestTitleListDescription, "Den Knopf drücken um Ihre freigeschalteten Titel zu laden." },
        { Phrase.JobNameTooltip, $"Konfiguriere den Titel für %s.\nNicht ersetzen - verändert den Titel nicht wenn du die Klasse wechselst\nKeinen Titel - Entfernt deinen Titel" },
        { Phrase.None, "Keinen Titel" },
        { Phrase.DoNotOverride, "Nicht ersetzen" },
        { Phrase.Search, "Suche" },
      }
    },
  };

  public static string Get(ClientLanguage language, Phrase phrase) =>
    Translations.TryGetValue(language, out var translations)
    && translations.TryGetValue(phrase, out var translation)
      ? translation
      : GetFallbackTranslation(phrase);

  public static string GetFallbackTranslation(Phrase phrase) =>
    Translations.TryGetValue(ClientLanguage.English, out var defaultTranslations)
    && defaultTranslations.TryGetValue(phrase, out var defaultTranslation)
      ? defaultTranslation
      : phrase.ToString();
}

public static class Logger
{
  private static readonly Dictionary<string, DateTime> _lastLogTime = new();
  private static readonly TimeSpan _throttleInterval = TimeSpan.FromSeconds(15);

  private enum LogType
  {
    Error,
    Debug,
  }

  private static void Log(LogType type, string text, string functionName, params object[] values)
  {
    string formattedText = $"[{functionName}] {text}";

    bool ShouldLog(string message)
    {
      if (_lastLogTime.TryGetValue(message, out var lastLogTime))
      {
        if (DateTime.UtcNow - lastLogTime < _throttleInterval)
        {
          return false;
        }
      }

      return true;
    }

    if (type == LogType.Error)
    {
      if (ShouldLog(formattedText))
      {
        Plugin.Logger.Error(formattedText, values);
        _lastLogTime[formattedText] = DateTime.UtcNow;
      }
    }
    else if (type == LogType.Debug && Plugin.Configuration.DebugMode)
    {
      if (ShouldLog(formattedText))
      {
        Plugin.Logger.Debug(formattedText, values);
        _lastLogTime[formattedText] = DateTime.UtcNow;
      }
    }
  }

  public static void Error(string text, [CallerMemberName] string? functionName = null, params object[] values) =>
    Log(LogType.Error, text, functionName ?? "UnknownFunction", values);

  public static void Debug(string text, [CallerMemberName] string? functionName = null, params object[] values) =>
    Log(LogType.Debug, text, functionName ?? "UnknownFunction", values);
}
