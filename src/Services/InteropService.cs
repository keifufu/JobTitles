using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Network;

namespace JobTitles.Services;

public interface IInteropService : IHostedService;

public class InteropService(
  ILogger _logger,
  IDataManager _dataManager,
  IObjectTable _objectTable,
  ITitleService _titleService,
  IGameInteropProvider _interopProvider) : IInteropService
{
  private Hook<PacketDispatcher.Delegates.HandleActorControlPacket>? _handleActorControlPacketHook;
  private static readonly uint AchievementCategory = 0x203;

  public Task StartAsync(CancellationToken cancellationToken)
  {
    _handleActorControlPacketHook = _interopProvider.HookFromAddress<PacketDispatcher.Delegates.HandleActorControlPacket>(
      PacketDispatcher.Addresses.HandleActorControlPacket.Value,
      HandleActorControlPacketDetour
    );
    _handleActorControlPacketHook.Enable();

    return _logger.ServiceLifecycle();
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    _handleActorControlPacketHook?.Dispose();

    return _logger.ServiceLifecycle();
  }

  private void HandleActorControlPacketDetour(
    uint entityId,
    uint category,
    uint achievementId,
    uint arg2, uint arg3, uint arg4, uint arg5, uint arg6, uint arg7, uint arg8,
    GameObjectId targetId,
    bool isRecorded)
  {
    _handleActorControlPacketHook!.Original(
      entityId, category, achievementId, arg2, arg3, arg4, arg5, arg6, arg7, arg8, targetId, isRecorded);

    // While we do fetch and cache unlocked titles upon opening `ConfigWindow`, this makes sure
    // the cache stays updated even if the user does not interact with the `ConfigWindow` for a
    // long amount of time.
    if (category != AchievementCategory || _objectTable.LocalPlayer?.EntityId != entityId) return;
    if (!_dataManager.Excel.GetSheet<Achievement>().TryGetRow(achievementId, out Achievement achievementRow))
    {
      _logger.Debug($"achievementId::{achievementId} is invalid");
      return;
    }

    _logger.Debug($"achievementId::{achievementId} is valid");
    TitleId titleId = TitleService.ToTitleId(achievementRow.Title.Value.RowId);
    if (titleId != TitleService.TitleIds.None)
    {
      _logger.Debug($"achievementId::{achievementId} has valid titleId::{titleId}");
      _titleService.AddTitleIdToCache(titleId);
    }
    else
    {
      _logger.Debug($"achievementId::{achievementId} has no associated title.");
    }
  }
}
