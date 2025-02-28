using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.Enums;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;

namespace JobTitles.Utils;

class TitleUtils
{
  private static unsafe TitleController TitleController => UIState.Instance()->TitleController;
  private static unsafe TitleList TitleList => UIState.Instance()->TitleList;
  private static bool shouldCache = false;

  public class TitleIds
  {
    public static int DoNotOverride = -1;
    public static int None = 0;
  };

  private static uint GetCurrentJob() => Plugin.ClientState.LocalPlayer?.ClassJob.RowId ?? 0;

  public static void RequestTitleList()
  {
    if (TitleList.DataPending || TitleList.DataReceived) return;

    Logger.Debug("Requesting title list");
    TitleList.RequestTitleList();
    shouldCache = true;
  }

  public static void CacheTitleList()
  {
    if (TitleList.DataReceived && shouldCache)
    {
      byte[] titlesUnlockBitmask = TitleList.TitlesUnlockBitmask.ToArray();
      CharacterConfig characterConfig = Configuration.GetCharacterConfig();
      characterConfig.CachedTitlesUnlockBitmask = new List<byte>(titlesUnlockBitmask.ToArray());
      Configuration.SaveCharacterConfig(characterConfig);
      Logger.Debug($"Cached title list: {string.Join(",", characterConfig.CachedTitlesUnlockBitmask)}");
      shouldCache = false;
    }
  }

  public static void AddTitleIdToCache(uint titleId)
  {
    Logger.Debug($"Adding titleId to cached titles: {titleId}");

    CharacterConfig characterConfig = Configuration.GetCharacterConfig();
    int byteIndex = (int)(titleId / 8);
    while (characterConfig.CachedTitlesUnlockBitmask.Count <= byteIndex)
    {
      characterConfig.CachedTitlesUnlockBitmask.Add(0);
    }
    int bitIndex = (int)(titleId % 8);
    characterConfig.CachedTitlesUnlockBitmask[byteIndex] |= (byte)(1 << bitIndex);
    Configuration.SaveCharacterConfig(characterConfig);
  }

  public static void SaveTitle(uint jobId, int titleId)
  {
    CharacterConfig characterConfig = Configuration.GetCharacterConfig();
    characterConfig.JobTitleMappings[jobId] = titleId;
    Configuration.SaveCharacterConfig(characterConfig);
  }

  public static void UpdateTitle() => SetTitle(GetCurrentJob());

  public static void SetTitle(uint currentJobId)
  {
    if (currentJobId == 0) return;
    int titleId;

    CharacterConfig characterConfig = Configuration.GetCharacterConfig();

    if (Plugin.ClientState.IsPvPExcludingDen && characterConfig.UseGAROTitleInPvP)
    {
      titleId = GetPvPTitleId();
      Plugin.ConfigWindow.ClosePvPPrompt();
    }
    else
    {
      if (!characterConfig.JobTitleMappings.TryGetValue(currentJobId, out titleId)) return;

      if (JobUtils.IsClass(currentJobId) && characterConfig.ClassMode == CharacterConfig.ClassModeOption.InheritJobTitles)
      {
        if (characterConfig.JobTitleMappings.TryGetValue(JobUtils.GetJobIdForClassId(currentJobId), out int newTitleId))
        {
          titleId = newTitleId;
        }
      }
    }

    if (titleId == TitleIds.DoNotOverride) return;
    if (!Plugin.DataManager.Excel.GetSheet<Title>(Loc.Language).TryGetRow((uint)titleId, out var _))
    {
      Logger.Error($"Unable to retrieve data for title row id: {titleId}. Not updating title.");
      return;
    }

    ushort currentTitleId = GetCurrentTitleId();
    if (currentTitleId == (ushort)titleId)
    {
      Logger.Debug($"Title is already set to {currentTitleId}. Not sending title id update.");
      return;
    }

    if (titleId > ushort.MaxValue)
    {
      Logger.Error($"Provided value was too large: {titleId}");
      return;
    }

    // We prevent users from selecting a title they haven't unlocked.
    // However, since we now cache the TitleList, we can verify if the user has unlocked it anyway for consistency.
    if (!IsTitleUnlocked((uint)titleId))
    {
      Logger.Error($"Attempted to set a locked title: {titleId}({(ushort)titleId}) \"{GetTitleName(titleId)}\"");
      return;
    }

    Logger.Debug($"Sending title id update: {titleId}({(ushort)titleId}) \"{GetTitleName(titleId)}\"");

    if (Plugin.Configuration.PrintTitleChangesInChat)
      Logger.Chat(Loc.Get(Loc.Phrase.TitleChangedTo) + " ", GetTitleName(titleId));

    TitleController.SendTitleIdUpdate((ushort)titleId);
  }

  private static unsafe ushort GetCurrentTitleId()
  {
    var localPlayer = Plugin.ClientState.LocalPlayer;
    if (localPlayer != null && localPlayer.Address != IntPtr.Zero)
    {
      Character* localChar = (Character*)localPlayer.Address;
      return localChar->CharacterData.TitleId;
    }
    else
    {
      return (ushort)TitleIds.None;
    }
  }

  public static bool IsTitleUnlocked(uint titleId)
  {
    if (titleId > ushort.MaxValue)
    {
      Logger.Error($"Provided value was too large: {titleId}");
      return false;
    }

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

    return TitleList.IsTitleUnlocked((ushort)titleId);
  }

  public static string GetTitleName(int titleId)
  {
    if (titleId == TitleIds.DoNotOverride)
      return Loc.Get(Loc.Phrase.DoNotOverride);

    if (titleId == TitleIds.None)
      return Loc.Get(Loc.Phrase.None);

    if (!Plugin.DataManager.Excel.GetSheet<Title>(Loc.Language).TryGetRow((uint)titleId, out var titleRow))
    {
      Logger.Error($"Unable to retrieve data for title row id: {titleId}.");
      return "[ERROR] See /xllog.";
    }

    return GetTitleName(titleRow);
  }

  public static string GetTitleName(Title titleRow) =>
    (Plugin.ClientState.LocalPlayer?.Customize[(int)CustomizeIndex.Gender] == 1
      ? titleRow.Feminine
      : titleRow.Masculine).ExtractText();

  public static bool IsGaroTitle(uint titleId) =>
    new uint[] {
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
    }.Contains(titleId);

  private static readonly Dictionary<JobUtils.Job, int> JobGAROTitleMap = new()
  {
    { JobUtils.Job.PLD, 326 },
    { JobUtils.Job.MNK, 327 },
    { JobUtils.Job.WAR, 328 },
    { JobUtils.Job.DRG, 329 },
    { JobUtils.Job.BRD, 330 },
    { JobUtils.Job.BLM, 331 },
    { JobUtils.Job.WHM, 332 },
    { JobUtils.Job.NIN, 333 },
    { JobUtils.Job.SMN, 334 },
    { JobUtils.Job.SCH, 335 },
    { JobUtils.Job.DRK, 336 },
    { JobUtils.Job.MCH, 337 },
    { JobUtils.Job.AST, 338 },
    { JobUtils.Job.SAM, 639 },
    { JobUtils.Job.RDM, 640 },
    { JobUtils.Job.GNB, 640 },
    { JobUtils.Job.DNC, 642 },
    { JobUtils.Job.RPR, 643 },
    { JobUtils.Job.SGE, 644 },
  };

  private static int GetPvPTitleId()
  {
    CharacterConfig characterConfig = Configuration.GetCharacterConfig();
    if (!characterConfig.TryUseGAROTitleForCurrentJob)
      return characterConfig.GAROTitleId;

    uint jobId = GetCurrentJob();
    int pvpTitleId = Enum.IsDefined(typeof(JobUtils.Job), jobId)
      ? JobGAROTitleMap.GetValueOrDefault((JobUtils.Job)jobId, characterConfig.GAROTitleId)
      : characterConfig.GAROTitleId;

    return IsTitleUnlocked((uint)pvpTitleId)
      ? pvpTitleId
      : characterConfig.GAROTitleId;
  }

  public static void OnEnterPvP()
  {
    if (!Plugin.ClientState.IsPvPExcludingDen) return;

    CharacterConfig characterConfig = Configuration.GetCharacterConfig();
    if (!characterConfig.UseGAROTitleInPvP) return;

    int pvpTitleId = GetPvPTitleId();
    if (pvpTitleId == TitleIds.None || pvpTitleId == TitleIds.DoNotOverride) return;
    if ((ushort)pvpTitleId == GetCurrentTitleId()) return;

    Plugin.ConfigWindow.OpenPvPPrompt(pvpTitleId);
  }
}
