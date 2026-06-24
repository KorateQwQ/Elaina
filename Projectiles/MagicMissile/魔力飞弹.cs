using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using System;
using KL.Extensions;
using 伊蕾娜.Managers;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace 伊蕾娜.Projectiles.MagicMissile
{
    public class 魔力飞弹 : KLProjectile
    {
        //ai[0]储存追踪的npc， ai[1]储存此魔弹的攻击方式
        //0为直线，1为追踪,2为强化版追踪
        Asset<Texture2D> texture2;
        Asset<Texture2D> MainColor;
        Asset<Texture2D> MainShape;
        Asset<Texture2D> MaskColor;
        Vector2[] oldcenter = new Vector2[30];
        Vector2 startposition = Vector2.Zero;
        float randrotation = 0;
        float 追击时间 = 0;
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 3;
            //DisplayName.SetDefault("魔力飞弹");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 30;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }
        public override void SetDefaults()
        {
            //projectile.ignoreWater = true;//无视水
            Projectile.friendly = true;//可以攻击敌人
            Projectile.ownerHitCheck = false;
            //Projectile.penetrate = 1; // 穿透数量
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;//瓷砖碰撞
                                           //projectile.timeLeft=30;
                                           //projectile.extraUpdates=1;
            Projectile.width = 35;
            Projectile.height = 35;
            Projectile.damage = 15;
            Projectile.timeLeft = 800;
            //Projectile.extraUpdates = ;
            Projectile.alpha = 255;
            Projectile.knockBack = -10086;
            Projectile.friendly = true;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.myPlayer == Projectile.owner)
            {
                Player player = Main.player[Projectile.owner];
                var p2 = player.GetModPlayer<魔力飞弹modplayer>();
                Projectile proj = Projectile.NewProjectileDirect(null, Projectile.Center, Vector2.Zero, ModContent.ProjectileType<onhit>(), 0, Projectile.knockBack, Projectile.owner, Projectile.rotation + MathHelper.ToRadians(-90f));
                proj.rotation = Projectile.rotation + MathHelper.ToRadians(-90f);
                proj.scale = 0.7f;
            }
        }

        public override void PostDraw(Color lightColor)
        {

        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D MainTexture = Mod.Assets.Request<Texture2D>("Projectiles/MagicMissile/魔力飞弹",AssetRequestMode.ImmediateLoad).Value;

            texture2 = Mod.Assets.Request<Texture2D>("Projectiles/渐变2");
            Player player = Main.player[Projectile.owner];

            var p2 = player.GetModPlayer<魔力飞弹modplayer>();
            Vector2 scale = new(0.02f, 0.15f);
            Vector2 origin = texture2.Size() * 0.5f;
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap,
            DepthStencilState.Default, RasterizerState.CullNone, null, Main.Transform);
            float alpha = MathHelper.Lerp( 1, 0, Projectile.alpha / 255f);

            if (Projectile.ai[1] > 0)
            {
                int length = 0;
                for (int i = 1; i < 20; i++)
                {
                    if (oldcenter[i]!=Vector2.Zero) length++;
                }
                DrawManager.顶点绘制shader(texture2.Value, oldcenter, new Color(20, 20, 20,100), 5,0.1f, 1* alpha*0.5f,0.01f* alpha, 1,2);

                DrawManager.顶点绘制shader(texture2.Value, oldcenter, new Color(255, 160, 239,0), 10,0.1f, 1* alpha*0.5f,0.01f* alpha*0.7f, 3);
                //开始顶点绘制
            }
            
            Vector2 move = Projectile.velocity;
            move.Normalize();

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
            return true;
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            //overWiresUI.Add(index);

        }
        public void 追踪()
        {
            NPC target = null;
            if (Projectile.ai[0] >= 0) target = Main.npc[(int)Projectile.ai[0]];
            if (target != null && target.active && !target.friendly && !target.dontTakeDamage)
            {
                Vector2 targetVec = target.Center - Projectile.Center;
                targetVec.Normalize();
                // 目标向量是朝向目标的大小为20的向量
                targetVec *= 20f;
                // 朝向npc的单位向量*20 + 3.33%偏移量
                Projectile.velocity = (Projectile.velocity * 15f + targetVec) / 15f;
                Projectile.velocity.Normalize();
                Projectile.velocity *= 20f;
            }
        }
        public void 追踪2()
        {
            NPC target = null;
            if (Projectile.ai[0] >= 0) target = Main.npc[(int)Projectile.ai[0]];
            if (target != null && target.active && !target.friendly && !target.dontTakeDamage)
            {
                Vector2 targetVec = target.Center - Projectile.Center;
                targetVec.Normalize();
                // 目标向量是朝向目标的大小为20的向量
                targetVec *= 28f;
                float 渐进 = MathHelper.Lerp(3, 1, Projectile.timeLeft / 800f);
                targetVec *= 渐进;
                // 朝向npc的单位向量*20 + 3.33%偏移量
                Projectile.velocity = (Projectile.velocity * 15f + targetVec) / 15f;
                Projectile.velocity.Normalize();
                Projectile.velocity *= 40f;
            }
        }
        public void 追踪3()//以贝塞尔曲线追击敌人
        {
            NPC target = null;
            if (Projectile.ai[0] >= 0)
            {
                target = Main.npc[(int)Projectile.ai[0]];
                float rotation = Projectile.rotation;
                if (oldcenter[1].Length() > 1) rotation = (oldcenter[0] - oldcenter[1]).ToRotation() + MathHelper.ToRadians(90f);
                if (target != null && target.active && !target.friendly && !target.dontTakeDamage)
                {
                    //Main.NewText(追击时间);
                    //target.velocity = Vector2.Zero;
                    //Projectile.velocity.Normalize();
                    Projectile.rotation = rotation;
                    Vector2 引导点 = (target.Center - startposition) * 0.5f;
                    引导点 = startposition + 引导点 + (引导点 * 0.5f).RotatedBy(MathHelper.ToRadians(randrotation));
                    float 追击所需时间 = 15;
                    //if (追击时间 > 追击所需时间) 追击时间 = 追击所需时间;
                    Vector2 point1 = Vector2.Lerp(startposition, 引导点, 追击时间 / 追击所需时间);
                    Vector2 point2 = Vector2.Lerp(引导点, target.Center, 追击时间 / 追击所需时间);
                    Projectile.Center = Vector2.Lerp(point1, point2, 追击时间 / 追击所需时间);

                    追击时间++; 
                }
            }
        }
        public override void AI()
        {
            if (startposition == Vector2.Zero&& !(Projectile.ai[0]>= 0))
            {
                randrotation = Main.rand.Next(360);
                startposition = Projectile.Center;
            }
            Projectile.knockBack = -10086;
            //if (Projectile.wet) Projectile.Kill();
            //Projectile.alpha -= 3;
            //Main.NewText(Projectile.CritChance+" " +Projectile.damage);
            Projectile.alpha = (int)MathHelper.Lerp(0, 255, (float)(Projectile.timeLeft - 780) / 20);
            if (Projectile.alpha < 0) Projectile.alpha = 0;

            Lighting.AddLight(Projectile.Center, 255f / 200f, 119f / 200f, 215f / 200f);
            Player player = Main.player[Projectile.owner];
            if (Projectile.timeLeft < 780) if (Collision.SolidTiles(Projectile.Center + Vector2.Normalize(Projectile.velocity) * 5, 1, 1))
                {
                    Projectile.Kill();
                }

            //if(projectile.ai[0]<1)p=player.position;
            //else player.position=p;
            //
            for (int i = Projectile.oldPos.Length - 1; i > 0; --i)
                oldcenter[i] = oldcenter[i - 1];
            oldcenter[0] = Projectile.Center;

            Projectile.frameCounter++;
            if (Projectile.frameCounter % 3 == 0)
            {
                Projectile.frame += 1;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= 3)
            {
                Projectile.frame = 0;
            }


            //projectile.spriteDirection = projectile.direction = (projectile.velocity.X > 0).ToDirectionInt();
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.ToRadians(90f);
            //if(projectile.timeLeft<800){//开始追踪

            if (Main.myPlayer == player.whoAmI)
            {
                Projectile.ai[0] = Projectile.FindTargetWithLineOfSight(800f);
                Projectile.netUpdate = true;
            }
            if (Projectile.ai[1] == 1) 追踪();
            else if (Projectile.ai[1] == 2) 追踪2();
            else
            {
                Projectile.scale = 0.7f;
                Dust d = Dust.NewDustDirect(Projectile.Center, 5, 5, DustID.PinkTorch, 0, 0, 0, new(255, 255, 255), 2);
                d.noGravity = true;
            }

            // 最大寻敌距离为1000像素
            /*float distanceMax = 300f;
			foreach (NPC npc in Main.npc)
			{
				// 如果npc活着且敌对
				if (npc.active && !npc.friendly && !npc.dontTakeDamage)//!npc.dontTakeDamage只寻找能攻击的敌人
				{
					// 计算与玩家的距离
					float currentDistance = Vector2.Distance(npc.Center, Projectile.Center);
					// 如果npc距离比当前最大距离小
					if (currentDistance < distanceMax)
					{
						// 就把最大距离设置为npc和玩家的距离
						// 并且暂时选取这个npc为距离最近npc
						distanceMax = currentDistance;
						target = npc;
					}
				}
			}
			// 如果找到符合条件的npc
			if (target != null)
			{
				Vector2 targetVec = target.Center - projectile.Center;
				targetVec.Normalize();
				// 目标向量是朝向目标的大小为20的向量
				targetVec *= 2f;
				// 朝向npc的单位向量*20 + 3.33%偏移量
				projectile.velocity = (projectile.velocity * 150f + targetVec) / 150f;

			}*/
            //}




        }

        /*public override bool? CanHitNPC(NPC target)
        {
			target.immune[Projectile.owner] = 60;
			return base.CanHitNPC(target);
        }*/
        private struct CustomVertexInfo : IVertexType
        {
            private static VertexDeclaration _vertexDeclaration = new(
            [
                new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
                new VertexElement(8, VertexElementFormat.Color, VertexElementUsage.Color, 0),
                new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0)
            ]);
            public Vector2 Position;
            public Color Color;
            public Vector3 TexCoord;

            public CustomVertexInfo(Vector2 position, Color color, Vector3 texCoord)
            {
                Position = position;
                Color = color;
                TexCoord = texCoord;
            }

            public VertexDeclaration VertexDeclaration
            {
                get
                {
                    return _vertexDeclaration;
                }
            }
        }
    }
}
