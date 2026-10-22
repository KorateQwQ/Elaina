using static 伊蕾娜.Localization.UIText;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using 伊蕾娜.ElainaModAlchemy.item.Recipes;

namespace 伊蕾娜.ElainaModAlchemy.Gameplay;

/// <summary>Physical scroll acquisition and provisional economy values.</summary>
public sealed class AlchemyRecipeSources : GlobalNPC
{
    public static int ScrollValue => Item.buyPrice(gold: 5);
    public const int BloodthirstDropDenominator = 100;

    public override void ModifyShop(NPCShop shop)
    {
        switch (shop.NpcType)
        {
            case NPCID.Dryad:
                shop.Add<PurificationDewRecipe>(Condition.Hardmode);
                break;
            case NPCID.Wizard:
                shop.Add<StarPowerPotionRecipe>();
                break;
            case NPCID.ArmsDealer:
                shop.Add<ConcentrationPotionRecipe>(Condition.TimeNight);
                break;
            case NPCID.WitchDoctor:
                shop.Add<ResonancePotionRecipe>(Condition.TimeNight);
                break;
        }
    }

    internal static bool IsCombatEnemy(NPC npc) => !npc.friendly && !npc.townNPC &&
        npc.lifeMax > 5 && npc.damage > 0 && npc.value > 0;

    public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
    {
        if (IsCombatEnemy(npc))
            npcLoot.Add(ItemDropRule.ByCondition(new BloodthirstRecipeCondition(),
                ModContent.ItemType<BloodthirstPotionRecipe>(), BloodthirstDropDenominator));
    }

    internal sealed class BloodthirstRecipeCondition : IItemDropRuleCondition
    {
        public bool CanDrop(DropAttemptInfo info) => !info.IsInSimulation && Main.hardMode &&
            Main.bloodMoon && !Main.dayTime && !info.npc.SpawnedFromStatue && IsCombatEnemy(info.npc) &&
            (info.npc.realLife < 0 || info.npc.realLife == info.npc.whoAmI);
        public bool CanShowItemDropInUI() => true;
        public string GetConditionDescription() => LocalizeUI("Catalog.HardmodeBloodMoonEnemyDrop");
    }
}
