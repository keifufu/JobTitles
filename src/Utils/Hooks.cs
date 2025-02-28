using System;
using Dalamud.Hooking;
using Lumina.Excel.Sheets;

namespace JobTitles.Utils;

public class Hooks : IDisposable
{
  private readonly string ActorControlSelfSig = "E8 ?? ?? ?? ?? 0F B7 0B 83 E9 64";
  private delegate void ActorControlSelfDelegate(uint entityId, uint id, uint arg0, uint arg1, uint arg2, uint arg3, uint arg4, uint arg5, ulong targetId, byte a10);
  private readonly Hook<ActorControlSelfDelegate> ActorControlSelfHook;
  private readonly uint ActorControlSelfAchivementId = 0x203;

  public Hooks()
  {
    ActorControlSelfHook = Plugin.InteropProvider.HookFromSignature<ActorControlSelfDelegate>(ActorControlSelfSig, ActorControlSelf);
    ActorControlSelfHook.Enable();
  }

  public void Dispose()
  {
    ActorControlSelfHook?.Dispose();
  }

  private void ActorControlSelf(uint entityId, uint id, uint arg0, uint arg1, uint arg2, uint arg3, uint arg4, uint arg5, ulong targetId, byte a10)
  {
    ActorControlSelfHook.Original(entityId, id, arg0, arg1, arg2, arg3, arg4, arg5, targetId, a10);

    // While we do fetch and cache unlocked titles upon opening `ConfigWindow`, this makes sure
    // the cache stays updated even if the user does not interact with the `ConfigWindow` for a
    // long amount of time.
    if (id == ActorControlSelfAchivementId)
    {
      uint achivementId = arg0;
      if (Plugin.DataManager.Excel.GetSheet<Achievement>(Loc.Language).TryGetRow(achivementId, out var achievementRow))
      {
        uint titleId = achievementRow.Title.Value.RowId;
        if (titleId != 0)
        {
          TitleUtils.AddTitleIdToCache(titleId);
        }
      }
    }
  }
}
