using System.Collections.Generic;
using Dalamud.Game;
using Lumina.Data;

namespace JobTitles.Utils;

public class Loc
{
  public static Language Language = GetClientLanguage();

  public enum Phrase
  {
    PleaseLogIn,
    RequestTitleList,
    RequestTitleListDescription,
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
  }

  private static readonly Dictionary<Language, Dictionary<Phrase, string>> Translations = new()
  {
    { Language.English, new Dictionary<Phrase, string>
      {
        { Phrase.PleaseLogIn, "Please log in to start configuring JobTitles." },
        { Phrase.RequestTitleList, "Request Title List" },
        { Phrase.RequestTitleListDescription, "Click the button below to load your unlocked titles." },
        { Phrase.JobNameTooltip, $"Configure title used for %s.\nDo not override - does not update title when you switch to this job\nNone - Clears your title" },
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
      }
    },
    { Language.German, new Dictionary<Phrase, string>
      {
        { Phrase.PleaseLogIn, "Bitte logge dich ein um JobTitles zu konfigurieren." },
        { Phrase.RequestTitleList, "Titelliste anfordern" },
        { Phrase.RequestTitleListDescription, "Den Knopf drücken um Ihre freigeschalteten Titel zu laden." },
        { Phrase.JobNameTooltip, $"Konfiguriere den Titel für %s.\nNicht ersetzen - verändert den Titel nicht wenn du die Klasse wechselst\nKeinen Titel - Entfernt deinen Titel" },
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
      }
    },
  };

  public static Language GetClientLanguage() => Plugin.ClientState.ClientLanguage switch
  {
    ClientLanguage.Japanese => Language.Japanese,
    ClientLanguage.English => Language.English,
    ClientLanguage.German => Language.German,
    ClientLanguage.French => Language.French,
    _ => Language.English,
  };

  public static void SetLanguage(Language language) =>
    Language = (language == Language.None)
      ? GetClientLanguage()
      : language;

  public static string GetLanguageName(Language language) => language switch
  {
    Language.None => Get(Phrase.ClientLanguage),
    Language.English => Get(Phrase.English),
    Language.German => Get(Phrase.German),
    _ => Get(Phrase.ClientLanguage)
  };

  public static string Get(Phrase phrase) =>
    Translations.TryGetValue(Language, out var translations)
    && translations.TryGetValue(phrase, out var translation)
      ? translation
      : GetFallbackTranslation(phrase);

  public static string GetFallbackTranslation(Phrase phrase) =>
    Translations.TryGetValue(Language.English, out var defaultTranslations)
    && defaultTranslations.TryGetValue(phrase, out var defaultTranslation)
      ? defaultTranslation
      : phrase.ToString();
}

