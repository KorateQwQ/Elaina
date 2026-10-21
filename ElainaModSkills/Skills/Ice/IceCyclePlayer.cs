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

    public void ReduceLockCooldown(float seconds)
    {
        if (Player.whoAmI != Main.myPlayer || !Player.active || Player.dead || seconds <= 0
            || !Player.GetModPlayer<ElainaSkillModPlayer>().TryGetUnlockedModSkill<ShardbindSkill>(out var skill)
            || skill.Stack >= skill.MaxStack || skill.CurrentCD <= 0)
            return;

        skill.CurrentCD = Math.Max(0, skill.CurrentCD - seconds);
        // 不依赖下一帧 UpdateCD：归零立即恢复一次，禁止储存负 CD 或溢出层数。
        if (skill.CurrentCD == 0)
            skill.Stack = Math.Min(skill.MaxStack, skill.Stack + 1);
    }

    public void RequestWaterHit(NPC target, bool waterBall)
    {
        if (Player.whoAmI != Main.myPlayer) return;
        if (Main.netMode == NetmodeID.MultiplayerClient)
            RPC(nameof(ApplyWaterHit), [target.whoAmI, target.type, waterBall], KLNetModule.NetSendType.ClientToServer);
        else
            ApplyWaterHit(target.whoAmI, target.type, waterBall);
    }

    // 使用玩家 RPC，水球命中后死亡也不会因弹幕已失效而丢失减 CD。
    public void ApplyWaterHit(int targetIndex, int expectedType, bool waterBall)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || !Player.active || Player.dead
            || targetIndex < 0 || targetIndex >= Main.maxNPCs) return;
        NPC target = Main.npc[targetIndex];
        if (!target.active || target.type != expectedType || target.friendly || target.immortal) return;
        target.ReduceFreezeCooldown(waterBall ? IceCycleBalance.BallFreezeRefund : IceCycleBalance.BeamFreezeRefund);
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
