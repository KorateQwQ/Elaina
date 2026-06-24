using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Terraria.GameContent;
using Terraria.Localization;
using System.Collections.Generic;
using Terraria.ModLoader.IO;
using System.IO;

namespace 伊蕾娜.Items.accessories
{
    public class 妮可的冒险谭 : ModItem
    {
        public string[] DefeatedNpcName = new string[100];
        int count = 0;
        public override void SaveData(TagCompound tag)
        {
            tag["count"] = count;
            for (int i = 0; i < 100; i++)
            {
                tag[$"DefeatedNpcName{i}"] = DefeatedNpcName[i];
            }
            base.SaveData(tag);
        }
        public override void LoadData(TagCompound tag)
        {
            count = tag.GetInt("count");
            for (int i = 0; i < 100; i++)
            {
                DefeatedNpcName[i] = tag.GetString($"DefeatedNpcName{i}");
            }
            base.LoadData(tag);
        }
        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(count);
            for (int i = 0; i < count; i++)
            {
                writer.Write(DefeatedNpcName[i]);
            }
            base.NetSend(writer);
        }
        public override void NetReceive(BinaryReader reader)
        {
            count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                DefeatedNpcName[i] = reader.ReadString();
            }
            base.NetReceive(reader);
        }
        public override void SetStaticDefaults()
        {
            Item.vanity = true;
            // DisplayName.SetDefault("The Adventures of Nikeh");
            //DisplayName.AddTranslation(7, "妮可的冒险谭");
            // Tooltip.SetDefault("Whenever you defeat a different boss, the magic damage to all boss-type enemies increases by 1%.");
            //Tooltip.AddTranslation(7, "每当你击败一个不同的boss，对所有boss类型的敌人造成的魔法伤害就增加百分之1。");
            //由星星面纱升级而来
        }
        public override void SetDefaults()
        {
            for (int i = 0; i < DefeatedNpcName.Length; i++)
            {
                DefeatedNpcName[i] = "";
            }

            Item.width = 34;
            Item.height = 34;
            Item.accessory = true; // Makes this item an accessory.
            Item.hasVanityEffects = true;
            Item.rare = ItemRarityID.Pink;
            Item.value = Item.sellPrice(silver: 1); // Sets the item sell price to one gold coin.
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            foreach (TooltipLine line2 in tooltips)
            {
                if (line2.Mod == "Terraria" && line2.Name == "ItemName")
                {
                    Color c = Color.Gray;
                    line2.OverrideColor = c;
                }
            }
            if (count > 0)
            {
                {
                    TooltipLine line = new(Mod, "NikehDamage", $"Increase damage to boss by {count}%");
                    if (Language.ActiveCulture.Name == "zh-Hans")
                        line = new TooltipLine(Mod, "妮可额外伤害", $"增加 {count}% 对boss伤害");
                    tooltips.Add(line);
                }
                {
                    TooltipLine line = new(Mod, "NikehIntroduction", $"The boss you have defeated:");
                    if (Language.ActiveCulture.Name == "zh-Hans")
                        line = new TooltipLine(Mod, "妮可的受害名单", $"已击败的boss:");
                    tooltips.Add(line);
                }
                string[] AllLine = ["", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", ""];//十行
                for (int i = 0; i < count; i++)
                {
                    AllLine[i % 20] = AllLine[i % 20] + DefeatedNpcName[i] + "      ";
                }
                for (int i = 0; i < 20; i++)
                {
                    TooltipLine line2 = new(Mod, "DefeatedBoss", AllLine[i]);
                    line2.OverrideColor = Color.Black;
                    tooltips.Add(line2);
                }
            }

            //Main.NewText(ChatManager.GetStringSize(FontAssets.MouseText.Value, " ", Vector2.One* Main.UIScale) +" "+ DefeatedNpcName[index]);
            base.ModifyTooltips(tooltips);
        }
        public override bool CanEquipAccessory(Player player, int slot, bool modded)
        {
            if (!player.GetModPlayer<ElainaModplayer>().Elaina)
            {
                return false;
            }
            return base.CanEquipAccessory(player, slot, modded);
        }
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<ElainaModplayer>().Nikeh = true;
            player.GetModPlayer<ElainaModplayer>().ExtraBossDamage = count;

            if (player.GetModPlayer<ElainaModplayer>().TheBossJustDefeat.Length >= 1 && count < 100)
            {
                //Main.NewText(player.GetModPlayer<ElainaModplayer>().TheBossJustDefeat);
                bool repeatname = false;
                int realcount = 0;
                foreach (string fullname in DefeatedNpcName)
                {
                    if (fullname.Length > 0) realcount++;
                    if (fullname.Equals(player.GetModPlayer<ElainaModplayer>().TheBossJustDefeat)) repeatname = true;
                }
                if (count != realcount) count = realcount;
                if (!repeatname)
                {
                    DefeatedNpcName[count++] = player.GetModPlayer<ElainaModplayer>().TheBossJustDefeat;
                    //Main.NewText(player.GetModPlayer<ElainaModplayer>().TheBossJustDefeat);
                }
                player.GetModPlayer<ElainaModplayer>().TheBossJustDefeat = "";
            }
            //Main.NewText(player.GetModPlayer<ElainaModplayer>().SayoProtectCD);

            //player.SetImmuneTimeForAllTypes
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Item.rare = ItemRarityID.Quest;
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, Main.DefaultSamplerState,
                DepthStencilState.Default, RasterizerState.CullNone, null, Main.UIScaleMatrix);
            spriteBatch.Draw(TextureAssets.Item[Type].Value, position, frame, drawColor, 0f, new Vector2(54 / 2, 54 / 2), scale * 1.5f, SpriteEffects.None, 0f);
            return false;
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
            Lighting.AddLight(Item.position, 1, 1, 1);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, Main.DefaultSamplerState,
                DepthStencilState.Default, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            spriteBatch.Draw(TextureAssets.Item[Type].Value, Item.position + new Vector2(24, 24), new Rectangle(0, 0, 57, 57), lightColor, Item.velocity.ToRotation(), new Vector2(54 * 1.5f / 2, 54 * 1.5f / 2), scale * 1.5f, SpriteEffects.None, 0f);

            return base.PreDrawInWorld(spriteBatch, lightColor, alphaColor, ref rotation, ref scale, whoAmI);
        }
        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.Default, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            base.PostDrawInWorld(spriteBatch, lightColor, alphaColor, rotation, scale, whoAmI);
        }
    }
}
