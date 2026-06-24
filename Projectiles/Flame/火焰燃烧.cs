using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;

namespace 伊蕾娜.Projectiles.Flame
{
    public class 火焰燃烧 : ModProjectile
    {
        static Asset<Texture2D>  火焰燃烧texture;
        private int 渐变系数 = 0;
        public Vector2[] 随机火焰位置 { get; private set; }
        public float[] 随机火焰大小 { get; private set; }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 4;
            // DisplayName.SetDefault("火焰");
        }
        public override void SetDefaults()
        {
            Projectile.penetrate = -1; // 穿透数量
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;//瓷砖碰撞
            Projectile.friendly = true;
            //projectile.timeLeft=30;
            //projectile.extraUpdates=1;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.damage = 20;
            Projectile.timeLeft = 300;
            Projectile.knockBack = 2;
            //Projectile.CritChance = (int)Main.player[Projectile.owner].GetTotalCritChance(DamageClass.Magic);
            //Projectile.extraUpdates = ;
            Projectile.alpha = 0;

        }
        public override void Load()
        {
            火焰燃烧texture = Mod.Assets.Request<Texture2D>("Projectiles/Flame/火焰燃烧");
            base.Load();
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (随机火焰位置 == null)
            {
                随机火焰位置 = new Vector2[(int)Projectile.ai[0]];
                随机火焰大小 = new float[(int)Projectile.ai[0]];
                {
                    for (int k = 0; k < (int)Projectile.ai[0]; k++)
                    {
                        随机火焰位置[k] = new Vector2(Main.rand.NextFloat(-18, 18), Main.rand.NextFloat(-18, 18));
                        随机火焰大小[k] = Main.rand.NextFloat(1.6f, 2f);
                    }
                }
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointWrap,
                DepthStencilState.Default, RasterizerState.CullNone, null, Main.Transform);
            Vector2 origin = 火焰燃烧texture.Size() / new Vector2(4f, 4f) * 0.5f;//除3相当于以图片中心为position.除以X,Y,此时为从上往下，从左往右的帧图
            int frameWidth = 火焰燃烧texture.Width() / 4;//图片总高度除以长度，得到每张图的长度
            int frameHeight = 火焰燃烧texture.Height() / 4;//图片总高度除以高度，得到每张图的高度
            int startX = frameWidth * (Projectile.frame % 4);//每一帧的起始坐标X
            int startY = frameHeight * (Projectile.frame / 4);//每一帧的起始坐标Y
            Rectangle sourceRectangle = new(startX, startY, frameWidth, frameHeight);//坐标x，坐标y，图片长度，图片高度，得到一张完整图片

            Color c = new(255, 255, 255);
            c.A = (byte)(255f - Projectile.alpha);
            //Main.NewText(df.alpha+" "+df.渐变系数);

            //Main.NewText( "数量2：" + df.随机火焰数量);
            for (int i = 0; i < (int)Projectile.ai[0]; i++)
            {
                Vector2 randomPosition = Vector2.Zero;
                //if(df.随机火焰位置!=null)
                randomPosition = 随机火焰位置[i];

                float 逐渐减小 = MathHelper.Lerp(1, 0, Projectile.alpha / 255f);
                Main.spriteBatch.Draw(火焰燃烧texture.Value, Projectile.position + randomPosition - Main.screenPosition, sourceRectangle, c, 0f, origin, 随机火焰大小[i] * 逐渐减小, SpriteEffects.None, 0f);
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
            DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overWiresUI.Add(index);
            base.DrawBehind(index, behindNPCsAndTiles, behindNPCs, behindProjectiles, overPlayers, overWiresUI);
        }
        public override void AI()
        {
            if (Projectile.wet) Projectile.Kill();
            渐变系数++;
            Projectile.alpha = (int)MathHelper.Lerp(0, 255,渐变系数 * 渐变系数 / 3600f);
            if(Projectile.alpha>250)Projectile.Kill();

            Projectile.frameCounter++;
            if (Projectile.frameCounter % 6 == 0)
            {
                Projectile.frame += 1;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= 16)
            {
                Projectile.frame = 0;
                Projectile.Kill();
            }
            base.AI();
        }
    }
}
