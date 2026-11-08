using KL.ActionsSystem;

namespace 伊蕾娜.ElainaActions;

public sealed class ElainaActionDrawSystem : ModSystem
{
    public override void Load()
    {
        On_Main.CacheProjDraws += OnCacheProjDraws;
    }

    public override void Unload()
    {
        On_Main.CacheProjDraws -= OnCacheProjDraws;
    }

    private static void OnCacheProjDraws(On_Main.orig_CacheProjDraws orig, Main self)
    {
        orig(self);

        foreach (Player player in Main.player)
        {
            if (!player.active || player.dead)
            {
                continue;
            }

            ActionModPlayer actionPlayer = player.GetModPlayer<ActionModPlayer>();
            if (actionPlayer.CurrentAnimAction is ElainaAction action)
            {
                action.SubmitActionVisuals(actionPlayer);
            }
        }
    }
}
