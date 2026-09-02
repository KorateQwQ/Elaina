using System;
using KL.SkillSystem;
using KL.SkillSystem.SilkyUI;

namespace 伊蕾娜.ElainaModSkills.Skills.MagicBarrier;

[SkillUIInfo(State = 0, Pixels = 100)]
public class MagicBarrierSkill : ElainaSkill
{
    public const float BaseShieldAmount = 50f;
    public const float MaximumManaShieldRatio = 0.25f;
    public const float ShieldRegenerationPerSecond = 0.01f;
    public const int BrokenShieldCooldownTicks = 10 * 60;

    public override bool IsPassiveSkill => true;
    public override bool IsToggleable => true;

    public override void Initialize()
    {
        MaxCD = -1;
        MagicPointCost = -1;
        base.Initialize();
    }

    public override void UpdateEquips(Player player)
    {
        if (IsEnabled)
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
        ShieldRegenerationPerSecond * 100f,
        BrokenShieldCooldownTicks / 60f,
    };

    public class MagicBarrierModPlayer : ModPlayer
    {
        private const int DotBlockVisualCooldownTicks = 30;
        private const int BarrierInvincibilityTicks = 30;

        private bool barrierEnabled;
        private bool shieldInitialized;
        private bool absorbingLifeRegenDamage;
        private int dotBlockVisualCooldown;

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

            if (BrokenShieldCooldownRemaining > 0)
            {
                BrokenShieldCooldownRemaining--;
                return;
            }

            if (!barrierEnabled || CurrentShield >= MaximumShield)
            {
                return;
            }

            float regenerationPerTick = MaximumShield * ShieldRegenerationPerSecond / 60f;
            CurrentShield = Math.Min(MaximumShield, CurrentShield + regenerationPerTick);
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

            PrintText($"护盾格挡了 {info.Damage}" );
            AbsorbDamage(info.Damage, playBlockVisual: true);

            // Returning true completely ignores this Hurt, including its hit-side debuffs.
            // Add a short immunity window so contact/projectile sources cannot immediately retry.
            Player.SetImmuneTimeForAllTypes(BarrierInvincibilityTicks);

            return true;
        }

        public override void OnHurt(Player.HurtInfo info)
        {
            PrintText($"受到{info.Damage}伤害");
            base.OnHurt(info);
        }

        private bool CanAbsorbDamage()
        {
            return barrierEnabled && shieldInitialized && CurrentShield > 0f;
        }

        private void AbsorbDamage(float damage, bool playBlockVisual)
        {
            CurrentShield = Math.Max(0f, CurrentShield - damage);

            bool shieldBroke = CurrentShield <= 0f;
            if (shieldBroke)
            {
                CurrentShield = 0f;
                BrokenShieldCooldownRemaining = BrokenShieldCooldownTicks;
            }

            if (playBlockVisual || shieldBroke)
            {
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
