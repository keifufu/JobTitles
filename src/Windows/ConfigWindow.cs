using System.Globalization;
using System.IO;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace JobTitles.Windows;

public class ConfigWindow : Window, IDisposable
{
  private string _jobSearchTerm = string.Empty;
  private string _titleSearchTerm = string.Empty;
  private Dictionary<JobService.Job, bool> _dropdownDrawState = new();

  private readonly Loc Loc;
  private readonly Logger Logger;
  private readonly Configuration Configuration;
  private readonly JobService JobService;
  private readonly TitleService TitleService;
  private readonly IClientState ClientState;
  private readonly IDataManager DataManager;
  private readonly ITextureProvider TextureProvider;
  private readonly IDalamudPluginInterface PluginInterface;

  public ConfigWindow(Loc loc, Logger logger, Configuration configuration, JobService jobService, TitleService titleService, IClientState clientState, IDataManager dataManager, ITextureProvider textureProvider, IDalamudPluginInterface pluginInterface) : base("JobTitles###JobTitles")
  {
    Loc = loc;
    Logger = logger;
    Configuration = configuration;
    JobService = jobService;
    TitleService = titleService;
    ClientState = clientState;
    DataManager = dataManager;
    TextureProvider = textureProvider;
    PluginInterface = pluginInterface;

    Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize;
    SizeCondition = ImGuiCond.Always;
  }

  public void Dispose() { }

  private float ScaledFloat(float value) => value * ImGuiHelpers.GlobalScale;
  private Vector2 ScaledVector2(float value) => new Vector2(value * ImGuiHelpers.GlobalScale);

  public override void Draw()
  {
    UpdateWindowTitle();

    if (ClientState.LocalPlayer == null)
    {
      UpdateSizeContraints(0);
      DrawLoginPrompt();
      return;
    }

    if (ImGui.IsWindowAppearing())
      TitleService.RequestTitleList();
    TitleService.CacheTitlesUnlockBitmask();

    UpdateSizeContraints();
    if (DrawJobSearch()) return;

    using ImRaii.IEndObject tabBar = ImRaii.TabBar("###TabBar");
    if (!tabBar.Success)
    {
      Logger.Error("Failed to draw TabBar?");
      return;
    }

    DrawJobTab($"{Loc.Get(Loc.Phrase.Tanks)}###Tanks", JobService.Tanks);
    DrawJobTab($"{Loc.Get(Loc.Phrase.Healers)}###Healers", JobService.Healers);
    DrawJobTab($"{Loc.Get(Loc.Phrase.Melee)}###Melee", JobService.Melee);
    DrawJobTab($"{Loc.Get(Loc.Phrase.Ranged)}###Ranged", JobService.Ranged);
    DrawJobTab($"{Loc.Get(Loc.Phrase.Crafters)}###Crafters", JobService.Crafters);
    DrawJobTab($"{Loc.Get(Loc.Phrase.Gatherers)}###Gatherers", JobService.Gatherers);
    DrawOptionsTab();
  }

  private void UpdateSizeContraints(float? minWidth = null)
  {
    string[] tabLabels = { Loc.Get(Loc.Phrase.Tanks), Loc.Get(Loc.Phrase.Healers), Loc.Get(Loc.Phrase.Melee), Loc.Get(Loc.Phrase.Ranged), Loc.Get(Loc.Phrase.Crafters), Loc.Get(Loc.Phrase.Gatherers), Loc.Get(Loc.Phrase.Options) };

    ImGuiStylePtr style = ImGui.GetStyle();
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
      MaximumSize = new Vector2(totalWidth, 500),
    };
  }

  private void UpdateWindowTitle()
  {
    string debugFlag = Configuration.Debug ? " (Debug)" : string.Empty;

    IPlayerCharacter? localPlayer = ClientState.LocalPlayer;
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
      if (ImGui.Button(text) && !disabledCondition)
        onClick?.Invoke();
  }

  private void DrawLoginPrompt()
  {
    DrawHorizontallyCenteredText(Loc.Get(Loc.Phrase.PleaseLogIn));
  }

  private bool DrawJobSearch()
  {
    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
    ImGui.InputTextWithHint("###JobSearch", Loc.Get(Loc.Phrase.Search), ref _jobSearchTerm, 256, ImGuiInputTextFlags.AutoSelectAll);

    string jobSearchTermTrimmed = _jobSearchTerm.Trim();
    if (jobSearchTermTrimmed.Length > 0)
    {
      Func<ClassJob, bool> shouldRenderJobRow = jobRow => jobSearchTermTrimmed.Length == 0 || jobRow.Name.ExtractText().IndexOf(jobSearchTermTrimmed, StringComparison.OrdinalIgnoreCase) >= 0;
      if (DrawJobTitleSelectRows(shouldRenderJobRow) == 0)
      {
        DrawHorizontallyCenteredText(Loc.Get(Loc.Phrase.NoResults));
      }
      return true;
    }

    return false;
  }

  private void DrawJobTab(string name, HashSet<JobService.Job> jobs)
  {
    using ImRaii.IEndObject tabItem = ImRaii.TabItem(name);
    if (!tabItem.Success) return;

    DrawJobTitleSelectRows(jobRow => jobs.Contains(JobService.ToJob(jobRow.RowId)));
  }

  private void DrawOptionsTab()
  {
    using ImRaii.IEndObject tabItem = ImRaii.TabItem($"{Loc.Get(Loc.Phrase.Options)}###Options");
    if (!tabItem.Success) return;

    CharacterConfig characterConfig = Configuration.GetCharacterConfig();

    uint infoIconId = 60407;
    if (!TextureProvider.TryGetFromGameIcon(new GameIconLookup(infoIconId), out ISharedImmediateTexture? infoIcon))
    {
      Logger.Error($"Unable to retrieve icon for infoIconId::{infoIconId}. Not drawing options tab.");
      return;
    }

    ImGui.TextUnformatted(Loc.Get(Loc.Phrase.Language));

    ImGui.SetNextItemWidth(ScaledFloat(200));
    using (ImRaii.IEndObject dropdown = ImRaii.Combo("###LanguageSelect", Loc.GetLanguageName(Configuration.Language)))
    {
      if (dropdown.Success)
      {
        DrawLanguageSelectable(Language.None);
        DrawLanguageSelectable(Language.English);
        DrawLanguageSelectable(Language.German);
      }
    }

    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 5);
    ImGui.TextUnformatted(Loc.Get(Loc.Phrase.ClassMode));
    ImGui.SameLine();
    ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 5);
    ImGui.SetCursorPosX(ImGui.GetCursorPosX() - 10);
    ImGui.Image(infoIcon!.GetWrapOrEmpty().ImGuiHandle, ScaledVector2(28));
    if (ImGui.IsItemHovered())
      using (ImRaii.Tooltip())
        ImGui.TextUnformatted(Loc.Get(Loc.Phrase.ClassModeTooltip));

    if (ImGui.RadioButton(Loc.Get(Loc.Phrase.InheritJobTitles), characterConfig.ClassMode == CharacterConfig.ClassModeOption.InheritJobTitles))
    {
      characterConfig.ClassMode = CharacterConfig.ClassModeOption.InheritJobTitles;
      Configuration.SaveCharacterConfig(characterConfig);
      TitleService.UpdateTitle();
    }

    if (ImGui.RadioButton(Loc.Get(Loc.Phrase.ShowClasses), characterConfig.ClassMode == CharacterConfig.ClassModeOption.ShowClasses))
    {
      characterConfig.ClassMode = CharacterConfig.ClassModeOption.ShowClasses;
      Configuration.SaveCharacterConfig(characterConfig);
      TitleService.UpdateTitle();
    }

    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 5);
    ImGui.TextUnformatted(Loc.Get(Loc.Phrase.PvP));
    ImGui.SameLine();
    ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 5);
    ImGui.SetCursorPosX(ImGui.GetCursorPosX() - 10);
    ImGui.Image(infoIcon!.GetWrapOrEmpty().ImGuiHandle, ScaledVector2(28));
    if (ImGui.IsItemHovered())
      using (ImRaii.Tooltip())
        ImGui.TextUnformatted(Loc.Get(Loc.Phrase.PvPTooltip));

    bool useGAROTitleInPvP = characterConfig.UseGAROTitleInPvP;
    if (ImGui.Checkbox(Loc.Get(Loc.Phrase.UseGAROTitleInPvP), ref useGAROTitleInPvP))
    {
      characterConfig.UseGAROTitleInPvP = useGAROTitleInPvP;
      Configuration.SaveCharacterConfig(characterConfig);
      TitleService.UpdateTitle();
    }

    if (characterConfig.UseGAROTitleInPvP)
    {
      bool tryUseGAROTitleForCurrentJob = characterConfig.TryUseGAROTitleForCurrentJob;
      if (ImGui.Checkbox(Loc.Get(Loc.Phrase.TryUseGAROTitleForCurrentJob), ref tryUseGAROTitleForCurrentJob))
      {
        characterConfig.TryUseGAROTitleForCurrentJob = tryUseGAROTitleForCurrentJob;
        Configuration.SaveCharacterConfig(characterConfig);
        TitleService.UpdateTitle();
      }

      ImGui.SetNextItemWidth(ScaledFloat(200));
      string selectedTitleName = characterConfig.GAROTitleIdV2 == TitleService.TitleIds.DoNotOverride
        ? Loc.Get(Loc.Phrase.SelectTitle)
        : TitleService.GetTitleName(characterConfig.GAROTitleIdV2);
      using (ImRaii.IEndObject dropdown = ImRaii.Combo("###GAROTitleSelect", selectedTitleName))
      {
        if (dropdown.Success)
        {
          int titles = 0;
          foreach (Title title in DataManager.Excel.GetSheet<Title>(Loc.Language).Where(t => TitleService.IsGaroTitle(TitleService.ToTitleId(t.RowId)) && TitleService.IsTitleUnlocked(TitleService.ToTitleId(t.RowId))))
          {
            titles++;
            if (ImGui.Selectable(TitleService.GetTitleName(title), characterConfig.GAROTitleIdV2 == title.RowId))
            {
              characterConfig.GAROTitleIdV2 = TitleService.ToTitleId(title.RowId);
              Configuration.SaveCharacterConfig(characterConfig);
              TitleService.UpdateTitle();
            }
          }

          if (titles == 0)
          {
            ImGui.TextUnformatted(Loc.Get(Loc.Phrase.NoGAROTitlesUnlocked));
          }
        }
      }
    }

    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 5);
    ImGui.TextUnformatted(Loc.Get(Loc.Phrase.Other));
    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 10);

    bool printTitleChangesInChat = Configuration.PrintTitleChangesInChat;
    if (ImGui.Checkbox(Loc.Get(Loc.Phrase.PrintTitleChangesInChat), ref printTitleChangesInChat))
    {
      Configuration.PrintTitleChangesInChat = printTitleChangesInChat;
      Configuration.Save();
    }

    bool debug = Configuration.Debug;
    if (ImGui.Checkbox(Loc.Get(Loc.Phrase.Debug), ref debug))
    {
      Configuration.Debug = debug;
      Configuration.Save();
    }

    ImGui.SameLine(ImGui.GetContentRegionAvail().X - (ScaledFloat(40)));
    ImGui.SetCursorPosY(ImGui.GetCursorPosY() - (ScaledFloat(25)));
    DrawGambler(ScaledVector2(50));
  }

  private void DrawGambler(Vector2 size)
  {
    CharacterConfig characterConfig = Configuration.GetCharacterConfig();
    if (!characterConfig.JobTitleMappingsV2.TryGetValue(JobService.Job.AST, out TitleId titleId)) return;
    if (titleId != 195) return; // Gambler

    string gamblerImagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "gambler.png");
    IDalamudTextureWrap? gamblerImage = TextureProvider.GetFromFile(gamblerImagePath).GetWrapOrDefault();
    if (gamblerImage == null)
    {
      Logger.Error($"Failed to find gambler from gamblerImagePath::{gamblerImagePath}");
      return;
    }

    IntPtr textureID = gamblerImage.ImGuiHandle;
    ImGui.Image(gamblerImage.ImGuiHandle, size);
    if (ImGui.IsItemHovered())
      using (ImRaii.Tooltip())
        ImGui.TextUnformatted("Gambi is the Gambler");
  }

  private void DrawLanguageSelectable(Language language)
  {
    if (ImGui.Selectable(Loc.GetLanguageName(language), Configuration.Language == language))
    {
      Configuration.Language = language;
      Configuration.Save();
    }
  }

  private int DrawJobTitleSelectRows(Func<ClassJob, bool> shouldRenderJobRow)
  {
    int rows = 0;
    CharacterConfig characterConfig = Configuration.GetCharacterConfig();
    foreach (JobService.Job job in JobService.AllJobs)
    {
      if (JobService.IsClass(job) && characterConfig.ClassMode != CharacterConfig.ClassModeOption.ShowClasses)
        continue;

      if (!DataManager.Excel.GetSheet<ClassJob>(Loc.Language).TryGetRow((uint)job, out ClassJob jobRow))
      {
        Logger.Error($"Unable to retrieve data for row {(uint)job}. Not drawing title selection.");
        continue;
      }

      if (!shouldRenderJobRow(jobRow)) continue;

      // 62100 - job icons with background
      uint jobIconId = 62100 + (uint)job;
      if (!TextureProvider.TryGetFromGameIcon(new GameIconLookup(jobIconId), out ISharedImmediateTexture? jobIcon))
      {
        Logger.Error($"Unable to retrieve icon for jobIconId::{jobIconId}. Not drawing title selection.");
        continue;
      }

      TitleId selectedTitleId = characterConfig.JobTitleMappingsV2.GetValueOrDefault(job, TitleService.TitleIds.DoNotOverride);
      DrawJobTitleSelectRow(jobRow, jobIcon, job, selectedTitleId);
      rows++;
    }

    return rows;
  }

  private void DrawJobTitleSelectRow(ClassJob jobRow, ISharedImmediateTexture jobIcon, JobService.Job job, TitleId selectedTitleId)
  {
    // Draw Job Icon with vertically centered Job Name
    string jobName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(jobRow.Name.ExtractText());
    Vector2 iconSize = ScaledVector2(24);
    float verticalOffset = (iconSize.Y - ImGui.CalcTextSize(jobName).Y) / 2.0f;
    ImGui.Image(jobIcon.GetWrapOrEmpty().ImGuiHandle, iconSize);
    ImGui.SameLine();
    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + verticalOffset);
    ImGui.Text(jobName);
    if (ImGui.IsItemHovered())
      using (ImRaii.Tooltip())
        ImGui.TextUnformatted(string.Format(Loc.Get(Loc.Phrase.JobNameTooltip), jobName));
    // ImGui.SetCursorPosY(ImGui.GetCursorPosY() - verticalOffset);

    // Set size and distance for the dropdown
    ImGui.SameLine(ScaledFloat(140));
    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);

    string selectedTitleName = TitleService.GetTitleName(selectedTitleId);
    using (ImRaii.IEndObject dropdown = ImRaii.Combo($"###Title{jobRow.RowId}", selectedTitleName))
    {
      if (dropdown.Success)
      {
        DrawJobTitleDropdownContents(job, selectedTitleId);
      }
      else
      {
        _dropdownDrawState[job] = true;
      }
    }
  }

  private void DrawJobTitleDropdownContents(JobService.Job job, int selectedTitleId)
  {
    if (_dropdownDrawState[job])
    {
      _titleSearchTerm = string.Empty;
      _dropdownDrawState[job] = false;
      ImGui.SetKeyboardFocusHere();
    }

    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - (ImGui.GetStyle().ScrollbarSize / ImGuiHelpers.GlobalScale));
    ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 2);
    ImGui.InputTextWithHint("###TitleSearch", Loc.Get(Loc.Phrase.Search), ref _titleSearchTerm, 256, ImGuiInputTextFlags.AutoSelectAll);

    DrawTitleSelectable(Loc.Get(Loc.Phrase.DoNotOverride), TitleService.TitleIds.DoNotOverride, job, selectedTitleId);
    DrawTitleSelectable(Loc.Get(Loc.Phrase.None), TitleService.TitleIds.None, job, selectedTitleId);

    foreach (Title title in DataManager.Excel.GetSheet<Title>(Loc.Language).Where(t => TitleService.IsTitleUnlocked(TitleService.ToTitleId(t.RowId))))
    {
      DrawTitleSelectable(TitleService.GetTitleName(title), TitleService.ToTitleId(title.RowId), job, selectedTitleId);
    }
  }

  private void DrawTitleSelectable(string option, TitleId titleId, JobService.Job job, int selectedTitleId)
  {
    string titleSearchTermTrimmed = _titleSearchTerm.Trim();
    if (titleSearchTermTrimmed.Length == 0 || option.IndexOf(titleSearchTermTrimmed, StringComparison.OrdinalIgnoreCase) >= 0)
    {
      if (ImGui.Selectable(option, selectedTitleId == titleId))
      {
        TitleService.SaveJobTitleMapping(job, titleId);
        TitleService.UpdateTitle();
      }
    }
  }
}
