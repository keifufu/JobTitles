using System;
using Dalamud.Game.ClientState.Objects.Enums;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;

namespace JobTitles.Utils;

class TitleUtils
{
  public static unsafe TitleController TitleController => UIState.Instance()->TitleController;
  public static unsafe TitleList TitleList => UIState.Instance()->TitleList;
  public class TitleIds
  {
    public static int DoNotOverride = -1;
    public static int None = 0;
  };

  public static void SaveTitle(uint jobId, int titleId)
  {
    ulong localContentId = Plugin.ClientState.LocalContentId;
    if (localContentId == 0)
    {
      Logger.Debug("Function was called before character was logged in.");
      return;
    }

    CharacterConfig characterConfig = Configuration.GetCharacterConfig();
    characterConfig.JobTitleMappings[jobId] = titleId;
    Plugin.Configuration.CharacterConfigs[localContentId] = characterConfig;
    Plugin.Configuration.Save();
  }

  public static void UpdateTitle() => SetTitle(Plugin.ClientState.LocalPlayer?.ClassJob.RowId ?? 0);

  public static void SetTitle(uint currentJobId)
  {
    if (currentJobId == 0) return;

    CharacterConfig characterConfig = Configuration.GetCharacterConfig();
    if (!characterConfig.JobTitleMappings.TryGetValue(currentJobId, out int titleId)) return;

    if (JobUtils.IsClass(currentJobId) && Plugin.Configuration.ClassMode == Configuration.ClassModeOption.InheritJobTitles)
    {
      if (characterConfig.JobTitleMappings.TryGetValue(JobUtils.GetJobIdForClassId(currentJobId), out int newTitleId))
      {
        titleId = newTitleId;
      }
    }

    if (titleId == TitleUtils.TitleIds.DoNotOverride) return;
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
    if (!TitleList.DataReceived)
    {
      Logger.Debug("Function was called before title data was received.");
      return false;
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
}
