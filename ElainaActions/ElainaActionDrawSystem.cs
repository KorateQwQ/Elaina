using KL.ActionsSystem;
using System.Collections.Generic;

namespace 伊蕾娜.ElainaActions;

public sealed class ElainaActionDrawSystem : ModSystem
{
    private static readonly List<ElainaTrailAfterimage> afterimages = new();

    internal static void RetainTrails(Player player, List<ElainaTrailSnapshot> trails)
    {
        if (!Main.dedServ && player.active && !player.dead && trails.Count > 0)
            afterimages.Add(new ElainaTrailAfterimage(player.whoAmI, trails));
    }

    public override void PostUpdatePlayers()
    {
        if (Main.dedServ || Main.gamePaused)
            return;
        for (int i = afterimages.Count - 1; i >= 0; i--)
        {
            Player player = Main.player[afterimages[i].PlayerIndex];
            if (!player.active || player.dead || !afterimages[i].Update())
                afterimages.RemoveAt(i);
        }
        // 所有玩家的动作与保持姿势更新完毕后，每个游戏 tick 只记录一次。
        foreach (Player player in Main.player)
            if (player.active && !player.dead
                && player.GetModPlayer<ActionModPlayer>().CurrentAnimAction is ElainaAction action)
                action.UpdateWandTrail();
    }

    public override void OnWorldUnload() => afterimages.Clear();
    public override void Load()
    {
        On_Main.CacheProjDraws += OnCacheProjDraws;
    }

    public override void Unload()
    {
        On_Main.CacheProjDraws -= OnCacheProjDraws;
        afterimages.Clear();
    }

    private static void OnCacheProjDraws(On_Main.orig_CacheProjDraws orig, Main self)
    {
        orig(self);

        foreach (ElainaTrailAfterimage trail in afterimages)
        {
            Player player = Main.player[trail.PlayerIndex];
            if (player.active && !player.dead)
                trail.RequestDraw();
        }

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
