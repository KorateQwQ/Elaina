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

    // 需要角色当前等级达到 5 级（不是等级上限）
    public override SkillUnlockCondition UnlockCondition { get; set; } =
        new CompositeSkillUnlockCondition(DefaultUnlockCost, RequireCharacterLevel(5));

    public override void Initialize()
    {
        MaxCD = -1;
        MagicPointCost = -1;
        base.Initialize();
    }

    private bool IsActiveFor(Player player)
    {
        return !player.dead && BasicStatus == Skill.SKillBasicStatus.UnLock && IsEnabled
            && player.GetModPlayer<ElainaAttributeModPlayer>().UniqueMagicEnabled
            && AreAllPrerequisitesActive(player.GetModPlayer<ElainaSkillModPlayer>());
    }

    public override void UpdateEquips(Player player)
    {
        // 被动自行检查拥有者的生效条件，不走面向本地主动施法的 CanUseSkill。
        if (IsActiveFor(player))
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
        private bool barrierEnabled;

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
        }

        public override void ResetEffects()
        {
            barrierEnabled = false;
        }

        public void EnableBarrier()
        {
            barrierEnabled = true;
        }

        public double DamageReduction
        {
            get
            {
                if (!barrierEnabled || !Player.GetModPlayer<ElainaAttributeModPlayer>().UniqueMagicEnabled
                    || !Player.GetModPlayer<ElainaSkillModPlayer>().TryGetUnlockedModSkill(out AshenWitchSkill witch))
                {
                    return 0d;
                }

                return BarrierDamageMath.GetReduction(Player.statLifeMax2, witch.LostMaxLife);
            }
        }

        public override void UpdateDead()
        {
            barrierEnabled = false;
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            if (CanProcessLocalDamage())
            {
                modifiers.ModifyHurtInfo += ResolveDirectHit;
            }
        }

        private void ResolveDirectHit(ref Player.HurtInfo info)
        {
            if (info.Cancelled || !CanAbsorbDamage() || info.Damage <= 0)
            {
                return;
            }

            double requested = info.Damage * DamageReduction;
            double consumed = ConsumeAbsorption(requested);
            int remainingDamage = BarrierDamageMath.GetRemainingDamage(info.Damage, consumed);
            int absorbed = info.Damage - remainingDamage;
            ReportReduction(info.Damage, consumed, absorbed);
            if (remainingDamage == 0)
            {
                // HurtInfo.Damage cannot be zero. Cancel sub-one damage after rounding
                // and preserve the normal minimum-hit immunity for this damage channel.
                info.Cancelled = true;
                ApplyMinimumHitImmunity(info);
                Player.lifeRegenTime = 0f;
            }
            else
            {
                info.Damage = remainingDamage;
            }
        }

        private void ApplyMinimumHitImmunity(Player.HurtInfo info)
        {
            int ticks = Player.longInvince ? 40 : 20;
            if (info.CooldownCounter == -1)
            {
                Player.immune = true;
                Player.immuneTime = Math.Max(Player.immuneTime, info.PvP ? 8 : ticks);
            }
            else if (info.CooldownCounter == 0 || info.CooldownCounter == 1
                || info.CooldownCounter == 3 || info.CooldownCounter == 4)
            {
                Player.hurtCooldowns[info.CooldownCounter] =
                    Math.Max(Player.hurtCooldowns[info.CooldownCounter], ticks);
            }
        }

        internal void ApplyLifeRegenFloor()
        {
            if (!barrierEnabled || Player.dead
                || !Player.GetModPlayer<ElainaAttributeModPlayer>().UniqueMagicEnabled)
            {
                return;
            }

            // Every endpoint applies the synchronized passive, independently of magic.
            // Clear previous negative progress too, so enabling it cannot leave a DoT tick pending.
            Player.lifeRegen = Math.Max(0, Player.lifeRegen);
            Player.lifeRegenCount = Math.Max(0, Player.lifeRegenCount);
        }

        private bool CanProcessLocalDamage()
        {
            // 受击方结算最终伤害与魔力消耗，远端只应用受伤包和生命同步。
            return Main.netMode != NetmodeID.Server && Player.whoAmI == Main.myPlayer;
        }

        private void ReportReduction(int incomingDamage, double paidDamage,
            int absorbedDamage, bool dot = false)
        {
            if (Main.netMode == NetmodeID.Server || Player.whoAmI != Main.myPlayer || absorbedDamage <= 0)
            {
                return;
            }

            double roundingBonus = Math.Max(0d, absorbedDamage - paidDamage);
            string extraInfo = roundingBonus > 0.00001d
                ? $"，其中向下取整额外减伤：{roundingBonus:0.######}"
                : "";
            string source = dot ? "持续伤害" : "直接受击";
            PrintText($"[魔力护盾/{source}] 本次实际减伤：{absorbedDamage}，"
                + $"伤害 {incomingDamage} → {incomingDamage - absorbedDamage}{extraInfo}");
            ShowAbsorbedDamage(absorbedDamage, dot);
        }

        private void ShowAbsorbedDamage(int amount, bool dot = false)
        {
            if (amount <= 0 || Main.netMode == NetmodeID.Server || Player.whoAmI != Main.myPlayer)
            {
                return;
            }

            CombatText.NewText(Player.Hitbox, new Color(200,200,200), $"-{amount}",
                dramatic: false, dot: dot);

            Projectile.NewProjectile(
                Player.GetSource_FromThis(),
                Player.MountedCenter,
                Vector2.Zero,
                ModContent.ProjectileType<MagicBarrierProj>(),
                0,
                0f,
                Player.whoAmI);
        }

        private double ConsumeAbsorption(double requested)
        {
            ElainaAttributeModPlayer attributePlayer = Player.GetModPlayer<ElainaAttributeModPlayer>();
            double payable = Math.Min(requested, Math.Max(0f, attributePlayer.MagicPoint));
            float consumed = attributePlayer.ConsumeAvailableMagicPoint((float)payable);
            // Resource storage uses floats. Subtracting a tiny payment from a large
            // pool loses precision; do not accumulate that subtraction error as damage.
            return consumed > 0f ? payable : 0f;
        }

        private bool CanAbsorbDamage()
        {
            return barrierEnabled
                && Player.GetModPlayer<ElainaAttributeModPlayer>().UniqueMagicEnabled
                && !Player.dead
                && CurrentShield > 0f;
        }
    }
}
