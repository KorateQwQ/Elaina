using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using 伊蕾娜.Dusts;
using Terraria.Audio;
using System;

namespace 伊蕾娜.Projectiles.Lightning
{
    public class 闪电链 : ModProjectile
    {
        static Asset<Texture2D> texture;
        static Asset<Texture2D> texture2;
        static Asset<Texture2D> texture3;
        static Effect DrawEffect;
        float time = 0;//-0.5和0.5之间
        float negative = -1;
        float random = 0;
        float[] rand = [0,0,0,0,0];
        Vector2[] randPosition = new Vector2[5];
        Vector2 魔杖中心;
        bool sound=false;
        int hitAmount = 0;

        int startDust = 0;

        SoundStyle LightningSound = (new SoundStyle($"伊蕾娜/Projectiles/Lightning/闪电攻击", 1, SoundType.Sound)) with
        {
            Volume = 0.5f,
            MaxInstances = 1,
            SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest,
            PlayOnlyIfFocused = true,
        };
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
        }
        public override void SetDefaults()
        {
            Projectile.friendly = true;
            Projectile.timeLeft = 20;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Magic;
            if(texture == null)
            {
                texture = Mod.Assets.Request<Texture2D>("Projectiles/Lightning/闪电链");
                DrawEffect = Mod.Assets.Request<Effect>("Projectiles/Effects/Content/闪电链effect", AssetRequestMode.ImmediateLoad).Value;
                texture2 = Mod.Assets.Request<Texture2D>("Projectiles/Perlin");

            }
            for (int i = 0; i < rand.Length; i++)
            {
                rand[i] = Main.rand.NextFloat(-2, 2);
                randPosition[i] = Main.rand.NextVector2Circular(80, 80);
            }
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 40;
            LightningSound = (new SoundStyle($"伊蕾娜/Projectiles/Lightning/闪电攻击", 1, SoundType.Sound)) with
            {
                Volume = 1.3f,
                MaxInstances = 5,
                SoundLimitBehavior = SoundLimitBehavior.IgnoreNew,
                PlayOnlyIfFocused = true,
                PitchVariance = 0.3f
            };
            base.SetDefaults();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            var player = Main.player[Projectile.owner];
            float f =80;
            if (Projectile.ai[0] != -1)
            {
                var npc = Main.npc[(int)Projectile.ai[0]].Center;//
                绘制两点闪电(0.5f + rand[0] * 0.1f, Projectile.Center, npc, 0 + rand[0], 0.5f, f,Math.Abs(rand[0]));
                绘制两点闪电(0.8f + rand[1] * 0.1f, Projectile.Center, npc, 3.3f + rand[1], 0.7f, f, Math.Abs(rand[1] ));
                绘制两点闪电(1.5f + rand[2] * 0.1f, Projectile.Center, npc, 5.3f + rand[2], 0.3f, f, Math.Abs(rand[2] ));
                绘制两点闪电(1.5f + rand[3] * 0.1f, Projectile.Center, npc, 6f + rand[3], 0.3f, f, Math.Abs(rand[3] ));
                绘制两点闪电(0.32f + rand[4] * 0.1f, Projectile.Center, npc, 6f + rand[4], 0.3f, f, Math.Abs(rand[4] ));
                /*for(int i = 0; i < 10; i++)
                {
                    绘制两点闪电(Main.rand.NextFloat(0, 1.5f) + rand[Main.rand.Next(0,5)] * 0.1f, Projectile.Center, npc, 6f + rand[Main.rand.Next(0, 5)], 0.3f, f, Math.Abs(rand[i%5]/2f));

                }*/
                if (!sound)
                {
                    startDust = 15;
                    SoundEngine.PlaySound(LightningSound, Projectile.position);
                    sound = true;

                }
                if (startDust > 0)
                {
                    Color c = new(59, 189, 245, 0);
                    Color c2 =  new(166, 255, 255, 0);
                    //Main.NewText(6);
                    var target = Main.npc[(int)Projectile.ai[0]];
                    Vector2 ve = (target.Center - Projectile.Center);
                    ve.Normalize();
                    float fadeinSpeed = 0.08f;
                    if (startDust % 2 == 0)
                    {
                        for(int i =0; i < 3; i++)
                        {
                            Vector2 v = ve * Main.rand.NextFloat(7, 15) + new Vector2(Main.rand.NextFloatDirection() * 8f, Main.rand.NextFloatDirection() * 8f);
                            v *= 1.5f;
                            Dust d = Dust.NewDustDirect(target.Center, 40, 40, ModContent.DustType<myDust>(),
                            v.X, v.Y, 255, Main.rand.NextBool() ? c : c2, Main.rand.NextFloat(1.5f, 2f) * 1.2f);
                            d.noGravity = true;
                            d.fadeIn = d.scale * fadeinSpeed;
                        }

                    }
                    else if(startDust%3==0)
                    {
                        Vector2 v = ve * Main.rand.NextFloat(10, 20) + new Vector2(Main.rand.NextFloatDirection() * 8f, Main.rand.NextFloatDirection() * 8f);

                        v *= -1;
                        Dust d1 = Dust.NewDustDirect(target.Center, 40, 40, ModContent.DustType<myDust>(),
                        v.X, v.Y, 255, Main.rand.NextBool() ? c : c2, Main.rand.NextFloat(1.3f, 1.8f)*0.5f);
                        d1.noGravity = true;
                        d1.fadeIn = d1.scale* fadeinSpeed;
                    }
                    //v = Vector2.Normalize(v) ;





                    /*switch (startDust)
                    {
                        case 8:
                            for (int i = 0; i < 5; i++)
                            {
                                Vector2 v = ve * Main.rand.NextFloat(10, 20) + new Vector2(Main.rand.NextFloatDirection() * 8f, Main.rand.NextFloatDirection() * 8f);
                                //v = Vector2.Normalize(v) ;
                                Dust d = Dust.NewDustDirect(target.Center, 40, 40, ModContent.DustType<myDust>(),
                                v.X, v.Y, 255, Main.rand.NextBool() ? c : c2, Main.rand.NextFloat(1.3f, 1.8f) * 0.8f);
                                d.noGravity = true;

                                v *= -1f;
                                d = Dust.NewDustDirect(target.Center, 40, 40, ModContent.DustType<myDust>(),
                                v.X, v.Y, 255, Main.rand.NextBool() ? c : c2, Main.rand.NextFloat(1.3f, 1.8f) * 0.35f);
                                d.noGravity = true;
                            }

                            break;
                        case 5:
                            for (int i = 0; i < 5; i++)
                            {
                                Vector2 v = ve * Main.rand.NextFloat(10, 20) + new Vector2(Main.rand.NextFloatDirection() * 8f, Main.rand.NextFloatDirection() * 8f);
                                //v = Vector2.Normalize(v) ;
                                Dust d = Dust.NewDustDirect(target.Center, 40, 40, ModContent.DustType<myDust>(),
                                v.X, v.Y, 255, Main.rand.NextBool() ? c : c2, Main.rand.NextFloat(1.3f, 1.8f) * 0.8f);
                                d.noGravity = true;

                                v *= -1f;
                                d = Dust.NewDustDirect(target.Center, 40, 40, ModContent.DustType<myDust>(),
                                v.X, v.Y, 255, Main.rand.NextBool() ? c : c2, Main.rand.NextFloat(1.3f, 1.8f) * 0.35f);
                                d.noGravity = true;
                            }

                            break;
                    }*/
                    startDust--;
                }

                
            }



            绘制两点闪电(0.2f + rand[0] * 0.1f, 魔杖中心, 魔杖中心+randPosition[0], 0 + rand[0], 0.1f, f,1);
            绘制两点闪电(0.3f + rand[1] * 0.1f, 魔杖中心, 魔杖中心 + randPosition[1], 3.3f + rand[1], 0.1f, f, 1);
            绘制两点闪电(0.6f + rand[2] * 0.1f, 魔杖中心, 魔杖中心 + randPosition[2], 5.3f + rand[2], 0.1f, f, 1);
            绘制两点闪电(0.3f + rand[3] * 0.1f, 魔杖中心, 魔杖中心 + randPosition[3], 6f + rand[3], 0.1f, f, 1);
            绘制两点闪电(0.12f + rand[4] * 0.1f, 魔杖中心, 魔杖中心 + randPosition[4], 6f + rand[4], 0.1f, f, 1);


            return false;
        }
        void 绘制两点闪电(float width,Vector2 start, Vector2 end,float rand,float scale,float frequency,float alpha)
        {
            Rectangle sourceRectangle = new(0, 0, texture.Width(), texture.Height());

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp,
            DepthStencilState.Default, RasterizerState.CullNone, null, Main.Transform);
            float utime = ((Main.GameUpdateCount+rand*10) % frequency) / frequency;
            if (Main.GameUpdateCount % 10 == 0) random = Main.rand.NextFloat(-3.14f+ rand, 3.14f+ rand);

            //DrawEffect.Parameters["tex0"].SetValue(伊蕾娜.render2);
            //float time = Utils.GetLerpValue(0, 1, Main.GameUpdateCount % 60 / 60f, true);
            DrawEffect.Parameters["tex0"].SetValue(texture2.Value);
            DrawEffect.Parameters["noiseStrength"].SetValue(0.15f*width);
            DrawEffect.Parameters["time"].SetValue(utime);//(Main.GameUpdateCount%60)/60f

            DrawEffect.Parameters["curveStrength"].SetValue(0.08f * width);
            DrawEffect.Parameters["uTime"].SetValue(utime);
            DrawEffect.Parameters["strength"].SetValue(0.02f * width);
            DrawEffect.Parameters["sinx"].SetValue(random );
            DrawEffect.Parameters["range"].SetValue(3.14f);
            DrawEffect.Parameters["rand"].SetValue(rand);
            float fade = Projectile.timeLeft / 15f;
            if (fade > 1) fade = 1;
            Vector4 c = (new Vector4(59, 189, 245, 255) / 255f) * fade* alpha;
            if (Main.rand.Next(0, (int)(15* fade)) == 0) c *= 0f;
            DrawEffect.Parameters["color"].SetValue(c);
            //Main.NewText (Math.Sin(Main.GameUpdateCount%7));
            DrawEffect.CurrentTechnique.Passes["move"].Apply();//开启shader

            SpriteEffects sp = SpriteEffects.None;
            if((int)rand%2==0) sp = SpriteEffects.FlipVertically;
            //for(int i = 0; i < 2; i++)
            Vector2 toward = end - start;
            //toward.Normalize();
            {
                Main.spriteBatch.Draw(texture.Value, Projectile.Center+ toward/2f - Main.screenPosition,
                sourceRectangle, Color.White, toward.ToRotation(), texture.Size() * 0.5f, new Vector2(5* toward.Length()/1000, 0.5f* scale), sp, 0f);

            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
            DepthStencilState.None, RasterizerState.CullCounterClockwise, null);

        }
        public override void PostDraw(Color lightColor)
        {
            base.PostDraw(lightColor);
        }
        public override void AI()//ai0为锁定敌人的id，若无则为-1,ai1为以敌人为载体发射时此敌人id
        {
            var player = Main.player[Projectile.owner];

            魔杖中心 = player.MountedCenter + (player.itemRotation + player.fullRotation).ToRotationVector2() * 38;
            if (player.direction < 0) 魔杖中心 = player.MountedCenter - (player.itemRotation + player.fullRotation).ToRotationVector2() * 38;
            Projectile.Center = 魔杖中心;
            if (Projectile.ai[1] != -1) Projectile.Center = Main.npc[(int)Projectile.ai[1]].Center;

            if (Main.myPlayer == Projectile.owner && Projectile.ai[0] == -1)//弹幕主人判定雷电索敌潮湿目标
            {
                foreach (NPC npc in Main.npc)
                {
                    if (伊蕾娜.iftarget(npc, player) && (npc.wet || npc.HasBuff(BuffID.Wet))&&(npc.Center-Projectile.Center).Length()<1500&&npc.GetGlobalNPC<Count>().lightning<=0)
                    {
                        Projectile.ai[0] = npc.whoAmI;
                        Projectile.netUpdate = true;
                    }
                }
            }
            if(Projectile.ai[1]== -1)
            {
                player.itemTime = 10;
                player.itemAnimation = 10;
            }

            base.AI();
        }
        public override bool? CanHitNPC(NPC target)
        {
            return base.CanHitNPC(target);
        }
        public override bool? Colliding(Rectangle myRect, Rectangle targetRect)
        {
            if (Projectile.ai[0] != -1)
            {
                var player = Main.player[Projectile.owner];
                float point = 0f;

                int weight = 107;
                //Main.NewText(projectile.velocity.Length());

                return Collision.CheckAABBvLineCollision(targetRect.TopLeft(), targetRect.Size(), Projectile.Center, Main.npc[(int)Projectile.ai[0]].Center, weight, ref point);//
            }

            return false;
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (hitAmount == 0) modifiers.FinalDamage *= 2;
            if (target.wet || target.HasBuff(BuffID.Wet)) modifiers.FinalDamage *= 2;
            base.ModifyHitNPC(target, ref modifiers);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            hitAmount++;
            if (target.whoAmI == Projectile.ai[0]&&Projectile.timeLeft>18)
            {
                target.GetGlobalNPC<Count>().lightning = 30;
                Projectile.NewProjectileDirect(null, target.Center, Vector2.Zero, ModContent.ProjectileType<闪电链>(), Projectile.damage, (float)伊蕾娜.HitType.HitByThunder, Projectile.owner, -1, target.whoAmI);

            }

            base.OnHitNPC(target, hit, damageDone);
        }
        public override void OnKill(int timeLeft)
        {
            base.OnKill(timeLeft);
        }
    }
}
