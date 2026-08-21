using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Terraria.GameContent;
using 伊蕾娜.ElainaAttribute;

namespace 伊蕾娜.Items.accessories
{
    public class ElainaHat : ModItem
    {

        public override void SetStaticDefaults()
        {
            Item.vanity = true;
            /*if (Language.ActiveCulture.Name == "en-US")
            {
                DisplayName.SetDefault("Elaina's Hat");
                Tooltip.SetDefault("It can probably be used to hide from the rain");
            }
            else
            {
                DisplayName.SetDefault("伊蕾娜的帽子");
                Tooltip.SetDefault("也许可以用来躲雨");
            }*/
            // DisplayName.SetDefault("Elaina's Hat");
            //DisplayName.AddTranslation(7, "伊蕾娜的帽子");
            // Tooltip.SetDefault("10% increased magic damage, 15% reduced mana cost and increases maximum mana by 20");
            //Tooltip.AddTranslation(7, "增加百分之10魔法伤害,减少百分之15魔力消耗并增加20最大魔力值");
        }
        public override void SetDefaults()
        {
            Item.manaIncrease = 20;
            Item.width = 40;
            Item.height = 70;
            Item.accessory = true; // Makes this item an accessory.
            Item.hasVanityEffects = true;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(silver: 1); // Sets the item sell price to one gold coin.
            Item.manaIncrease = 20;
        }
        public override void UpdateVanity(Player player)
        {
            //player.manaCost -= 1f;
            var p = player.GetModPlayer<ElainaModplayer>();
            if (p.Elaina && !p.hide) p.hat = true;
            base.UpdateVanity(player);
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {   //34*27
            position += new Vector2(-10, -13);

            Rectangle f = new(5, 0, 34, 27);
            spriteBatch.Draw(TextureAssets.Item[Item.type].Value, position + new Vector2(15, 10), f, drawColor, 0, new Vector2(22, 15), scale * 2.5f, SpriteEffects.None, 0f);
            return false;
        }
        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            return base.PreDrawInWorld(spriteBatch, lightColor, alphaColor, ref rotation, ref scale, whoAmI);
        }
        public override bool CanEquipAccessory(Player player, int slot, bool modded)
        {
            if (!player.GetModPlayer<ElainaModplayer>().Elaina) return false;
            return base.CanEquipAccessory(player, slot, modded);
        }
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetDamage<MagicDamageClass>() += 0.1f;
            player.statManaMax2 += 120;
            //player.manaCost -= 0.15f;
            player.manaRegenBonus += 120;//145每个饰品给60
            player.manaRegenDelayBonus += 1;//145延迟4，每个饰品给1，星星瓶0.5，站立不动1，钩爪状态1，魔力药水1
            player.GetModPlayer<ElainaAttributeModPlayer>().MaxMagicPoint += 20;
            //player.GetCritChance<MagicDamageClass>()+= 100f;
            if (!hideVisual)
            {
                //player.statManaMax = 20;
                var p = player.GetModPlayer<ElainaModplayer>();
                if (p.Elaina && !p.hide) p.hat = true;
            }
        }
    }
}
