using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria.Audio;
using 伊蕾娜.System;

namespace 伊蕾娜.Projectiles.Lightning
{
    public class 雷击 : ModProjectile
    {
        Asset<Texture2D> texture;
        Asset<Texture2D> texture2;
        Effect 环形扭曲shader;

        int time = 0;
        float 大小;
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 8;
            //DisplayName.SetDefault("天雷"); // Set the projectile name to Example Flail Ball
            Projectile.knockBack = 10;
        }
        public override void SetDefaults()
        {
            Projectile.penetrate = -1; // 穿透数量
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;//瓷砖碰撞
            Projectile.friendly = true;
            //projectile.timeLeft=30;
            //projectile.extraUpdates=1;
            Projectile.width = 150;
            Projectile.height = 40;
            Projectile.damage = 20;
            Projectile.timeLeft = 500;
            //Projectile.knockBack = 10;
            //Projectile.extraUpdates = ;
            Projectile.alpha = 0;
            Projectile.ownerHitCheck = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 60;
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 3000;

        }
        public override void OnKill(int timeLeft)
        {
            var player = Main.player[Projectile.owner];
            Vector2 distance = Main.player[Main.myPlayer].Center - Projectile.Center;
            float sound = MathHelper.Lerp(1, 0.1f, distance.Length() / 1000f);
            SoundStyle t = SoundID.Thunder with
            {
                Volume = sound,
            };
            //t.Type = SoundType.Sound;
            //t.Volume = 0.5f;
            SoundEngine.PlaySound(t, Projectile.Center + new Vector2(0, -700));
            base.OnKill(timeLeft);
        }
        public override void AI()
        {
            time++;
            var player = Main.player[Projectile.owner];
            Projectile.CritChance = (int)Main.player[Projectile.owner].GetTotalCritChance(DamageClass.Magic);
            大小 = MathHelper.Lerp(1f, 2.5f, Projectile.ai[1] / 5)*Projectile.width/150f;
            //Projectile.CritChance = (int)(Projectile.CritChance*大小);
            //Main.NewText(Projectile.CritChance);
            //Projectile.timeLeft++;
            float 伤害系数 = player.GetTotalDamage(DamageClass.Magic).Additive;
            Projectile.damage = (int)(6666 * 大小 * 伤害系数 * EXPmodplayer.DamageScale);
            Projectile.knockBack = (int)伊蕾娜.HitType.HitByThunder;
            float 渐变 = MathHelper.Lerp(10, 0, time / 45f);
            Lighting.AddLight(Projectile.Center, 渐变, 渐变, 渐变);
            Projectile.frameCounter++;
            if (Projectile.frameCounter % 4 == 0)
            {
                Projectile.frame += 1;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= 8)
            {
                Projectile.Kill();
                Projectile.frame = 0;
            }
            if ((Main.player[Main.myPlayer].Center - Projectile.Center).Length() <= 1000)
            {
                Vector2 distance = Main.player[Main.myPlayer].Center - Projectile.Center;
                float time = MathHelper.Lerp(30, 2, distance.Length() / 1000);
                Main.player[Main.myPlayer].GetModPlayer<天雷modplayer>().雷击距离 = new Vector3(distance, time);
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            GraphicsDevice gd = Main.instance.GraphicsDevice;
            SpriteBatch sb = Main.spriteBatch;
            var player = Main.player[Projectile.owner];

            if (texture == null)
            {
                texture = Mod.Assets.Request<Texture2D>("Projectiles/Lightning/雷击" + (int)Projectile.ai[0]);
                texture2 = Mod.Assets.Request<Texture2D>("Projectiles/空间扭曲角度");
                环形扭曲shader = Mod.Assets.Request<Effect>("Projectiles/Effects/Content/环形扭曲", AssetRequestMode.ImmediateLoad).Value;
            }

            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            gd.SetRenderTarget(Main.screenTargetSwap);//在这个上面绘制一遍原图，相当于“保存”
            sb.Draw(Main.screenTarget, Vector2.Zero, Color.White);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointWrap,
            DepthStencilState.Default, RasterizerState.CullNone, null,Main.Transform);
            gd.SetRenderTarget(伊蕾娜.render);//在这个上面绘制天雷
            gd.Clear(Color.Transparent);
            //Texture2D texture = mod.GetTexture("C"+(int)projectile.ai[0]);
            Vector2 origin = texture.Size() / new Vector2(4f, 2f) * 0.5f;//除3相当于以图片中心为position.除以X,Y,此时为从上往下，从左往右的帧图
            int frameWidth = texture.Width() / 4;//图片总高度除以长度，得到每张图的长度
            int frameHeight = texture.Height() / 2;//图片总高度除以高度，得到每张图的高度
            int startX = frameWidth * (Projectile.frame % 4);//每一帧的起始坐标X
            int startY = frameHeight * (Projectile.frame / 4);//每一帧的起始坐标Y

            Rectangle sourceRectangle = new(startX, startY, frameWidth, frameHeight);//坐标x，坐标y，图片长度，图片高度，得到一张完整图片
            SpriteEffects spriteEffects = Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;//调整图片方向，当弹幕方向不是1时水平翻转图片
            Main.spriteBatch.Draw(texture.Value, Projectile.Center - Main.screenPosition + new Vector2(0, -770f) * 大小, 
                sourceRectangle, new Color(255, 255, 255, 255), 0, origin, 大小*new Vector2(1.5f,1), SpriteEffects.None, 0f);
            Vector2 scale2 = new(0.5f, 3.5f);
            //Main.spriteBatch.Draw(texture2.Value, Projectile.Center - Main.screenPosition + new Vector2(0, -470f) * 大小, 
              //  new Rectangle(0,0, texture2.Width(), texture2.Height()),
                //new Color(255, 255, 255, 255), 0, new Vector2(150,150), 大小* scale2 * new Vector2(1.5f, 1), SpriteEffects.None, 0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
            gd.SetRenderTarget(伊蕾娜.render2);//换回屏幕renderTarget
            gd.Clear(Color.Transparent);//用透明清除

            //sb.Draw(Main.screenTargetSwap, Vector2.Zero, Color.White);//绘制自己的rendertarget
            //Main.spriteBatch.Draw(texture.Value, Projectile.Center - Main.screenPosition + new Vector2(0, -770f) * 大小, sourceRectangle, new Color(255, 255, 255, 255), 0, origin, 大小, SpriteEffects.None, 0f);
            //sb.Draw(伊蕾娜.render, Vector2.Zero, Color.White);//绘制自己的rendertarget
            Main.spriteBatch.Draw(texture.Value, Projectile.Center - Main.screenPosition + new Vector2(0, -770f) * 大小,
            sourceRectangle, new Color(255, 255, 255, 255), 0, origin, 大小 * new Vector2(1.5f, 1), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(texture2.Value, Projectile.Center - Main.screenPosition + new Vector2(0, -470f) * 大小, 
            new Rectangle(0,0, texture2.Width(), texture2.Height()),
            new Color(255, 255, 255, 255), 0, new Vector2(150,150), 大小* scale2 * new Vector2(1.5f, 1), SpriteEffects.None, 0f);


            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullCounterClockwise, null);
            gd.SetRenderTarget(Main.screenTarget);//换回屏幕renderTarget
            gd.Clear(Color.Transparent);//用透明清除
            float 放大半径 = 0.32f;
            环形扭曲shader.Parameters["tex0"].SetValue(伊蕾娜.render2);
            环形扭曲shader.Parameters["i"].SetValue(放大半径);
            环形扭曲shader.CurrentTechnique.Passes["expand"].Apply();//开启shader
            sb.Draw(伊蕾娜.render, Vector2.Zero, Color.White);
            sb.Draw(Main.screenTargetSwap, Vector2.Zero, Color.White);//绘制自己的rendertarget

            //sb.Draw(伊蕾娜.render2, Vector2.Zero, Color.White);//绘制自己的rendertarget

            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullCounterClockwise, null);
            sb.Draw(伊蕾娜.render, Vector2.Zero, Color.White);//绘制自己的rendertarget
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overWiresUI.Add(index);
        }
        /*public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float point = 0f;
            float 大小 = MathHelper.Lerp(1f, 2.5f, Projectile.ai[1] / 5);
            Vector2 lengthS = Projectile.Center;
            Vector2 lengthE = Projectile.Center + new Vector2(0, -800);
            float width = 230;
            //lengthS *= 大小;
            //lengthE *= 大小;
            //width *= 大小;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), lengthS, lengthE, width, ref point);
        }*/
        public override void ModifyDamageHitbox(ref Rectangle hitbox)
        {
            float width = 230;
            float height = 1200;
            hitbox.Width = (int)(width * 大小);
            hitbox.Height = (int)(height * 大小);
            hitbox.Y -= (int)(height * 大小) - 30;
            int xmove = (hitbox.Width - 150) / 2;
            hitbox.X -= xmove;
            //hitbox.X-= (int)(width * 大小 / 3.5 );
            //Main.NewText(hitbox);
            //base.ModifyDamageHitbox(ref hitbox);
        }

        public class 雷魔法modplayer : ModPlayer
        {
            public override bool? CanHitNPCWithProj(Projectile proj, NPC target)
            {

                return base.CanHitNPCWithProj(proj, target);
            }

        }
    }
}
