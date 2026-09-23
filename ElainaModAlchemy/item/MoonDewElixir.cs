using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using 伊蕾娜.ElainaAttribute;

namespace 伊蕾娜.ElainaModAlchemy.item;

/// <summary>
/// 月露合剂的炼金物品。
/// 配方暂未配置；饮用后会像魔力合剂一样写入伊蕾娜的专属魔力药水槽。
/// </summary>
public sealed class MoonDewElixir : AlchemyItem
{
    private const int RestoreAmount = 50;

    public override string LocalizationCategory => "Alchemy.Items";

    // 月露合剂使用琉璃空瓶作为基础贴图；液体由物品绘制阶段的着色器叠加。
    public override string Texture => "伊蕾娜/ElainaModAlchemy/item/RuriPotionEmpty";

    // AlchemyRecipe 暂为空，避免在材料尚未确定时引入临时配方。
    public override bool IsRecipeUnlocked(Player player) => false;

    public override void SetDefaults()
    {
        Item.width = 20;
        Item.height = 30;
        Item.maxStack = Item.CommonMaxStack;
        Item.consumable = true;
        Item.useStyle = ItemUseStyleID.DrinkLiquid;
        Item.useTime = 17;
        Item.useAnimation = 17;
        Item.UseSound = SoundID.Item3;
        Item.useTurn = true;
        Item.rare = ItemRarityID.Blue;
        Item.value = 0;
    }

    public override bool CanUseItem(Player player)
    {
        return player.GetModPlayer<ElainaManaElixirPlayer>().CanStoreCharge(RestoreAmount);
    }

    public override bool? UseItem(Player player) => true;

    public override bool ConsumeItem(Player player)
    {
        return player.GetModPlayer<ElainaManaElixirPlayer>().CanStoreCharge(RestoreAmount);
    }

    public override void OnConsumeItem(Player player)
    {
        player.GetModPlayer<ElainaManaElixirPlayer>().TryStoreCharge(RestoreAmount);
    }

    // The item texture is the empty bottle; the liquid is supplied by PotionLiquid.fx.
    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame,
        Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        PotionLiquidRenderer.DrawRuri(position, drawColor, 0f, origin, scale,
            PotionLiquidRenderer.MoonDewColor, 0.52f, inUI: true, flowSpeed: 0.85f, waveStrength: 0.8f);
        return false;
    }

    public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor,
        ref float rotation, ref float scale, int whoAmI)
    {
        // Match vanilla's item draw origin so the 50x50 bottle is anchored by Item.Bottom.
        Vector2 bottleOrigin = PotionLiquidRenderer.RuriBottle.Size() / 2f;
        Vector2 position = Item.Bottom - Main.screenPosition - new Vector2(0f, bottleOrigin.Y);
        PotionLiquidRenderer.DrawRuri(position, alphaColor, rotation, bottleOrigin, scale,
            PotionLiquidRenderer.MoonDewColor, 0.52f, inUI: false, flowSpeed: 0.85f, waveStrength: 0.8f);
        return false;
    }
}
