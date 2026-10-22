using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace 伊蕾娜.System.Experience;

public sealed class ExperienceDropNPC : GlobalNPC
{
    internal static bool CanDrop(NPC npc) => !npc.friendly && !npc.townNPC && npc.lifeMax > 0
        && !npc.SpawnedFromStatue && npc.type != NPCID.TargetDummy
        && !NPCID.Sets.CountsAsCritter[npc.type] && !NPCID.Sets.ProjectileNPC[npc.type]
        && (npc.realLife < 0 || npc.realLife == npc.whoAmI);

    public override void OnKill(NPC npc)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || !CanDrop(npc)) return;

        foreach (Player player in Main.ActivePlayers)
        {
            if (player.dead || player.ghost || !player.GetModPlayer<ElainaModplayer>().Elaina
                || Vector2.DistanceSquared(player.Center, npc.Center) > ExperienceRules.DistributionRadius * ExperienceRules.DistributionRadius)
                continue;

            // owner 使用生成端（服务器为 255），接收玩家单独放在 ai[0]。
            // 指定客户端 owner 会跳过服务器 NewProjectile 的 ai 初始化与自动同步。
            float drift = ((player.whoAmI * 17 + npc.whoAmI * 7) % 11 - 5) * .22f;
            int index = Projectile.NewProjectile(npc.GetSource_Loot(), npc.Center, new Vector2(drift, -2.4f),
                ModContent.ProjectileType<ExperienceOrb>(), 0, 0, Main.myPlayer,
                player.whoAmI, ExperienceRules.ExperiencePerEnemy);
            if (index >= 0 && index < Main.maxProjectiles && Main.projectile[index].ModProjectile is ExperienceOrb orb)
                orb.BindRecipient(player);
        }
    }
}
