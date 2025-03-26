using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

using JobTitles.Windows;

namespace JobTitles.Services;

public class TitleService
{
  public class TitleIds
  {
    public const TitleId DoNotOverride = TitleId.MaxValue;
    public const TitleId None = 0;
  };

  private unsafe TitleController TitleController { get => UIState.Instance()->TitleController; }
  private unsafe TitleList TitleList { get => UIState.Instance()->TitleList; }

  private TitleId _lastTitleId = 0;
  private bool _shouldCacheTitlesUnlockBitmask = true;

  private readonly Loc Loc;
  private readonly Logger Logger;
  private readonly Configuration Configuration;
  private readonly JobService JobService;
  private readonly IClientState ClientState;
  private readonly IDataManager DataManager;

  public TitleService(Loc loc, Logger logger, Configuration configuration, JobService jobService, IClientState clientState, IDataManager dataManager)
  {
    Loc = loc;
    Logger = logger;
    Configuration = configuration;
    JobService = jobService;
    ClientState = clientState;
    DataManager = dataManager;
  }

  public static TitleId ToTitleId(uint titleId) =>
    titleId > TitleId.MaxValue ? TitleIds.None : (TitleId)titleId;

  public unsafe TitleId GetAndCacheCurrentTitleId()
  {
    IPlayerCharacter? localPlayer = ClientState.LocalPlayer;
    if (localPlayer == null || localPlayer.Address == IntPtr.Zero)
      return _lastTitleId;

    Character* localChar = (Character*)localPlayer.Address;
    return _lastTitleId = localChar->CharacterData.TitleId;
  }

  public void RequestTitleList()
  {
    if (TitleList.DataPending || TitleList.DataReceived) return;
    Logger.Debug("Requesting title list");
    TitleList.RequestTitleList();
    _shouldCacheTitlesUnlockBitmask = true;
  }

  public void CacheTitlesUnlockBitmask()
  {
    if (_shouldCacheTitlesUnlockBitmask && TitleList.DataReceived)
    {
      CharacterConfig characterConfig = Configuration.GetCharacterConfig();
      characterConfig.CachedTitlesUnlockBitmask = new List<byte>(TitleList.TitlesUnlockBitmask.ToArray());
      Configuration.SaveCharacterConfig(characterConfig);
      Logger.Debug($"CachedTitlesUnlockBitmask::{string.Join(",", characterConfig.CachedTitlesUnlockBitmask)}");
      _shouldCacheTitlesUnlockBitmask = false;
    }
  }

  public void AddTitleIdToCache(TitleId titleId)
  {
    Logger.Debug($"titleId::{titleId}");

    CharacterConfig characterConfig = Configuration.GetCharacterConfig();
    int byteIndex = (int)(titleId / 8);
    while (characterConfig.CachedTitlesUnlockBitmask.Count <= byteIndex)
      characterConfig.CachedTitlesUnlockBitmask.Add(0);
    int bitIndex = (int)(titleId % 8);
    characterConfig.CachedTitlesUnlockBitmask[byteIndex] |= (byte)(1 << bitIndex);
    Configuration.SaveCharacterConfig(characterConfig);
  }

  public void SaveJobTitleMapping(JobService.Job job, TitleId titleId)
  {
    CharacterConfig characterConfig = Configuration.GetCharacterConfig();
    characterConfig.JobTitleMappingsV2[job] = titleId;
    Configuration.SaveCharacterConfig(characterConfig);
  }

  public string GetTitleName(TitleId titleId)
  {
    if (titleId == TitleIds.DoNotOverride)
      return Loc.Get(Loc.Phrase.DoNotOverride);

    if (titleId == TitleIds.None)
      return Loc.Get(Loc.Phrase.None);

    if (!DataManager.Excel.GetSheet<Title>(Loc.Language).TryGetRow(titleId, out Title titleRow))
    {
      Logger.Error($"Unable to retrieve data for titleId::{titleId}.");
      return "[ERROR] See /xllog.";
    }

    return GetTitleName(titleRow);
  }

  public string GetTitleName(Title titleRow) =>
    (ClientState.LocalPlayer?.Customize[(int)CustomizeIndex.Gender] == 1
      ? titleRow.Feminine
      : titleRow.Masculine).ExtractText();

  public bool IsTitleUnlocked(TitleId titleId)
  {
    if (titleId == TitleIds.None) return true;

    if (!TitleList.DataReceived)
    {
      Logger.Debug("TitleList is not received. Using cached list.");
      CharacterConfig characterConfig = Configuration.GetCharacterConfig();
      int byteIndex = (int)(titleId / 8);
      if (byteIndex >= characterConfig.CachedTitlesUnlockBitmask.Count)
      {
        Logger.Debug($"Cached TitleList does not have enough indexes. Trying to index {byteIndex} / {characterConfig.CachedTitlesUnlockBitmask.Count}");
        return false;
      }
      int bitIndex = (int)(titleId % 8);
      byte byteValue = characterConfig.CachedTitlesUnlockBitmask[byteIndex];
      return (byteValue & (1 << bitIndex)) != 0;
    }

    return TitleList.IsTitleUnlocked(titleId);
  }

  private readonly HashSet<TitleId> GaroTitles = new()
  {
    325, // Makai Master
    326, // Garo
    327, // Makai Monk
    328, // Barago
    329, // Dan
    330, // Makai Bard
    331, // Makai Black Mage
    332, // Makai White Mage
    333, // Zero
    334, // Makai Summoner
    335, // Makai Scholar
    336, // Kiba
    337, // Makai Machinist
    338, // Makai Astrologian
    639, // Makai Samurai
    640, // Makai Red Mage
    641, // Makai Gunbreaker
    642, // Makai Dancer
    643, // Makai Reaper
    644, // Makai Sage
  };

  public bool IsGaroTitle(TitleId titleId) => GaroTitles.Contains(titleId);

  private readonly Dictionary<JobService.Job, TitleId> JobGAROTitleMap = new()
  {
    { JobService.Job.PLD, 326 },
    { JobService.Job.MNK, 327 },
    { JobService.Job.WAR, 328 },
    { JobService.Job.DRG, 329 },
    { JobService.Job.BRD, 330 },
    { JobService.Job.BLM, 331 },
    { JobService.Job.WHM, 332 },
    { JobService.Job.NIN, 333 },
    { JobService.Job.SMN, 334 },
    { JobService.Job.SCH, 335 },
    { JobService.Job.DRK, 336 },
    { JobService.Job.MCH, 337 },
    { JobService.Job.AST, 338 },
    { JobService.Job.SAM, 639 },
    { JobService.Job.RDM, 640 },
    { JobService.Job.GNB, 640 },
    { JobService.Job.DNC, 642 },
    { JobService.Job.RPR, 643 },
    { JobService.Job.SGE, 644 },
  };

  public TitleId GetPvPTitleId(JobService.Job currentJob)
  {
    CharacterConfig characterConfig = Configuration.GetCharacterConfig();
    if (!characterConfig.TryUseGAROTitleForCurrentJob)
      return characterConfig.GAROTitleIdV2;

    ushort pvpTitleId = JobGAROTitleMap.GetValueOrDefault(currentJob, characterConfig.GAROTitleIdV2);

    return IsTitleUnlocked(pvpTitleId)
      ? pvpTitleId
      : characterConfig.GAROTitleIdV2;
  }

  public (bool, string) UpdateTitle() => SetTitle(JobService.GetCurrentJob());

  public (bool, string) SetTitle(JobService.Job currentJob)
  {
    if (currentJob == JobService.Job.ADV) return (false, string.Empty);
    TitleId titleId = TitleIds.DoNotOverride;
    CharacterConfig characterConfig = Configuration.GetCharacterConfig();

    if (ClientState.IsPvPExcludingDen && characterConfig.UseGAROTitleInPvP)
    {
      titleId = GetPvPTitleId(currentJob);
    }
    else
    {
      JobService.Job job = currentJob;
      if (JobService.IsClass(currentJob) && characterConfig.ClassMode == CharacterConfig.ClassModeOption.InheritJobTitles)
        job = JobService.GetJobFromClass(currentJob);

      titleId = characterConfig.JobTitleMappingsV2.GetValueOrDefault(job, TitleIds.DoNotOverride);
    }

    string titleName = GetTitleName(titleId);
    if (titleName.Contains("[ERROR]")) return (false, string.Empty);
    if (titleId == TitleIds.DoNotOverride) return (true, titleName);

    TitleId currentTitleId = GetAndCacheCurrentTitleId();
    if (currentTitleId == titleId)
    {
      Logger.Debug($"Title is already set to titleId::{titleId} titleName::'{titleName}'. Not sending title id update.");
      return (true, titleName);
    }

    // We prevent users from selecting a title they haven't unlocked.
    // However, since we now cache the TitleList, we can verify if the user has unlocked it anyway for consistency.
    if (!IsTitleUnlocked(titleId))
    {
      Logger.Error($"Attempted to set a locked title. titleId::{titleId} titleName::'{titleName}'");
      return (false, string.Empty);
    }

    Logger.Debug($"Sending title id update. titleId::{titleId} titleName::'{titleName}'");

    if (Configuration.PrintTitleChangesInChat)
      Logger.Chat(Loc.Get(Loc.Phrase.TitleChangedTo) + " ", titleName);

    // if (Configuration.PrintTitleChangesToToast)
    // Logger.Toast(Loc.Get(Loc.Phrase.TitleChangedTo) + " ", titleName);

    TitleController.SendTitleIdUpdate(titleId);
    _lastTitleId = titleId;

    return (true, titleName);
  }
}
