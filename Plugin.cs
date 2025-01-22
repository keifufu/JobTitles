using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;

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

using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Numerics;
using System.Linq;
using System;

namespace JobTitles;

[Serializable]
public class CharacterConfig
{
  public Dictionary<uint, int> JobTitleMappings { get; set; } = new Dictionary<uint, int>();
}

[Serializable]
public class Configuration : IPluginConfiguration
{
  public int Version { get; set; } = 0;

  public Dictionary<ulong, CharacterConfig> CharacterConfigs { get; set; } = new Dictionary<ulong, CharacterConfig>();
  public bool DebugMode = false;

  public void Save()
  {
    Plugin.PluginInterface.SavePluginConfig(this);
  }

  public static Configuration Load()
  {
    return Plugin.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
  }
}

public class Localizations
{
  public enum Phrase
  {
    PleaseLogIn,
    RequestTitleList,
    RequestTitleListDescription,
    None,
    DoNotOverride,
    Search,
  }

  private static readonly Dictionary<ClientLanguage, Dictionary<Phrase, string>> Translations = new()
  {
    {
      ClientLanguage.English, new Dictionary<Phrase, string>
      {
        { Phrase.PleaseLogIn, "Please log in to start configuring JobTitles." },
        { Phrase.RequestTitleList, "Request Title List" },
        { Phrase.RequestTitleListDescription, "Click the button below to load your unlocked titles." },
        { Phrase.None, "None" },
        { Phrase.DoNotOverride, "Do not override" },
        { Phrase.Search, "Search" },
      }
    },
    {
      ClientLanguage.German, new Dictionary<Phrase, string>
      {
        { Phrase.PleaseLogIn, "Bitte logge dich ein um JobTitles zu konfigurieren." },
        { Phrase.RequestTitleList, "Titelliste anfordern" },
        { Phrase.RequestTitleListDescription, "Den Knopf drücken um Ihre freigeschalteten Titel zu laden." },
        { Phrase.None, "Keinen Titel" },
        { Phrase.DoNotOverride, "Nicht ersetzen" },
        { Phrase.Search, "Suche" },
      }
    },
  };

  public static string Get(ClientLanguage language, Phrase phrase)
  {
    if (Translations.TryGetValue(language, out var translations) && translations.TryGetValue(phrase, out var translation))
    {
      return translation;
    }

    if (language == ClientLanguage.English) return phrase.ToString();
    return Get(ClientLanguage.English, phrase);
  }
}

public sealed class Plugin : IDalamudPlugin
{
  [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
  [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
  [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
  [PluginService] internal static IClientState ClientState { get; private set; } = null!;
  [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
  [PluginService] internal static IPluginLog Log { get; private set; } = null!;

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

    ClientState.ClassJobChanged += ClassJobChanged;
  }

  public void Dispose()
  {
    WindowSystem.RemoveAllWindows();
    ConfigWindow.Dispose();
    CommandManager.RemoveHandler(CommandName);

    ClientState.ClassJobChanged -= ClassJobChanged;
  }

  private void DrawUI() => WindowSystem.Draw();
  private void ToggleConfigUI() => ConfigWindow.Toggle();
  private void OnCommand(string command, string args) => ToggleConfigUI();

  private void ClassJobChanged(uint classJobId)
  {
    CharacterConfig config = GetCharacterConfig();
    if (config.JobTitleMappings.TryGetValue(classJobId, out int titleId))
    {
      SetTitle(titleId);
    }
  }

  public static CharacterConfig GetCharacterConfig()
  {
    ulong localContentId = ClientState.LocalContentId;
    if (localContentId == 0) return new CharacterConfig();

    if (!Configuration.CharacterConfigs.TryGetValue(localContentId, out CharacterConfig? config))
    {
      config = new CharacterConfig();
      Configuration.CharacterConfigs[localContentId] = config;
      Configuration.Save();
    }

    return config;
  }

  public static void SaveTitle(uint classJobId, int titleId)
  {
    ulong localContentId = ClientState.LocalContentId;
    if (localContentId == 0) return;

    CharacterConfig config = GetCharacterConfig();
    config.JobTitleMappings[classJobId] = titleId;
    Configuration.CharacterConfigs[localContentId] = config;
    Configuration.Save();

    if (ClientState.LocalPlayer?.ClassJob.RowId == classJobId)
    {
      SetTitle(titleId);
    }
  }

  public static void SetTitle(int titleId)
  {
    if (titleId == -1) return; // "Do not override"

    // Make sure it exists
    if (DataManager.Excel.GetSheet<Title>().TryGetRow((uint)titleId, out var _))
    {
      unsafe
      {
        var uiState = UIState.Instance();
        uiState->TitleController.SendTitleIdUpdate((ushort)titleId);
      }
    }
  }

  public static unsafe bool IsTitleUnlocked(uint titleId)
  {
    var titleList = UIState.Instance()->TitleList;
    if (!titleList.DataReceived) return false;
    return titleList.IsTitleUnlocked((ushort)titleId);
  }

  public static string GetTitleName(Title titleRow)
  {
    return ClientState.LocalPlayer?.Customize[(int)CustomizeIndex.Gender] == 1 ? titleRow.Feminine.ExtractText() : titleRow.Masculine.ExtractText();
  }
}

public class ConfigWindow : Window, IDisposable
{
  private string SearchTerm = string.Empty;
  private Dictionary<uint, bool> FirstDropdownRenderMap = new Dictionary<uint, bool>();

  public ConfigWindow(Plugin plugin) : base("JobTitles###JobTitles")
  {
    Flags = ImGuiWindowFlags.NoResize;
    Size = new Vector2(360, 400);
    SizeCondition = ImGuiCond.Always;
  }

  public void Dispose() => SearchTerm = string.Empty;

  private void DrawSelectable(string option, int titleId, uint classJobId, int selectedTitleId)
  {
    if (SearchTerm.Length == 0 || option.IndexOf(SearchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
    {
      if (ImGui.Selectable(option, selectedTitleId == titleId))
      {
        Plugin.SaveTitle(classJobId, titleId);
      }
    }
  }

  public override void Draw()
  {
    ClientLanguage language = Plugin.ClientState.ClientLanguage;

    var localPlayer = Plugin.ClientState.LocalPlayer;
    if (localPlayer == null)
    {
      ImGui.TextUnformatted(Localizations.Get(language, Localizations.Phrase.PleaseLogIn));
      WindowName = $"JobTitles###JobTitles";
      return;
    }

    string windowTitle = $"JobTitles ({localPlayer.Name})";
    if (Plugin.Configuration.DebugMode) windowTitle += $" (Debug)";
    windowTitle += "###JobTitles";
    WindowName = windowTitle;

    unsafe
    {
      var titleList = UIState.Instance()->TitleList;
      if (!titleList.DataReceived)
      {
        Size = new Vector2(360, 75);

        string requestTitleListDescription = Localizations.Get(language, Localizations.Phrase.RequestTitleListDescription);
        float textWidth = ImGui.CalcTextSize(requestTitleListDescription).X;
        float textX = (ImGui.GetContentRegionAvail().X - textWidth) * 0.5f;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + textX);
        ImGui.TextUnformatted(requestTitleListDescription);

        string requestTitleList = Localizations.Get(language, Localizations.Phrase.RequestTitleList);
        float availableWidth = ImGui.GetContentRegionAvail().X;
        float buttonWidth = ImGui.CalcTextSize(requestTitleList).X + ImGui.GetStyle().FramePadding.X * 2;
        float buttonX = (availableWidth - buttonWidth) * 0.5f;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + buttonX);
        using (ImRaii.Disabled(titleList.DataPending))
        {
          if (ImGui.Button(requestTitleList))
          {
            titleList.RequestTitleList();
          }
        }

        return;
      }
      else
      {
        Size = new Vector2(360, 400);
      }
    }

    CharacterConfig config = Plugin.GetCharacterConfig();

    // Will have to manually update this list but I do like a proper order:
    // (Tanks -> Healers -> Melee -> Phys. Ranged -> Magical Ranged -> Crafters -> Gatherers)
    uint[] classJobIdOrder = [
      19, // PLD
      21, // WAR
      32, // DRK
      37, // GNB
      24, // WHM
      28, // SCH
      33, // AST
      40, // SGE
      20, // MNK
      22, // DRG
      30, // NIN
      34, // SAM
      39, // RPR
      41, // VPR
      23, // BRD
      31, // MCH
      38, // DNC
      25, // BLM
      27, // SMN
      35, // RDM
      42, // PCT
      36, // BLU
      08, // CRP
      09, // BSM
      10, // ARM
      11, // GSM
      12, // LTW
      13, // WVR
      14, // ALC
      15, // CUL
      16, // MIN
      17, // BTN
      18, // FSH
    ];

    foreach (uint classJobId in classJobIdOrder)
    {
      if (!Plugin.DataManager.Excel.GetSheet<ClassJob>().TryGetRow(classJobId, out var classJobRow)) continue;
      if (!Plugin.TextureProvider.TryGetFromGameIcon(new GameIconLookup(62100 + classJobId), out var classJobIcon)) continue;
      if (!config.JobTitleMappings.TryGetValue(classJobId, out int selectedTitleId)) selectedTitleId = -1;
      if (!FirstDropdownRenderMap.ContainsKey(classJobId)) FirstDropdownRenderMap[classJobId] = true;

      // ClassJob Icon & Centered Name
      string classJobName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(classJobRow.Name.ExtractText());
      Vector2 imageSize = new Vector2(24 * ImGuiHelpers.GlobalScale, 24 * ImGuiHelpers.GlobalScale);
      var verticalOffset = (imageSize.Y - ImGui.CalcTextSize(classJobName).Y) / 2.0f;
      ImGui.Image(classJobIcon.GetWrapOrEmpty().ImGuiHandle, imageSize);
      if (classJobId == 18 && ImGui.IsItemClicked()) // Debug Button for FSH
      {
        Plugin.Configuration.DebugMode = !Plugin.Configuration.DebugMode;
        Plugin.Configuration.Save();
      }
      ImGui.SameLine();
      ImGui.SetCursorPosY(ImGui.GetCursorPosY() + verticalOffset);
      ImGui.Text(classJobName);
      ImGui.SetCursorPosY(ImGui.GetCursorPosY() - verticalOffset);

      // Title Select Dropdown w/ Search
      ImGui.SameLine(140 * ImGuiHelpers.GlobalScale);
      ImGui.SetNextItemWidth(200 * ImGuiHelpers.GlobalScale);
      string selectedTitleName =
        selectedTitleId == -1
        ? Localizations.Get(language, Localizations.Phrase.DoNotOverride)
        : selectedTitleId == 0
        ? Localizations.Get(language, Localizations.Phrase.None)
        : Plugin.DataManager.Excel.GetSheet<Title>().TryGetRow((uint)selectedTitleId, out var selectedTitleRow)
        ? Plugin.GetTitleName(selectedTitleRow)
        : "Invalid title id";

      using (var dropdown = ImRaii.Combo($"###Title{classJobRow.RowId}", selectedTitleName))
      {
        if (dropdown)
        {
          ImGui.SetNextItemWidth(190 * ImGuiHelpers.GlobalScale);
          if (FirstDropdownRenderMap[classJobId])
          {
            SearchTerm = string.Empty;
            ImGui.SetKeyboardFocusHere();
            FirstDropdownRenderMap[classJobId] = false;
          }
          ImGui.InputTextWithHint($"###TextSearch", Localizations.Get(language, Localizations.Phrase.Search), ref SearchTerm, 256, ImGuiInputTextFlags.AutoSelectAll);

          DrawSelectable(Localizations.Get(language, Localizations.Phrase.DoNotOverride), -1, classJobId, selectedTitleId);
          DrawSelectable(Localizations.Get(language, Localizations.Phrase.None), 0, classJobId, selectedTitleId);

          foreach (var titleRow in Plugin.DataManager.Excel.GetSheet<Title>()!.Where(row => row.RowId != 0 && Plugin.IsTitleUnlocked(row.RowId)))
          {
            DrawSelectable(Plugin.GetTitleName(titleRow), (int)titleRow.RowId, classJobId, selectedTitleId);
          }
        }
        else
        {
          FirstDropdownRenderMap[classJobId] = true;
        }
      }
    }
  }
}
