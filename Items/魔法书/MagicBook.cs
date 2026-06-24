using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using 伊蕾娜.System;

namespace 伊蕾娜.Items.魔法书
{
    public abstract class MagicBook : ModItem
    {
        private bool consume;
        public static Condition IsElaina => ElainaSystem.IsElaina;
        public abstract int Skill { get; }
        public abstract int Frame { get; }
        public override void SetStaticDefaults()
        {
            Main.itemAnimations[Type] = new DrawAnimationVertical(7, Frame);
            Main.itemAnimationsRegistered.Add(Type);
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            spriteBatch.Draw(TextureAssets.Item[Type].Value, position, frame, drawColor, 0f, new Vector2(60/ 2, 60 / 2), scale * 1.7f, SpriteEffects.None, 0f);

            return false;
        }
        public override void SetDefaults()
        {
            Item.noMelee = true;
            Item.width = 60;
            Item.height = 60;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.consumable = false;
            Item.healMana = 0;
            Item.rare = -11;
        }
        public override bool CanUseItem(Player player) => player.GetModPlayer<ElainaModplayer>().Elaina;
        public void UseBook(Player player)
        {
            if (!consume)
            {
                consume = true;
                player.itemTime = 30;
                player.itemAnimation = 30;
            }
            if (consume && player.itemAnimation <= 1)
            {
                /*var p = player.SKP();
                if (!p.IfLearnSkill[Skill])
                {
                    p.IfLearnSkill[Skill] = true;
                    if (Main.mouseItem != null && Main.mouseItem.type == Item.type)
                        Main.mouseItem.TurnToAir();
                    else
                    {
                        Item.TurnToAir();
                    }
                }*/
            }
        }
    }
}
