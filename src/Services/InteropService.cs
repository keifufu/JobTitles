using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Network;

namespace JobTitles.Services;

public class InteropService : IHostedService
{
  private readonly Logger Logger;
  private readonly TitleService TitleService;
  private readonly IGameInteropProvider InteropProvider;
  private readonly IDataManager DataManager;
  private readonly IObjectTable ObjectTable;

  private Hook<PacketDispatcher.Delegates.HandleActorControlPacket>? _handleActorControlPacketHook;

  public InteropService(Logger logger, TitleService titleService, IGameInteropProvider interopProvider, IDataManager dataManager, IObjectTable objectTable)
  {
    Logger = logger;
    TitleService = titleService;
    InteropProvider = interopProvider;
    DataManager = dataManager;
    ObjectTable = objectTable;
  }

  public Task StartAsync(CancellationToken cancellationToken)
  {
    _handleActorControlPacketHook ??= InteropProvider.HookFromAddress<PacketDispatcher.Delegates.HandleActorControlPacket>(
      PacketDispatcher.Addresses.HandleActorControlPacket.Value,
      HandleActorControlPacketDetour
    );

    if (!_handleActorControlPacketHook.IsEnabled)
      _handleActorControlPacketHook.Enable();

    Logger.Debug("InteropService started");
    return Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    _handleActorControlPacketHook?.Dispose();

    Logger.Debug("InteropService stopped");
    return Task.CompletedTask;
  }

  private static readonly uint AchievementCategory = 0x203;

  private void HandleActorControlPacketDetour(uint entityId, uint category, uint arg1, uint arg2, uint arg3, uint arg4, uint arg5, uint arg6, uint arg7, uint arg8, GameObjectId targetId, bool isRecorded)
  {
    _handleActorControlPacketHook!.Original(entityId, category, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, targetId, isRecorded);
    // Logger.Debug($"ActorControlSelf ({entityId} {category} {arg1} {arg2} {arg3} {arg4} {arg5} {arg6} {arg7} {arg8} {targetId} {isRecorded})");

    // While we do fetch and cache unlocked titles upon opening `ConfigWindow`, this makes sure
    // the cache stays updated even if the user does not interact with the `ConfigWindow` for a
    // long amount of time.
    if (category == AchievementCategory && ObjectTable.LocalPlayer?.EntityId == entityId)
    {
      uint achievementId = arg1;
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
