using Microsoft.Xna.Framework.Graphics;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using 伊蕾娜.System;

namespace 伊蕾娜.ElainaModSkills.item;

public sealed class FlameSpellbook : ModItem
{
    public override string Texture => "伊蕾娜/Items/魔法书/火焰魔法书";
    public override void SetStaticDefaults()
    {
        Main.RegisterItemAnimation(Type, new DrawAnimationVertical(7, 6));
        Item.ResearchUnlockCount = 1;
    }
    public override void SetDefaults()
    {
        Item.width = Item.height = 60;
        Item.maxStack = Item.CommonMaxStack;
        Item.rare = ItemRarityID.LightRed;
    }
    public override bool PreDrawInInventory(SpriteBatch batch, Vector2 position, Rectangle frame, Color drawColor,
        Color itemColor, Vector2 origin, float scale)
    {
        batch.Draw(TextureAssets.Item[Type].Value, position, frame, drawColor, 0, new Vector2(30), scale * 1.7f, SpriteEffects.None, 0);
        return false;
    }
    public override void AddRecipes() => CreateRecipe()
        .AddIngredient(ItemID.MagmaStone)
        .AddIngredient(ItemID.SpellTome)
        .AddTile(TileID.Bookcases)
        .AddCondition(ElainaSystem.IsElaina)
        .AddCondition(Condition.DownedMechBossAny)
        .Register();
}
