namespace JobTitles.Windows;

public class ConfigWindow(
  ILoc _loc,
  ILogger _logger,
  IJobService _jobService,
  IDataManager _dataManager,
  IObjectTable _objectTable,
  ITitleService _titleService,
  Configuration _configuration,
  ITextureProvider _textureProvider,
  IDalamudPluginInterface _pluginInterface) : Window("JobTitles###JobTitles"), IHostedService
{
  private readonly Dictionary<JobService.Job, bool> _dropdownDrawState = [];
  private string _titleSearchTerm = string.Empty;
  private string _jobSearchTerm = string.Empty;

  public Task StartAsync(CancellationToken cancellationToken)
  {
    Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize;
    SizeCondition = ImGuiCond.Always;
    return _logger.ServiceLifecycle();
  }

  public Task StopAsync(CancellationToken cancellationToken)
    => _logger.ServiceLifecycle();

  private float Scaled(float value) => value * ImGuiHelpers.GlobalScale;

  public override void Draw()
  {
    UpdateWindowTitle();

    if (_objectTable.LocalPlayer == null)
    {
      UpdateSizeContraints(0.0f);
      DrawLoginPrompt();
      return;
    }

    if (ImGui.IsWindowAppearing())
      _titleService.RequestTitleList();
    _titleService.CacheTitlesUnlockBitmask();

    UpdateSizeContraints();
    if (DrawJobSearch()) return;

    using ImRaii.TabBarDisposable tabBar = ImRaii.TabBar("###TabBar");
    if (!tabBar.Success)
    {
      _logger.Error("Failed to draw TabBar?");
      return;
    }

    DrawJobTab($"{_loc.Get(Loc.Phrase.Tanks)}###Tanks", JobService.Tanks);
    DrawJobTab($"{_loc.Get(Loc.Phrase.Healers)}###Healers", JobService.Healers);
    DrawJobTab($"{_loc.Get(Loc.Phrase.Melee)}###Melee", JobService.Melee);
    DrawJobTab($"{_loc.Get(Loc.Phrase.Ranged)}###Ranged", JobService.Ranged);
    DrawJobTab($"{_loc.Get(Loc.Phrase.Crafters)}###Crafters", JobService.Crafters);
    DrawJobTab($"{_loc.Get(Loc.Phrase.Gatherers)}###Gatherers", JobService.Gatherers);
    DrawOptionsTab();
  }

  private void UpdateSizeContraints(float? minWidth = null)
  {
    string[] tabLabels = [
      _loc.Get(Loc.Phrase.Tanks),
      _loc.Get(Loc.Phrase.Healers),
      _loc.Get(Loc.Phrase.Melee),
      _loc.Get(Loc.Phrase.Ranged),
      _loc.Get(Loc.Phrase.Crafters),
      _loc.Get(Loc.Phrase.Gatherers),
      _loc.Get(Loc.Phrase.Options)
    ];

    ImGuiStylePtr style = ImGui.GetStyle();
    float windowPadding = style.WindowPadding.X;
    float framePadding = style.FramePadding.X;
    float itemInnerSpacing = style.ItemInnerSpacing.X;

    float width = windowPadding * 2.0f;

    for (int i = 0; i < tabLabels.Length; i++)
    {
      width += ImGui.CalcTextSize(tabLabels[i]).X + (framePadding * 2.0f);

      if (i < tabLabels.Length - 1)
      {
        width += itemInnerSpacing;
      }
    }

    float totalWidth = (width / ImGuiHelpers.GlobalScale) + windowPadding;

    SizeConstraints = new WindowSizeConstraints
    {
      MinimumSize = new Vector2(minWidth ?? totalWidth, 0.0f),
      MaximumSize = new Vector2(totalWidth, 500.0f),
    };
  }

  private void UpdateWindowTitle()
  {
    string debugFlag = _configuration.Debug ? " (Debug)" : string.Empty;

    IPlayerCharacter? localPlayer = _objectTable.LocalPlayer;
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
    ImGui.Text(text);
  }

  private void DrawLoginPrompt()
    => DrawHorizontallyCenteredText(_loc.Get(Loc.Phrase.PleaseLogIn));

  private bool DrawJobSearch()
  {
    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
    ImGui.InputTextWithHint("###JobSearch", _loc.Get(Loc.Phrase.Search),
      ref _jobSearchTerm, 256, ImGuiInputTextFlags.AutoSelectAll);

    string jobSearchTermTrimmed = _jobSearchTerm.Trim();
    if (jobSearchTermTrimmed.Length > 0)
    {
      bool ShouldRenderJobRow(ClassJob jobRow) =>
        jobSearchTermTrimmed.Length == 0
        || jobRow.Name.ToString().Contains(jobSearchTermTrimmed, StringComparison.OrdinalIgnoreCase);

      if (DrawJobTitleSelectRows(ShouldRenderJobRow) == 0)
      {
        DrawHorizontallyCenteredText(_loc.Get(Loc.Phrase.NoResults));
      }
      return true;
    }

    return false;
  }

  private void DrawJobTab(string name, HashSet<JobService.Job> jobs)
  {
    using ImRaii.TabItemDisposable tabItem = ImRaii.TabItem(name);
    if (!tabItem.Success) return;

    DrawJobTitleSelectRows(jobRow => jobs.Contains(JobService.ToJob(jobRow.RowId)));
  }

  private void DrawOptionsTab()
  {
    using ImRaii.TabItemDisposable tabItem = ImRaii.TabItem($"{_loc.Get(Loc.Phrase.Options)}###Options");
    if (!tabItem.Success) return;

    CharacterConfig characterConfig = _configuration.GetCharacterConfig();

    uint infoIconId = 60407;
    if (!_textureProvider.TryGetFromGameIcon(new GameIconLookup(infoIconId), out ISharedImmediateTexture? infoIcon))
    {
      _logger.Error($"Unable to retrieve icon for infoIconId::{infoIconId}. Not drawing options tab.");
      return;
    }

    ImGui.Text(_loc.Get(Loc.Phrase.Language));

    ImGui.SetNextItemWidth(Scaled(200.0f));
    using (ImRaii.ComboDisposable dropdown = ImRaii.Combo("###LanguageSelect", _loc.GetLanguageName(_configuration.Language)))
    {
      if (dropdown.Success)
      {
        DrawLanguageSelectable(Language.None);
        DrawLanguageSelectable(Language.English);
        DrawLanguageSelectable(Language.German);
      }
    }

    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 5.0f);
    ImGui.Text(_loc.Get(Loc.Phrase.ClassMode));
    ImGui.SameLine();
    ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 5.0f);
    ImGui.SetCursorPosX(ImGui.GetCursorPosX() - 10.0f);
    ImGui.Image(infoIcon.GetWrapOrEmpty().Handle, new(Scaled(28.0f)));
    if (ImGui.IsItemHovered())
    {
      using (ImRaii.Tooltip())
      {
        ImGui.Text(_loc.Get(Loc.Phrase.ClassModeTooltip));
      }
    }

    if (ImGui.RadioButton(_loc.Get(Loc.Phrase.InheritJobTitles),
      characterConfig.ClassMode == CharacterConfig.ClassModeOption.InheritJobTitles))
    {
      characterConfig.ClassMode = CharacterConfig.ClassModeOption.InheritJobTitles;
      _configuration.SaveCharacterConfig(characterConfig);
      _titleService.UpdateTitle();
    }

    if (ImGui.RadioButton(_loc.Get(Loc.Phrase.ShowClasses),
      characterConfig.ClassMode == CharacterConfig.ClassModeOption.ShowClasses))
    {
      characterConfig.ClassMode = CharacterConfig.ClassModeOption.ShowClasses;
      _configuration.SaveCharacterConfig(characterConfig);
      _titleService.UpdateTitle();
    }

    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 5);
    ImGui.Text(_loc.Get(Loc.Phrase.PvP));
    ImGui.SameLine();
    ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 5);
    ImGui.SetCursorPosX(ImGui.GetCursorPosX() - 10);
    ImGui.Image(infoIcon.GetWrapOrEmpty().Handle, new(Scaled(28.0f)));
    if (ImGui.IsItemHovered())
    {
      using (ImRaii.Tooltip())
      {
        ImGui.Text(_loc.Get(Loc.Phrase.PvPTooltip));
      }
    }

    bool useGAROTitleInPvP = characterConfig.UseGAROTitleInPvP;
    if (ImGui.Checkbox(_loc.Get(Loc.Phrase.UseGAROTitleInPvP), ref useGAROTitleInPvP))
    {
      characterConfig.UseGAROTitleInPvP = useGAROTitleInPvP;
      _configuration.SaveCharacterConfig(characterConfig);
      _titleService.UpdateTitle();
    }

    if (characterConfig.UseGAROTitleInPvP)
    {
      bool tryUseGAROTitleForCurrentJob = characterConfig.TryUseGAROTitleForCurrentJob;
      if (ImGui.Checkbox(_loc.Get(Loc.Phrase.TryUseGAROTitleForCurrentJob), ref tryUseGAROTitleForCurrentJob))
      {
        characterConfig.TryUseGAROTitleForCurrentJob = tryUseGAROTitleForCurrentJob;
        _configuration.SaveCharacterConfig(characterConfig);
        _titleService.UpdateTitle();
      }

      ImGui.SetNextItemWidth(Scaled(200.0f));
      string selectedTitleName = characterConfig.GAROTitleIdV2 == TitleService.TitleIds.DoNotOverride
        ? _loc.Get(Loc.Phrase.SelectTitle)
        : _titleService.GetTitleName(characterConfig.GAROTitleIdV2);

      using ImRaii.ComboDisposable dropdown = ImRaii.Combo("###GAROTitleSelect", selectedTitleName);
      if (dropdown.Success)
      {
        int titles = 0;
        IEnumerable<Title> validTitles = _dataManager.Excel.GetSheet<Title>(_loc.Language)
          .Where(t =>
            _titleService.IsGaroTitle(TitleService.ToTitleId(t.RowId)) &&
            _titleService.IsTitleUnlocked(TitleService.ToTitleId(t.RowId)));

        foreach (Title title in validTitles)
        {
          titles++;
          if (ImGui.Selectable(_titleService.GetTitleName(title), characterConfig.GAROTitleIdV2 == title.RowId))
          {
            characterConfig.GAROTitleIdV2 = TitleService.ToTitleId(title.RowId);
            _configuration.SaveCharacterConfig(characterConfig);
            _titleService.UpdateTitle();
          }
        }

        if (titles == 0)
        {
          ImGui.Text(_loc.Get(Loc.Phrase.NoGAROTitlesUnlocked));
        }
      }
    }

    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 5.0f);
    ImGui.Text(_loc.Get(Loc.Phrase.Other));
    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 10.0f);

    bool printTitleChangesInChat = _configuration.PrintTitleChangesInChat;
    if (ImGui.Checkbox(_loc.Get(Loc.Phrase.PrintTitleChangesInChat), ref printTitleChangesInChat))
    {
      _configuration.PrintTitleChangesInChat = printTitleChangesInChat;
      _configuration.Save();
    }

    bool debug = _configuration.Debug;
    if (ImGui.Checkbox(_loc.Get(Loc.Phrase.Debug), ref debug))
    {
      _configuration.Debug = debug;
      _configuration.Save();
    }

    ImGui.SameLine(ImGui.GetContentRegionAvail().X - Scaled(40.0f));
    ImGui.SetCursorPosY(ImGui.GetCursorPosY() - Scaled(25.0f));
    DrawGambler(new(Scaled(50.0f)));
  }

  private void DrawGambler(Vector2 size)
  {
    CharacterConfig characterConfig = _configuration.GetCharacterConfig();
    if (!characterConfig.JobTitleMappingsV2.TryGetValue(JobService.Job.AST, out TitleId titleId)) return;
    if (titleId != 195) return; // Gambler

    string gamblerImagePath = Path.Combine(_pluginInterface.AssemblyLocation.Directory?.FullName!, "gambler.png");
    IDalamudTextureWrap? gamblerImage = _textureProvider.GetFromFile(gamblerImagePath).GetWrapOrDefault();
    if (gamblerImage == null)
    {
      _logger.Error($"Failed to find gambler from gamblerImagePath::{gamblerImagePath}");
      return;
    }

    ImGui.Image(gamblerImage.Handle, size);
    if (ImGui.IsItemHovered())
    {
      using (ImRaii.Tooltip())
      {
        ImGui.Text("Gambi is the Gambler");
      }
    }
  }

  private void DrawLanguageSelectable(Language language)
  {
    if (ImGui.Selectable(_loc.GetLanguageName(language), _configuration.Language == language))
    {
      _configuration.Language = language;
      _configuration.Save();
    }
  }

  private int DrawJobTitleSelectRows(Func<ClassJob, bool> shouldRenderJobRow)
  {
    int rows = 0;
    CharacterConfig characterConfig = _configuration.GetCharacterConfig();
    foreach (JobService.Job job in JobService.AllJobs)
    {
      if (_jobService.IsClass(job) && characterConfig.ClassMode != CharacterConfig.ClassModeOption.ShowClasses)
        continue;

      if (!_dataManager.Excel.GetSheet<ClassJob>(_loc.Language).TryGetRow((uint)job, out ClassJob jobRow))
      {
        _logger.Error($"Unable to retrieve data for row {(uint)job}. Not drawing title selection.");
        continue;
      }

      if (!shouldRenderJobRow(jobRow)) continue;

      // 62100 - job icons with background
      uint jobIconId = 62100 + (uint)job;
      if (!_textureProvider.TryGetFromGameIcon(new GameIconLookup(jobIconId), out ISharedImmediateTexture? jobIcon))
      {
        _logger.Error($"Unable to retrieve icon for jobIconId::{jobIconId}. Not drawing title selection.");
        continue;
      }

      TitleId selectedTitleId = characterConfig.JobTitleMappingsV2.GetValueOrDefault(job, TitleService.TitleIds.DoNotOverride);
      DrawJobTitleSelectRow(jobRow, jobIcon, job, selectedTitleId);
      rows++;
    }

    return rows;
  }

  private void DrawJobTitleSelectRow(
    ClassJob jobRow,
    ISharedImmediateTexture jobIcon,
    JobService.Job job,
    TitleId selectedTitleId)
  {
    // Draw Job Icon with vertically centered Job Name
    string jobName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(jobRow.Name.ExtractText());
    Vector2 iconSize = new(Scaled(24.0f));
    float verticalOffset = (iconSize.Y - ImGui.CalcTextSize(jobName).Y) / 2.0f;
    ImGui.Image(jobIcon.GetWrapOrEmpty().Handle, iconSize);
    ImGui.SameLine();
    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + verticalOffset);
    ImGui.Text(jobName);
    if (ImGui.IsItemHovered())
    {
      using (ImRaii.Tooltip())
      {
        ImGui.Text(string.Format(CultureInfo.CurrentCulture, _loc.Get(Loc.Phrase.JobNameTooltip), jobName));
      }
    }
    // ImGui.SetCursorPosY(ImGui.GetCursorPosY() - verticalOffset);

    // Set size and distance for the dropdown
    ImGui.SameLine(Scaled(140.0f));
    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);

    string selectedTitleName = _titleService.GetTitleName(selectedTitleId);
    using ImRaii.ComboDisposable dropdown = ImRaii.Combo($"###Title{jobRow.RowId}", selectedTitleName);
    if (dropdown.Success)
    {
      DrawJobTitleDropdownContents(job, selectedTitleId);
    }
    else
    {
      _dropdownDrawState[job] = true;
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
    ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 2.0f);
    ImGui.InputTextWithHint("###TitleSearch", _loc.Get(Loc.Phrase.Search), ref _titleSearchTerm, 256, ImGuiInputTextFlags.AutoSelectAll);

    DrawTitleSelectable(_loc.Get(Loc.Phrase.DoNotOverride), TitleService.TitleIds.DoNotOverride, job, selectedTitleId);
    DrawTitleSelectable(_loc.Get(Loc.Phrase.None), TitleService.TitleIds.None, job, selectedTitleId);

    IEnumerable<Title> validTitles = _dataManager.Excel.GetSheet<Title>(_loc.Language)
      .Where(t => t.RowId != 0 && _titleService.IsTitleUnlocked(TitleService.ToTitleId(t.RowId)));
    foreach (Title title in validTitles)
    {
      DrawTitleSelectable(_titleService.GetTitleName(title), TitleService.ToTitleId(title.RowId), job, selectedTitleId);
    }
  }

  private void DrawTitleSelectable(string option, TitleId titleId, JobService.Job job, int selectedTitleId)
  {
    string titleSearchTermTrimmed = _titleSearchTerm.Trim();
    if (titleSearchTermTrimmed.Length == 0 || option.Contains(titleSearchTermTrimmed, StringComparison.OrdinalIgnoreCase))
    {
      if (ImGui.Selectable(option, selectedTitleId == titleId))
      {
        _titleService.SaveJobTitleMapping(job, titleId);
        _titleService.UpdateTitle();
      }
    }
  }
}
