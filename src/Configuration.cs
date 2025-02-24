using System;
using System.Collections.Generic;
using Dalamud.Configuration;
using JobTitles.Utils;
using Lumina.Data;

namespace JobTitles;

[Serializable]
public class CharacterConfig
{
  public Dictionary<uint, int> JobTitleMappings { get; set; } = new();
  public List<byte> CachedTitlesUnlockBitmask { get; set;} = new List<byte>();
  public ClassModeOption ClassMode { get; set; } = ClassModeOption.InheritJobTitles;
  public bool UseGAROTitleInPvP { get; set; } = false;
  public bool TryUseGAROTitleForCurrentJob { get; set; } = false;
  public int GAROTitleId { get; set; } = TitleUtils.TitleIds.None;

  public enum ClassModeOption
  {
    InheritJobTitles,
    ShowClasses,
  }
}

[Serializable]
public class Configuration : IPluginConfiguration
{
  public int Version { get; set; } = 0;
  public Dictionary<ulong, CharacterConfig> CharacterConfigs { get; set; } = new();
  public Language Language { get; set; } = Language.None;
  public bool PrintTitleChangesInChat { get; set; } = false;
  public bool Debug { get; set; } = false;

  public void Save() =>
    Plugin.PluginInterface.SavePluginConfig(this);

  public static Configuration Load() =>
    Plugin.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

  public static CharacterConfig GetCharacterConfig()
  {
    ulong localContentId = Plugin.ClientState.LocalContentId;
    if (localContentId == 0)
    {
      Logger.Debug("Function was called before character was logged in, returning temporary config.");
      return new CharacterConfig();
    }

    if (!Plugin.Configuration.CharacterConfigs.TryGetValue(localContentId, out CharacterConfig? characterConfig))
    {
      Logger.Debug($"No configuration was found for {localContentId}, creating one.");
      characterConfig = new CharacterConfig();
      Plugin.Configuration.CharacterConfigs[localContentId] = characterConfig;
      Plugin.Configuration.Save();
    }

    return characterConfig;
  }

  public static void SaveCharacterConfig(CharacterConfig characterConfig)
  {
    ulong localContentId = Plugin.ClientState.LocalContentId;
    if (localContentId == 0)
    {
      Logger.Debug("Function was called before character was logged in, returning temporary config.");
      return;
    }

    Plugin.Configuration.CharacterConfigs[localContentId] = characterConfig;
    Plugin.Configuration.Save();
  }
}
