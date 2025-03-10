using Dalamud.Hooking;
using Dalamud.Utility.Signatures;

namespace JobTitles.Services;

public class InteropService : IHostedService
{
  private readonly Logger Logger;
  private readonly TitleService TitleService;
  private readonly IGameInteropProvider InteropProvider;
  private readonly IDataManager DataManager;

  public InteropService(Logger logger, TitleService titleService, IGameInteropProvider interopProvider, IDataManager dataManager)
  {
    Logger = logger;
    TitleService = titleService;
    InteropProvider = interopProvider;
    DataManager = dataManager;
  }

  public Task StartAsync(CancellationToken cancellationToken)
  {
    InteropProvider.InitializeFromAttributes(this);
    ActorControlSelfHook.Enable();

    Logger.Debug("InteropService started");
    return Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    ActorControlSelfHook.Dispose();

    Logger.Debug("InteropService stopped");
    return Task.CompletedTask;
  }

  private unsafe delegate void ActorControlSelfDelegate(
    uint entityId, uint id, uint arg0, uint arg1, uint arg2, uint arg3, uint arg4, uint arg5, ulong targetId, byte a10);

  [Signature("E8 ?? ?? ?? ?? 0F B7 0B 83 E9 64", DetourName = nameof(ActorControlSelfDetour))]
  private readonly Hook<ActorControlSelfDelegate> ActorControlSelfHook = null!;

  private static readonly uint ActorControlSelfAchievementId = 0x203;

  private void ActorControlSelfDetour(uint entityId, uint id, uint arg0, uint arg1, uint arg2, uint arg3, uint arg4, uint arg5, ulong targetId, byte a10)
  {
    ActorControlSelfHook.Original(entityId, id, arg0, arg1, arg2, arg3, arg4, arg5, targetId, a10);
    // Logger.Debug($"ActorControlSelf ({entityId} {id} {arg0} {arg1} {arg2} {arg3} {arg4} {arg5} {targetId} {a10})");

    // While we do fetch and cache unlocked titles upon opening `ConfigWindow`, this makes sure
    // the cache stays updated even if the user does not interact with the `ConfigWindow` for a
    // long amount of time.
    if (id == ActorControlSelfAchievementId)
    {
      Logger.Debug($"ActorControlSelf::Achievement ({entityId} {id} {arg0} {arg1} {arg2} {arg3} {arg4} {arg5} {targetId} {a10})");
      uint achievementId = arg0;
      if (DataManager.Excel.GetSheet<Achievement>().TryGetRow(achievementId, out Achievement achievementRow))
      {
        Logger.Debug($"ActorControlSelf::Achievement achievementId::{achievementId} is valid");
        TitleId titleId = TitleService.ToTitleId(achievementRow.Title.Value.RowId);
        if (titleId != TitleService.TitleIds.None)
        {
          Logger.Debug($"ActorControlSelf::Achievement achievementId::{achievementId} has valid titleId::{titleId}");
          TitleService.AddTitleIdToCache(titleId);
        }
        else
        {
          Logger.Debug($"ActorControlSelf::Achievement achievementId::{achievementId} has no associated title.");
        }
      }
      else
      {
        Logger.Debug($"ActorControlSelf::Achievement achievementId::{achievementId} is invalid");
      }
    }
  }
}
