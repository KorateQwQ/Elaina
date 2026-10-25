using KL.Utils;
using KL.Utils.Net;
using Terraria.ID;
using Terraria.ModLoader.IO;

namespace 伊蕾娜.ElainaModSkills.Skills.Ice;

/// <summary>冰凌锁的启封与研习记录随角色保存：经历过霜月即启封，参与击败霜月女王满足研习条件。</summary>
public sealed class ShardbindDiscoveryPlayer : ModPlayer
{
    public bool ExperiencedFrostMoon { get; private set; }
    public bool DefeatedIceQueen { get; private set; }

    public override void Initialize() => ExperiencedFrostMoon = DefeatedIceQueen = false;

    public override void PostUpdate()
    {
        if (!ExperiencedFrostMoon && Main.snowMoon && Player.whoAmI == Main.myPlayer) ExperiencedFrostMoon = true;
    }

    public void MarkIceQueenDefeated() => ExperiencedFrostMoon = DefeatedIceQueen = true;

    public override void SaveData(TagCompound tag)
    {
        tag[nameof(ExperiencedFrostMoon)] = ExperiencedFrostMoon;
        tag[nameof(DefeatedIceQueen)] = DefeatedIceQueen;
    }

    public override void LoadData(TagCompound tag)
    {
        ExperiencedFrostMoon = tag.GetBool(nameof(ExperiencedFrostMoon));
        DefeatedIceQueen = tag.GetBool(nameof(DefeatedIceQueen));
    }
}

/// <summary>霜月女王不是 boss，KL 的击杀事件不会触发；由服务端按参与伤害的玩家逐个通知各自客户端。</summary>
public sealed class ShardbindDiscoverySystem : KLModSystem
{
    public void IceQueenDefeated(int whoAmI)
    {
        if (whoAmI == Main.myPlayer && Main.netMode != NetmodeID.Server)
            Main.LocalPlayer.GetModPlayer<ShardbindDiscoveryPlayer>().MarkIceQueenDefeated();
    }

    public static void NotifyKill(NPC npc)
    {
        var system = ModContent.GetInstance<ShardbindDiscoverySystem>();
        foreach (Player player in Main.ActivePlayers)
            if (npc.playerInteraction[player.whoAmI])
                system.RPC(nameof(IceQueenDefeated), [player.whoAmI], KLNetModule.NetSendType.ServerToAll);
    }
}

public sealed class ShardbindDiscoveryNPC : GlobalNPC
{
    public override bool AppliesToEntity(NPC entity, bool lateInstantiation) => entity.type == NPCID.IceQueen;

    public override void OnKill(NPC npc) => ShardbindDiscoverySystem.NotifyKill(npc);
}
