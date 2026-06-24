using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System;
using 伊蕾娜.Buffs;

namespace 伊蕾娜.Items.消耗品
{
    public class 焰火药水 : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Flame Power Potion");
            //DisplayName.AddTranslation(7, "焰火药水");
            // Tooltip.SetDefault("20% increased Flame magic damage");
            //Tooltip.AddTranslation(7, "火魔法伤害提高20%");
            //DisplayName.SetDefault("究极魔力药水");
        }
        public override void SetDefaults()
        {
            //item.damage = 0;//伤害
            Item.noUseGraphic = true;
            Item.consumable = true;
            Item.useTime = 15;
            Item.useAnimation = 15;
            Item.width = 34;
            Item.height = 34;
            Item.noMelee = true;
            //item.knockBack = 6;//击退力
            Item.value = Item.sellPrice(0, 0, 1, 0);//价格
            Item.holdStyle = 0;
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            Item.UseSound = SoundID.Item3;//物品声音
            Item.maxStack = 30;//最大堆叠数量
            Item.buffType = ModContent.BuffType<焰火buff>(); // 药剂的buff类型，这个是魔力再生药水的效果
            Item.buffTime = 14400; // 药效持续时间，7200帧即2分钟
        }
        public override bool ConsumeItem(Player player)
        {
            return base.ConsumeItem(player);
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            float 渐变 = 30 - (float)Main.GameUpdateCount % 60;//30到 -30

            foreach (TooltipLine line2 in tooltips)
            {
                if (line2.Mod == "Terraria" && line2.Name == "ItemName")
                {
                    Color c = new(221, 0, 27, 255);
                    Color c1 = new(236, 65, 65, 255);
                    Color c2 = Color.Lerp(c, c1, (float)Math.Abs(渐变) / 30f);
                    line2.OverrideColor = c;
                }
            }
            base.ModifyTooltips(tooltips);
        }
        public override bool? UseItem(Player player)
        {
            Item.holdStyle = 0;
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            return base.UseItem(player);
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Item.rare = ItemRarityID.Quest;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, Main.DefaultSamplerState,
                DepthStencilState.Default, RasterizerState.CullNone, null, Main.UIScaleMatrix);
            return base.PreDrawInInventory(spriteBatch, position, frame, drawColor, itemColor, origin, scale);
        }
        public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.Default, RasterizerState.CullNone, null, Main.UIScaleMatrix);
            base.PostDrawInInventory(spriteBatch, position, frame, drawColor, itemColor, origin, scale);
        }
        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Item.rare = ItemRarityID.Quest;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, Main.DefaultSamplerState,
                DepthStencilState.Default, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            return base.PreDrawInWorld(spriteBatch, lightColor, alphaColor, ref rotation, ref scale, whoAmI);
        }
        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.Default, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            base.PostDrawInWorld(spriteBatch, lightColor, alphaColor, rotation, scale, whoAmI);
        }
        /*
         *         public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            return base.PreDrawInInventory(spriteBatch, position, frame, drawColor, itemColor, origin, scale);
        }
        public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            base.PostDrawInInventory(spriteBatch, position, frame, drawColor, itemColor, origin, scale);
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            return base.PreDrawInWorld(spriteBatch, lightColor, alphaColor, ref rotation, ref scale, whoAmI);
        }
        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            base.PostDrawInWorld(spriteBatch, lightColor, alphaColor, rotation, scale, whoAmI);
        }
         * 
         * 
         */
    }
}
