using Terraria.ID;
using 伊蕾娜.ElainaModSkills.ElainaSkillUI;

namespace 伊蕾娜.ElainaModSkills.Skills.Recall;

public sealed class RecallPortalPlayer : ModPlayer
{
    internal readonly RecallTransition Transition = new();
    internal int TravelCooldown;
    private int sentSpawnX = int.MinValue, sentSpawnY = int.MinValue;

    public override void OnEnterWorld()
    {
        Transition.Clear();
        TravelCooldown = 0;
        sentSpawnX = sentSpawnY = int.MinValue;
    }

    public override void PreUpdate()
    {
        if (TravelCooldown > 0) TravelCooldown--;
        // 原版换床不总会立即发 PlayerSpawn；换床后经服务器广播，施法者才能在自己这端打开友军家里的出口。
        if (Main.netMode != NetmodeID.MultiplayerClient || Player.whoAmI != Main.myPlayer ||
            sentSpawnX == Player.SpawnX && sentSpawnY == Player.SpawnY) return;
        sentSpawnX = Player.SpawnX; sentSpawnY = Player.SpawnY;
        RecallPortalSystem.SendSpawnPoint(Player);
    }

    // 玩家加入时：客户端把自己的出生点发给服务器（再转发给其他人），服务器把已有玩家的出生点发给新玩家。
    public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) =>
        RecallPortalSystem.SendSpawnPoint(Player, toWho, fromWho);

    public override void SetControls()
    {
        if (Player.whoAmI != Main.myPlayer || Player.dead || !Main.hasFocus || Main.gameMenu || Main.drawingPlayerChat ||
            Main.ingameOptionsWindow || Main.inFancyUI || Main.editSign || Main.editChest ||
            Player.mouseInterface || ElainaHealingWheel.OwnsPointer) return;
        if (ElainaSkillWheel.IsShowing) return;
        RecallPortalProjectile nearest = FindHovered(Player, inspectOnly: true);
        if (nearest == null) return;
        Main.instance.MouseText(RecallPortalPresentation.Tooltip(Player, nearest));
        if (!RecallPortalPresentation.CanTravel(Player, nearest)) return;
        if (!Main.mouseRight) return;
        // 阻止右键同时触发魔杖技能盘、床或其他物品交互；松开后才可再次传送。
        Player.mouseInterface = true;
        Player.controlUseTile = false;
        Player.controlUseItem = false;
        if (!Main.mouseRightRelease) return;
        Main.mouseRightRelease = false;
        if (TravelCooldown > 0) return;
        RecallPortalSystem.RequestTravel(Player, nearest);
        if (Main.netMode == NetmodeID.MultiplayerClient) TravelCooldown = RecallPortalSystem.TravelCooldownTicks;
    }

    internal static RecallPortalProjectile FindHovered(Player player, bool inspectOnly = false)
    {
        RecallPortalPresentation.Refresh();
        RecallPortalProjectile nearest = null;
        float nearestDistance = float.MaxValue;
        foreach (Projectile projectile in Main.ActiveProjectiles)
        {
            if (projectile.ModProjectile is not RecallPortalProjectile portal ||
                projectile.owner < 0 || projectile.owner >= Main.maxPlayers ||
                (!inspectOnly && !RecallPortalPresentation.CanTravel(player, portal)) || !portal.Hovered(Main.MouseWorld)) continue;
            float distance = Vector2.DistanceSquared(Main.MouseWorld, portal.DisplayCenter);
            if (distance >= nearestDistance) continue;
            nearest = portal; nearestDistance = distance;
        }
        return nearest;
    }
}
