using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using System.IO;
using System;
using Terraria.Audio;
using Terraria.Localization;

namespace 伊蕾娜.Projectiles.MagicBarrier
{
    public class 护盾 : ModProjectile
    {
        static Asset<Texture2D> texture;
        static Asset<Texture2D> texture2;
        static Asset<Texture2D> texture3;
        static Effect DrawEffect;
        Vector2 towards = Vector2.Zero;
        int 发射速率 = 0;
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.WriteVector2(towards);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            towards = reader.ReadVector2();
        }
        public override void SetStaticDefaults()
        {
            texture = Mod.Assets.Request<Texture2D>("Projectiles/MagicBarrier/护盾");
            texture2 = Mod.Assets.Request<Texture2D>("Projectiles/光线4.1");
            texture3 = Mod.Assets.Request<Texture2D>("Projectiles/MagicBarrier/护盾2");

            if (DrawEffect == null) DrawEffect = Mod.Assets.Request<Effect>("Projectiles/Effects/Content/Draw", AssetRequestMode.ImmediateLoad).Value;
            Main.projFrames[Projectile.type] = 4;
            //DisplayName.SetDefault("魔力屏障");
        }
        public override void SetDefaults()
        {
            Projectile.penetrate = -1; // 穿透数量
            Projectile.tileCollide = false;//瓷砖碰撞
            Projectile.friendly = true;
            Projectile.knockBack = 20;
            //projectile.timeLeft=30;
            //projectile.extraUpdates=1;
            Projectile.width = 90;
            Projectile.height = 120;
            Projectile.timeLeft = 40;
            //Projectile.extraUpdates = ;
            Projectile.alpha = 0;
        }
        public override bool PreDraw(ref Color lightColor)
        {

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap,
            DepthStencilState.Default, RasterizerState.CullNone, DrawEffect, Main.Transform);
            Player player = Main.player[Projectile.owner];
            Vector2 origin = texture.Size() * 0.5f;//除3相当于以图片中心为position
            origin.Y /= 4;//动图还要除以帧数
            int frameHeight = texture.Height() / Main.projFrames[Projectile.type];//图片总高度除以帧数，得到每张图的高度
            int startY = frameHeight * Projectile.frame;//每一帧的起始坐标Y
            float 渐变 = 60 - (float)Main.GameUpdateCount % 120;//60到 -60
            Vector2 scale = Vector2.One * MathHelper.Lerp(1, 1.1f, (float)Math.Abs(渐变) / 60f);
            Rectangle sourceRectangle = new(0, startY, texture.Width(), frameHeight);//坐标x，坐标y，图片长度，图片高度，得到一张完整图片
            for (int i = 0; i < 2; i++)
            {
                SpriteEffects se = 0;
                if (i == 0) se = SpriteEffects.FlipVertically;
                Main.spriteBatch.Draw(texture.Value, Projectile.Center - Main.screenPosition, sourceRectangle, Color.White, Projectile.rotation + i * MathHelper.Pi, origin, scale, se, 0f);

            }
            //Main.spriteBatch.Draw(texture.Value, Projectile.Center - Main.screenPosition, sourceRectangle, Color.White, Projectile.rotation , origin, scale, SpriteEffects.None, 0f);

            sourceRectangle = new Rectangle(0, 0, 500, 500);
            origin = texture2.Size() * 0.5f;
            //origin.Y /= 6;
            scale = new Vector2(0.25f, 0.25f) * MathHelper.Lerp(1, 1.1f, (float)Math.Abs(渐变) / 60f);
            Color c = Color.White;
            c.A = 2;
            Main.spriteBatch.Draw(texture2.Value, Projectile.Center - Main.screenPosition, sourceRectangle, c*0.1f, 0, origin, scale*0.9f, 0, 0f);


            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overPlayers.Add(index);

        }
        public override void OnKill(int timeLeft)
        {
            var p = Main.player[Projectile.owner].GetModPlayer<护盾modplayer>();
            p.护盾 = false;
        }
        public void PreventDrowning(Player player)
        {
            if (player.wet)
            {
                player.gills = true;
                player.breath += 1;
            }
        }
        public override void AI()
        {
            Projectile.knockBack = 10;
            Player player = Main.player[Projectile.owner];
            Projectile.damage = 1;
            if (Projectile.ai[0] == 1)
            {
                Projectile.timeLeft = 30;
                Projectile.ai[0] = -1;

            }
            var p = player.GetModPlayer<护盾modplayer>();
            p.护盾 = true;
            Lighting.AddLight(Projectile.Center, 2.5f, 1.1f, 2.1f);
            PreventDrowning(player);
            //动画
            Projectile.frameCounter++;
            if (Projectile.frameCounter % 10 == 0)
            {
                Projectile.frame += 1;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= 4)
            {
                Projectile.frame = 0;
            }
            if (Main.myPlayer == player.whoAmI)
            {
                if (Main.mouseRight)
                {
                    towards = Main.MouseWorld - player.Center;
                    towards.Normalize();
                    Projectile.timeLeft = 60;
                    Projectile.netUpdate = true;
                }
                else if (Projectile.ai[0] != -1)
                {
                    Projectile.Kill();
                }
            }

            player.itemTime = 2;
            player.itemAnimation = 2;
            player.direction = towards.X < 0 ? -1 : 1;//玩家朝向根据鼠标
            Projectile.Center = new Vector2(player.MountedCenter.X, player.MountedCenter.Y + player.gfxOffY);
            //Projectile.velocity = towards;
            Projectile.rotation = towards.ToRotation();
            player.itemRotation = (float)Math.Atan2(Projectile.rotation.ToRotationVector2().Y * player.direction, Projectile.rotation.ToRotationVector2().X * player.direction);//武器朝向


        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.immune[Projectile.owner] = 60;
            hit.Damage = 1;
            hit.Crit = false;
            var player = Main.player[Projectile.owner];

            base.OnHitNPC(target, hit, damageDone);
        }

    }

    public class 护盾modplayer : ModPlayer
    {
        public bool 护盾 = false;
        public int 护盾cd = 0;
        public int 熟练度 = 0;
        public int exp = 0;
        public int maxexp = 0;
        SoundStyle 护盾抵挡 = (new SoundStyle($"伊蕾娜/Projectiles/MagicBarrier/护盾抵挡", 1, SoundType.Sound)) with
        {
            Volume = 1f,
            MaxInstances = 3,
            Pitch = 0,
            PitchVariance = 0.5f,
            SoundLimitBehavior = SoundLimitBehavior.IgnoreNew,
            PlayOnlyIfFocused = true,
        };
        public override void FrameEffects()
        {
            if (护盾cd > 0)
            {
                护盾cd--;
                护盾 = false;
            }
            if(Player.immuneTime>0)
            //Main.NewText(Player.immuneTime);

            maxexp = (int)MathHelper.Lerp(100f, 10000f, (float)(熟练度 * 熟练度) / 16);
            base.FrameEffects();
        }
        public override bool CanBeHitByProjectile(Projectile proj)
        {
            // The active magic barrier is handled by MagicBarrierSkill.MagicBarrierModPlayer.
            // This legacy player must not spend vanilla mana or reduce projectile damage.
            return base.CanBeHitByProjectile(proj);
        }
        public override void PostHurt(Player.HurtInfo info)
        {
            if (Player.GetModPlayer<ElainaModplayer>().SayoNecklace)
            {
                int time = 60;
                Player.immune = true;
                Player.AddImmuneTime(info.CooldownCounter, time);

                //Main.NewText(info.CooldownCounter);
                //Main.NewText(cooldownCounter);
                /*if(cooldownCounter>=0) Player.hurtCooldowns[cooldownCounter] += time;
                else Player.immuneTime += time;
                for (int i = 0; i < Player.hurtCooldowns.Length; i++)
                {
                    //Player.hurtCooldowns[i] += time;
                }*/
            }
            base.PostHurt(info);
        }
        public override void ModifyHitByNPC(NPC npc, ref Player.HurtModifiers modifiers)
        {
            if (护盾)
            {
                护盾cd = 120;
                Main.mouseRight = false;
                护盾 = false;
            }
            base.ModifyHitByNPC(npc, ref modifiers);
        }
        public override bool CanBeHitByNPC(NPC npc, ref int cooldownSlot)
        {
            //return false;
            return base.CanBeHitByNPC(npc, ref cooldownSlot);
        }

    }
}
