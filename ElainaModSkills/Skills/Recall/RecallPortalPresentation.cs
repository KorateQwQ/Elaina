using System.Collections.Generic;
using static 伊蕾娜.Localization.UIText;

namespace 伊蕾娜.ElainaModSkills.Skills.Recall;

internal static class RecallPortalPresentation
{
    private static readonly List<RecallPortalProjectile> observed = [];
    private static readonly List<RecallPortalLayout.Entry> snapshot = [], arranged = [];

    // 原始阵心、成员或配对键变化才重排。绘制、命中和服务器距离校验共用同一个布局。
    internal static void Refresh()
    {
        int count = 0;
        bool changed = false;
        foreach (Projectile projectile in Main.ActiveProjectiles)
        {
            if (projectile.ModProjectile is not RecallPortalProjectile portal) continue;
            if (count >= observed.Count || !ReferenceEquals(observed[count], portal) ||
                snapshot[count].Anchor != projectile.Center || snapshot[count].Owner != projectile.owner ||
                snapshot[count].Pair != portal.PairId || snapshot[count].Side != portal.Side) changed = true;
            count++;
        }
        if (!changed && count == observed.Count) return;
        Clear();
        foreach (Projectile projectile in Main.ActiveProjectiles)
        {
            if (projectile.ModProjectile is not RecallPortalProjectile portal) continue;
            observed.Add(portal);
            var entry = new RecallPortalLayout.Entry(projectile.owner, portal.PairId, portal.Side, projectile.Center);
            snapshot.Add(entry);
            arranged.Add(entry);
        }
        RecallPortalLayout.Arrange(arranged, Main.maxTilesX * 16f);
        for (int i = 0; i < observed.Count; i++) observed[i].DisplayCenter = snapshot[i].Center;
    }

    internal static void Clear() { observed.Clear(); snapshot.Clear(); arranged.Clear(); }

    internal static bool InReach(Player player, RecallPortalProjectile portal) =>
        RecallPortalSystem.InArea(player, portal.DisplayCenter);

    internal static bool CanTravel(Player player, RecallPortalProjectile portal)
    {
        int owner = portal.Projectile.owner;
        if (owner < 0 || owner >= Main.maxPlayers || !portal.Ready ||
            !RecallPortalSystem.IsAlly(Main.player[owner], player) || !InReach(player, portal)) return false;
        int other = RecallPortalSystem.FindPortal(owner, portal.PairId, 1 - portal.Side);
        return other >= 0 && ((RecallPortalProjectile)Main.projectile[other].ModProjectile).Ready;
    }

    // 仅用世界高度分层，不扫描远端未加载区块，也不把当前玩家的群系误当成目的地群系。
    internal static string DepthKey(float tileY) => tileY > Main.UnderworldLayer ? "RecallDepthUnderworld" :
        tileY > Main.rockLayer ? "RecallDepthCaverns" : tileY > Main.worldSurface ? "RecallDepthUnderground" :
        tileY <= Main.worldSurface * .35 ? "RecallDepthSky" : "RecallDepthSurface";

    internal static string Tooltip(Player player, RecallPortalProjectile portal)
    {
        Refresh();
        int owner = portal.Projectile.owner;
        bool detailed = HasOtherPair(portal);
        int other = RecallPortalSystem.FindPortal(owner, portal.PairId, 1 - portal.Side);
        if (other < 0) return (detailed ? LocalizeUI("RecallPortalOwner", Main.player[owner].name) + "\n" : "") +
            LocalizeUI("RecallLinkPending");
        string status = !portal.Ready || !((RecallPortalProjectile)Main.projectile[other].ModProjectile).Ready ? "RecallCharging" :
            !RecallPortalSystem.IsAlly(Main.player[owner], player) ? "RecallAlliesOnly" :
            !InReach(player, portal) ? "RecallMoveCloser" : "RecallInteract";
        if (!detailed) return LocalizeUI(status);
        Vector2 destination = ((RecallPortalProjectile)Main.projectile[other].ModProjectile).DisplayCenter;
        string text = LocalizeUI("RecallPortalOwner", Main.player[owner].name) + "\n" +
            LocalizeUI("RecallPortalDestination", LocalizeUI(DepthKey(destination.Y / 16f)),
                (int)(destination.X / 16f), (int)(destination.Y / 16f));
        return text + "\n" + LocalizeUI(status);
    }

    private static bool HasOtherPair(RecallPortalProjectile hovered)
    {
        // 同组的入口和出口只算一组；不同施法者可以使用相同的 PairId。
        foreach (Projectile projectile in Main.ActiveProjectiles)
            if (projectile.ModProjectile is RecallPortalProjectile portal &&
                (projectile.owner != hovered.Projectile.owner || portal.PairId != hovered.PairId)) return true;
        return false;
    }
}
