using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaModAlchemy.Buffs;

/// <summary>新炼金药剂的玩家效果，不依赖旧炼金系统。</summary>
public sealed class AlchemyBuffPlayer : ModPlayer
{
    public bool PainkillerActive;
    public bool BloodthirstActive;
    public bool StarPowerActive;
    public bool ConcentrationActive;
    public bool ResonanceActive;
    public bool FeatherlightActive;

    public const float FeatherlightSpeedMultiplier = 1.2f;
    public float TravelSpeedMultiplier => FeatherlightActive ? FeatherlightSpeedMultiplier : 1f;

    private double regenerationRemainder;
    private double lifeStealRemainder;

    public override void ResetEffects()
    {
        PainkillerActive = false;
        BloodthirstActive = false;
        StarPowerActive = false;
        ConcentrationActive = false;
        ResonanceActive = false;
        FeatherlightActive = false;
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
        // 保留小数，避免生命上限不是 50 的倍数时损失恢复量。
        regenerationRemainder += Player.statLifeMax2 * 0.02d;
        int regeneration = (int)regenerationRemainder;
        regenerationRemainder -= regeneration;
        Player.lifeRegen += regeneration;
    }

    public override void PostUpdateMiscEffects()
    {
        // 星力同时提高魔力上限，并按当前魔力超过生命值的差额提高魔法伤害。
        if (StarPowerActive)
        {
            Player.statManaMax2 += 40;
            float difference = Math.Max(0, Player.statMana - Player.statLife);
            Player.GetDamage(DamageClass.Magic) += Math.Min(1f, difference / 300f) * .30f;
        }

        if (!BloodthirstActive || Player.moonLeech || Player.statLife >= Player.statLifeMax2)
            lifeStealRemainder = 0;
    }

    public override void ModifyHurt(ref Player.HurtModifiers modifiers)
    {
        if (BloodthirstActive)
            modifiers.FinalDamage *= 1.2f;
    }

    public override void PostUpdateRunSpeeds()
    {
        // 原版先计算跑鞋、翅膀/悬停和地形修正，然后才调用此钩子。
        // 同时提高步行与跑鞋上限，水平飞行也经过这里，不能再在翅膀钩子中重复叠加。
        // 坐骑使用独立速度；本模组扫帚在自己的控制器中读取 TravelSpeedMultiplier。
        if (!FeatherlightActive || Player.mount.Active) return;

        Player.maxRunSpeed *= FeatherlightSpeedMultiplier;
        Player.accRunSpeed *= FeatherlightSpeedMultiplier;
        Player.runAcceleration *= FeatherlightSpeedMultiplier;
        Player.runSlowdown *= FeatherlightSpeedMultiplier;
    }

    public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
    {
        if (proj.owner != Player.whoAmI) return;

        if (ConcentrationActive && proj.CountsAsClass(DamageClass.Ranged))
            modifiers.SourceDamage *= 1f + proj.GetGlobalProjectile<ConcentrationProjectile>().DamageBonus(target.Center);

        if (ResonanceActive && !ProjectileID.Sets.IsAWhip[proj.type]
            && (proj.minion || proj.sentry || ProjectileID.Sets.MinionShot[proj.type] || ProjectileID.Sets.SentryShot[proj.type])
            && target.GetGlobalNPC<ResonanceGlobalNPC>().IsMarkedBy(Player.whoAmI))
            modifiers.SourceDamage *= 1.2f;
    }

    public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (item.CountsAsClass(DamageClass.Melee))
            TryLifeSteal(target, damageDone);
    }

    public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (proj.owner != Player.whoAmI) return;

        if (ResonanceActive && Player.whoAmI == Main.myPlayer && ProjectileID.Sets.IsAWhip[proj.type]
            && !target.friendly && !target.immortal && damageDone > 0)
            target.GetGlobalNPC<ResonanceGlobalNPC>().Mark(Player.whoAmI);

        if (proj.CountsAsClass(DamageClass.Melee))
            TryLifeSteal(target, damageDone);
    }

    private void TryLifeSteal(NPC target, int damageDone)
    {
        // 只由攻击者本地结算治疗，生命变化通过原版玩家同步发送。
        // 假人、友方和不能被吸血的目标不提供恢复；月噬沿用原版的禁疗规则。
        if (!BloodthirstActive || Player.whoAmI != Main.myPlayer || Player.dead || Player.moonLeech
            || target.friendly || target.immortal || !target.canGhostHeal || target.lifeMax <= 5
            || damageDone <= 0 || Player.statLife >= Player.statLifeMax2 || Player.lifeSteal < 1f)
            return;

        lifeStealRemainder += damageDone * 0.05d;
        int healing = (int)lifeStealRemainder;
        lifeStealRemainder -= healing;
        healing = Math.Min(healing, Player.statLifeMax2 - Player.statLife);
        // 与吸血飞刀共用原版吸血预算，避免多段近战无限恢复。
        healing = Math.Min(healing, (int)Player.lifeSteal);

        if (healing > 0)
        {
            Player.lifeSteal -= healing;
            Player.Heal(healing);
        }
    }
}
