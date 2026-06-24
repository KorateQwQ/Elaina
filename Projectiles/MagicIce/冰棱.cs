using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.IO;
using System;
using Terraria.GameContent;
using Terraria.Audio;

namespace 伊蕾娜.Projectiles.MagicIce
{
    public class 冰棱 : ModProjectile
    {
        public bool shoot = false;
        static Asset<Texture2D> texture;
        static Asset<Texture2D> texture2;
        int GenerateTime = 0;
        int GenerateTimes = 0;
        Vector2 mouseWorld;
        bool syncFrame = false;
        int realTimeLeft = 0;

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.WriteVector2(mouseWorld);
            writer.Write(shoot);
            base.SendExtraAI(writer);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            mouseWorld = reader.ReadVector2();
            shoot = reader.ReadBoolean();
            base.ReceiveExtraAI(reader);
        }
        public override void SetStaticDefaults()
        {
            //DisplayName.SetDefault("冰棱");
            Main.projFrames[Projectile.type] = 17;
            base.SetStaticDefaults();
        }
        public override void SetDefaults()
        {
            if (texture == null) texture = TextureAssets.Projectile[Type];
            if (texture2 == null) texture2 = Mod.Assets.Request<Texture2D>("Projectiles/MagicIce/冰施法");
            Projectile.tileCollide = false;
            Projectile.timeLeft = 200;
            Projectile.width = 100;
            Projectile.height = 100;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.friendly = true;
            base.SetDefaults();
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Player player = Main.player[Projectile.owner];
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointWrap,
            DepthStencilState.Default, RasterizerState.CullNone, null, Main.Transform);
            Vector2 origin = new(139, 127);//Utils.Size(texture) / new Vector2(0.75f, 17f) * 0.5f;//除3相当于以图片中心为position
            int frameHeight = texture2.Height() / 5;//图片总高度除以帧数，得到每张图的高度
            int startY = frameHeight * Projectile.frame;//每一帧的起始坐标Y
            SpriteEffects se = SpriteEffects.None;
            //if (player.direction < 0) se = SpriteEffects.FlipHorizontally;
            //Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, Projectile.Center - Main.screenPosition, new Rectangle(0,0, TextureAssets.MagicPixel.Width(), TextureAssets.MagicPixel.Height()), new(255, 255, 255, 255), 0, new Vector2(TextureAssets.MagicPixel.Width(), TextureAssets.MagicPixel.Height())*0.5f, 1.2f, se, 0f);
            //Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, Projectile.Center - Main.screenPosition, new Rectangle(0, 0, TextureAssets.MagicPixel.Width(), TextureAssets.MagicPixel.Height()), new(255, 255, 255, 255), 1.57f, new Vector2(TextureAssets.MagicPixel.Width(), TextureAssets.MagicPixel.Height()) * 0.5f, 1.2f, se, 0f);

            Rectangle sourceRectangle = new(0, startY, texture.Width(), frameHeight);//坐标x，坐标y，图片长度，图片高度，得到一张完整图片

            Player p = Main.player[Projectile.owner];
            Vector2 Center = p.WandCenter();

            Vector2 center = Center;// player.MountedCenter + (player.itemRotation + player.fullRotation).ToRotationVector2() * 38;
            //if (player.direction < 0) center = player.MountedCenter - (player.itemRotation + player.fullRotation).ToRotationVector2() * 38;
            switch (Projectile.frame)
            {
                case 0:
                    {
                        float 渐变 = 30 - (float)Main.GameUpdateCount % 60;//60到 -60
                        if (渐变 < 0) 渐变 = -渐变;
                        float scale = MathHelper.Lerp(0.8f, 1.2f, 渐变 / 30);
                        if (player.itemTime > 0) Main.spriteBatch.Draw(texture2.Value, center - Main.screenPosition, sourceRectangle, new Color(255, 255, 255, 255), Projectile.rotation, origin, scale, se, 0f);
                    }
                    break;
                case 1:
                    Projectile.frameCounter++;
                    if (Projectile.frameCounter % 7 == 0)
                    {
                        Projectile.frameCounter = 0;
                        Projectile.frame++;
                    }
                    Main.spriteBatch.Draw(texture2.Value, center - Main.screenPosition, sourceRectangle, new(255, 255, 255, 255), Projectile.rotation, origin, 1.2f, se, 0f);
                    break;
                case 2:
                    {
                        Color c = new(255, 255, 255, 255);
                        Color c1 = new(0, 0, 0, 0);
                        Color 渐变 = Color.Lerp(c, c1, Projectile.frameCounter / 157f);
                        Projectile.frameCounter++;
                        if (Projectile.frameCounter % 7 == 0)
                        {
                            Projectile.frameCounter = 0;
                            Projectile.frame = 3;
                        }
                        Main.spriteBatch.Draw(texture2.Value, center - Main.screenPosition, sourceRectangle, c, Projectile.rotation, origin, 1.2f, se, 0f);

                    }
                    break;
                case 3:
                    {
                        Projectile.frameCounter++;
                        if (Projectile.frameCounter % 3 == 0)
                        {
                            Projectile.frameCounter = 0;
                            Projectile.frame = 4;
                        }
                        Color c = new(255, 255, 255, 255);
                        Color c1 = new(0, 0, 0, 0);
                        Color 渐变 = Color.Lerp(c, c1, Projectile.frameCounter / 7f);
                        float scale = 2f;
                        if (Projectile.frameCounter > 1) scale = 1.5f;
                        Main.spriteBatch.Draw(texture2.Value, center - Main.screenPosition, sourceRectangle, c, Projectile.rotation, origin, scale, se, 0f);
                    }
                    break;
                case 4:
                    {
                        Projectile.frameCounter++;
                        if (Projectile.frameCounter % 10 == 0)
                        {
                            Projectile.frameCounter = 0;
                            Projectile.frame = 0;
                        }
                        if (Projectile.frameCounter <= 10)
                        {
                            Color c = new(255, 255, 255, 255);
                            Color c1 = new(0, 0, 0, 0);
                            Color 渐变 = Color.Lerp(c, c1, Projectile.frameCounter / 10f);
                            if (Projectile.frameCounter % 3 == 0) Main.spriteBatch.Draw(texture2.Value, center - Main.screenPosition, sourceRectangle, 渐变, Projectile.rotation, origin, 1, se, 0f);
                        }
                    }
                    break;
                default:
                    Main.spriteBatch.Draw(texture2.Value, center - Main.screenPosition, sourceRectangle, new Color(255, 255, 255, 255), Projectile.rotation, origin, 1, se, 0f);
                    break;
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.Transform);
            return false;
        }
        public override void OnKill(int timeLeft)
        {
            base.OnKill(timeLeft);
        }
        public override bool PreAI()
        {
            Projectile.friendly = false;
            return base.PreAI();
        }
        public override void AI()//ai1是锁定的敌人ID，-1则为没有 ai0为冰锥攻击类型
        {
            //Projectile.Kill();
            Player player = Main.player[Projectile.owner];
            player.itemTime = 10;
            player.itemAnimation = 10;
            if (GenerateTime > 0) GenerateTime--;
            int kkk = 50;
            if (Projectile.ai[0]==0 )
            {
                if (mouseWorld == Vector2.Zero)
                {
                    mouseWorld = Projectile.Center - player.Center;
                }
                else Projectile.Center = player.Center + mouseWorld;
            }
            if (Projectile.ai[0] == 1)
            {
                Projectile.Center = player.Center;
            }
            if (Main.myPlayer == Projectile.owner)
            {
                int damage = Projectile.damage;
                switch (Projectile.ai[0])
                {
                    case 0:
                        if (GenerateTime == 0&& GenerateTimes<3&& player.CheckMana(player.HeldItem, (int)(10 * player.manaCost), true))
                        {   
                            Vector2 towards =Vector2.Normalize(Projectile.velocity)*100;
                            Projectile proj =  Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), player.Center - towards + Main.rand.NextVector2Circular(150, 150),
                               Projectile.velocity, ModContent.ProjectileType<冰锥>(), damage, Projectile.whoAmI, Projectile.owner, 0, Projectile.ai[1]);
                            //proj.Center = player.Center + -towards + Main.rand.NextVector2Circular(20, 20);
                            GenerateTime = Main.rand.Next(0,5);
                            proj.width = (int)(proj.width * Main.rand.NextFloat(1.2f, 1.6f));
                            proj.height = (int)(proj.height * Main.rand.NextFloat(0.7f, 1.1f));
                            proj.rotation = (Main.MouseWorld - proj.Center).ToRotation();
                            NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, proj.whoAmI);
                            GenerateTimes++;
                        }
                        if (GenerateTimes == 3)
                        {
                            GenerateTimes = 4;
                            Projectile.timeLeft = 30;
                        }
                        break;
                   case 1:
                        if(player.CheckMana(player.HeldItem, (int)(100 * player.manaCost), true))
                        {
                            for (; GenerateTimes < 10; GenerateTimes++)
                            {
                                Vector2 towards = Vector2.Normalize(Projectile.velocity) * 100;
                                Vector2 position = Main.MouseWorld + (Vector2.One * 300).RotatedBy(2f * Math.PI * GenerateTimes / 10f + towards.ToRotation());
                                Projectile proj = Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), position,
                                    Main.MouseWorld - position, ModContent.ProjectileType<冰锥>(), damage, (int)伊蕾娜.HitType.HitByIce - 1, Projectile.owner, 1, Projectile.ai[1]);
                                //proj.Center = player.Center + -towards + Main.rand.NextVector2Circular(20, 20);
                                GenerateTime = Main.rand.Next(0, 5);
                                proj.width = (int)(proj.width * 1.3f);
                                proj.height = (int)(proj.height * 0.8f);
                                NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, proj.whoAmI);
                            }
                        }

                        Projectile.Kill();
                        break;
                    case 2:
                        Projectile.Center = Main.MouseWorld;
                        if (mouseWorld != Main.MouseWorld)
                        {
                            mouseWorld = Main.MouseWorld;
                            NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, Projectile.whoAmI);
                        }
                        Projectile.rotation = (Projectile.Center - player.Center).ToRotation();
                        
                        Projectile.netImportant = true;
                        if (GenerateTime == 0 && GenerateTimes < kkk&& player.CheckMana(player.HeldItem, (int)(10 * player.manaCost), true))
                        {
                            Vector2 towards = Vector2.Normalize(Projectile.velocity) * 100;
                            Projectile proj = Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), player.Center - towards + Main.rand.NextVector2Circular(350, 350),
                               Projectile.velocity, ModContent.ProjectileType<冰锥>(), (int)(damage*1f), (int)伊蕾娜.HitType.HitByIce, Projectile.owner, 2, Projectile.whoAmI);
                            //proj.Center = player.Center + -towards + Main.rand.NextVector2Circular(20, 20);
                            GenerateTime = Main.rand.Next(0, 5);
                            proj.width = (int)(proj.width * Main.rand.NextFloat(1.2f, 1.6f));
                            proj.height = (int)(proj.height * Main.rand.NextFloat(0.7f, 1.1f));
                            //NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, proj.whoAmI);
                            GenerateTimes++;
                        }

                        if (Projectile.ai[1] != 999)
                        {
                            if (Main.mouseLeft && Main.mouseLeftRelease)
                            {
                                Projectile.frame = 1;
                                Projectile.ai[1] = 999;
                                realTimeLeft = 100;
                                shoot = true;
                                Projectile.netUpdate = true;
                            }
                        }
                        //Main.NewText(Projectile.localAI[0]);

                        //Projectile.Kill();
                        break;
                }
            }
            else
            {
                Projectile.velocity = Vector2.Zero;
                if (Projectile.ai[1] == 999&&!syncFrame)
                {
                    Projectile.frame = 1;
                    syncFrame = true;
                }
            }
            if (Projectile.ai[0] == 2)
            {
                if (shoot) realTimeLeft--;
                else Projectile.timeLeft++;
                Vector2 towards = Vector2.Normalize(Projectile.Center - player.Center);
                player.direction = towards.X < 0 ? -1 : 1;//玩家朝向根据鼠标
                Projectile.rotation = towards.ToRotation();
                player.itemRotation = (float)Math.Atan2(Projectile.rotation.ToRotationVector2().Y * player.direction, Projectile.rotation.ToRotationVector2().X * player.direction);//武器朝向
                if (Projectile.localAI[0] == kkk|| realTimeLeft<0)
                {
                    Projectile.Kill();
                }
                if (Projectile.localAI[0] < kkk)
                {
                    Projectile.timeLeft = 200;
                }
            }
            base.AI();
        }


        public class 冰锥 : ModProjectile
        {   
            public int state = 0;//攻击阶段 0生成阶段,生成阶段后一帧索敌阶段,2准备发射阶段,3发射阶段

            public NPC target;
            public Projectile target2;
            public static Asset<Texture2D> texture;
            public Vector2 randposition;
            public Vector2 randomScale;
            public Vector2 lockposition;
            public Vector2 velocity;
            bool sound = false;
            static SoundStyle 发射 = (new SoundStyle($"伊蕾娜/Projectiles/MagicIce/发射", 1, SoundType.Sound)) with
            {
                Volume = 0.5f,
                MaxInstances = 5,
                Pitch = 1f,
                PitchVariance = 0.2f,
                SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest,
                PlayOnlyIfFocused = true,
            };
            static SoundStyle 碎裂 = (new SoundStyle($"伊蕾娜/Projectiles/MagicIce/碎裂", 1, SoundType.Sound)) with
            {
                Volume = 1f,
                MaxInstances = 2,
                Pitch = 0,
                PitchVariance = 0.2f,
                SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest,
                PlayOnlyIfFocused = true,
            };
            public override void SetStaticDefaults()
            {
                //DisplayName.SetDefault("冰锥");
                Main.projFrames[Projectile.type] = 17;
                base.SetStaticDefaults();
            }
            public override void SendExtraAI(BinaryWriter writer)
            {
                writer.Write(Projectile.width);
                writer.Write(Projectile.height);
                //writer.WriteVector2(velocity);
                //writer.Write(Projectile.rotation);
                base.SendExtraAI(writer);
            }
            public override void ReceiveExtraAI(BinaryReader reader)
            {
                Projectile.width = reader.ReadInt32();
                Projectile.height = reader.ReadInt32();
                //velocity = reader.ReadVector2();
                //Projectile.rotation = reader.Read();
                base.ReceiveExtraAI(reader);
            }
            public override void SetDefaults()
            {   
                Projectile.DamageType = DamageClass.Magic;
                if (texture==null) texture =  Mod.Assets.Request<Texture2D>("Projectiles/MagicIce/冰棱");
                Projectile.width = 50;
                Projectile.height = 50;
                //randposition = Projectile.Center;
                //if (Projectile.ai[0] == 0)
                  //  randposition = Main.player[Projectile.owner].Center - Projectile.Center;
                Projectile.timeLeft = 150;
                //Main.NewText("111" + Main.npc[(int)Projectile.ai[1]].FullName);
                Projectile.extraUpdates = 5;
                Projectile.penetrate = -1;
                Projectile.friendly = true;
                Projectile.usesLocalNPCImmunity = true;
                Projectile.localNPCHitCooldown = 60;

                base.SetDefaults();
            }
            public override bool PreDraw(ref Color lightColor)
            {
                Vector2 origin = new(374, 56);
                int frameHeight = texture.Height() / 17;//图片总高度除以帧数，得到每张图的高度
                int startY = frameHeight * Projectile.frame;//每一帧的起始坐标Y
                Vector2 scale =new(Projectile.width / 50f, Projectile.height / 50f);
                Rectangle sourceRectangle = new(0, startY, texture.Width(), frameHeight);//坐标x，坐标y，图片长度，图片高度，得到一张完整图片
                Main.spriteBatch.Draw(texture.Value, Projectile.Center - Main.screenPosition, sourceRectangle, new Color(255, 255, 255, 255), Projectile.rotation, origin, scale, SpriteEffects.None, 0f);
                return false;
            }
            public override void OnKill(int timeLeft)
            {
                base.OnKill(timeLeft);
            }
            public override bool? Colliding(Rectangle myRect, Rectangle targetRect)
            {
                float point = 0f;
                Vector2 length = Vector2.Normalize(Projectile.rotation.ToRotationVector2());
                length *= Projectile.width / 50f*130;
                float width = Projectile.height / 50f * 52f;
                //Main.NewText(Projectile.height);
                return Collision.CheckAABBvLineCollision(targetRect.TopLeft(), targetRect.Size(), Projectile.Center-(length/2f), Projectile.Center + (length / 2f), width, ref point);
            }
            public override bool PreAI()
            {
                if (Projectile.ai[0] != 0)
                {
                    Projectile.tileCollide = false;
                    if (Projectile.ai[0] == 2) Projectile.penetrate = -1;
                }
                if (Projectile.frame <= 2)
                {
                    Projectile.friendly = false;
                }
                    return base.PreAI();
            }
            public override void AI()
            {
                if (Projectile.ai[0] != 2)
                {
                    if (Projectile.ai[1] >= 0 && Main.npc[(int)Projectile.ai[1]].active) target = Main.npc[(int)Projectile.ai[1]];
                }
                else
                {
                    if (Projectile.ai[1] >= 0)
                    {
                        target2 = Main.projectile[(int)Projectile.ai[1]];
                    }
                }

                if (Projectile.frame <= 2 )
                {   
                    if (Projectile.frameCounter++ > 30)
                    {
                        Projectile.frame++;
                        Projectile.frameCounter = 0;
                    }
                }
                if (Projectile.frame == 3 && state == 0 && Projectile.ai[0]!=2)state=1;
                if(state==0) Projectile.timeLeft++;

                switch (Projectile.ai[0])
                {
                    case 0:
                        AI0();
                        break;
                    case 1:
                        AI1();
                        break;
                    case 2:
                        AI2();
                        break;

                }
                if(state == 2)
                {   
                    for(int i =0;i<3; i++)
                    {
                        Dust d = Dust.NewDustDirect(Projectile.Center, 0, 0, DustID.Snow, 0, 0, 1, Color.White, 1);
                        d.noGravity = true;
                    }
                }
            }
            void AI0()
            {
                Projectile.rotation = velocity.ToRotation();
                switch (state)
                {
                    case 0:
                        if (velocity == Vector2.Zero)
                        {
                            if (Main.projectile[(int)Projectile.knockBack] != null)
                            {
                                velocity = Vector2.Normalize(Main.projectile[(int)Projectile.knockBack].Center - Projectile.Center);
                            }
                            else
                            {
                                velocity = Vector2.One;
                            }
                            Projectile.knockBack = (float)伊蕾娜.HitType.HitByIce;
                            //NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, Projectile.whoAmI);
                        }
                        Projectile.velocity = Vector2.Zero;

                        if (randposition == Vector2.Zero)
                        {
                            randposition = Projectile.Center - Main.player[Projectile.owner].Center;
                        }
                        else Projectile.Center = randposition + Main.player[Projectile.owner].Center;
                        break;
                    case 1:
                        if (target!=null && target.active && !target.friendly)
                        {
                            velocity = Vector2.Normalize(target.Center - Projectile.Center);
                        }
                        Projectile.Center = randposition + Main.player[Projectile.owner].Center-velocity*3*Projectile.frameCounter++;
                        if (Projectile.frameCounter == 30)
                        {
                            state = 2;
                            Projectile.friendly = true;
                            Projectile.frameCounter = 0;
                            SoundEngine.PlaySound(发射, Projectile.position);

                        }
                        break;
                    case 2:
                        Projectile.velocity = velocity * 20f;
                        break;
                    case 3:
                        if (Projectile.frame <= 17)
                        {
                            if (Projectile.frameCounter++ > 30)
                            {
                                Projectile.frame++;
                                if (Projectile.frame == 18)
                                {
                                    Projectile.Kill();
                                }
                                Projectile.frameCounter = 0;
                            }
                        }
                        break;
                }
                if (state == 2)
                {
                    if (Projectile.frame <= 5)
                    {
                        if (Projectile.frameCounter++ > 30)
                        {
                            Projectile.frame++;
                            if (Projectile.frame == 6)
                            {
                                Projectile.frame = 3;
                            }
                            Projectile.frameCounter = 0;
                        }
                    }

                }
            }
            void AI1()
            {
                Projectile.tileCollide = false;
                if (target!=null&& target.active) lockposition = target.Center;

                switch (state)
                {
                    case 0:
                        if (randposition == Vector2.Zero)
                        {
                            if (target == null || !target.active) lockposition = Main.MouseWorld;
                            randposition = Projectile.Center - lockposition;

                        }
                        else Projectile.Center = randposition + lockposition;

                        if (velocity == Vector2.Zero)
                        {
                            velocity = Vector2.Normalize(Projectile.velocity);
                            if (target != null && target.active) velocity = Vector2.Normalize(lockposition - Projectile.Center);
                            //NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, Projectile.whoAmI);
                        }
                        Projectile.velocity = Vector2.Zero;
                        break;
                    case 1:
                        if (target != null && target.active && !target.friendly)
                        {
                            velocity = Vector2.Normalize(target.Center - Projectile.Center);
                        }
                        Projectile.Center = randposition + lockposition - velocity * 3 * Projectile.frameCounter++;
                        if (Projectile.frameCounter == 60)
                        {
                            state = 2;
                            Projectile.friendly = true;
                            Projectile.frameCounter = 0;
                            Projectile.velocity = velocity * 20f;
                            SoundEngine.PlaySound(发射, Projectile.position);

                        }
                        break;
                    case 2:
                        Projectile.frameCounter++;
                        if (Projectile.frameCounter == 30)
                        {
                            Projectile.friendly = false;
                            state = 3;
                            Projectile.velocity = Vector2.Zero;
                            Projectile.frame = 13;
                            Projectile.frameCounter = 0;
                        }
                        break;
                    case 3:
                        if (Projectile.frame <= 17)
                        {
                            if (Projectile.frameCounter++ > 30)
                            {
                                Projectile.frame++;
                                if (Projectile.frame == 18)
                                {
                                    Projectile.Kill();
                                }
                                Projectile.frameCounter = 0;
                            }
                        }
                        break;
                }
                Projectile.rotation = velocity.ToRotation();
            }
            void AI2()
            {
                if (state < 2)
                {
                    velocity = Vector2.Normalize(target2.Center - Projectile.Center);
                    if(Main.myPlayer == Projectile.owner)velocity = Vector2.Normalize(Main.MouseWorld - Projectile.Center);
                }
                Projectile.rotation = velocity.ToRotation();
                switch (state)
                {
                    case 0:
                        Projectile.velocity = Vector2.Zero;
                        if (randposition == Vector2.Zero)
                        {
                            randposition = Projectile.Center - Main.player[Projectile.owner].Center;
                            if(Projectile.owner==Main.myPlayer)
                            Projectile.netUpdate = true;
                        }
                        else Projectile.Center = randposition + Main.player[Projectile.owner].Center;
                        if (Projectile.frame == 3)
                        {   
                            if(target2.ai[1] == 999|| target2==null|| !target2.active|| (Main.myPlayer == Projectile.owner&&Main.mouseLeft))
                            {
                                state = 1;
                            }
                            Projectile.frameCounter = 0;

                        }
                        break;
                    case 1:
                        Projectile.Center = randposition + Main.player[Projectile.owner].Center - velocity * 3 * Projectile.frameCounter++;
                        if (Projectile.frameCounter == 30)
                        {
                            SoundEngine.PlaySound(发射, Projectile.position);

                            state = 2;
                            Projectile.friendly = true;
                            Projectile.frameCounter = 0;
                            target2.localAI[0]++;
                        }
                        break;
                    case 2:
                        Projectile.velocity = velocity * 20f;
                        break;
                    case 3:
                        if (Projectile.frame <= 17)
                        {
                            if (Projectile.frameCounter++ > 30)
                            {
                                Projectile.frame++;
                                if (Projectile.frame == 18)
                                {
                                    Projectile.Kill();
                                }
                                Projectile.frameCounter = 0;
                            }
                        }
                        break;
                }
                if (state == 2)
                {
                    if (Projectile.frame <= 5)
                    {
                        if (Projectile.frameCounter++ > 30)
                        {
                            Projectile.frame++;
                            if (Projectile.frame == 6)
                            {
                                Projectile.frame = 3;
                            }
                            Projectile.frameCounter = 0;
                        }
                    }

                }

            }
            public override bool OnTileCollide(Vector2 oldVelocity)
            {
                if (Projectile.ai[0] > 0)
                {
                    Projectile.velocity = oldVelocity;
                    return false;
                }
                SoundEngine.PlaySound(碎裂, Projectile.position);

                Projectile.friendly = false;
                Projectile.velocity = Vector2.Zero;
                Projectile.frame = 13;
                Projectile.frameCounter = 0;
                state = 3;
                Projectile.tileCollide = false;
                return false;
            }
            public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
            {
                bool onfire = 伊蕾娜.NpcIsOnFire(target);
                if (Projectile.ai[0] == 1 || onfire)
                {
                    SoundEngine.PlaySound(碎裂, Projectile.position);
                    Projectile.friendly = false;
                    Projectile.velocity = Vector2.Zero;
                    Projectile.frame = 13;
                    Projectile.frameCounter = 0;
                    state = 3;
                    Projectile.tileCollide = false;
                    if (onfire)
                    {
                        for (int i = 0; i < 50; i++)
                        {
                            Vector2 v = new(Main.rand.NextFloatDirection() * 40f, Main.rand.NextFloatDirection() * 40f);
                            v = Vector2.Normalize(v) * 10;
                            Dust d = Dust.NewDustDirect(target.Center, 0, 0, DustID.Snow, v.X, v.Y, 1, Color.White, 1.5f);
                            d.noGravity = true;
                        }
                        foreach (NPC npc in Main.npc)
                        {
                            if (npc.active && !npc.dontTakeDamage && !npc.friendly && npc.getRect().Intersects(new Rectangle((int)target.Center.X, (int)target.Center.Y, 75, 75)))
                            {
                                Main.player[Projectile.owner].ApplyDamageToNPC(npc, hit.Damage, hit.Knockback, Projectile.direction, hit.Crit);
                            }
                        }
                    }
                }
                base.OnHitNPC(target, hit, damageDone);
            }

        }
    }
}
