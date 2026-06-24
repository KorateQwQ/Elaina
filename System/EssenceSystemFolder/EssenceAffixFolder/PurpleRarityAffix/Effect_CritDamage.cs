namespace 伊蕾娜.System.EssenceSystemFolder.EssenceAffixFolder.PurpleRarityAffix;

public sealed class Effect_CritDamage : IEssenceAffixEffect
{
    public static readonly Effect_CritDamage Instance = new();

    public void UpdateAccessory(Player player, in EssenceAffixEffectContext ctx)
    {
        player.GetModPlayer<Effect_CritExtraDamageModifier>().CritDamage+= ctx.GetLinearFactor()*0.24f;
    }

    public string GetDescription(in EssenceAffixEffectContext ctx)
    {
        float value = ctx.GetLinearFactor()*24f;
        string valueText = value.ToString("0.0");

        return Language.GetText("Mods.伊蕾娜.EssenceSystem.CommonItemTooltip.PercentIncreasedCritDamage")
            .WithFormatArgs(valueText).Value;
    }

    class Effect_CritExtraDamageModifier : ModPlayer
    {
        public float CritDamage = 0;
        public override void ResetEffects()
        {
            CritDamage = 0;
            base.ResetEffects();
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.CritDamage += CritDamage;
            base.ModifyHitNPC(target, ref modifiers);
        }
        
    }
    
}