using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using System;
using 伊蕾娜.Dusts;

namespace 伊蕾娜.Projectiles.Magicwater
{
    public class 水粒子2 : ModProjectile
    {
        Asset<Texture2D> MainColor;
        Asset<Texture2D> MainShape;
        Asset<Texture2D> MaskColor;
        Asset<Texture2D> texture;
        public float 旋转角度 = 0;
        public 粒子[] 粒子组 = new 粒子[60];
        bool 逆向=false;
        float totalRotation = 0;

        public override void SetDefaults()
        {
            if (MainColor == null) MainColor = Mod.Assets.Request<Texture2D>("Projectiles/颜色2");
            if (MainShape == null) MainShape = Mod.Assets.Request<Texture2D>("Projectiles/Magicwater/水流");
            if (MaskColor == null) MaskColor = Mod.Assets.Request<Texture2D>("Projectiles/渐变");
            if (texture == null) texture = texture = Mod.Assets.Request<Texture2D>("Dusts/水花粒子");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 30;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 1200;
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = false;
            Projectile.extraUpdates = 0;
            base.SetDefaults();
        }
        public override bool PreDraw(ref Color lightColor)
        {
            /*float scale = MathHelper.Lerp(1, 3f, 1);
            for (int i = 0; i < scale * 5; i++)
            {
                Vector2 dustrandpos = Projectile.Center + Main.rand.NextVector2Circular(Projectile.width / 2f, Projectile.height / 2f);
                Dust d = Dust.NewDustDirect(dustrandpos, 0, 0, ModContent.DustType<水花粒子>(),
                0, 0, 255, Color.White, 1);
                d.noGravity = true;
            }*/
            绘制粒子(texture);
            return false;
        }
        public override void OnKill(int timeLeft)
        {
            

        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overPlayers.Add(index);
        }
        public override void AI()
        {   //damage决定跟随的类型，0为玩家，1为弹幕，2为敌人，knockback为跟随的实体id
            //ai[0]决定跟随方式
            //if (Main.time%15==0) 旋转角度 = Main.rand.NextFloat(-3.14f, 3.14f);
            Player player = Main.player[Projectile.owner];//读取主人位置
            if (Projectile.rotation < 0) 逆向 = true;
            //if (Projectile.owner == Main.myPlayer) Projectile.Kill();
            Projectile.localAI[0] += 1f;//计时器
            Entity target= player;
            if(Projectile.localAI[0] > 60) Projectile.localAI[0] = 0;
            Projectile.rotation = MathHelper.Lerp(0, (float)(2*Math.PI), Projectile.localAI[0] / 60f);
            switch (Projectile.damage)
            {
                case 0:
                    //Main.NewText("1231231");

                    if (Projectile.knockBack is >= 0 and <= 254 && Main.player[(int)Projectile.knockBack].active)
                        target = Main.player[(int)Projectile.knockBack];
                    else Projectile.Kill();
                    if (Projectile.ai[0] == 0)//椭圆环绕
                    {
                        Vector2 move = target.Center + new Vector2((float)Math.Cos(Projectile.rotation), 3 * (float)Math.Sin(Projectile.rotation)).RotatedBy(逆向 ? Math.PI / 2 : -Math.PI / 2 + Projectile.localAI[1] + 0.3f) * 20 - target.Center;
                        创造粒子(move, target, 0.6f);//创造的粒子相对于追踪目标的绝对位置
                    }
                    break;
                case 1:
                    if (Projectile.knockBack is >= 0 and <= 1000 && Main.projectile[(int)Projectile.knockBack].active&& Main.projectile[(int)Projectile.knockBack].ai[1] == 0)
                        target = Main.projectile[(int)Projectile.knockBack];
                    else Projectile.Kill();
                    
                    //Main.projectile[(int)Projectile.knockBack].Kill();
                    if (Projectile.ai[0] == 0)//椭圆环绕
                    {
                        Vector2 Center = target.Center + new Vector2((float)Math.Cos(Projectile.rotation), 3 * (float)Math.Sin(Projectile.rotation)).RotatedBy(Projectile.localAI[1]) * 60;
                        //创造粒子(move, target, 1f);//创造的粒子相对于追踪目标的绝对位置
                        for(int i = 0; i < 5; i++)
                        {
                            Dust d = Dust.NewDustDirect(Center, 0, 0, ModContent.DustType<水花粒子>(),
                            0, 0, 255, Color.White, 2);
                        }
                        Projectile.timeLeft=10;
                    }
                    break;
                case 2:
                    if (Projectile.knockBack is >= 0 and <= 200 && Main.npc[(int)Projectile.knockBack].active)
                        target = Main.npc[(int)Projectile.knockBack];
                    else
                        Projectile.Kill();
                    break;
                default:
                    break;
            }
            Projectile.Center = player.Center;


        }
        /*
         *   float rotation = MathHelper.Lerp(0, (float)Math.PI, Projectile.ai[1] / 60f); ;
             Projectile.ai[1] += 0.05f;
             //Main.NewText(Projectile.ai[1]);
             if (Projectile.ai[1] > 60) Projectile.ai[1] = 0;
             float n = 5f;
             float a = 250;
             float r = (float)Math.Pow((float) Math.Pow(a, 5) * Math.Cos(n * rotation),1/n);
             Projectile.Center =Main.projectile[(int)Projectile.ai[0]].Center+ (r * Vector2.One).RotatedBy(rotation);
             Vector2 len = Projectile.Center - Main.projectile[(int)Projectile.ai[0]].Center;
             len.Normalize();
         */
        public void 创造粒子(Vector2 pos, Entity player,float scale)
        {
            //if (Main.time % 2 == 0)
            {
                for (int i = 0; i < 60; i++)
                {
                    if (粒子组[i] == null)
                    {
                        粒子组[i] = new 粒子(pos, player, scale);
                        break;
                    }
                    if (!粒子组[i].active)
                    {
                        粒子组[i] = new 粒子(pos, player, scale);
                        break;
                    }
                }

            }
        }
        public void 绘制粒子(Asset<Texture2D> texture)
        {
            for (int i = 0; i < 60; i++)
            {
                if (粒子组[i] != null && 粒子组[i].active)
                {
                    粒子组[i].draw(texture);
                }
            }
        }
        public class 粒子
        {
            public Vector2 Position;
            public bool active = false;
            public Vector2 velocity;
            int frame = 0;
            float scale = 1;
            Entity target;
            Vector2 randomPosition;
            public 粒子(Vector2 Pos, Entity player, float scale)
            {
                Position = Pos;
                velocity = Main.rand.NextVector2Circular(15, 15);
                active = true;
                this.target = player;
                this.scale = scale;
                randomPosition = Main.rand.NextVector2Circular(5, 5);
            }
            public void draw(Asset<Texture2D> texture)
            {
                scale -= 0.015f;
                if (scale < 0.2f) active = false;
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.Default, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
                Main.spriteBatch.Draw(texture.Value, Position+target.Center + randomPosition - Main.screenPosition, new Rectangle(0, 0, 50, 50), Color.White, 0, new Vector2(25, 25),scale, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(texture.Value, Position + target.Center + randomPosition - Main.screenPosition, new Rectangle(0, 0, 50, 50), Color.White, 0, new Vector2(25, 25), scale*1.1f, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(texture.Value, Position + target.Center + randomPosition - Main.screenPosition, new Rectangle(0, 0, 50, 50), Color.White, 0, new Vector2(25, 25), scale*1.2f, SpriteEffects.None, 0);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                    DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
            }

        }
    }
}
