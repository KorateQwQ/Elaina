using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria.Audio;
using System.IO;
using System;
using ReLogic.Utilities;
using 伊蕾娜.Dusts;
using 伊蕾娜.System;
using 伊蕾娜.ReProjs;
using 伊蕾娜.Projectiles.MagicMissile;

namespace 伊蕾娜.Projectiles.Magicwater
{
    public class WaterMagicBall : ModProjectile
    {
        int 吸收间隔 = 10;
        int 凝聚间隔 = 5;
        bool shoot = false;
        Vector2 mouseworld=Vector2.Zero;
        Vector2 towards;
        float maxwater = 510f;
        float 水量比例 = 0;
        static Asset<Texture2D> 空间扭曲;
        static Asset<Texture2D> 空间扭曲角度;
        static Asset<Texture2D> noise;
        static Effect 环形扭曲shader;
        static Effect 虚化shader;
        static Effect 扰动shader;
        bool haskill = false;
        float 放大倍数 = 5;
        float 放大半径 = 0;
        float 环绕粒子数量 = 0;
        bool extraAttack=false;
        float width = 12.2f;
        float height = 12.2f;
        bool born = false;
        int time = 0;

        SlotId s;
        SoundStyle 聚集水声 = (new SoundStyle($"伊蕾娜/Projectiles/Magicwater/音效/聚集水", 1, SoundType.Sound)) with
        {
            Volume = 0.7f,
            MaxInstances = 2,
            //SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest,
            PlayOnlyIfFocused = true,
        };
        SoundStyle 水魔法冲击 = (new SoundStyle($"伊蕾娜/Projectiles/Magicwater/音效/水魔法冲击", 1, SoundType.Sound)) with
        {
            Volume = 1f,
            MaxInstances = 1,
            SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest,
            PlayOnlyIfFocused = true,
        };
        SoundStyle 水流 = (new SoundStyle($"伊蕾娜/Projectiles/Magicwater/音效/水流", 1, SoundType.Sound)) with
        {
            Volume = 1f,
            MaxInstances = 1,
            SoundLimitBehavior = SoundLimitBehavior.IgnoreNew,
            PlayOnlyIfFocused = true,
        };
        enum LockType : byte
        {
            锁定玩家,
            锁定弹幕,
            锁定敌人
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.WriteVector2(mouseworld);
            writer.Write(shoot);
            writer.Write(maxwater);
            writer.Write(吸收间隔);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            mouseworld = reader.ReadVector2();
            shoot = reader.ReadBoolean();
            maxwater = reader.ReadSingle();
            吸收间隔 = reader.ReadInt32();
        }
        public override void SetDefaults()
        {
            Projectile.penetrate = -1;
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.damage = 1;
            Projectile.friendly = true;
            Projectile.ownerHitCheck = false;
            Projectile.timeLeft = 60;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
            Projectile.hide = true;
            base.SetStaticDefaults();
        }
        public override void PostDraw(Color lightColor)
        {

            base.PostDraw(lightColor);
        }   
        public override bool PreDraw(ref Color lightColor)
        {
            GraphicsDevice gd = Main.instance.GraphicsDevice;
            SpriteBatch sb = Main.spriteBatch;
            if (noise == null) noise = Mod.Assets.Request<Texture2D>("Projectiles/Perlin");
            if (虚化shader == null) 虚化shader = Mod.Assets.Request<Effect>("Projectiles/Effects/Content/两端虚化", AssetRequestMode.ImmediateLoad).Value;
            if (扰动shader == null) 扰动shader = Mod.Assets.Request<Effect>("Projectiles/Effects/Content/扰动", AssetRequestMode.ImmediateLoad).Value;
            if (Projectile.ai[1] > 0) Projectile.ai[1]--;
            if (Projectile.ai[1] is > 0 and < 26 && (Projectile.ai[0] > 3000))//从25-0
            {
                if (空间扭曲 == null) 空间扭曲 = Mod.Assets.Request<Texture2D>("Projectiles/空间扭曲");
                if (空间扭曲角度 == null) 空间扭曲角度 = Mod.Assets.Request<Texture2D>("Projectiles/空间扭曲角度");
                if (环形扭曲shader == null) 环形扭曲shader = Mod.Assets.Request<Effect>("Projectiles/Effects/Content/环形扭曲", AssetRequestMode.ImmediateLoad).Value;
                sb.End();
                sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                gd.SetRenderTarget(Main.screenTargetSwap);//在这个上面绘制一遍原图，相当于“保存”

                sb.Draw(Main.screenTarget, Vector2.Zero, Color.White);

                //鼠标点击();
                float 最大范围 = MathHelper.Lerp(0.5f, 3.5f, Projectile.ai[0] / 10200) * width / 12f;
                放大倍数 = MathHelper.Lerp(最大范围, 0.1f, Projectile.ai[1] / 25);
                放大半径 = MathHelper.Lerp(0f, 0.25f, Projectile.ai[1] / 25);
                sb.End();
                sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                gd.SetRenderTarget(伊蕾娜.render);//设置成自己的RenderTarget进行绘制
                gd.Clear(Color.Transparent);//用透明清除

                sb.Draw(空间扭曲.Value, Projectile.Center - Main.screenPosition + new Vector2(-150 * 放大倍数, -150 * 放大倍数), new Rectangle(0, 0, 300, 300), Color.White, 0, Vector2.Zero, 放大倍数, SpriteEffects.None, 0);
                sb.Draw(空间扭曲角度.Value, Projectile.Center - Main.screenPosition + new Vector2(-150 * 放大倍数, -150 * 放大倍数), new Rectangle(0, 0, 300, 300), Color.White, 0, Vector2.Zero, 放大倍数, SpriteEffects.None, 0);

                sb.End();
                sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                gd.SetRenderTarget(Main.screenTarget);//换回屏幕renderTarget
                gd.Clear(Color.Transparent);//用透明清除
                //sb.Draw(Main.screenTargetSwap, Vector2.Zero, Color.White);//绘制自己的rendertarget
                环形扭曲shader.Parameters["tex0"].SetValue(伊蕾娜.render);
                环形扭曲shader.Parameters["i"].SetValue(放大半径);
                环形扭曲shader.CurrentTechnique.Passes["expand"].Apply();//开启shader
                sb.Draw(Main.screenTargetSwap, Vector2.Zero, Color.White);//绘制自己的rendertarget
            }
            else if (Projectile.ai[1] == 0&&shoot)
            {

                float utime = Utils.GetLerpValue(0, 1, Main.GameUpdateCount % 60 / 60f, true);

                sb.End();
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap,
                DepthStencilState.Default, RasterizerState.CullNone, null);
                gd.SetRenderTarget(Main.screenTargetSwap);//在这个上面绘制一遍原图，相当于“保存”
                sb.Draw(Main.screenTarget, Vector2.Zero, Color.White);


                sb.End();
                sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                gd.SetRenderTarget(伊蕾娜.render);//在这个上面绘制一遍原图，相当于“保存”
                gd.Clear(Color.Transparent);//用透明清除
                虚化shader.Parameters["uTime"].SetValue(utime);
                虚化shader.CurrentTechnique.Passes["move2"].Apply();

                float scale = Projectile.width / 122f * 0.5f;
                //Main.NewText(Projectile.width);
                sb.Draw(noise.Value, Projectile.Center - Main.screenPosition, new Rectangle(0, 0, noise.Width(), noise.Height()), Color.White, 0, new Vector2(noise.Width(), noise.Height()) * 0.5f, scale, SpriteEffects.None, 0);


                sb.End();
                sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                gd.SetRenderTarget(Main.screenTarget);//换回屏幕renderTarget
                扰动shader.Parameters["uTime"].SetValue(0);
                扰动shader.Parameters["strength"].SetValue(0.02f * Projectile.width / 122f);
                扰动shader.Parameters["tex0"].SetValue(伊蕾娜.render);
                扰动shader.CurrentTechnique.Passes["move"].Apply();

                sb.Draw(Main.screenTargetSwap, Vector2.Zero, Color.White);//绘制自己的rendertarget

            }
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            //overWiresUI.Add(index);
            if (shoot) Projectile.hide = false;
        }
        public override void ModifyDamageHitbox(ref Rectangle hitbox)
        {

            if (Projectile.timeLeft <= 25)
            {
                Projectile.velocity = Vector2.Zero;
                int move = (int)(Projectile.width * 5/2f -hitbox.Width * 0.5f);
                hitbox.Width = Projectile.width*5;
                hitbox.Height = Projectile.height*5;

                hitbox.X -= move;
                hitbox.Y -= move;
            }
        }
        void 爆炸粒子()
        {
            if (Projectile.ai[0] > 3000) SoundEngine.PlaySound(水魔法冲击, Projectile.position);
            if (ElainaModplayer.water && Main.myPlayer == Projectile.owner && Projectile.ai[0] > 0) 制造水((int)Projectile.Center.X / 16, (int)Projectile.Center.Y / 16);

            float scale = (int)MathHelper.Lerp(2, 4, 水量比例);
            //Main.NewText(scale);
            int 粒子数量 = (int)MathHelper.Lerp(20,200, 水量比例);
            int 粒子生成范围 = (int)(MathHelper.Lerp(0, 40, 水量比例)*width/12f);
            float 粒子速度 = (MathHelper.Lerp(5, 30, 水量比例) * width / 12f);
            for (int i = 0; i < 粒子数量; i++)
            {
                Vector2 v = new(Main.rand.NextFloatDirection() * 180f, Main.rand.NextFloatDirection() * 180f);
                v = Vector2.Normalize(v) * 粒子速度;
                Vector2 dustrandpos = Projectile.Center + Main.rand.NextVector2Circular(粒子生成范围, 粒子生成范围);
                Dust d = Dust.NewDustDirect(dustrandpos, 0, 0, ModContent.DustType<水花粒子>(),
                v.X, v.Y, 255, Color.White, scale);
                d.noGravity = true;
            }
        }
        public override bool PreAI()
        {
            if (!born)
            {
                born = true;
                Projectile.friendly = false;
                width = Projectile.width;
                height = Projectile.height;
            }
            return base.PreAI();
        }
        public override void AI()
        {
            if (Projectile.timeLeft > 25 && Projectile.ai[1] < 26)
            {
                float scale = MathHelper.Lerp(1, 3f, 水量比例);
                for (int i = 0; i < scale * 5; i++)
                {
                    Vector2 dustrandpos = Projectile.Center + Main.rand.NextVector2Circular(Projectile.width / 2f, Projectile.height / 2f);
                    Dust d = Dust.NewDustDirect(dustrandpos, 0, 0, ModContent.DustType<水花粒子>(),
                    0, 0, 255, Color.White, scale);
                    d.noGravity = true;
                    //d.fadeIn = Projectile.whoAmI;
                }
            }

            if (time < 5) time++;
            var player = Main.player[Projectile.owner];
            //Main.NewText(Projectile.ai[1]);
            float 伤害系数 = player.GetTotalDamage(DamageClass.Magic).Additive;
            var expmoplayer = player.GetModPlayer <EXPmodplayer>();
            //Main.NewText(player.GetTotalCritChance(DamageClass.Magic));
            Projectile.knockBack = (float)伊蕾娜.HitType.HitByWater;
            if (Projectile.owner == Main.myPlayer&&maxwater!=expmoplayer.GetMaxWater())
            {
                maxwater = expmoplayer.GetMaxWater();
    
            }
            水量比例 = MathHelper.Clamp(Projectile.ai[0] / 10200f, 0,3);//限制最大值为3倍
            if (Projectile.ai[0]+255<= maxwater && !shoot&&--吸收间隔 <= 0)
            {
                if (吸收水())
                {
                    SoundEngine.PlaySound(聚集水声, Projectile.position);
                }
                else if (player.CheckMana(player.HeldItem, (int)(3 * player.manaCost), true))
                {
                    SoundEngine.PlaySound(水流, Projectile.position);

                    if (Projectile.owner == Main.myPlayer)
                    {
                        Projectile.ai[0] += 255;
                        吸收间隔 = expmoplayer.吸收间隔();
                        Projectile.netUpdate = true;
                    }
                }
            }
            //Main.NewText(Projectile.ai[0]+" "+ 环绕粒子数量);
            if (Projectile.ai[0] > 3000 && 环绕粒子数量 < 3)
            {
                if (环绕粒子数量 == 0)
                {
                    Projectile proj = Projectile.NewProjectileDirect(null, Projectile.Center, Vector2.Zero, ModContent.ProjectileType<水粒子2>(), (int)LockType.锁定弹幕, Projectile.whoAmI, Projectile.owner);
                    环绕粒子数量++;
                }
                if (Projectile.ai[0] > 6000 && 环绕粒子数量 == 1)
                {

                    Projectile proj = Projectile.NewProjectileDirect(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<水粒子2>(), (int)LockType.锁定弹幕, Projectile.whoAmI, Projectile.owner);
                    proj.localAI[0] = ++环绕粒子数量 * 20;
                    proj.localAI[1] = (float)(环绕粒子数量*Math.PI/3f);
                    //Main.NewText(proj.localAI[1]);
                }
                if (Projectile.ai[0] > 10000 && 环绕粒子数量 == 2)
                {
                    Projectile proj = Projectile.NewProjectileDirect(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<水粒子2>(), (int)LockType.锁定弹幕, Projectile.whoAmI, Projectile.owner);
                    proj.localAI[0] = ++环绕粒子数量 * 20;
                    proj.localAI[1] = (float)((环绕粒子数量-1) *2*Math.PI/ 3f);
                    //Main.NewText(proj.localAI[1]);

                }
            }
            float size = MathHelper.Lerp(12.2f, 152, 水量比例);
            Projectile.width =(int)(width/12f* size);
            Projectile.height = (int)(height / 12f * size);
            //Main.NewText(Projectile.width);
            if (mouseworld == Vector2.Zero) mouseworld = Projectile.Center;
            if (Projectile.owner == Main.myPlayer)//同步鼠标位置
            {
                if (!shoot&&mouseworld != Main.MouseWorld)
                {
                    mouseworld = Main.MouseWorld;
                    Projectile.netUpdate = true;
                }
                if (!shoot&& time>2)
                {
                    //Main.NewText(Main.projectile[player.heldProj]);

                    //Main.projectile[player.heldProj].Kill();
                    //Main.mouseLeft = Main.mouseLeftRelease = true;
                    if ((Main.mouseLeft && Projectile.ai[0] + 255 >= maxwater))
                    {
                        towards = Vector2.Normalize(mouseworld - player.Center);
                        Projectile.velocity = towards * 20f;
                        shoot = true;
                        Projectile.tileCollide = true;
                    }
                }
            }
            if (!shoot)//手持弹幕时
            {
                Projectile.tileCollide = false;
                player.heldProj = Projectile.whoAmI;
                player.manaRegenDelay = 10;
                控制武器位置(player);
            }
            else
            {
                
                Projectile.damage = (int)(MathHelper.Lerp(80, 2000* EXPmodplayer.DamageScale, 水量比例) * 伤害系数);
                Projectile.friendly = true;

            }
            if (Collision.LavaCollision(Projectile.Center, Projectile.width, Projectile.width)&& !haskill)
            {
                shoot = true;
                Projectile.velocity = Vector2.Zero;
                Projectile.ai[1] = 25;
                Projectile.timeLeft = 25;
                Projectile.netUpdate = true;
                Projectile.tileCollide = false;
                haskill = true;
                爆炸粒子();

            }
            if (Projectile.ai[1]>0&&Projectile.ai[1] <= 24&& !haskill)
            {
                haskill = true;
                爆炸粒子();
            }
        }
        void 控制武器位置(Player player)
        {
            player.manaRegenDelay = 60;
            var towards = Vector2.Normalize(mouseworld - player.Center);
            player.itemTime = 2;
            player.itemAnimation = 2;
            player.direction = towards.X < 0 ? -1 : 1;//玩家朝向根据鼠标
            Projectile.rotation = towards.ToRotation();
            Projectile.timeLeft = 600;
            player.itemRotation = (float)Math.Atan2(towards.ToRotation().ToRotationVector2().Y * player.direction, towards.ToRotation().ToRotationVector2().X * player.direction);//武器朝向
            Projectile.Center = player.MountedCenter + (player.itemRotation + player.fullRotation).ToRotationVector2() * 38;
            if (player.direction < 0) Projectile.Center = player.MountedCenter - (player.itemRotation + player.fullRotation).ToRotationVector2() * 38;
        }
        public override void OnKill(int timeLeft)
        {
            if (!haskill)
            {
                haskill = true;

                爆炸粒子();
            }
                base.OnKill(timeLeft);
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            //爆炸粒子();
            Projectile.velocity = Vector2.Zero;
            Projectile.ai[1] = 25;
            Projectile.timeLeft = 25;
            Projectile.netUpdate = true;
            Projectile.tileCollide = false;
            return false;
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Projectile.ai[0] > 3000)
            {
                modifiers.SetCrit();
            }
            base.ModifyHitNPC(target, ref modifiers);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {

            if (!extraAttack)
            {
                extraAttack = true;
            }
            //target.immune[Projectile.owner] = 30;

            //target.immune[Projectile.owner] = 15;
            Projectile.velocity = Vector2.Zero;
            if (Projectile.timeLeft > 25)
            {
                //爆炸粒子();
                Projectile.ai[1] = 25;
                Projectile.tileCollide = false;
                Projectile.timeLeft = 25;
                Projectile.netUpdate = true;
            }
            base.OnHitNPC(target, hit, damageDone);
        }

        bool 吸收水()
        {
            bool result = false;
            int raincount = 0;
            foreach(Rain rain in Main.rain)
            {
                if (rain.active && (rain.position - Main.player[Projectile.owner].Center).Length() < 700)
                {
                    Dust d = Dust.NewDustDirect(rain.position, 0, 0, ModContent.DustType<水花粒子>(),
                    0, 0, 255, Color.White, 1);
                    //d.velocity =Vector2.Normalize(Projectile.Center - d.position)*10;
                    d.fadeIn = Projectile.whoAmI;
                    rain.active = false;
                    凝聚间隔 = 5;
                    Projectile.ai[0] += 255;
                    //Main.NewText("haha");
                    result =  true; 
                    if (raincount++>5|| Projectile.ai[0] + 255 >= maxwater) break;

                }
            }
            int x = (int)(mouseworld.X / 16f);
            int y = (int)(mouseworld.Y / 16f);
            //int i = Main.rand.Next(x - 20, x + 20);
            //int j = Main.rand.Next(y - 20, y + 20);
            int length = 0;
            Vector2[] tile = new Vector2[400];
            for(int i = x - 10; i < x + 10; i++)
            {
                for(int j =y -10;j < y + 10; j++)
                {
                    if (i > 0 && i < (int)(Main.rightWorld / 16f) && j > 0 && j < (int)(Main.bottomWorld / 16f))
                    {
                        Tile t = Main.tile[i, j];
                        if (t.LiquidType == 0 && t.LiquidAmount > 0)
                        {
                            tile[length++] = new Vector2(i,j);
                        }
                    }
                }
            }
            if(length > 0)
            {
                if ((Projectile.ai[0] + 255) > maxwater) return result;

                //Console.WriteLine(6 + " " + Main.myPlayer+" "+ Projectile.ai[0] + 255+" "+ maxwater);
                int index = Main.rand.Next(0, length);
                int i = (int)tile[index].X;
                int j = (int)tile[index].Y;
                Tile t = Main.tile[i, j];
                t.LiquidAmount = 0;
                t.LiquidType = 0;
                Dust d = Dust.NewDustDirect(new Vector2(i * 16, j * 16), 0, 0, ModContent.DustType<水花粒子>(),
                                0, 0, 255, Color.White, 1);
                //d.velocity =Vector2.Normalize(Projectile.Center - d.position)*10;
                d.fadeIn = Projectile.whoAmI;
                凝聚间隔 = 5;
                Projectile.ai[0] += 255;
                result = true;
                WorldGen.SquareTileFrame(i, j, resetFrame: false);
                if (Main.myPlayer == Projectile.owner&&Main.netMode == 1)
                {
                    NetMessage.sendWater(i, j);
                }
                else Liquid.AddWater(i, j);
            }
            return result;
        }
        public void 制造水(int tileTargetX, int tileTargetY)
        {
            for (int i = tileTargetX - 2; i < tileTargetX + 2; i++)
            {
                for (int j = tileTargetY - 7; j < tileTargetY + 1; j++)
                {
                    if (i > 0 && j > 0&&i*16< Main.rightWorld-1 && j*16< Main.bottomWorld-1&& Main.tile[i, j].LiquidType == 0)
                    {
                        if (Projectile.ai[0]>0)
                        {
                            int lack = 255 - Main.tile[i, j].LiquidAmount;
                            if (Projectile.ai[0] >= lack)
                            {
                                Projectile.ai[0] -= lack;
                                Main.tile[i, j].LiquidAmount += (byte)lack;
                            }
                            else
                            {
                                Main.tile[i, j].LiquidAmount += (byte)Projectile.ai[0];

                            }
                            WorldGen.SquareTileFrame(i, j, resetFrame: false);
                            if (Main.myPlayer==Projectile.owner&& Main.netMode == 1 && i > 0 && j > 0)
                            {
                                NetMessage.sendWater(i, j);
                            }
                            else if (i > 0 && j > 0) Liquid.AddWater(i, j);
                        }
                    }
                }
            }
        }

    }
}
