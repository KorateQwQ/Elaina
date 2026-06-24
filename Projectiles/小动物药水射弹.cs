using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace 伊蕾娜.Projectiles
{
    public class 小动物药水射弹 : ModProjectile
    {
        bool flag = false;
        int hp = 500;
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Critter Potion");
            //DisplayName.AddTranslation(7, "小动物药水");
        }
        public override void SetDefaults()
        {
            Projectile.ignoreWater = false;//无视水
            Projectile.friendly = false;//可以攻击敌人	    
            //Projectile.hostile = true;//可以攻击友军
            Projectile.penetrate = 1; // 穿透数量
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = true;//瓷砖碰撞
            Projectile.timeLeft = 300;
            Projectile.width = 35;
            Projectile.height = 35;
            Projectile.scale = 1f;
            Projectile.damage = 1;
        }
        public override void AI()
        {
            var player = Main.player[Projectile.owner];
            hp = player.statLifeMax2;
            //if (Main.myPlayer==player.whoAmI)
            if (Projectile.timeLeft > 290) Projectile.damage = 0;
            else Projectile.damage = 10;
            
            Projectile.hostile = true;
            Projectile.rotation += 0.4f;
            if (Projectile.velocity.Y < 10)
                Projectile.velocity.Y += 0.17f;
            if (Projectile.timeLeft < 290||Projectile.velocity==Vector2.Zero)
            {
                foreach (Player target in Main.player)
                {
                    if ((target.Center - Projectile.Center).Length() < 50)
                    {
                        Projectile.Kill();
                    }
                }
            }


            base.AI();
        }
        public override bool? CanHitNPC(NPC target)
        {   
            return false;
        }
        public override bool CanHitPlayer(Player target)
        {
            return true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.ai[1] == 1) return false;
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointWrap, DepthStencilState.Default, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
            return base.PreDraw(ref lightColor);
        }
        public override void PostDraw(Color lightColor)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
            base.PostDraw(lightColor);
        }
        public override void OnKill(int timeLeft)
        {
            var player1 = Main.player[Projectile.owner];
            hp = player1.statLifeMax2;
            {
                if (Projectile.ai[1] == 1)
                {
                    施加小动物buff(player1);
                }
                else
                {
                    释放粒子();
                    foreach (var player in Main.player)
                    {
                        if (player.active && !player.dead && (player.Center - Projectile.Center).Length() < 60)
                        {
                            hp = player.statLifeMax2;
                            施加小动物buff(player);
                            break;
                        }
                    }
                }

            }

            base.OnKill(timeLeft);
        }
        void 释放粒子()
        {
            for(int i =0; i< 350; i++)
            {
                Vector2 v =  Main.rand.NextVector2Circular(15, 15);
                Dust d = Dust.NewDustDirect(Projectile.position, 10, 10, 16,
                -v.X, -v.Y, 0, Color.White, 2f);
                d.noLight = false;
                d.noGravity = true;
            }

        }
        void 施加小动物buff(Player player)
        {
            //if (Main.netMode != 1)
            {
                if (!player.HasBuff<Buffs.小动物>() || Projectile.ai[1]==1)
                {
                    player.AddBuff(ModContent.BuffType<Buffs.小动物>(), 20);
                    //NPC.SpawnOnPlayer(player.whoAmI, 443);
                    //Main.NewText("施加buff");
                    int n = NPC.NewNPC(null, (int)player.Center.X, (int)player.Center.Y, (int)Projectile.ai[0]);//(int)Projectile.ai[0]
                    Main.npc[n].GetGlobalNPC<CrittersGlobalnpc>().player = player.whoAmI;
                    Main.npc[n].GetGlobalNPC<CrittersGlobalnpc>().IfCountrolByPlayer = true;
                    Main.npc[n].lifeMax = hp;
                    Main.npc[n].life = hp;
                    Main.npc[n].GetGlobalNPC<CrittersGlobalnpc>().hp = hp;
                    Main.npc[n].GetGlobalNPC<CrittersGlobalnpc>().enterworld = true;
                    if (Main.netMode == 2)
                    {
                        //NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, n);
                    }

                    //if (Main.netMode == NetmodeID.MultiplayerClient) NetMessage.SendData(23, -1, -1, null, n);
                }
            }

        }
    }
}
