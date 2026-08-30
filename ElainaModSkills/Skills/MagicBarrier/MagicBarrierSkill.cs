using System;
using KL.Extensions;
using KL.SkillSystem;
using KL.SkillSystem.SilkyUI;
using KL.Utils;
using 伊蕾娜.ElainaAttribute;
using 伊蕾娜.Items;

namespace 伊蕾娜.ElainaModSkills.Skills.MagicBarrier;

[SkillUIInfo(State = 0, Pixels = 100)]
public class MagicBarrierSkill : ElainaSkill
{

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
            player.endurance += GetEndurance(player);
            //
        }
        base.UpdateEquips(player);
    }

    public static float GetEndurance(Player player)
    {
        //魔力屏障：10%魔力以下生效最小值20%减伤，50%魔力时达到50%减伤，80%魔力时达到最大70%减伤（超过50%蓝量的额外减伤有生效条件：最大蓝量首先需要大于600，当前蓝量高于80%并且最大魔力值高于1000时可达到此最大值）
        float minimumMagicPointRatio = 0.1f;
        float midpointMagicPointRatio = 0.5f;
        float fullReductionMagicPointRatio = 0.8f;
        
        float minimumDamageReduction = 0.2f;
        float midpointDamageReduction = 0.5f;
        float fullDamageReduction = 0.7f;
        
        //ElainaAttributeModPlayer attributePlayer = player.GetModPlayer<ElainaAttributeModPlayer>();
        if (player.statMana> 0f&&
            (player.HeldItem.IsAir||!player.HeldItem.IsWeapon()||player.HeldItem.type == ModContent.ItemType<ElainaWand>())||player.HeldItem.DamageType== DamageClass.Magic)
        {
            float magicPointRatio = (player.statMana / (float)player.statManaMax2);
            if (magicPointRatio >= minimumMagicPointRatio)
            {
                float reduction;
                if (magicPointRatio <= midpointMagicPointRatio)
                {
                    float reductionProgress = midpointMagicPointRatio <= minimumMagicPointRatio
                        ? 1f
                        : MathHelper.Clamp(
                            (magicPointRatio - minimumMagicPointRatio) /
                            (midpointMagicPointRatio - minimumMagicPointRatio), 0f, 1f);
                    reduction = MathHelper.Lerp(
                        minimumDamageReduction, midpointDamageReduction, reductionProgress);
                }
                else
                {
                    // 50% 以上的额外减伤由当前蓝量和最大魔力共同决定。
                    // 最大魔力 600 时没有额外减伤，1000 时才允许达到完整的额外减伤。
                    float maximumDamageReduction = GetMaximumDamageReduction(player);
                    float reductionProgress = fullReductionMagicPointRatio <= midpointMagicPointRatio
                        ? 1f
                        : MathHelper.Clamp(
                            (magicPointRatio - midpointMagicPointRatio) /
                            (fullReductionMagicPointRatio - midpointMagicPointRatio), 0f, 1f);
                    reduction = MathHelper.Lerp(
                        midpointDamageReduction, maximumDamageReduction, reductionProgress);
                }
                return reduction;
            }

            return minimumDamageReduction;
        }

        return 0;
    }

    public static float GetMaximumDamageReduction(Player player)
    {
        float maximumManaProgress = MathHelper.Clamp(
            (player.statManaMax2 - 600f) / 400f, 0f, 1f);
        return MathHelper.Lerp(0.5f, 0.7f, maximumManaProgress);
    }

    protected override object[] SkillDescriptionArgs => new object[]
    {
        Math.Max(0,GetMaximumDamageReduction(Player) * 100f-50),
    };

    class MagicBarrierModPlayer : ModPlayer
    {

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            base.ModifyHurt(ref modifiers);
        }
        

        public override void OnHurt(Player.HurtInfo info)
        {
            //ElainaAttributeModPlayer attributePlayer = Player.GetModPlayer<ElainaAttributeModPlayer>();
            ElainaSkillModPlayer skillPlayer = Player.GetModPlayer<ElainaSkillModPlayer>();
            if (skillPlayer.TryGetUnlockedModSkill<MagicBarrierSkill>(out var magicBarrierSkill))
            {
                if (magicBarrierSkill.IsEnabled)
                {
                    float endurance = GetEndurance(Player);
                    float reductionDamage = endurance*info.SourceDamage;
                    if (endurance > 0)
                    {
                        float magicPointCost = reductionDamage;//Math.Min(reductionDamage, Player.statManaMax2*0.2f);
                        if (Player.statMana>magicPointCost)
                        {
                            //attributePlayer.ConsumeMagicPoint(magicPointCost);
                            Player.statMana -= (int)magicPointCost;
                        }
                        else Player.statMana = 0;

                        Player.manaRegenDelay =150;
                        Projectile.NewProjectile(Player.GetSource_FromThis(), Player.MountedCenter, Vector2.Zero, ModContent.ProjectileType<MagicBarrierProj>(), 0,
                            0, Player.whoAmI);
                        //PrintText($"魔法屏障：受到 {info.SourceDamage}伤害 护盾减免率{endurance} 消耗魔力：{magicPointCost:0.#}, 最终受到伤害：{info.Damage}"); 
                    }
                }
            }


            base.OnHurt(info);
        }
    }
}
