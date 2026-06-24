using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using 伊蕾娜.炼金;

namespace 伊蕾娜.Items.accessories
{
    public class ElainaHatLayer : PlayerDrawLayer
    {
        private Asset<Texture2D> ElainaHatTexture;
        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {

            //if (drawInfo.drawPlayer.name == "Korate") return true;

            if (drawInfo.drawPlayer.GetModPlayer<ElainaModplayer>().hat&& !drawInfo.drawPlayer.sleeping.isSleeping)
            {
                return true;
            }
            return false;
        }
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);
        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            drawInfo.hatHair = true;
            Vector2 move = new(-0, 0);
            if (ElainaHatTexture == null) ElainaHatTexture =
                    Mod.Assets.Request<Texture2D>("Items/accessories/ElainHatFrame");
            SpriteEffects SE = drawInfo.playerEffect;
            // var position = drawInfo.drawPlayer.MountedCenter.Floor() + new Vector2(-19f, -4f + drawInfo.drawPlayer.gfxOffY) - Main.screenPosition;
            Vector2 position = new Vector2((int)(drawInfo.Position.X - Main.screenPosition.X - (float)(drawInfo.drawPlayer.bodyFrame.Width / 2) + (float)(drawInfo.drawPlayer.width / 2)), (int)(drawInfo.Position.Y - Main.screenPosition.Y + (float)drawInfo.drawPlayer.height - (float)drawInfo.drawPlayer.bodyFrame.Height + 4f)) + drawInfo.drawPlayer.headPosition + drawInfo.headVect;
            //if (drawInfo.drawPlayer.GetModPlayer<炼金modplayer>().搅拌中) move += new Vector2(0, -10);
            //if (drawInfo.drawPlayer.mount.Active) position.Y += drawInfo.drawPlayer.mount.HeightBoost/2;
            int frameHeight = ElainaHatTexture.Height() / 20;
            int startY = 0;
            float rotation = drawInfo.drawPlayer.headRotation;

            if (drawInfo.drawPlayer.bodyFrame.Y >= 392) startY = frameHeight * (drawInfo.drawPlayer.legFrame.Y / 56);

            
            if (drawInfo.drawPlayer.GetModPlayer<ElainaModplayer>().掉落帽子 >= 2)
            {
                if (drawInfo.drawPlayer.GetModPlayer<ElainaModplayer>().掉落帽子 > 2 && drawInfo.shadow == 0) drawInfo.drawPlayer.GetModPlayer<ElainaModplayer>().掉落帽子--;
                Vector2 endposition = new(0, 56);
                move = Vector2.Lerp(endposition, Vector2.Zero, drawInfo.drawPlayer.GetModPlayer<ElainaModplayer>().掉落帽子 / 150f);
            }

            var p = drawInfo.drawPlayer.GetModPlayer<炼金modplayer>();
            if (p.搅拌中)
            {
                rotation = 0;
                move += new Vector2(0, drawInfo.drawPlayer.HeightOffsetVisual/2);

                if (p.bodyFrame >= 5 && p.bodyFrame <= 8)
                {
                    move += new Vector2(p.bodyFrame - 4, 0);
                }
                if (p.bodyFrame == 9)
                {
                    move += new Vector2(p.bodyFrame - 4, 0);
                }
                if (p.bodyFrame >= 10 && p.bodyFrame <= 12)
                {
                    move += new Vector2(13 - p.bodyFrame, 0);
                }
            }

            if (p.炼制中)
            {
                rotation = 0;
            }
            if (drawInfo.drawPlayer.sitting.isSitting)
            {
                //position += new Vector2(0, -5);
            }

            position = position.Floor();


            Rectangle sourceRectangle = new(0, startY, ElainaHatTexture.Width(), frameHeight);
            Vector2 origin = drawInfo.headVect;
            drawInfo.DrawDataCache.Add(new DrawData(
                ElainaHatTexture.Value, // The texture to render.
                position + move, // Position to render at.
                sourceRectangle, // Source rectangle.
                drawInfo.colorArmorHead, // Color.
                rotation, // Rotation.
                origin, // Origin. Uses the texture's center.
                1f, // Scale.
                SE, // SpriteEffects.
                0 // 'Layer'. This is always 0 in Terraria.
            )
            {  shader = drawInfo.cHead});//shader = drawInfo.drawPlayer.dye[itemslot%10].dye
        }
    }
}