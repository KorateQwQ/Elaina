using System;
using KL.SkillSystem;
using KL.SkillSystem.SilkyUI;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using 伊蕾娜.ElainaAttribute;
using 伊蕾娜.ElainaModSkills.Skills.AshenWitch;

namespace 伊蕾娜.ElainaModSkills.Skills.MagicBarrier;

[SkillUIInfo(State = 1, Pixels = 100)]
public class MagicBarrierSkill : ElainaSkill
{
    public static float BaseShieldAmount => 0f;
    public static float MaximumManaShieldRatio => 1f;
    public static int NaturalManaPerShieldPoint => 1;
    public static int BrokenShieldCooldownTicks => 0;
    public static int ShieldRegenDelayTicks => 0;

    public override bool IsPassiveSkill => true;
    public override bool IsToggleable => true;
    public override Type[] PrerequisiteSkills => new[] { typeof(AshenWitchSkill) };

    public override void Initialize()
    {
        MaxCD = -1;
        MagicPointCost = -1;
        base.Initialize();
    }

    public override bool CanUseSkill()
    {
        if (!AreAllPrerequisitesActive(Player.GetModPlayer<ElainaSkillModPlayer>()) || !IsEnabled)
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
        ElainaAttributeModPlayer attributePlayer = player.GetModPlayer<ElainaAttributeModPlayer>();
        return Math.Max(0f, attributePlayer.MaxMagicPoint * MaximumManaShieldRatio + BaseShieldAmount);
    }

    protected override object[] SkillDescriptionArgs => Array.Empty<object>();

    public class MagicBarrierModPlayer : ModPlayer
    {
        private const int BarrierInvincibilityTicks = 30;
        private const int DotBlockVisualCooldownTicks = 30;

        private bool barrierEnabled;
        private bool absorbingLifeRegenDamage;
        private int dotBlockVisualCooldown;

        public float CurrentShield => Player.GetModPlayer<ElainaAttributeModPlayer>().MagicPoint;
        public float MaximumShield => GetMaximumShield(Player);
        public int BrokenShieldCooldownRemaining => 0;
        public bool BarrierEnabled => barrierEnabled;
        public bool ShieldBroken => barrierEnabled && CurrentShield <= 0f;
        public float ShieldRatio => MaximumShield <= 0f
            ? 0f
            : MathHelper.Clamp(CurrentShield / MaximumShield, 0f, 1f);

        public override void Initialize()
        {
            barrierEnabled = false;
            absorbingLifeRegenDamage = false;
            dotBlockVisualCooldown = 0;
        }

        public override void ResetEffects()
        {
            barrierEnabled = false;
            absorbingLifeRegenDamage = false;
        }

        public void EnableBarrier()
        {
            barrierEnabled = true;
        }

        /// <summary>
        /// Direct damage is resolved exactly once in the finalized hurt-info callback.
        /// Full absorption cancels the hit and applies the old barrier-style immunity;
        /// partial absorption leaves only the uncovered damage for vanilla to apply.
        /// </summary>
        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            if (!CanProcessLocalDamage())
            {
                return;
            }

            modifiers.ModifyHurtInfo += ResolveDirectHit;
        }

        private void ResolveDirectHit(ref Player.HurtInfo info)
        {
            if (!CanAbsorbDamage() || info.Damage <= 0)
            {
                return;
            }

            ElainaAttributeModPlayer attributePlayer = Player.GetModPlayer<ElainaAttributeModPlayer>();
            float incomingDamage = info.Damage;
            float consumed = attributePlayer.ConsumeAvailableMagicPoint(incomingDamage);
            if (consumed >= incomingDamage)
            {
                info.Cancelled = true;
                Player.SetImmuneTimeForAllTypes(BarrierInvincibilityTicks);
            }
            else
            {
                info.Damage = Math.Max(1, (int)MathF.Ceiling(incomingDamage - consumed));
            }

            ShowBarrierHit(consumed);
        }

        /// <summary>
        /// Restores the previous barrier behavior for bleeding, burning, starving and other
        /// life-regen damage. Fully covered DoT is removed; overflow remains vanilla damage.
        /// </summary>
        public override void UpdateLifeRegen()
        {
            absorbingLifeRegenDamage = false;

            if (!CanProcessLocalDamage())
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

            int projectedLifeRegenCount = Player.lifeRegenCount + Player.lifeRegen;
            if (projectedLifeRegenCount >= 0)
            {
                return;
            }

            int incomingDamage = GetLifeRegenDamage(ref projectedLifeRegenCount);
            Player.lifeRegenCount = projectedLifeRegenCount;
            Player.lifeRegen = 0;
            if (incomingDamage <= 0)
            {
                return;
            }

            ElainaAttributeModPlayer attributePlayer = Player.GetModPlayer<ElainaAttributeModPlayer>();
            float consumed = attributePlayer.ConsumeAvailableMagicPoint(incomingDamage);
            int remainingDamage = Math.Max(0, (int)MathF.Ceiling(incomingDamage - consumed));
            if (remainingDamage > 0)
            {
                // Vanilla applies one life point per -120 lifeRegenCount. Reinsert only
                // the uncovered portion after extracting the full DoT amount above.
                Player.lifeRegen = -remainingDamage * 120;
            }
            else
            {
                absorbingLifeRegenDamage = true;
            }

            if (consumed > 0f && dotBlockVisualCooldown <= 0)
            {
                dotBlockVisualCooldown = DotBlockVisualCooldownTicks;
                ShowBarrierHit(consumed);
            }
        }

        public override void NaturalLifeRegen(ref float regen)
        {
            if (absorbingLifeRegenDamage)
            {
                regen = 0f;
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

        private bool CanProcessLocalDamage()
        {
            return Main.netMode != NetmodeID.MultiplayerClient || Player.whoAmI == Main.myPlayer;
        }

        private bool CanAbsorbDamage()
        {
            return barrierEnabled
                && Player.GetModPlayer<ElainaAttributeModPlayer>().UniqueMagicEnabled
                && CurrentShield > 0f;
        }

        private void ShowBarrierHit(float amount)
        {
            if (amount <= 0f || Player.whoAmI != Main.myPlayer)
            {
                return;
            }

            CombatText.NewText(Player.Hitbox, new Color(180, 180, 180, 255), $"-{amount:0.#}");
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
