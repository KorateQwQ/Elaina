using System;
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using ReLogic.Content;
using 伊蕾娜.Items.消耗品;
using 伊蕾娜.Items.accessories;
using System.Collections.ObjectModel;

namespace 伊蕾娜.Items
{

    public class gItem : GlobalItem
    {
        public override bool InstancePerEntity => true;
        static Effect DrawEffect;
        static Effect 扰动shader;
        static Asset<Texture2D> noise;
        bool predraw = false;

        public override void Load()
        {
            if (Main.netMode != 2)
            {
                DrawEffect = Mod.Assets.Request<Effect>("Projectiles/Effects/Content/DrawByGivenColor", AssetRequestMode.ImmediateLoad).Value;
                扰动shader = Mod.Assets.Request<Effect>("Projectiles/Effects/Content/扰动", AssetRequestMode.ImmediateLoad).Value;
                noise = Mod.Assets.Request<Texture2D>("Projectiles/Perlin");
            }

            base.Load();
        }
        public override bool PreDrawTooltip(Item item, ReadOnlyCollection<TooltipLine> lines, ref int x, ref int y)
        {
            return base.PreDrawTooltip(item, lines, ref x, ref y);
        }
        public override bool PreDrawTooltipLine(Item item, DrawableTooltipLine line, ref int yOffset)
        {
            //DrawEffect = Mod.Assets.Request<Effect>("Projectiles/Effects/Content/Draw", AssetRequestMode.ImmediateLoad).Value;
            //if (!predraw)
            {
                if (line.Name == "ItemName" && line.Mod == "Terraria")
                {
                    if (item.type == ModContent.ItemType<寒霜药水>())
                    {
                        //line.BaseScale = new Vector2(1.5f,1.5f);
                        line.Spread = 1.3f;
                        float 渐变 = 30 - (float)Main.GameUpdateCount % 60;//30到 -30
                        Vector4 c = new(52 / 255f, 188 / 255f, 185 / 255f, 105 / 255f);
                        Vector4 c1 = c*0.7f;
                        Vector4 c2 = Vector4.Lerp(c, c1, (float)Math.Abs(渐变) / 30f);
                        Main.spriteBatch.End();
                        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointWrap,
                        DepthStencilState.Default, RasterizerState.CullNone, null, Main.UIScaleMatrix);
                        DrawEffect.Parameters["GivenColor"].SetValue(c2);
                        DrawEffect.CurrentTechnique.Passes["DrawByGivenColor"].Apply();//开启shader
                    }
                    if (item.type == ModContent.ItemType<雷引药水>())
                    {
                        line.Spread = 1.3f;
                        float 渐变 = 30 - (float)Main.GameUpdateCount % 60;//30到 -30
                        Vector4 c = new(255 / 255f, 207 / 255f, 73 / 255f, 105 / 255f);
                        Vector4 c1 = new(205 / 255f, 167 / 255f, 43 / 255f, 75 / 255f);
                        Vector4 c2 = Vector4.Lerp(c, c1, (float)Math.Abs(渐变) / 30f);
                        Main.spriteBatch.End();
                        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointWrap,
                        DepthStencilState.Default, RasterizerState.CullNone, null, Main.UIScaleMatrix);
                        DrawEffect.Parameters["GivenColor"].SetValue(c2);
                        DrawEffect.CurrentTechnique.Passes["DrawByGivenColor"].Apply();//开启shader
                    }
                    if (item.type == ModContent.ItemType<妮可的冒险谭>())
                    {
                        line.Spread = 1.3f;

                        float 渐变 = 30 - (float)Main.GameUpdateCount % 60;//30到 -30
                        Vector4 c = new(255 / 255f, 255 / 255f, 255 / 255f, 105 / 255f);
                        Vector4 c1 = new(55 / 55f, 67 / 255f, 43 / 255f, 75 / 255f);
                        Vector4 c2 = Vector4.Lerp(c, c1, (float)Math.Abs(渐变) / 30f);
                        Main.spriteBatch.End();
                        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointWrap,
                        DepthStencilState.Default, RasterizerState.CullNone, null, Main.UIScaleMatrix);
                        DrawEffect.Parameters["GivenColor"].SetValue(c2);
                        DrawEffect.CurrentTechnique.Passes["DrawByGivenColor"].Apply();//开启shader
                    }
                    if (item.type == ModContent.ItemType<白河豚项链>())
                    {
                        line.Spread = 1.1f;
                        float 最大亮度 = 0.7f;
                        float 渐变亮度 = 0.5f;
                        float 渐变 = 30 - (float)Main.GameUpdateCount % 60;//30到 -30
                        Vector4 c = new(255* 最大亮度 / 255f, 160 * 最大亮度 / 255f, 239 * 最大亮度 / 255f, 155*最大亮度 / 255f);
                        Vector4 c1 = new(255* 渐变亮度 / 255f, 160 * 渐变亮度 / 255f, 239 * 渐变亮度 / 255f, 155 * 渐变亮度 / 255f);
                        Vector4 c2 = Vector4.Lerp(c, c1, (float)Math.Abs(渐变) / 30f);
                        Main.spriteBatch.End();
                        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointWrap,
                        DepthStencilState.Default, RasterizerState.CullNone, null, Main.UIScaleMatrix);
                        DrawEffect.Parameters["GivenColor"].SetValue(c2);
                        DrawEffect.CurrentTechnique.Passes["DrawByGivenColor"].Apply();//开启shader
                    }
                }
                if (line.Name == "DefeatedBoss" && line.Mod == "伊蕾娜")
                {
                    float 渐变 = 30 - (float)Main.GameUpdateCount % 60;//30到 -30
                    Vector4 c = new(155 / 255f, 155 / 255f, 155 / 255f, 255 / 255f);
                    Vector4 c1 = new(100 / 255f,100 / 255f, 100 / 255f, 255 / 255f);
                    Vector4 c2 = Vector4.Lerp(c, c1, (float)Math.Abs(渐变) / 30f);
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap,
                    DepthStencilState.Default, RasterizerState.CullNone, null, Main.UIScaleMatrix);
                    noise = Mod.Assets.Request<Texture2D>("Projectiles/Perlin");
                    扰动shader.Parameters["tex0"].SetValue(noise.Value);
                    扰动shader.Parameters["uTime"].SetValue((Main.GameUpdateCount % 60) / 60f);
                    扰动shader.Parameters["strength"].SetValue(0.008f);
                    扰动shader.Parameters["GivenColor"].SetValue(c2);
                    扰动shader.CurrentTechnique.Passes["moveAndColor"].Apply();//开启shader
                                                                     //DrawEffect.Parameters["GivenColor"].SetValue(c2);
                                                                     //DrawEffect.CurrentTechnique.Passes["DrawByGivenColor"].Apply();//开启shader
                }
            }
            //else predraw = false;
            return base.PreDrawTooltipLine(item, line, ref yOffset);
        }
        public override void PostDrawTooltipLine(Item item, DrawableTooltipLine line)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap,
            DepthStencilState.Default, RasterizerState.CullNone, null, Main.UIScaleMatrix);
            base.PostDrawTooltipLine(item, line);
        }
    }


}
