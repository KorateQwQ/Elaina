using System;
using Terraria;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaModAlchemy.Buffs;

/// <summary>新炼金药剂的玩家效果，不依赖旧炼金系统。</summary>
public sealed class AlchemyBuffPlayer : ModPlayer
{
    public bool PainkillerActive;
    public bool BloodthirstActive;
    public bool StarPowerActive;

    private double regenerationRemainder;
    private double lifeStealRemainder;

    public override void ResetEffects()
    {
        PainkillerActive = false;
        BloodthirstActive = false;
        StarPowerActive = false;
    }

    public override void UpdateDead()
    {
        ResetEffects();
        regenerationRemainder = 0;
        lifeStealRemainder = 0;
    }

    public override void UpdateLifeRegen()
    {
        if (!PainkillerActive)
        {
            regenerationRemainder = 0;
            return;
        }

        // 原版每 2 点 lifeRegen 对应每秒恢复 1 点生命。
        // 保留小数，避免生命上限不是 25 的倍数时损失恢复量。
        regenerationRemainder += Player.statLifeMax2 * 0.04d;
        int regeneration = (int)regenerationRemainder;
        regenerationRemainder -= regeneration;
        Player.lifeRegen += regeneration;
    }

    public override void PostUpdateMiscEffects()
    {
        // 在装备、增益计算之后读取最终魔力上限：200 最大魔力 = 10% 魔法伤害。
        if (StarPowerActive)
            Player.GetDamage(DamageClass.Magic) += Math.Max(0, Player.statManaMax2) * 0.0005f;

        if (!BloodthirstActive || Player.moonLeech || Player.statLife >= Player.statLifeMax2)
            lifeStealRemainder = 0;
    }

    public override void ModifyHurt(ref Player.HurtModifiers modifiers)
    {
        if (BloodthirstActive)
            modifiers.FinalDamage *= 1.2f;
    }

    public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (item.CountsAsClass(DamageClass.Melee))
            TryLifeSteal(target, damageDone);
    }

    public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (proj.owner == Player.whoAmI && proj.CountsAsClass(DamageClass.Melee))
            TryLifeSteal(target, damageDone);
    }

    private void TryLifeSteal(NPC target, int damageDone)
    {
        // 只由攻击者本地结算治疗，生命变化通过原版玩家同步发送。
        // 假人、友方和不能被吸血的目标不提供恢复；月噬沿用原版的禁疗规则。
        if (!BloodthirstActive || Player.whoAmI != Main.myPlayer || Player.dead || Player.moonLeech
            || target.friendly || target.immortal || !target.canGhostHeal || target.lifeMax <= 5
            || damageDone <= 0 || Player.statLife >= Player.statLifeMax2)
            return;

        lifeStealRemainder += damageDone * 0.05d;
        int healing = (int)lifeStealRemainder;
        lifeStealRemainder -= healing;
        healing = Math.Min(healing, Player.statLifeMax2 - Player.statLife);

        if (healing > 0)
            Player.Heal(healing);
    }
}
