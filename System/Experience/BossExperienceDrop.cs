using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace 伊蕾娜.System.Experience;

/// <summary>仅响应 KL 确认的 Boss 击杀；服务器不读取或改写角色拥有端的存档。</summary>
public sealed class BossExperienceDrop : ModSystem
{
    private static readonly Dictionary<int, (Player Owner, Dictionary<string, int> Kills)> KillNumbers = new();

    public override void OnWorldLoad() => KillNumbers.Clear();
    public override void OnWorldUnload() => KillNumbers.Clear();
    public override void Unload() => KillNumbers.Clear();

    internal static void Spawn(NPC boss, List<int> killers)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || boss.SpawnedFromStatue) return;
        string key = ExperienceNpcRules.BossKey(boss);
        var visited = new HashSet<int>();
        foreach (int index in killers)
        {
            if (index < 0 || index >= Main.maxPlayers || !visited.Add(index)) continue;
            Player target = Main.player[index];
            if (!target.active || !target.GetModPlayer<ElainaModplayer>().Elaina) continue;
            if (!KillNumbers.TryGetValue(index, out var entry) || !ReferenceEquals(entry.Owner, target))
                KillNumbers[index] = entry = (target, new(StringComparer.Ordinal));
            int number = entry.Kills.GetValueOrDefault(key) + 1;
            entry.Kills[key] = number;
            string rewardKey = ExperienceNpcRules.BossRewardKey(key, number);
            // 死亡时仍递增序号，与同一回调的拥有端对齐；只跳过实际光球生成。
            if (target.dead || target.ghost) continue;
            if (Main.netMode == NetmodeID.SinglePlayer
                && !target.GetModPlayer<ElainaStatePlayer>().HasPendingBossExperience(rewardKey)) continue;
            ExperienceDropNPC.SpawnOrb(boss, target, 0, rewardKey);
        }
    }
}
