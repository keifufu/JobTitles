using Dalamud.Configuration;

namespace JobTitles;

[Serializable]
public class CharacterConfig
{
  public Dictionary<JobService.Job, TitleId> JobTitleMappingsV2 { get; set; } = new();
  public List<byte> CachedTitlesUnlockBitmask { get; set; } = new();
  public ClassModeOption ClassMode { get; set; } = ClassModeOption.InheritJobTitles;
  public bool UseGAROTitleInPvP { get; set; } = false;
  public bool TryUseGAROTitleForCurrentJob { get; set; } = false;
  public TitleId GAROTitleIdV2 { get; set; } = TitleService.TitleIds.DoNotOverride;

  public enum ClassModeOption
  {
    InheritJobTitles,
    ShowClasses,
  }

  // Migrated to JobTitleMappingsV2 in v2
  public Dictionary<uint, int> JobTitleMappings { get; set; } = new();
  // Migrated to GAROTitleIdV2 in v2
  public int GAROTitleId { get; set; } = -1; // V1: TitleIds.DoNotOverride = -1
}

[Serializable]
public class Configuration : IPluginConfiguration
{
  public int Version { get; set; } = 2;
  public Dictionary<ulong, CharacterConfig> CharacterConfigs { get; set; } = new();
  public Language Language { get; set; } = Language.None;
  public bool PrintTitleChangesInChat { get; set; } = false;
  public bool Debug { get; set; } = false;

  [NonSerialized]
  private Logger? Logger;
  [NonSerialized]
  private IDalamudPluginInterface? PluginInterface;
  [NonSerialized]
  private IClientState? ClientState;

  public void Initialize(Logger logger, IDalamudPluginInterface pluginInterface, IClientState clientState)
  {
    Logger = logger;
    PluginInterface = pluginInterface;
    ClientState = clientState;

    Logger.Configuration = this;
    ConfigurationMigrator.Migrate(this, Logger!);
  }

  public void Save() => PluginInterface!.SavePluginConfig(this);

  public CharacterConfig GetCharacterConfig()
  {
    ulong localContentId = ClientState!.LocalContentId;
    if (localContentId == 0)
    {
      Logger!.Error("Not logged in, returning temporary character config.");
      return new CharacterConfig();
    }

    if (!CharacterConfigs.TryGetValue(localContentId, out CharacterConfig? characterConfig))
    {
      Logger!.Debug($"Found no configuration for CID::{localContentId}, creating one.");
      characterConfig = new CharacterConfig();
      CharacterConfigs.TryAdd(localContentId, characterConfig);
      Save();
    }

    return characterConfig;
  }

  public void SaveCharacterConfig(CharacterConfig characterConfig)
  {
    ulong localContentId = ClientState!.LocalContentId;
    if (localContentId == 0)
    {
      Logger!.Error("Not logged in, not saving character config.");
      return;
    }

    CharacterConfigs[localContentId] = characterConfig;
    Save();
  }
}

public static class ConfigurationMigrator
{
  public static void Migrate(Configuration configuration, Logger logger)
  {
    if (configuration.Version == 0)
    {
      MigrateV0ToV1(configuration, logger);
      MigrateV1ToV2(configuration, logger);
    }
    else if (configuration.Version == 1)
    {
      MigrateV1ToV2(configuration, logger);
    }
    else
    {
      logger.Debug($"Configuration up-to-date: v{configuration.Version}");
    }
  }

  private static void MigrateV0ToV1(Configuration configuration, Logger logger)
  {
    // Migrated from using TitleIds.None as the GAROTitleId default value to TitleIds.DoNotOverride
    logger.Debug($"Migrating configuration using {nameof(MigrateV0ToV1)}");

    foreach (CharacterConfig characterConfig in configuration.CharacterConfigs.Values)
    {
      if (characterConfig.GAROTitleId == 0) // V1: TitleIds.None = 0
        characterConfig.GAROTitleId = -1; // V1: TitleIds.DoNotOverride = -1
    }

    configuration.Version = 1;
    configuration.Save();
  }

  private static void MigrateV1ToV2(Configuration configuration, Logger logger)
  {
    // Migrated from using int as titleId to using TitleId (global alias for ushort)
    // and using JobService.Job as the key in JobTitleMappings instead of uint
    logger.Debug($"Migrating configuration using {nameof(MigrateV1ToV2)}");

    foreach (CharacterConfig characterConfig in configuration.CharacterConfigs.Values)
    {
      foreach (KeyValuePair<uint, int> mapping in characterConfig.JobTitleMappings)
      {
        if (mapping.Value == -1) // V1: TitleIds.DoNotOverride = -1
          characterConfig.JobTitleMappingsV2[JobService.ToJob(mapping.Key)] = TitleService.TitleIds.DoNotOverride;
        else
          characterConfig.JobTitleMappingsV2[JobService.ToJob(mapping.Key)] = TitleService.ToTitleId((uint)mapping.Value);
      }

      characterConfig.JobTitleMappings = new();

      if (characterConfig.GAROTitleId == -1) // V1: TitleIds.DoNotOverride = -1
        characterConfig.GAROTitleIdV2 = TitleService.TitleIds.DoNotOverride;
      else
        characterConfig.GAROTitleIdV2 = TitleService.ToTitleId((uint)characterConfig.GAROTitleId);

      characterConfig.GAROTitleId = 0;
    }

    configuration.Version = 2;
    configuration.Save();
  }
}
