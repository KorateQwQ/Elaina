using System;
using KL.Buffs.ElementalDebuff;
using KL.Extensions;
using KL.Utils;
using KL.Utils.Net;
using Terraria.ID;

namespace 伊蕾娜.ElainaModSkills.Skills.Ice;

/// <summary>技能冷却由拥有者维护，NPC 冻结冷却及冻结奖励确认由服务器维护。</summary>
public sealed class IceCyclePlayer : KLModPlayer
{
    private long freezeEventSequence;
    private long receivedFreezeSequence;

    public override void Initialize() { freezeEventSequence = receivedFreezeSequence = 0; }
    public override void OnEnterWorld()
    {
        freezeEventSequence = receivedFreezeSequence = 0;
        SealLock();
    }
    public override void UpdateDead() => SealLock();

    private void SealLock()
    {
        if (Player.GetModPlayer<ElainaSkillModPlayer>().TryGetUnlockedModSkill<ShardbindSkill>(out var skill))
            skill.Seal();
    }

    internal void OnFreezeTriggered(NPC anchor)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || !Player.active || Player.dead) return;
        if (!anchor.active || !anchor.HasBuff<ElementalAffliction_Freeze>()) return;
        long sequence = ++freezeEventSequence;
        if (Main.netMode == NetmodeID.Server)
            RPC(nameof(ReceiveFreezeReward), [sequence], KLNetModule.NetSendType.ServerToClients);
        else
            ReceiveFreezeReward(sequence);
    }

    public void ReceiveFreezeReward(long sequence)
    {
        if (sequence <= receivedFreezeSequence) return;
        receivedFreezeSequence = sequence;
        if (Player.whoAmI != Main.myPlayer || !Player.active || Player.dead) return;
        if (Player.GetModPlayer<ElainaSkillModPlayer>().TryGetUnlockedModSkill<ShardbindSkill>(out var skill)
            && skill.BasicStatus == KL.SkillSystem.Skill.SKillBasicStatus.UnLock)
            skill.Unseal();
    }
}
