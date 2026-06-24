using Terraria.ModLoader;
using Terraria;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace 伊蕾娜.Items
{
    public class MagicPowerPotionGItem : GlobalItem
    {
        public override bool InstancePerEntity => true;
        public override string Name => base.Name;
        public override bool PreDrawTooltip(Item item, ReadOnlyCollection<TooltipLine> lines, ref int x, ref int y)
        {
            return base.PreDrawTooltip(item, lines, ref x, ref y);
        }
        public override void PostDrawTooltip(Item item, ReadOnlyCollection<DrawableTooltipLine> lines)
        {
            base.PostDrawTooltip(item, lines);
        }
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(item, tooltips);
        }
        public override bool PreDrawInInventory(Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            return base.PreDrawInInventory(item, spriteBatch, position, frame, drawColor, itemColor, origin, scale);
        }
        public override void PostDrawInInventory(Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            base.PostDrawInInventory(item, spriteBatch, position, frame, drawColor, itemColor, origin, scale);
        }
        public override bool PreDrawInWorld(Item item, SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            return base.PreDrawInWorld(item, spriteBatch, lightColor, alphaColor, ref rotation, ref scale, whoAmI);
        }
        public override void PostDrawInWorld(Item item, SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            base.PostDrawInWorld(item, spriteBatch, lightColor, alphaColor, rotation, scale, whoAmI);
        }
        public override bool? UseItem(Item item, Player player)
        {
            return base.UseItem(item, player);
        }
    }
}
