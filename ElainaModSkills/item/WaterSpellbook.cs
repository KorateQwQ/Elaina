using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using 伊蕾娜.System;

namespace 伊蕾娜.ElainaModSkills.item;

/// <summary>
/// 研习水流术所需的水魔法书，研习时消耗一本。复用旧版水魔法书的贴图（60×60，10 帧）。
/// 合成需持有水矢但不消耗；禁止微光拆解、不可出售，避免凭一本水矢刷出水矢或钱币。
/// 每个角色只能制作一本，由 <see cref="WaterSpellbookPlayer"/> 记录。
/// </summary>
public class WaterSpellbook : ModItem
{
    private const int FrameCount = 10;

    public override string Texture => "伊蕾娜/Items/魔法书/水魔法书";

    public override void SetStaticDefaults()
    {
        Main.RegisterItemAnimation(Type, new DrawAnimationVertical(7, FrameCount));
        Item.ResearchUnlockCount = 1;
    }

    public override void SetDefaults()
    {
        Item.width = 60;
        Item.height = 60;
        Item.rare = ItemRarityID.Green;
        Item.value = 0;
    }

    // 与旧版魔法书保持相同的物品栏显示大小。
    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor,
        Color itemColor, Vector2 origin, float scale)
    {
        spriteBatch.Draw(TextureAssets.Item[Type].Value, position, frame, drawColor, 0f, new Vector2(30, 30),
            scale * 1.7f, SpriteEffects.None, 0f);
        return false;
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.WaterBolt)
            .AddCondition(ElainaSystem.IsElaina)
            .AddCondition(NeverCrafted)
            .AddConsumeIngredientCallback(KeepWaterBolt)
            .DisableDecraft()
            .Register();
    }

    private static readonly Condition NeverCrafted = new("Mods.伊蕾娜.Conditions.NeverCraftedWaterSpellbook",
        () => !Main.LocalPlayer.GetModPlayer<WaterSpellbookPlayer>().Crafted);

    public override void OnCreated(ItemCreationContext context)
    {
        if (context is RecipeItemCreationContext)
            Main.LocalPlayer.GetModPlayer<WaterSpellbookPlayer>().Crafted = true;
    }

    private static void KeepWaterBolt(Recipe recipe, int type, ref int amount, bool isDecrafting)
    {
        if (type == ItemID.WaterBolt) amount = 0;
    }
}

public sealed class WaterSpellbookPlayer : ModPlayer
{
    public bool Crafted { get; set; }

    public override void SaveData(TagCompound tag) => tag[nameof(Crafted)] = Crafted;
    public override void LoadData(TagCompound tag) => Crafted = tag.GetBool(nameof(Crafted));
}
