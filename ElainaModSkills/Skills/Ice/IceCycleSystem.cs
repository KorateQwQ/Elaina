using KL.DamageSystem;
using Terraria.ID;

namespace 伊蕾娜.ElainaModSkills.Skills.Ice;

/// <summary>消费KL的权威冻结事件，不依赖拥有者客户端的积蓄预测。</summary>
public sealed class IceCycleSystem : ModSystem
{
    public override void Load() => ElementBuildUpSystem.FreezeTriggered += OnFreezeTriggered;
    public override void Unload() => ElementBuildUpSystem.FreezeTriggered -= OnFreezeTriggered;

    private static void OnFreezeTriggered(NPC anchor, int sourcePlayer)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || sourcePlayer < 0 || sourcePlayer >= Main.maxPlayers) return;
        Player player = Main.player[sourcePlayer];
        if (player is { active: true, dead: false })
            player.GetModPlayer<IceCyclePlayer>().OnFreezeTriggered(anchor);
    }
}
