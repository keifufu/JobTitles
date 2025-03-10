using Dalamud.Game;

namespace JobTitles;

public class Loc
{
  private readonly Configuration Configuration;
  private readonly IClientState ClientState;

  public Loc(Configuration configuration, IClientState clientState)
  {
    Configuration = configuration;
    ClientState = clientState;
  }

  public Language Language
  {
    get => Configuration.Language == Language.None ? GetClientLanguage() : Configuration.Language;
  }

  private Language GetClientLanguage() => ClientState.ClientLanguage switch
  {
    ClientLanguage.Japanese => Language.Japanese,
    ClientLanguage.English => Language.English,
    ClientLanguage.German => Language.German,
    ClientLanguage.French => Language.French,
    _ => Language.English,
  };

  public enum Phrase
  {
    PleaseLogIn,
    JobNameTooltip,
    None,
    DoNotOverride,
    Search,
    Tanks,
    Healers,
    Melee,
    Ranged,
    Crafters,
    Gatherers,
    Options,
    ClientLanguage,
    English,
    German,
    NoResults,
    Language,
    ClassMode,
    ClassModeTooltip,
    InheritJobTitles,
    ShowClasses,
    Debug,
    PrintTitleChangesInChat,
    TitleChangedTo,
    Other,
    PvP,
    UseGAROTitleInPvP,
    TryUseGAROTitleForCurrentJob,
    SetTitleToX,
    Yes,
    No,
    PvPTooltip,
    SelectTitle,
    NoGAROTitlesUnlocked,
    ReappliedTitle,
    FailedToReapplyTitle,
    AvailableComands,
    InvalidCommand
  }

  private readonly Dictionary<Language, Dictionary<Phrase, string>> Translations = new()
  {
    { Language.English, new Dictionary<Phrase, string>
      {
        { Phrase.PleaseLogIn, "Please log in to start configuring JobTitles." },
        { Phrase.JobNameTooltip, "Configure title used for {0}.\nDo not override - does not update title when you switch to this job\nNone - Clears your title" },
        { Phrase.None, "None" },
        { Phrase.DoNotOverride, "Do not override" },
        { Phrase.Search, "Search" },
        { Phrase.Tanks, "Tanks" },
        { Phrase.Healers, "Healers" },
        { Phrase.Melee, "Melee" },
        { Phrase.Ranged, "Ranged" },
        { Phrase.Crafters, "Crafters" },
        { Phrase.Gatherers, "Gatherers" },
        { Phrase.Options, "Options" },
        { Phrase.ClientLanguage, "Client Language" },
        { Phrase.English, "English" },
        { Phrase.German, "German" },
        { Phrase.NoResults, "No Results" },
        { Phrase.Language, "Language" },
        { Phrase.ClassMode, "Class Mode" },
        { Phrase.ClassModeTooltip, "Whether to allow for class titles to be set independently,\nor for them to inherit from the job they upgrade to." },
        { Phrase.InheritJobTitles, "Inherit class titles from jobs" },
        { Phrase.ShowClasses, "Show classes separately" },
        { Phrase.Debug, "Enable Debug Logging" },
        { Phrase.PrintTitleChangesInChat, "Print Title Changes in Chat" },
        { Phrase.TitleChangedTo, "Title changed to:" },
        { Phrase.Other, "Other" },
        { Phrase.PvP, "PvP" },
        { Phrase.UseGAROTitleInPvP, "Use GARO Title in PvP" },
        { Phrase.TryUseGAROTitleForCurrentJob, "Use GARO Title for current Job when possible" },
        { Phrase.SetTitleToX, "Set title to '{0}' ?"},
        { Phrase.Yes, "Yes"},
        { Phrase.No, "No"},
        { Phrase.PvPTooltip, "Prompts you to change your Title upon entering a PvP duty.\nUseful for GARO achievements."},
        { Phrase.SelectTitle, "Select Title"},
        { Phrase.NoGAROTitlesUnlocked, "No GARO Titles Unlocked"},
        { Phrase.ReappliedTitle, "Reapplied Title: "},
        { Phrase.FailedToReapplyTitle, "Failed to reapply title; see /xllog for more."},
        { Phrase.AvailableComands, "Available commands:"},
        { Phrase.InvalidCommand, "Invalid command:"},
      }
    },
    { Language.German, new Dictionary<Phrase, string>
      {
        { Phrase.PleaseLogIn, "Bitte logge dich ein um JobTitles zu konfigurieren." },
        { Phrase.JobNameTooltip, "Konfiguriere den Titel für {0}.\nNicht ersetzen - verändert den Titel nicht wenn du die Klasse wechselst\nKeinen Titel - Entfernt deinen Titel" },
        { Phrase.None, "Keinen Titel" },
        { Phrase.DoNotOverride, "Nicht ersetzen" },
        { Phrase.Search, "Suchen" },
        { Phrase.Tanks, "Verteidiger" },
        { Phrase.Healers, "Heiler" },
        { Phrase.Melee, "Nahkampf" },
        { Phrase.Ranged, "Fernkampf" },
        { Phrase.Crafters, "Handwerker" },
        { Phrase.Gatherers, "Sammler" },
        { Phrase.Options, "Optionen" },
        { Phrase.ClientLanguage, "Clientsprache" },
        { Phrase.English, "Englisch" },
        { Phrase.German, "Deutsch" },
        { Phrase.NoResults, "Keine Ergebnisse" },
        { Phrase.Language, "Sprache" },
        { Phrase.ClassMode, "Klassen Modus" },
        { Phrase.ClassModeTooltip, "Ob Klassentitel unabhängig festgelegt werden dürfen,\norder ob sie von dem Job übernommen werden, zu dem Sie wechseln." },
        { Phrase.InheritJobTitles, "Klassentitel von Jobs übernehmen" },
        { Phrase.ShowClasses, "Klassen separat anzeigen" },
        { Phrase.Debug, "Debug-Logging aktivieren" },
        { Phrase.PrintTitleChangesInChat, "Titeländerungen im Chat drucken" },
        { Phrase.TitleChangedTo, "Titel geändert zu:" },
        { Phrase.Other, "Sonstiges" },
        { Phrase.PvP, "PvP" },
        { Phrase.UseGAROTitleInPvP, "GARO Titel in PvP benutzen" },
        { Phrase.TryUseGAROTitleForCurrentJob, "GARO Title für den aktuellen Job benutzen wenn möglich" },
        { Phrase.SetTitleToX, "Titel zu '{0}' ändern?"},
        { Phrase.Yes, "Ja"},
        { Phrase.No, "Nein"},
        { Phrase.PvPTooltip, "Fordert dich beim Betreten einer PvP duty auf, deinen Titel zu ändern.\nNützlich für GARO-Erfolge."},
        { Phrase.SelectTitle, "Titel auswählen"},
        { Phrase.NoGAROTitlesUnlocked, "Keine GARO Titel verfügbar"},
        { Phrase.ReappliedTitle, "Titel erneut gesetzt: "},
        { Phrase.FailedToReapplyTitle, "Titel konnte nicht erneut gesetzt werden. Weitere Informationen unter /xllog."},
        { Phrase.AvailableComands, "Verfügbare Befehle:"},
        { Phrase.InvalidCommand, "Ungültiger Befehl:"},
      }
    },
  };

  public string GetLanguageName(Language language) => language switch
  {
    Language.None => Get(Phrase.ClientLanguage),
    Language.English => Get(Phrase.English),
    Language.German => Get(Phrase.German),
    _ => Get(Phrase.ClientLanguage)
  };

  public string Get(Phrase phrase) =>
    Translations.TryGetValue(Language, out var translations)
    && translations.TryGetValue(phrase, out var translation)
      ? translation
      : GetFallbackTranslation(phrase);

  public string GetFallbackTranslation(Phrase phrase) =>
    Translations.TryGetValue(Language.English, out var defaultTranslations)
    && defaultTranslations.TryGetValue(phrase, out var defaultTranslation)
      ? defaultTranslation
      : phrase.ToString();
}
