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
    public bool HappyActive;

    public const float FeatherlightSpeedMultiplier = 1.2f;
    public const float HappySpeedBonus = 0.05f;
    // 同类旅行速度加成相加，所有移动入口共用同一倍率，避免水平飞行和坐骑重复加成。
    public float TravelSpeedMultiplier => 1f + (FeatherlightActive ? FeatherlightSpeedMultiplier - 1f : 0f)
        + (HappyActive ? HappySpeedBonus : 0f);

    private int painkillerTicks;
    private int painkillerHealingHundredths;
    private double lifeStealRemainder;

    public override void ResetEffects()
    {
        PainkillerActive = false;
        BloodthirstActive = false;
        StarPowerActive = false;
        ConcentrationActive = false;
        ResonanceActive = false;
        FeatherlightActive = false;
        HappyActive = false;
    }

    public override void UpdateDead()
    {
        ResetEffects();
        painkillerTicks = 0;
        painkillerHealingHundredths = 0;
        lifeStealRemainder = 0;
    }

    private void UpdatePainkillerHealing()
    {
        // 由所属玩家结算直接治疗，避免服务器和远端重复回血。
        // 满血时不储存治疗，效果结束或死亡也清空计时和小数余量。
        if (!PainkillerActive || Player.dead || Player.whoAmI != Main.myPlayer
            || Player.statLife >= Player.statLifeMax2)
        {
            painkillerTicks = 0;
            painkillerHealingHundredths = 0;
            return;
        }

        if (++painkillerTicks < 60) return;
        painkillerTicks = 0;

        // 每秒直接恢复最大生命的 1%。用百分之一生命为单位保存余量，避免浮点误差。
        // 例如三次应恢复 1.33、1.33、1.34，实际依次治疗 1、1、2。
        painkillerHealingHundredths += Player.statLifeMax2 % 100;
        int healing = Player.statLifeMax2 / 100 + painkillerHealingHundredths / 100;
        painkillerHealingHundredths %= 100;
        healing = Math.Min(healing, Player.statLifeMax2 - Player.statLife);
        if (healing > 0) Player.Heal(healing);
        if (Player.statLife >= Player.statLifeMax2) painkillerHealingHundredths = 0;
    }

    public override void PostUpdateMiscEffects()
    {
        UpdatePainkillerHealing();

        // 星力由 ElainaStarPowerPlayer 接入当前角色使用的魔力上限。

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
        float multiplier = TravelSpeedMultiplier;
        if (multiplier == 1f || Player.mount.Active) return;

        Player.maxRunSpeed *= multiplier;
        Player.accRunSpeed *= multiplier;
        Player.runAcceleration *= multiplier;
        Player.runSlowdown *= multiplier;
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
