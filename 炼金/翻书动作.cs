using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace 伊蕾娜.炼金
{
    public class 翻书动作 : PlayerDrawLayer
    {
        private Asset<Texture2D> Texture;
        private int bodyFramecounter = 0;
        private int bodyFrame = 0;

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            if (drawInfo.drawPlayer.GetModPlayer<炼金modplayer>().炼制中)
            {
                return true;
            }
            return false;
        }
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Torso);

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            if (Texture == null) Texture = Mod.Assets.Request<Texture2D>("炼金/翻书");

            //Main.NewText("haha");
            Vector2 origin = Texture.Size() / 11 * 0.5f;
            drawInfo.DrawDataCache.Add(new DrawData(
                Texture.Value, // The texture to render.
                drawInfo.Center - Main.screenPosition + new Vector2(-18, -3), // Position to render at.
                new Rectangle(0, 56 * drawInfo.drawPlayer.GetModPlayer<炼金modplayer>().bodyFrame, 40, 56), // Source rectangle.
                drawInfo.colorArmorBody, // Color.
                0, // Rotation.
                origin, // Origin. Uses the texture's center.
                1f, // Scale.
                SpriteEffects.None, // SpriteEffects.
                0 // 'Layer'. This is always 0 in Terraria.
            )
            { shader = drawInfo.drawPlayer.cBody });
        }

    }
}
