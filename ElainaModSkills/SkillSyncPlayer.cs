using System;
using KL.SkillSystem;
using KL.Utils.Net;
using Terraria.ID;
using 伊蕾娜.ElainaModSkills.Skills.AshenWitch;
using 伊蕾娜.ElainaModSkills.Skills.MagicBarrier;

namespace 伊蕾娜.ElainaModSkills;

/// <summary>统一同步会影响联机战斗的被动技能；属性与受伤效果仍由各技能正常更新。</summary>
public sealed class SkillSyncPlayer : KLModPlayer
{
    // 数组下标就是 RPC 位编号，新增技能只在末尾追加，避免改变现有状态的含义。
    // 护盾的负生命恢复保护需要各端执行；直接受击与耗魔仍只由受击方结算。
    private static readonly Type[] SyncedSkills = [typeof(AshenWitchSkill), typeof(MagicBarrierSkill)];
    private bool hasSentState;
    private int lastLearnedMask;
    private int lastEnabledMask;

    public override void Initialize()
    {
        hasSentState = false;
        lastLearnedMask = 0;
        lastEnabledMask = 0;
    }

    public override void OnEnterWorld()
    {
        if (Main.netMode != NetmodeID.MultiplayerClient || Player.whoAmI != Main.myPlayer)
            return;

        BroadcastState();
        // 已在线的拥有者补发自己的当前状态，包括已关闭的被动技能。
        RPC(nameof(RequestExistingStates), []);
    }

    public override void PostUpdate()
    {
        if (Main.netMode != NetmodeID.MultiplayerClient || Player.whoAmI != Main.myPlayer)
            return;

        ReadState(out int learnedMask, out int enabledMask);
        if (!hasSentState || learnedMask != lastLearnedMask || enabledMask != lastEnabledMask)
            BroadcastState();
    }

    private void ReadState(out int learnedMask, out int enabledMask)
    {
        learnedMask = 0;
        enabledMask = 0;
        var skills = Player.GetModPlayer<ElainaSkillModPlayer>();
        for (int i = 0; i < SyncedSkills.Length; i++)
        {
            if (!skills.UnlockedSkill.TryGetValue(SyncedSkills[i].Name, out var skill)
                || skill?.ModSkill == null || skill.BasicStatus != Skill.SKillBasicStatus.UnLock)
                continue;

            learnedMask |= 1 << i;
            if (skill.ModSkill.IsEnabled)
                enabledMask |= 1 << i;
        }
    }

    private void BroadcastState()
    {
        ReadState(out lastLearnedMask, out lastEnabledMask);
        hasSentState = true;
        RPC(nameof(ReceiveState), [lastLearnedMask, lastEnabledMask]);
    }

    private void RequestExistingStates()
    {
        // ClientToAll 会在请求者本地和服务器执行，但只有其他客户端的本地拥有者应答。
        if (Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI != Main.myPlayer)
            Main.LocalPlayer.GetModPlayer<SkillSyncPlayer>().BroadcastState();
    }

    private void ReceiveState(int learnedMask, int enabledMask)
    {
        // 发送者先本地执行 RPC，不能据此替换自己的技能实例、等级或冷却。
        if (Main.netMode != NetmodeID.Server && Player.whoAmI == Main.myPlayer)
            return;

        var skills = Player.GetModPlayer<ElainaSkillModPlayer>();
        for (int i = 0; i < SyncedSkills.Length; i++)
        {
            skills.UnlockedSkill.TryGetValue(SyncedSkills[i].Name, out var owned);
            if ((learnedMask & (1 << i)) == 0)
            {
                if (owned != null)
                    skills.LockSkill(owned);
                continue;
            }

            // 远端没有拥有者的技能存档，必须创建独立实例才能执行被动效果。
            if (owned?.ModSkill == null)
            {
                owned = Skill.NewSkill(SyncedSkills[i], Mod);
                skills.UnlockSkill(owned);
            }

            owned.BasicStatus = Skill.SKillBasicStatus.UnLock;
            owned.ModSkill.IsEnabled = (enabledMask & (1 << i)) != 0;
        }
    }
}
