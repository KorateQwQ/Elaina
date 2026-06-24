using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KL.Extensions;
using Terraria.GameContent;

namespace 伊蕾娜.Managers
{
    internal static class DrawHelper
    {
        #region 弹幕绘制
        public static void endBegin(this Projectile projectile, int state = 0, int Defferred = 0, bool adjustToScreen = true,SamplerState samplerState=null)
        {
            BlendState blendState = BlendState.AlphaBlend;
            if(samplerState==null) samplerState = Main.DefaultSamplerState;

            if (state == 1) blendState = BlendState.Additive;
            if (state == 2) blendState = BlendState.NonPremultiplied;
            Main.spriteBatch.End();
            if (!adjustToScreen)
            {
                Main.spriteBatch.Begin((SpriteSortMode)Defferred, blendState, samplerState,
                    DepthStencilState.None, RasterizerState.CullNone, null);
            }
            else
            {
                Main.spriteBatch.Begin((SpriteSortMode)Defferred, blendState, samplerState,
                    DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            }
        }
        public static void endBegin(this Dust dust, int state = 0, int Defferred = 0, bool adjustToScreen = true, SamplerState samplerState = null)
        {
            BlendState blendState = BlendState.AlphaBlend;
            if (samplerState == null) samplerState = Main.DefaultSamplerState;

            if (state == 1) blendState = BlendState.Additive;
            if (state == 2) blendState = BlendState.NonPremultiplied;
            Main.spriteBatch.End();
            if (!adjustToScreen)
            {
                Main.spriteBatch.Begin((SpriteSortMode)Defferred, blendState, samplerState,
                    DepthStencilState.None, RasterizerState.CullNone, null);
            }
            else
            {
                Main.spriteBatch.Begin((SpriteSortMode)Defferred, blendState, samplerState,
                    DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            }
        }
        public static void beginEnd(this Projectile projectile, int state = 0, int Defferred = 0, bool adjustToScreen = true)
        {
            BlendState blendState = BlendState.AlphaBlend;
            if (state == 1) blendState = BlendState.Additive;
            if (state == 2) blendState = BlendState.NonPremultiplied;

            if (!adjustToScreen)
            {
                Main.spriteBatch.Begin((SpriteSortMode)Defferred, blendState, Main.DefaultSamplerState,
                    DepthStencilState.None, RasterizerState.CullNone, null);
            }
            else
            {
                Main.spriteBatch.Begin((SpriteSortMode)Defferred, blendState, Main.DefaultSamplerState,
                    DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            }
            Main.spriteBatch.End();
        }
        public static void BeginDraw(this Projectile projectile, int state = 0, int Defferred = 0, bool adjustToScreen = true)
        {
            BlendState blendState = BlendState.AlphaBlend;
            if (state == 1) blendState = BlendState.Additive;
            if (state == 2) blendState = BlendState.NonPremultiplied;

            if (!adjustToScreen)
            {
                Main.spriteBatch.Begin((SpriteSortMode)Defferred, blendState, Main.DefaultSamplerState,
                    DepthStencilState.None, RasterizerState.CullNone, null);
            }
            else
            {
                Main.spriteBatch.Begin((SpriteSortMode)Defferred, blendState, Main.DefaultSamplerState,
                    DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            }
        }
        
        public static void DrawSelf(this Projectile projectile, float scale = 1, Color? color = null)
        {
            if (color == null) color = Color.White;
            var tex = TextureAssets.Projectile[projectile.type].Value;
            Main.spriteBatch.Draw(tex, projectile.Center - Main.screenPosition, tex.Frame(), (Color)color,
                projectile.rotation, tex.Origin(), scale, SpriteEffects.None, 0);
        }
        public static void Draw(this Projectile projectile,Texture2D tex, float scale = 1, Color? color = null)
        {
            if (color == null) color = Color.White;
            Main.spriteBatch.Draw(tex, projectile.Center - Main.screenPosition, tex.Frame(), (Color)color,
                projectile.rotation, tex.Origin(), scale, SpriteEffects.None, 0);
        }
        public static void Draw(this Projectile projectile, Texture2D tex, Vector2? scale = null, Color? color = null)
        {
            if (color == null) color = Color.White;
            Main.spriteBatch.Draw(tex, projectile.Center - Main.screenPosition, tex.Frame(), (Color)color,
                projectile.rotation, tex.Origin(), scale==null?Vector2.One:(Vector2)scale, SpriteEffects.None, 0);
        }
        public static void SaveScreen(Effect shader=null)
        {
            GraphicsDevice gd = Main.instance.GraphicsDevice;
            SpriteBatch sb = Main.spriteBatch;
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, shader);
            gd.SetRenderTarget(Main.screenTargetSwap);//在这个上面绘制一遍原图，相当于“保存”
            gd.Clear(Color.Transparent);
            sb.Draw(Main.screenTarget, Vector2.Zero, Color.White);
        }
        public static void SwitchRender(RenderTarget2D target,Effect shader = null)
        {
            GraphicsDevice gd = Main.instance.GraphicsDevice;
            SpriteBatch sb = Main.spriteBatch;
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, shader);

            gd.SetRenderTarget(target);
            gd.Clear(Color.Transparent);

        }

        #endregion

    }
}
