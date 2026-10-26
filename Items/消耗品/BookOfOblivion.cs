using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace 伊蕾娜.Items.消耗品;

/// <summary>
/// 忘却之书：持有时技能手札的技能详情出现“忘却”操作，消耗一本把已习得技能退回 Lv.1，
/// 并返还进修花费的研习点。规则见 <see cref="ElainaModSkills.SkillForgetting"/>。暂无获取途径。
/// </summary>
public class BookOfOblivion : ModItem
{
    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 3;
    }

    public override void SetDefaults()
    {
        Item.width = 32;
        Item.height = 32;
        Item.maxStack = Item.CommonMaxStack;
        Item.rare = ItemRarityID.Yellow;
        Item.value = Item.sellPrice(0, 5);
    }
}
