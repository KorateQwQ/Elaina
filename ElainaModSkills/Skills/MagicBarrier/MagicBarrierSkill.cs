using System;
using KL.SkillSystem;
using KL.SkillSystem.SilkyUI;
using 伊蕾娜.ElainaAttribute;

namespace 伊蕾娜.ElainaModSkills.Skills.MagicBarrier;

[SkillUIInfo(State = 0, Pixels = 60)]
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
        //魔力屏障：20%魔力时开始生效，80%魔力时完全生效，最小30%伤害减免，最大50%伤害减免
        float minimumMagicPointRatio = 0.2f;
        float fullReductionMagicPointRatio = 0.8f;
        float minimumDamageReduction = 0.3f;
        float fullDamageReduction = 0.5f;
        
        ElainaAttributeModPlayer attributePlayer = Player.GetModPlayer<ElainaAttributeModPlayer>();
        if (IsEnabled && attributePlayer.MaxMagicPoint > 0f)
        {
            float magicPointRatio = attributePlayer.MagicPoint / attributePlayer.MaxMagicPoint;
            if (magicPointRatio >= minimumMagicPointRatio)
            {
                float reductionProgress = fullReductionMagicPointRatio <= minimumMagicPointRatio
                    ? 1f
                    : MathHelper.Clamp(
                        (magicPointRatio - minimumMagicPointRatio) /
                        (fullReductionMagicPointRatio - minimumMagicPointRatio), 0f, 1f);
                float reduction = MathHelper.Lerp(
                    minimumDamageReduction, fullDamageReduction, reductionProgress);
                Player.endurance += reduction;
            }
        }
        base.UpdateEquips(player);
    }

    class MagicBarrierModPlayer : ModPlayer
    {
        private bool barrierAppliedToCurrentHit;

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            ElainaSkillModPlayer skillPlayer = Player.GetModPlayer<ElainaSkillModPlayer>();

            base.ModifyHurt(ref modifiers);
        }

        public override void OnHurt(Player.HurtInfo info)
        {
            //魔力屏障：20%魔力时开始生效
            float minimumMagicPointRatio = 0.2f;
            ElainaAttributeModPlayer attributePlayer = Player.GetModPlayer<ElainaAttributeModPlayer>();
            float magicPointRatio = attributePlayer.MagicPoint / attributePlayer.MaxMagicPoint;
            if (magicPointRatio >= minimumMagicPointRatio)
            {
                float magicPointCost = Math.Min(info.Damage, attributePlayer.MagicPoint);
                attributePlayer.ConsumeMagicPoint(magicPointCost);
                barrierAppliedToCurrentHit = false;
                PrintText($"魔法屏障：受到 {info.Damage} 伤害，消耗魔力：{magicPointCost:0.#}");
            }
            PrintText($"魔法屏障：受到 {info.Damage} 伤害");

            base.OnHurt(info);
        }
    }
}