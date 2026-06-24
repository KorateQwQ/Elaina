using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Terraria.Localization;
using ReLogic.Content;
using Terraria.DataStructures;
using System.Collections.Generic;
using 伊蕾娜.炼金;

namespace 伊蕾娜.Items.accessories
{
    public class 白河豚项链 : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.vanity = true;
            // DisplayName.SetDefault("White dolphin necklace");
            //DisplayName.AddTranslation(7, "白河豚项链");
            /* Tooltip.SetDefault("The necklace will protect you when you face death, 5 min cd\n" +
                "The barrier will automatically activate when you are hit by a projectile\n" +
                "Increases length of invincibility after taking damage\n" +
                "10% increased magic damage\n"); */
            /*Tooltip.AddTranslation(7, "白河豚的力量会在你濒死时为你抵挡致命伤害，冷却时间5分钟\n" +
                "即将受到弹幕伤害时，自动展开护盾\n" +
                "延长受伤后无敌状态时间\n" +
                "增加百分之10魔法伤害\n");*/    
            //由星星面纱升级而来
        }
        public override void SetDefaults()
        {
            Item.width = 34;
            Item.height = 34;
            Item.accessory = true; // Makes this item an accessory.
            Item.hasVanityEffects = true;
            Item.rare = ItemRarityID.Pink;
            Item.value = Item.sellPrice(silver: 1); // Sets the item sell price to one gold coin.
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {

            var player = Main.LocalPlayer;
            if (player.GetModPlayer<ElainaModplayer>().SayoProtectCD > 0)
            {
                Color c = new(255, 48, 215, 255);
                string time = "";
                string unit = "";
                if (player.GetModPlayer<ElainaModplayer>().SayoProtectCD > 3600)
                {
                    time = (int)(player.GetModPlayer<ElainaModplayer>().SayoProtectCD / 3600) + "";
                    unit = " minutes";
                    if (Language.ActiveCulture.Name == "zh-Hans")
                    {
                        unit = " 分钟";
                    }
                }
                else
                {
                    time = (int)(player.GetModPlayer<ElainaModplayer>().SayoProtectCD / 60) + "";
                    unit = " seconds";
                    if (Language.ActiveCulture.Name == "zh-Hans")
                    {
                        unit = " 秒钟";
                    }
                }
                string CDTip = "Need ";
                string end = " to get ready.";
                if (Language.ActiveCulture.Name == "zh-Hans")
                {
                    CDTip = "还有 ";
                    end = "准备完毕";
                }
                TooltipLine line3 = new(Mod, "物品描述", CDTip+ time+ unit+ end);
                line3.OverrideColor = c;
                tooltips.Add(line3);
            }
            base.ModifyTooltips(tooltips);
        }
        public override void AddRecipes()
        {
            Condition NearElaina = new("RecipeConditions.NearElaina", () => Main.LocalPlayer.GetModPlayer<ElainaModplayer>().Elaina);
            Recipe recipe = CreateRecipe();
            //recipe.AddRecipeGroup(RecipeGroupID.Wood, 1);//任意木头1
            recipe.AddIngredient(ItemID.CrossNecklace, 1);//十字项链
            recipe.AddIngredient(ItemID.AvengerEmblem, 1);//复仇者勋章
            recipe.AddIngredient(ItemID.FragmentNebula, 150);//星云碎片
            recipe.AddCondition(NearElaina);
            recipe.Register();
        }
        public override void UpdateVanity(Player player)
        {
            base.UpdateVanity(player);
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {   //34*27
            Item.rare = ItemRarityID.Pink;

            return true;
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
            player.GetModPlayer<ElainaModplayer>().SayoNecklace = true;
            player.longInvince = true;
            if(!hideVisual&& player.GetModPlayer<ElainaModplayer>().SayoProtectCD<=0) player.GetModPlayer<ElainaModplayer>().SayoNecklaceVisual = true;
            //Main.NewText(player.GetModPlayer<ElainaModplayer>().SayoProtectCD);

            //player.SetImmuneTimeForAllTypes
        }

    }
    public class 项链闪光 : PlayerDrawLayer {
        static Asset<Texture2D> texture;
        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {

            //if (drawInfo.drawPlayer.name == "Korate") return true;

            if ((drawInfo.drawPlayer.GetModPlayer<ElainaModplayer>().SayoProtectCD<=0|| 
                drawInfo.drawPlayer.GetModPlayer<ElainaModplayer>().SayoProtectCD>17970) && !drawInfo.drawPlayer.sleeping.isSleeping&& drawInfo.drawPlayer.GetModPlayer<ElainaModplayer>().SayoNecklace)
            {
                return true;
            }
            return false;
        }
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.ArmOverItem);
        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            if (texture == null) texture =
                    Mod.Assets.Request<Texture2D>("Projectiles/Extra_98");
            SpriteEffects SE = SpriteEffects.None;
            if (drawInfo.drawPlayer.direction != 1) SE = SpriteEffects.FlipHorizontally;
            // var position = drawInfo.drawPlayer.MountedCenter.Floor() + new Vector2(-19f, -4f + drawInfo.drawPlayer.gfxOffY) - Main.screenPosition;
            var position = drawInfo.Position.Floor() -Main.screenPosition;

            position += new Vector2(0, drawInfo.drawPlayer.HeightOffsetVisual + drawInfo.drawPlayer.HeightOffsetHitboxCenter);
            position += new Vector2(10, 20);
            float timescale = MathHelper.Lerp(0.3f, 0.5f, Math.Abs(30f - Main.GameUpdateCount % 60) / 30f);
            var p = drawInfo.drawPlayer.GetModPlayer<炼金modplayer>();
            if (p.搅拌中)
            {
                position += new Vector2(0, -3);
                if (p.bodyFrame >= 5 && p.bodyFrame <= 8)
                {
                    position += new Vector2(p.bodyFrame - 4, 0);
                }
                if (p.bodyFrame == 9)
                {
                    position += new Vector2(p.bodyFrame - 4, 0);
                }
                if (p.bodyFrame >= 10 && p.bodyFrame <= 12)
                {
                    position += new Vector2(13 - p.bodyFrame, 0);
                }
            }
            //Main.NewText(drawInfo.rotation + " " + drawInfo.drawPlayer.fullRotation);
            if (drawInfo.drawPlayer.GetModPlayer<ElainaModplayer>().SayoProtectCD > 17970)
            {
                timescale = MathHelper.Lerp(5f, 0f, Math.Abs(18000 - drawInfo.drawPlayer.GetModPlayer<ElainaModplayer>().SayoProtectCD) / 30f);
            }
            float fade = drawInfo.colorArmorBody.A/255f;
            for (int i = 0; i < 5; i++)
            {
                float rotation = 1.57f-drawInfo.rotation;
                Vector2 scale = new Vector2(0.2f*i, 0.7f+0.2f*i)*0.7f* timescale;
                Rectangle sourceRectangle = new(0, 0, texture.Width(), texture.Height());
                Vector2 origin = texture.Size() * 0.5f;
                Color c = new Color(232, 136, 232, 30) * fade;
                //Main.spriteBatch.Draw(texture.Value, position, sourceRectangle, c, rotation, origin, scale, SE, 0);
                drawInfo.DrawDataCache.Add(new DrawData(texture.Value, position, sourceRectangle, c, rotation, origin, scale, SE, 0));
            }
            for (int i = 0; i < 5; i++)
            {
                float rotation =- drawInfo.rotation;
                Vector2 scale = new Vector2(0.1f * i, 0.3f + 0.2f * i) * 0.7f * timescale;
                Rectangle sourceRectangle = new(0, 0, texture.Width(), texture.Height());
                Vector2 origin = texture.Size() * 0.5f;
                Color c = new Color(232, 136, 232, 30) * fade;
                //Main.spriteBatch.Draw(texture.Value, position, sourceRectangle, c, rotation, origin, scale, SE, 0);
                drawInfo.DrawDataCache.Add(new DrawData(texture.Value, position, sourceRectangle, c, rotation, origin, scale, SE, 0));
            }

        }
    }

}
