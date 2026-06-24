using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace 伊蕾娜.System
{
    public class SleepLayer : PlayerDrawLayer
    {
        private Asset<Texture2D> ElainaHatTexture;
        public override void Load() => ElainaHatTexture = Mod.Assets.Request<Texture2D>("Items/Armors/sleep");
        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) => drawInfo.drawPlayer.GetModPlayer<ElainaModplayer>().Elaina && drawInfo.drawPlayer.sleeping.isSleeping;
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);
        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Vector2 move = new();
            SpriteEffects SE = SpriteEffects.None;
            if (drawInfo.drawPlayer.direction != 1)
            {
                SE = SpriteEffects.FlipHorizontally;
                move = new Vector2(-25, 0);
            }
            var position = drawInfo.Center - Main.screenPosition;
            position = new Vector2((int)position.X, (int)position.Y);
            int frameHeight = ElainaHatTexture.Height();
            float rotation = 0;

            Rectangle sourceRectangle = new(0, 0, ElainaHatTexture.Width(), frameHeight);
            Vector2 origin = new(52, 46);
            drawInfo.DrawDataCache.Add(new DrawData(ElainaHatTexture.Value, position + move,
                sourceRectangle, drawInfo.colorArmorHead, rotation, origin, 1f, SE, 0));
        }
    }
}
