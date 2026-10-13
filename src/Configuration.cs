using Dalamud.Configuration;

namespace JobTitles;

[Serializable]
public class CharacterConfig
{
  public Dictionary<JobService.Job, TitleId> JobTitleMappingsV2 { get; set; } = [];
  public List<byte> CachedTitlesUnlockBitmask { get; set; } = [];
  public ClassModeOption ClassMode { get; set; } = ClassModeOption.InheritJobTitles;
  public bool UseGAROTitleInPvP { get; set; }
  public bool TryUseGAROTitleForCurrentJob { get; set; }
  public TitleId GAROTitleIdV2 { get; set; } = TitleService.TitleIds.DoNotOverride;

  public enum ClassModeOption
  {
    InheritJobTitles,
    ShowClasses,
  }

  // Migrated to JobTitleMappingsV2 in v2
  public Dictionary<uint, int> JobTitleMappings { get; set; } = [];
  // Migrated to GAROTitleIdV2 in v2
  public int GAROTitleId { get; set; } = -1; // V1: TitleIds.DoNotOverride = -1
}

[Serializable]
public class Configuration : IPluginConfiguration
{
  public int Version { get; set; } = 2;
  public Dictionary<ulong, CharacterConfig> CharacterConfigs { get; set; } = [];
  public Language Language { get; set; } = Language.None;
  public bool PrintTitleChangesInChat { get; set; }
  public bool Debug { get; set; }

  [NonSerialized]
  private ILogger _logger = null!;
  [NonSerialized]
  private IDalamudPluginInterface _pluginInterface = null!;
  [NonSerialized]
  private IPlayerState _playerState = null!;

  public void Initialize(ILogger logger, IDalamudPluginInterface pluginInterface, IPlayerState playerState)
  {
    _logger = logger;
    _pluginInterface = pluginInterface;
    _playerState = playerState;

    _logger.SetConfiguration(this);
    ConfigurationMigrator.Migrate(this, _logger);
  }

  public void Save() => _pluginInterface.SavePluginConfig(this);

  public CharacterConfig GetCharacterConfig()
  {
    ulong localContentId = _playerState.ContentId;
    if (localContentId == 0)
    {
      _logger.Error("Not logged in, returning temporary character config.");
      return new CharacterConfig();
    }

    if (!CharacterConfigs.TryGetValue(localContentId, out CharacterConfig? characterConfig))
    {
      _logger.Debug($"Found no configuration for CID::{localContentId}, creating one.");
      characterConfig = new CharacterConfig();
      CharacterConfigs.TryAdd(localContentId, characterConfig);
      Save();
    }

    return characterConfig;
  }

  public void SaveCharacterConfig(CharacterConfig characterConfig)
  {
    ulong localContentId = _playerState.ContentId;
    if (localContentId == 0)
    {
      _logger.Error("Not logged in, not saving character config.");
      return;
    }

    CharacterConfigs[localContentId] = characterConfig;
    Save();
  }
}

public static class ConfigurationMigrator
{
  public static void Migrate(Configuration configuration, ILogger logger)
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

  private static void MigrateV0ToV1(Configuration configuration, ILogger logger)
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

  private static void MigrateV1ToV2(Configuration configuration, ILogger logger)
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

      characterConfig.JobTitleMappings = [];

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
