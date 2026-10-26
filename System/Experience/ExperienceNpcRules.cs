using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace 伊蕾娜.System.Experience;

/// <summary>按当前世界中的普通难度、单人默认值取基础血量；不修改 Main 的难度或共享样本。</summary>
public sealed class ExperienceNpcRules : ModSystem
{
    private static readonly Dictionary<int, int> BaseLife = new();

    public override void OnWorldLoad() => BaseLife.Clear();
    public override void OnWorldUnload() => BaseLife.Clear();
    public override void Unload() => BaseLife.Clear();

    internal static int GetBaseLife(NPC npc)
    {
        if (BaseLife.TryGetValue(npc.netID, out int life)) return life;
        var sample = new NPC();
        sample.SetDefaults(npc.netID, new NPCSpawnParams
        {
            gameModeData = Main.RegisteredGameModes[0],
            playerCountForMultiplayerDifficultyOverride = 1,
            strengthMultiplierOverride = 1f
        });
        BaseLife[npc.netID] = sample.lifeMax;
        return sample.lifeMax;
    }

    // 与现有 KL 击杀事件保持一致：boss 标记才走 Boss 结算。未标记为 boss 的小 Boss 仍按血量奖励。
    internal static bool IsBossOrPart(NPC npc) => npc.boss || npc.type is
            NPCID.EaterofWorldsHead or NPCID.EaterofWorldsBody or NPCID.EaterofWorldsTail
            or NPCID.SkeletronHand or NPCID.WallofFleshEye or NPCID.TheHungry or NPCID.TheHungryII
            or NPCID.LeechHead or NPCID.LeechBody or NPCID.LeechTail or NPCID.Creeper
            or NPCID.ServantofCthulhu or NPCID.Probe or NPCID.PrimeCannon or NPCID.PrimeSaw
            or NPCID.PrimeVice or NPCID.PrimeLaser or NPCID.MoonLordHead or NPCID.MoonLordHand
            or NPCID.MoonLordFreeEye or NPCID.MoonLordLeechBlob;

    // 原版同一场战斗的多体节共享持久化身份，模组 NPC 默认使用 ModName/NPCName。
    internal static string BossKey(NPC npc) => npc.type switch
    {
        NPCID.EaterofWorldsHead or NPCID.EaterofWorldsBody or NPCID.EaterofWorldsTail => "Terraria/EaterofWorlds",
        NPCID.Retinazer or NPCID.Spazmatism => "Terraria/TheTwins",
        NPCID.WallofFlesh or NPCID.WallofFleshEye => "Terraria/WallofFlesh",
        NPCID.MoonLordHead or NPCID.MoonLordHand or NPCID.MoonLordCore => "Terraria/MoonLord",
        _ => npc.ModNPC?.FullName ?? "Terraria/" + NPCID.Search.GetName(npc.type)
    };

    // 每次 KL 回调都有独立领取凭据，同一种 Boss 的多颗在途光球不会互相覆盖。
    internal static string BossRewardKey(string bossKey, int killNumber) => bossKey + "#" + killNumber;

}
