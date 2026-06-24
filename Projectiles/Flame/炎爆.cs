using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria.Audio;

using Terraria.ModLoader.IO;
using ReLogic.Utilities;
using Terraria.GameContent.Achievements;
using 伊蕾娜.Buffs;
using 伊蕾娜.炼金;

namespace 伊蕾娜.Projectiles.Flame
{
    public class 炎爆 : ModProjectile
    {
        static Asset<Texture2D> texture;
        static Asset<Texture2D> noise;
        bool shoot = false;
        static Effect effect;
        static Effect 扰动shader;
        static Effect 渐变shader;
        ActiveSound result;
        SlotId s;
        SoundStyle flamesound = (new SoundStyle($"伊蕾娜/Projectiles/Flame/火球飞行", 1, SoundType.Sound)) with
        {
            Volume = 0.5f,
            MaxInstances = 1,
            SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest,
            PlayOnlyIfFocused = true,
        };
        bool flyingsound = false;
        public override void Load()
        {
        }
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 22;
            // DisplayName.SetDefault("炎爆");
        }
        public override void SetDefaults()
        {
            if (texture == null)
            {
                texture = Mod.Assets.Request<Texture2D>("Projectiles/Flame/炎爆");
                effect = Mod.Assets.Request<Effect>("Projectiles/Effects/Content/Draw", AssetRequestMode.ImmediateLoad).Value;
                扰动shader = Mod.Assets.Request<Effect>("Projectiles/Effects/Content/扰动", AssetRequestMode.ImmediateLoad).Value;
                noise = Mod.Assets.Request<Texture2D>("Projectiles/Perlin");

            }
            渐变shader = Mod.Assets.Request<Effect>("Projectiles/Effects/Content/两端虚化", AssetRequestMode.ImmediateLoad).Value;
            Projectile.ignoreWater = true;//无视水
            Projectile.friendly = true;//可以攻击敌人	
            Projectile.penetrate = -1; // 穿透数量
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;//瓷砖碰撞
            Projectile.timeLeft = 300;
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.scale = 1f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 60;
            //Projectile.CritChance = (int)Main.player[Projectile.owner].GetTotalCritChance(DamageClass.Magic);

        }
        public override void OnKill(int timeLeft)
        {
            {
                //SoundEngine.PlaySound(flamesound);
                if (result != null)
                {
                    //Main.NewText("关闭声音2");
                    result.Stop();
                }
                {
                    var Flame = Main.player[Projectile.owner].GetModPlayer<FlameModplayer>();
                    Flame.exp += 2 + Flame.熟练度;
                    Flame.lvUp();
                    //Main.NewText(Flame.熟练度 + " 火焰 " + Flame.exp);
                }
                SoundEngine.PlaySound(SoundID.Item62, Projectile.position);//62

            }
            if (Main.myPlayer == Projectile.owner)
            {
                Projectile proj = Projectile.NewProjectileDirect(null, Projectile.Center, Vector2.Zero, ModContent.ProjectileType<FlameOnhit>(), 0, Projectile.knockBack, Projectile.owner, Projectile.rotation + MathHelper.ToRadians(-90f));
                proj.rotation = Projectile.rotation + MathHelper.ToRadians(-90f);
            }
            if(Main.netMode != NetmodeID.Server)
            {
                for (int i = 0; i < 100; i++)
                {
                    Vector2 v = Projectile.velocity * 2f + new Vector2(Main.rand.NextFloatDirection() * 40f, Main.rand.NextFloatDirection() * 40f);
                    v = Vector2.Normalize(v) * 30;
                    Dust d = Dust.NewDustDirect(Projectile.position, 40, 40, 127,
                    -v.X, -v.Y, 255, Color.White, 5f);
                    d.noGravity = true;

                }
            }


            if ( Main.netMode != NetmodeID.Server)// Main.myPlayer == Projectile.owner)
            {
                //Main.NewText("6666");
                int explosionRadius = 3;
                //if (projectile.type == 29 || projectile.type == 470 || projectile.type == 637)
                {
                    explosionRadius = 10;
                }
                int minTileX = (int)(Projectile.Center.X / 16f - explosionRadius);
                int maxTileX = (int)(Projectile.Center.X / 16f + explosionRadius);
                int minTileY = (int)(Projectile.Center.Y / 16f - explosionRadius);
                int maxTileY = (int)(Projectile.Center.Y / 16f + explosionRadius);
                /*
                int minTileX = (int)(projectile.Center.X / 16f - (float)explosionRadius);
                int maxTileX = (int)(projectile.Center.X / 16f + (float)explosionRadius);
                int minTileY = (int)(projectile.Center.Y / 16f - (float)explosionRadius);
                int maxTileY = (int)(projectile.Center.Y / 16f + (float)explosionRadius);
                */
                if (minTileX < 0)
                {
                    minTileX = 0;
                }
                if (maxTileX > Main.maxTilesX)
                {
                    maxTileX = Main.maxTilesX;
                }
                if (minTileY < 0)
                {
                    minTileY = 0;
                }
                if (maxTileY > Main.maxTilesY)
                {
                    maxTileY = Main.maxTilesY;
                }
                bool canKillWalls = false;
                for (int x = minTileX; x <= maxTileX; x++)
                {
                    for (int y = minTileY; y <= maxTileY; y++)
                    {
                        float diffX = Math.Abs(x - Projectile.Center.X / 16f);
                        float diffY = Math.Abs(y - Projectile.Center.Y / 16f);
                        double distance = Math.Sqrt((double)(diffX * diffX + diffY * diffY));
                        if (distance < explosionRadius && Main.tile[x, y] != null && Main.tile[x, y].WallType == 0)
                        {
                            canKillWalls = true;
                            break;
                        }
                    }
                }
                AchievementsHelper.CurrentlyMining = true;

                for (int i = minTileX; i <= maxTileX; i++)
                {
                    for (int j = minTileY; j <= maxTileY; j++)
                    {
                        float diffX = Math.Abs(i - Projectile.Center.X / 16f);
                        float diffY = Math.Abs(j - Projectile.Center.Y / 16f);
                        double distanceToTile = Math.Sqrt((double)(diffX * diffX + diffY * diffY));

                        if (distanceToTile < explosionRadius)
                        {
                            bool canKillTile = true;
                            if (Main.tile[i, j] != null && Main.tile[i, j].HasTile)
                            {
                                canKillTile = true;
                                if (Main.tile[i, j].TileType == 26 || Main.tile[i, j].TileType == 107 || Main.tile[i, j].TileType == 108 || Main.tile[i, j].TileType == 111 || Main.tile[i, j].TileType == 226 || Main.tile[i, j].TileType == 237 || Main.tile[i, j].TileType == 221 || Main.tile[i, j].TileType == 222 || Main.tile[i, j].TileType == 223 || Main.tile[i, j].TileType == 211 || Main.tile[i, j].TileType == 404)
                                {
                                    canKillTile = false;
                                }
                                if (!Main.hardMode && Main.tile[i, j].TileType == 58)
                                {
                                    canKillTile = false;
                                }
                                if (!TileLoader.CanExplode(i, j))
                                {
                                    canKillTile = false;
                                }

                                if (TileID.Sets.BasicChest[Main.tile[i, j].TileType])
                                {
                                    Tile tile = Main.tile[i, j];
                                    if (!Chest.CanDestroyChest(i - tile.TileFrameX / 18 % 2, j - tile.TileFrameY / 18)) canKillTile = false;
                                    if (Main.netMode == NetmodeID.MultiplayerClient) canKillTile = false;
                                    //Main.NewText(Chest.CanDestroyChest(i - tile.TileFrameX/ 18 % 2, j - tile.TileFrameY / 18));
                                }
                                if (canKillTile)
                                {
                                    WorldGen.KillTile(i, j, false, false, false);
                                    if (!Main.tile[i, j].HasTile && Main.netMode != NetmodeID.SinglePlayer)
                                    {
                                        NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 0, i, j, 0f, 0, 0, 0);
                                    }
                                }
                            }
                            if (canKillTile)
                            {
                                for (int x = i - 1; x <= i + 1; x++)
                                {
                                    for (int y = j - 1; y <= j + 1; y++)
                                    {
                                        if (Main.tile[x, y] != null && Main.tile[x, y].WallType > 0 && canKillWalls && WallLoader.CanExplode(x, y, Main.tile[x, y].WallType))
                                        {
                                            WorldGen.KillWall(x, y, false);
                                            if (Main.tile[x, y].WallType == 0 && Main.netMode != NetmodeID.SinglePlayer)
                                            {
                                                NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 2, x, y, 0f, 0, 0, 0);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }


                AchievementsHelper.CurrentlyMining = false;
                Projectile.netUpdate = true;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.velocity = oldVelocity;
            if (Projectile.ai[1] != 1)
            {
                if (Projectile.frame < 7) Projectile.velocity = Projectile.ai[0].ToRotationVector2() * 1;
                Projectile.timeLeft = 2;
                Projectile.ai[1] = 1;
            }
            return false;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            GraphicsDevice gd = Main.instance.GraphicsDevice;
            SpriteBatch sb = Main.spriteBatch;

            var player = Main.player[Projectile.owner];
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap,
            DepthStencilState.Default, RasterizerState.CullNone, null);
            gd.SetRenderTarget(Main.screenTargetSwap);//在这个上面绘制一遍原图，相当于“保存”
            sb.Draw(Main.screenTarget, Vector2.Zero, Color.White);

            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            gd.SetRenderTarget(伊蕾娜.render2);//在伊蕾娜.render2上面绘制火球
            gd.Clear(Color.Transparent);//用透明清除
            Vector2 origin = texture.Size() / new Vector2(2f, 11f) * 0.5f;
            int frameWidth = texture.Width() / 2;//图片总高度除以长度，得到每张图的长度
            int frameHeight = texture.Height() / 11;//图片总高度除以高度，得到每张图的高度
            int startX = frameWidth * (Projectile.frame % 2);//每一帧的起始坐标X
            int startY = frameHeight * (Projectile.frame / 2);//每一帧的起始坐标Y
            Rectangle sourceRectangle = new(startX, startY, frameWidth, frameHeight);//坐标x，坐标y，图片长度，图片高度，得到一张完整图片
            Color c = Color.White;
            SpriteEffects spriteEffects = Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;//调整图片方向，当弹幕方向不是1时水平翻转图片	
            //Main.spriteBatch.Draw(texture.Value, Projectile.Center - Main.screenPosition, sourceRectangle, c, Projectile.rotation, origin, 1.05f, spriteEffects, 0f);
            //Main.spriteBatch.Draw(texture.Value, Projectile.Center - Main.screenPosition, sourceRectangle, c, Projectile.rotation, origin, 1.1f, spriteEffects, 0f);
            Main.spriteBatch.Draw(texture.Value, Projectile.Center - Main.screenPosition, sourceRectangle, c, Projectile.rotation, origin, 1f, spriteEffects, 0f);

            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap,
            DepthStencilState.Default, RasterizerState.CullNone, null, Main.Transform);
            gd.SetRenderTarget(伊蕾娜.render);//绘制噪声图
            gd.Clear(Color.Transparent);
            float time = Utils.GetLerpValue(0, 1, Main.GameUpdateCount % 60 / 60f, true);
            渐变shader.Parameters["uTime"].SetValue(time);
            渐变shader.CurrentTechnique.Passes["move2"].Apply();//开启shader
            Main.spriteBatch.Draw(noise.Value, Projectile.Center - Main.screenPosition, new Rectangle(0, 0,512,512), c, Projectile.rotation, new Vector2(256,256), 0.75f, SpriteEffects.None, 0f);
            //(int)(512 * time)
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            gd.SetRenderTarget(Main.screenTarget);//在这个上面绘制一遍原图，相当于“保存”
            扰动shader.Parameters["tex0"].SetValue(伊蕾娜.render);
            扰动shader.Parameters["uTime"].SetValue(0);
            扰动shader.Parameters["strength"].SetValue(0.02f );
            扰动shader.CurrentTechnique.Passes["move"].Apply();//开启shader
            sb.Draw(Main.screenTargetSwap, Vector2.Zero, Color.White);
            sb.Draw(伊蕾娜.render2, Vector2.Zero, Color.White);
            //Projectile.velocity.Normalize();
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }

        public override void AI()
        {
            //
            Projectile.rotation = Projectile.velocity.ToRotation()+ MathHelper.Pi / 2;
            Projectile.friendly = true;
            Player player = Main.player[Projectile.owner];
            //Projectile.CritChance = (int)player.GetTotalCritChance(DamageClass.Magic);
            //Projectile.rotation = Projectile.ai[0] ;

            Projectile.frameCounter++;
            if (Main.myPlayer == Projectile.owner)
            {
                if (!flyingsound)
                {
                    if (Projectile.position.Y < 700 || Projectile.position.X < 700 || Projectile.position.X > Main.rightWorld - 700 || Projectile.position.Y > Main.bottomWorld - 700) { }
                    else
                    {
                        //Main.NewText("生成声音");
                        flyingsound = true;
                        s = SoundEngine.PlaySound(flamesound, Projectile.position);
                    }
                }
                if (result != null)
                {
                    result.Position += Projectile.velocity;
                    if (Projectile.position.Y < 700 || Projectile.position.X < 700 || Projectile.position.X > Main.rightWorld - 700 || Projectile.position.Y > Main.bottomWorld - 700)
                    {
                        if (result.IsPlaying)
                        {
                            //Main.NewText("关闭声音1");
                            result.Stop();
                        }
                    }
                }
                else if (flyingsound)
                {
                    SoundEngine.TryGetActiveSound(s, out result);
                }
            }

            if (Projectile.frameCounter % 3 == 0)
            {

                Projectile.frame += 1;
                Projectile.frameCounter = 0;
                if (Projectile.frame > 8)
                {
                    for (int i = 0; i < 50; i++)
                    {
                        Vector2 v = Projectile.velocity * 2f + new Vector2(Main.rand.NextFloatDirection() * 25f, Main.rand.NextFloatDirection() * 25f);
                        v = Vector2.Normalize(v) * 25;
                       // Dust d = Dust.NewDustDirect(Projectile.position, 40, 40, 127,
                        //-v.X, -v.Y, 255, Color.White, 3f);
                        //d.noGravity = true;

                    }
                }

            }
            if (Projectile.frame >= 22)
            {
                Projectile.frame = 8;
            }

            if (Projectile.frame == 7 && Projectile.frameCounter == 0)
            {

                Projectile.tileCollide = true;//瓷砖碰撞
                //Projectile.velocity = Projectile.ai[0].ToRotationVector2() * 20;
                Projectile.friendly = true;
                if ((player.velocity - Vector2.Normalize(Projectile.velocity) * 15).Length() < 20)
                {
                    player.velocity += -Vector2.Normalize(Projectile.velocity) * 15;
                }
                for (int i = 0; i < 75; i++)
                {
                    Vector2 v = Vector2.Normalize(Projectile.velocity) * 25f + new Vector2(Main.rand.NextFloatDirection() * 25f, Main.rand.NextFloatDirection() * 25f);
                    v = Vector2.Normalize(v) * 35;
                    Dust d = Dust.NewDustDirect(player.MountedCenter - Projectile.velocity * 0.2f, 20, 20, 127,
                    -v.X, -v.Y, 0, Color.White, 5f);
                    d.noGravity = true;
                }
                Projectile.netUpdate = true;
            }
            else if (Projectile.frame < 7)
            {
                Projectile.Center = player.Center + 150 * Projectile.ai[0].ToRotationVector2();
                int x = (int)Projectile.Center.X / 16;
                int y = (int)Projectile.Center.Y / 16;
                Tile t = Main.tile[x, y];
                if (!t.IsActuated && t.HasTile && Main.tileSolid[t.TileType] && Projectile.ai[1] != 1)
                {
                    Projectile.ai[1] = 1;
                    Projectile.friendly = false;
                    Projectile.damage /= 2;
                    Projectile.velocity = Projectile.ai[0].ToRotationVector2() * 1;
                    if ((player.velocity - Vector2.Normalize(Projectile.velocity) * 5).Length() < 10)
                    {
                        player.velocity += -Vector2.Normalize(Projectile.velocity) * 5;
                    }
                    Projectile.timeLeft = 2;
                }
            }
            if (Collision.WetCollision(Projectile.Center, 50, 50) && !Collision.honey && !Collision.LavaCollision(Projectile.Center, 50, 50))
            //if (Collision.wet)
            {
                if (Projectile.ai[1] != 1)
                {
                    Projectile.ai[1] = 1;
                    Projectile.timeLeft = 2;
                    Projectile.damage /= 2;
                    Projectile.velocity = Projectile.ai[0].ToRotationVector2() * 1;
                    Projectile.friendly = false;
                }
            }
        }
        public override bool? Colliding(Rectangle myRect, Rectangle targetRect)
        {

            var player = Main.player[Projectile.owner];
            float point = 0f;
            Vector2 length = Projectile.velocity;
            length.Normalize();
            length *= 80f;
            float weight = 95;
            if (Projectile.timeLeft <= 2)
            {
                Projectile.friendly = true;
                length *= 1.5f;
                weight *= 2.5f;
            }
            //Main.NewText(projectile.velocity.Length());

            return Collision.CheckAABBvLineCollision(targetRect.TopLeft(), targetRect.Size(), Projectile.Center + length, Projectile.Center - length, weight, ref point);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<灼烧>(), 500);
            //damage += 10000;// (int)((target.lifeMax - target.life) * 0.75f);
            if (Projectile.ai[1] != 1)
            {
                Projectile.ai[1] = 1;
                Projectile.timeLeft = 2;
                Projectile.damage /= 2;
                Projectile.velocity = Projectile.ai[0].ToRotationVector2() * 1;
            }
            base.OnHitNPC(target, hit, damageDone);
        }

    }
    public class FlameModplayer : ModPlayer
    {
        public int damage = 10;
        public int knockback = 20;
        public int 熟练度 = 0;
        public int exp = 0;
        public int maxexp = 100;
        public int mana = 50;//6
        public int[] 火焰信息;
        public float 咏唱时间 = 120f;
        public int 最大熟练度 = 5;
        public override void SaveData(TagCompound tag)
        {
            tag["火焰信息"] = new int[7] { damage, knockback, 熟练度, exp, maxexp, mana, 最大熟练度 };
        }

        public float 火球间隔()
        {
            return 咏唱时间 * MathHelper.Lerp(1, 0.1f, 熟练度 / 10);
        }

        public override void LoadData(TagCompound tag)
        {
            火焰信息 = (int[])tag["火焰信息"];
            damage = 火焰信息[0];
            knockback = 火焰信息[1];
            熟练度 = 火焰信息[2];
            exp = 火焰信息[3];
            maxexp = 火焰信息[4];
            mana = 火焰信息[5];
            最大熟练度 = 火焰信息[6];
        }
        public int Getdamage()
        {
            return (int)MathHelper.Lerp(15f, 350f, 熟练度 * 熟练度 / 100f);

        }
        public override void FrameEffects()
        {
            //熟练度 = 10;
            //Main.NewText(最大熟练度);

            /*if (Main.mouseLeft && Main.mouseLeftRelease)
			{
				熟练度--;
				//maxexp = (int)MathHelper.Lerp(100f, 10000f, (float)Math.Sqrt(熟练度) / (float)Math.Sqrt(5));
				maxexp = (int)MathHelper.Lerp(100f, 10000f, (float)(熟练度* 熟练度) / 16);
				damage = (int)MathHelper.Lerp(15f, 50f, (float)(熟练度) / 6f);
				Main.NewText(熟练度 + " "+maxexp+" "+damage);
			}

			if (Main.mouseRight && Main.mouseRightRelease)
			{
				熟练度++;
				//maxexp = (int)MathHelper.Lerp(100f, 10000f, (float)Math.Sqrt(熟练度) / (float)Math.Sqrt(5));
				maxexp = (int)MathHelper.Lerp(100f, 10000f, (float)(熟练度 * 熟练度) / 16);
				damage = (int)MathHelper.Lerp(15f, 50f, (float)(熟练度 * 熟练度) / 25f);
				Main.NewText(熟练度 + " " + maxexp + " " + damage);
			}*/
        }
        public void lvUp()
        {
            if (Main.hardMode) 最大熟练度 = Player.GetModPlayer<炼金modplayer>().炼金最大lv;
            else 最大熟练度 = 5;
            if (熟练度 < 最大熟练度)
            {
                if (exp >= maxexp)
                {
                    exp = 0;
                    maxexp = (int)MathHelper.Lerp(100f, 15000f, (float)(熟练度 * 熟练度) / 81);
                    熟练度++;
                }
            }
        }
    }
}
