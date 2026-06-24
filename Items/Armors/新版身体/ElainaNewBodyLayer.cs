using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace 伊蕾娜.Items.Armors.新版身体
{
    public class ElainaNewBodyLayer : PlayerDrawLayer
    {
        private static Asset<Texture2D> ElainaBodyTexture;
        private static Asset<Texture2D> ElainaFrontHandTexture;
        private static Asset<Texture2D> ElainaSittingBodyTexture;
        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            if (drawInfo.drawPlayer.GetModPlayer<ElainaModplayer>().Elaina&&!drawInfo.drawPlayer.invis&&
                (!drawInfo.drawPlayer.GetModPlayer<ElainaModplayer>().BecomeAnimal|| !drawInfo.drawPlayer.active) && !drawInfo.drawPlayer.sleeping.isSleeping
                && !drawInfo.drawPlayer.GetModPlayer<炼金.炼金modplayer>().炼制中 && !drawInfo.drawPlayer.GetModPlayer<炼金.炼金modplayer>().搅拌中)
            return true;
            return false;
        }

        public override Position GetDefaultPosition() => new Between(PlayerDrawLayers.Skin,PlayerDrawLayers.Leggings);
        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            //测试手部
            int timeframe = (int)(Main.GameUpdateCount / 6 % 10 + 7) * 56;
            //timeframe = 11 * 56;
            //drawInfo.drawPlayer.bodyFrame.Y = timeframe;
            //drawInfo.drawPlayer.legFrame.Y = timeframe;
            //drawInfo.drawPlayer.headFrame.Y = timeframe;
            
            Vector2 ps = drawInfo.Position.Floor() - Main.screenPosition; 
            //drawInfo.drawPlayer.gravDir = -1;
            Asset<Texture2D> bodyTexture = ElainaBodyTexture;
            //Main.NewText(drawInfo.drawPlayer.bodyFrame.Y);
            SpriteEffects SE = SpriteEffects.None;
            if (drawInfo.drawPlayer.direction < 0) SE = SpriteEffects.FlipHorizontally;
            ps += new Vector2(10, 18);
            ps += new Vector2(0, drawInfo.drawPlayer.HeightOffsetVisual+ drawInfo.drawPlayer.HeightOffsetHitboxCenter);

            int frame = drawInfo.drawPlayer.legFrame.Y;

            //PrintText(Main.gamePaused);
            //PrintText(drawInfo.drawPlayer.headFrame);
            Vector2 origin = new(0.5f, 0.025f);
            //Main.NewText(drawInfo.drawPlayer.legFrame + " "+drawInfo.drawPlayer.bodyFrame);
            if (drawInfo.drawPlayer.GetModPlayer<ElainaModplayer>().sittingmount || (drawInfo.drawPlayer.mount.Active&& drawInfo.drawPlayer.legFrame.Y==336))
            {
                if (drawInfo.drawPlayer.sitting.isSitting)
                {
                    ps += new Vector2(0, drawInfo.seatYOffset);

                    //drawInfo.drawPlayer.bodyPosition = new Vector2(0, 2);
                }
                bodyTexture = ElainaSittingBodyTexture;
                frame = 0;
                origin = Vector2.One * 0.5f;
                //drawInfo.drawPlayer.headPosition = new Vector2(0, -3);
                drawInfo.drawPlayer.GetModPlayer<ElainaModplayer>().sittingmount = false;
            }
            else
            {
                drawInfo.drawPlayer.bodyFrame.Y = frame;
            }

            float graRotation = 0;
            if (drawInfo.drawPlayer.gravDir < 0)//判断重力翻转时
            {
                //ps += new Vector2(1f, 21f);// y越大图越上
                if (drawInfo.drawPlayer.direction == 1)  {
                    SE = SpriteEffects.FlipVertically;
                    ps += new Vector2(0, 6);
                }
                else
                {
                    graRotation = 3.14f;
                    ps += new Vector2(0, 6f);// y越大图越上
                    SE = SpriteEffects.None;
                }
            }
            Vector2 bodyOrigin = bodyTexture.Size() * origin;
            drawInfo.DrawDataCache.Add(new DrawData(
                bodyTexture.Value, // The texture to render.
                ps.Floor(), // Position to render at.
                new Rectangle(0, frame, 40, 56), // Source rectangle.
                drawInfo.colorArmorBody, // Color.
                drawInfo.drawPlayer.bodyRotation+ graRotation, // Rotation.
                bodyOrigin, // Origin. Uses the texture's center.
                1f, // Scale.
                SE, // SpriteEffects.
                0 // 'Layer'. This is always 0 in Terraria.
            )
            { shader = drawInfo.drawPlayer.cBody });
            
            //如果身体帧数大于等于7则绘制手臂
            if (bodyTexture == ElainaBodyTexture && drawInfo.drawPlayer.bodyFrame.Y / 56 >= 7&&!drawInfo.drawPlayer.compositeFrontArm.enabled&&drawInfo.drawPlayer.itemAnimation<=0)
            {
                int frontHandFrame = drawInfo.drawPlayer.bodyFrame.Y - 7 * 56;
                if (frontHandFrame + 56 <= ElainaFrontHandTexture.Value.Height)
                {
                    drawInfo.DrawDataCache.Add(new DrawData(
                        ElainaFrontHandTexture.Value,
                        ps.Floor(),
                        new Rectangle(0, frontHandFrame, 40, 56),
                        drawInfo.colorArmorBody,
                        drawInfo.drawPlayer.bodyRotation + graRotation,
                        bodyOrigin,
                        1f,
                        SE,
                        0
                    )
                    { shader = drawInfo.drawPlayer.cBody });
                }
            }
            
        }

        public override void Load()
        {
            ElainaBodyTexture = Mod.Assets.Request<Texture2D>("Items/Armors/新版身体/身体部分");
            ElainaSittingBodyTexture = Mod.Assets.Request<Texture2D>("Items/Armors/新版身体/坐姿");
            ElainaFrontHandTexture = Mod.Assets.Request<Texture2D>("Items/Armors/新版身体/Elaina_FrontHand_Running");
            base.Load();
        }
    }
}