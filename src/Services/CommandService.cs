using Dalamud.Game.Command;

namespace JobTitles.Services;

public interface ICommandService : IHostedService;

public class CommandService(
  ILoc _loc,
  ILogger _logger,
  ConfigWindow _configWindow,
  ITitleService _titleService,
  ICommandManager _commandManager) : ICommandService
{
  private readonly string JobTitlesCommand = "/jobtitles";
  private readonly string JobTitlesCommandAlias = "/jt";

  public Task StartAsync(CancellationToken cancellationToken)
  {
    _commandManager.AddHandler(JobTitlesCommand, new CommandInfo(OnCommand)
    {
      HelpMessage = $"See '{JobTitlesCommand} help' for more."
    });
    _commandManager.AddHandler(JobTitlesCommandAlias, new CommandInfo(OnCommand)
    {
      HelpMessage = $"Alias for {JobTitlesCommand}."
    });

    return _logger.ServiceLifecycle();
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    _commandManager.RemoveHandler(JobTitlesCommand);
    _commandManager.RemoveHandler(JobTitlesCommandAlias);

    return _logger.ServiceLifecycle();
  }

  private void OnCommand(string command, string arguments)
  {
    _logger.Debug($"command::'{command}' arguments::'{arguments}'");

    string[] args = arguments.Split(" ", StringSplitOptions.RemoveEmptyEntries);
    if (args.Length == 0)
    {
      _configWindow.Toggle();
      return;
    }

    switch (args[0])
    {
      case "reapply":
        (bool success, string title) = _titleService.UpdateTitle();
        if (success)
          _logger.Chat(_loc.Get(Loc.Phrase.ReappliedTitle), title);
        else
          _logger.Chat(_loc.Get(Loc.Phrase.FailedToReapplyTitle));
        break;
      case "help":
        _logger.Chat(_loc.Get(Loc.Phrase.AvailableComands));
        _logger.Chat($"  {command} help - Display this menu menu");
        _logger.Chat($"  {command} version - Print the plugin's version");
        _logger.Chat($"  {command} reapply - Try to reapply the current title");
        _logger.Chat($"  {command} - Toggle the configuration window");
        break;
      default:
        _logger.Chat(_loc.Get(Loc.Phrase.InvalidCommand));
        _logger.Chat($"  {command} {arguments}");
        goto case "help";
    }
  }
}
