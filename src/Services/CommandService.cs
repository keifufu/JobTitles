using Dalamud.Game.Command;

namespace JobTitles.Services;

public class CommandService : IHostedService
{
  private string JobTitlesCommand = "/jobtitles";
  private string JobTitlesCommandAlias = "/jt";

  private readonly Loc Loc;
  private readonly Logger Logger;
  private readonly TitleService TitleService;
  private readonly ICommandManager CommandManager;
  private readonly ConfigWindow ConfigWindow;

  public CommandService(Loc loc, Logger logger, TitleService titleService, ICommandManager commandManager, ConfigWindow configWindow)
  {
    Loc = loc;
    Logger = logger;
    TitleService = titleService;
    CommandManager = commandManager;
    ConfigWindow = configWindow;
  }

  public Task StartAsync(CancellationToken cancellationToken)
  {
    CommandManager.AddHandler(JobTitlesCommand, new CommandInfo(OnCommand)
    {
      HelpMessage = "See '/jobtitles help' for more."
    });
    CommandManager.AddHandler(JobTitlesCommandAlias, new CommandInfo(OnCommand)
    {
      HelpMessage = $"Alias for {JobTitlesCommand}."
    });

    Logger.Debug("EventService started");
    return Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    CommandManager.RemoveHandler(JobTitlesCommand);
    CommandManager.RemoveHandler(JobTitlesCommandAlias);

    Logger.Debug("EventService stopped");
    return Task.CompletedTask;
  }

  private void OnCommand(string command, string arguments)
  {
    Logger.Debug($"command::'{command}' arguments::'{arguments}'");

    string[] args = arguments.Split(" ", StringSplitOptions.RemoveEmptyEntries);
    if (args.Length == 0)
    {
      ConfigWindow.Toggle();
      return;
    }

    switch (args[0])
    {
      case "reapply":
        (bool success, string title) = TitleService.UpdateTitle();
        if (success)
          Logger.Chat(Loc.Get(Loc.Phrase.ReappliedTitle), title);
        else
          Logger.Chat(Loc.Get(Loc.Phrase.FailedToReapplyTitle));
        break;
      case "help":
        Logger.Chat(Loc.Get(Loc.Phrase.AvailableComands));
        Logger.Chat($"  {command} reapply");
        Logger.Chat($"  {command} help");
        Logger.Chat($"  {command}");
        break;
      default:
        Logger.Chat(Loc.Get(Loc.Phrase.InvalidCommand));
        Logger.Chat($"  {command} {arguments}");
        goto case "help";
    }
  }
}
