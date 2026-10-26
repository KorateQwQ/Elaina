using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace 伊蕾娜.System.Experience;

public sealed class ExperienceDropNPC : GlobalNPC
{
    public override bool InstancePerEntity => true;
    private bool bossSummon;

    public override void SetDefaults(NPC npc) => bossSummon = false;

    public override void OnSpawn(NPC npc, IEntitySource source)
    {
        if (source is EntitySource_Parent { Entity: NPC parent })
            bossSummon = ExperienceNpcRules.IsBossOrPart(parent) || parent.GetGlobalNPC<ExperienceDropNPC>().bossSummon;
    }

    internal static bool CanDrop(NPC npc) => !npc.friendly && !npc.townNPC && npc.lifeMax > 0
        && !npc.SpawnedFromStatue && npc.type != NPCID.TargetDummy
        && !NPCID.Sets.CountsAsCritter[npc.type] && !NPCID.Sets.ProjectileNPC[npc.type]
        && (npc.realLife < 0 || npc.realLife == npc.whoAmI);

    public override void OnKill(NPC npc)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || !CanDrop(npc)
            || bossSummon || ExperienceNpcRules.IsBossOrPart(npc)) return;
        int amount = ExperienceRules.ForEnemyLife(ExperienceNpcRules.GetBaseLife(npc));
        if (amount <= 0) return;

        foreach (Player player in Main.ActivePlayers)
        {
            if (player.dead || player.ghost || !player.GetModPlayer<ElainaModplayer>().Elaina
                || Vector2.DistanceSquared(player.Center, npc.Center) > ExperienceRules.DistributionRadius * ExperienceRules.DistributionRadius)
                continue;

            SpawnOrb(npc, player, amount);
        }
    }

    internal static bool SpawnOrb(NPC npc, Player player, int amount, string bossKey = null)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return false;
        // owner 使用生成端（服务器为 255），接收玩家单独放在 ai[0]。
        float drift = ((player.whoAmI * 17 + npc.whoAmI * 7) % 11 - 5) * .22f;
        int index = Projectile.NewProjectile(npc.GetSource_Loot(), npc.Center, new Vector2(drift, -2.4f),
            ModContent.ProjectileType<ExperienceOrb>(), 0, 0, Main.myPlayer, player.whoAmI, amount);
        if (index < 0 || index >= Main.maxProjectiles || Main.projectile[index].ModProjectile is not ExperienceOrb orb)
            return false;
        orb.BindRecipient(player, bossKey);
        Main.projectile[index].netUpdate = true;
        return true;
    }
}
