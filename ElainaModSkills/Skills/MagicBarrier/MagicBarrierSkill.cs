using System;
using KL.SkillSystem;
using KL.SkillSystem.SilkyUI;
using 伊蕾娜.ElainaAttribute;
using 伊蕾娜.ElainaModSkills.Skills.AshenWitch;

namespace 伊蕾娜.ElainaModSkills.Skills.MagicBarrier;

[SkillUIInfo(State = 1, Pixels = 0)]
public class MagicBarrierSkill : ElainaSkill
{
    public static float BaseShieldAmount  => 50f;
    public static float MaximumManaShieldRatio => 0.25f;

    //每自然恢复25点魔力，恢复1点护盾值。
    public static int NaturalManaPerShieldPoint => 25;
    public static int BrokenShieldCooldownTicks => 0 * 60;
    //护盾受伤后，延迟多少Tick开始恢复
    public static int ShieldRegenDelayTicks => 3 * 60;

    public override bool IsPassiveSkill => true;
    public override bool IsToggleable => true;

    /// <summary>
    /// 魔力护盾需要前置技能：灰之魔女
    /// </summary>
    public override Type[] PrerequisiteSkills => new[] { typeof(AshenWitchSkill) };

    public override void Initialize()
    {
        MaxCD = -1;
        MagicPointCost = -1;
        base.Initialize();
    }

    public override bool CanUseSkill()
    {
        // 检查所有前置技能是否都已生效
        if (!AreAllPrerequisitesActive()||!IsEnabled)
        {
            return false;
        }
        return base.CanUseSkill();
    }
    
    public override void UpdateEquips(Player player)
    {
        if (CanUseSkill())
        {
            player.GetModPlayer<MagicBarrierModPlayer>().EnableBarrier();
        }

        base.UpdateEquips(player);
    }

    public static float GetMaximumShield(Player player)
    {
        return Math.Max(0f, BaseShieldAmount + player.statManaMax2 * MaximumManaShieldRatio);
    }

    protected override object[] SkillDescriptionArgs => new object[]
    {
        (int)BaseShieldAmount,
        MaximumManaShieldRatio * 100f,
        ShieldRegenDelayTicks / 60f,
    };

    public class MagicBarrierModPlayer : ModPlayer
    {
        private const int DotBlockVisualCooldownTicks = 30;
        private const int BarrierInvincibilityTicks = 30;

        private bool barrierEnabled;
        private bool shieldInitialized;
        private bool absorbingLifeRegenDamage;
        private int dotBlockVisualCooldown;
        private int naturalManaRecoveryForShield;
        private int shieldRegenDelayRemaining;

        public float CurrentShield { get; private set; }
        public float MaximumShield => GetMaximumShield(Player);
        public int BrokenShieldCooldownRemaining { get; private set; }
        public bool BarrierEnabled => barrierEnabled;
        public bool ShieldBroken => CurrentShield <= 0f && BrokenShieldCooldownRemaining > 0;
        public float ShieldRatio => MaximumShield <= 0f
            ? 0f
            : MathHelper.Clamp(CurrentShield / MaximumShield, 0f, 1f);

        public override void Initialize()
        {
            CurrentShield = 0f;
            BrokenShieldCooldownRemaining = 0;
            barrierEnabled = false;
            shieldInitialized = false;
            absorbingLifeRegenDamage = false;
            dotBlockVisualCooldown = 0;
            naturalManaRecoveryForShield = 0;
            shieldRegenDelayRemaining = 0;
        }

        public override void ResetEffects()
        {
            barrierEnabled = false;
        }

        public void EnableBarrier()
        {
            barrierEnabled = true;

            if (!shieldInitialized)
            {
                CurrentShield = MaximumShield;
                shieldInitialized = true;
            }
        }

        public override void UpdateLifeRegen()
        {
            absorbingLifeRegenDamage = false;

            if (Player.whoAmI != Main.myPlayer)
            {
                return;
            }

            if (dotBlockVisualCooldown > 0)
            {
                dotBlockVisualCooldown--;
            }

            if (!CanAbsorbDamage())
            {
                return;
            }

            // UpdateLifeRegen runs before vanilla adds this tick's lifeRegen to lifeRegenCount.
            // Mirror the later vanilla thresholds so fractional DoT accumulation is preserved.
            int projectedLifeRegenCount = Player.lifeRegenCount + Player.lifeRegen;
            if (projectedLifeRegenCount >= 0)
            {
                return;
            }

            int incomingDamage = GetLifeRegenDamage(ref projectedLifeRegenCount);
            Player.lifeRegen = 0;
            Player.lifeRegenCount = projectedLifeRegenCount;

            if (incomingDamage <= 0)
            {
                return;
            }

            absorbingLifeRegenDamage = true;
            bool playVisual = dotBlockVisualCooldown <= 0;
            AbsorbDamage(incomingDamage, playVisual);
            if (playVisual)
            {
                dotBlockVisualCooldown = DotBlockVisualCooldownTicks;
            }
        }

        private int GetLifeRegenDamage(ref int lifeRegenCount)
        {
            if (Player.burned || Player.suffocating || (Player.tongued && Main.expertMode))
            {
                int damage = 0;
                while (lifeRegenCount <= -600)
                {
                    lifeRegenCount += 600;
                    damage += 5;
                }

                return damage;
            }

            if (Player.starving)
            {
                int damagePerTick = Math.Max(Player.statLifeMax2 / 50, 2);
                if (Player.ZoneDesert || Player.ZoneSnow)
                {
                    damagePerTick *= 2;
                }

                int threshold = 120 * damagePerTick;
                int damage = 0;
                while (lifeRegenCount <= -threshold)
                {
                    lifeRegenCount += threshold;
                    damage += damagePerTick;
                }

                return damage;
            }

            int normalDamage = 0;
            while (lifeRegenCount <= -120)
            {
                if (lifeRegenCount <= -480)
                {
                    lifeRegenCount += 480;
                    normalDamage += 4;
                }
                else if (lifeRegenCount <= -360)
                {
                    lifeRegenCount += 360;
                    normalDamage += 3;
                }
                else if (lifeRegenCount <= -240)
                {
                    lifeRegenCount += 240;
                    normalDamage += 2;
                }
                else
                {
                    lifeRegenCount += 120;
                    normalDamage++;
                }
            }

            return normalDamage;
        }

        public override void NaturalLifeRegen(ref float regen)
        {
            if (absorbingLifeRegenDamage)
            {
                regen = 0f;
            }
        }

        public override void PostUpdate()
        {
            if (Player.whoAmI != Main.myPlayer || !shieldInitialized)
            {
                return;
            }

            CurrentShield = MathHelper.Clamp(CurrentShield, 0f, MaximumShield);

            int naturalManaGain = Player.GetModPlayer<Elaina145ManaRegenPlayer>().NaturalManaGainThisTick;
            if (!barrierEnabled)
            {
                naturalManaRecoveryForShield = 0;
                shieldRegenDelayRemaining = 0;
            }
            else if (BrokenShieldCooldownRemaining > 0)
            {
                naturalManaRecoveryForShield = 0;
                shieldRegenDelayRemaining = 0;
            }
            else if (CurrentShield < MaximumShield)
            {
                // 如果护盾未满且延迟倒计时还在进行，则减少延迟
                if (shieldRegenDelayRemaining > 0)
                {
                    shieldRegenDelayRemaining--;
                }
                // 延迟结束后才开始恢复护盾
                else
                {
                    naturalManaRecoveryForShield += naturalManaGain;
                    int shieldGain = naturalManaRecoveryForShield / NaturalManaPerShieldPoint;
                    /*PrintText($"每秒魔力恢复为: {Player.GetModPlayer<Elaina145ManaRegenPlayer>().NaturalManaRegenPerSecond}");
                    PrintText($"此帧魔力恢复为: {Player.GetModPlayer<Elaina145ManaRegenPlayer>().NaturalManaGainThisTick} ");
                    PrintText($"此帧护盾恢复为: {shieldGain} ");*/

                    naturalManaRecoveryForShield %= NaturalManaPerShieldPoint;
                    CurrentShield = Math.Min(MaximumShield, CurrentShield + shieldGain);
                }
            }
            else
            {
                naturalManaRecoveryForShield = 0;
                shieldRegenDelayRemaining = 0;
            }

            if (BrokenShieldCooldownRemaining > 0)
            {
                BrokenShieldCooldownRemaining--;
                return;
            }

        }

        public override void UpdateDead()
        {
            if (Player.whoAmI == Main.myPlayer && BrokenShieldCooldownRemaining > 0)
            {
                BrokenShieldCooldownRemaining--;
            }
        }

        public override bool ConsumableDodge(Player.HurtInfo info)
        {
            if (!CanAbsorbDamage())
            {
                return false;
            }

            //PrintText($"护盾格挡了 {info.Damage}" );
            AbsorbDamage(info.Damage, playBlockVisual: true);

            // Returning true completely ignores this Hurt, including its hit-side debuffs.
            // Add a short immunity window so contact/projectile sources cannot immediately retry.
            Player.SetImmuneTimeForAllTypes(BarrierInvincibilityTicks);

            return true;
        }

        public override void OnHurt(Player.HurtInfo info)
        {
            //PrintText($"受到{info.Damage}伤害");
            base.OnHurt(info);
        }

        private bool CanAbsorbDamage()
        {
            return barrierEnabled && shieldInitialized && CurrentShield > 0f;
        }

        private void AbsorbDamage(float damage, bool playBlockVisual)
        {
            CurrentShield = Math.Max(0f, CurrentShield - damage);

            // 护盾受伤后，重置恢复延迟计时器
            shieldRegenDelayRemaining = ShieldRegenDelayTicks;

            bool shieldBroke = CurrentShield <= 0f;
            if (shieldBroke)
            {
                CurrentShield = 0f;
                BrokenShieldCooldownRemaining = BrokenShieldCooldownTicks;
            }

            if (playBlockVisual || shieldBroke)
            {
                CombatText.NewText(Player.Hitbox, new Color(180,180,180,255), $"-{damage}");
                SpawnBarrierVisual();
            }
        }

        private void SpawnBarrierVisual()
        {
            Projectile.NewProjectile(
                Player.GetSource_FromThis(),
                Player.MountedCenter,
                Vector2.Zero,
                ModContent.ProjectileType<MagicBarrierProj>(),
                0,
                0f,
                Player.whoAmI);
        }
    }
}
