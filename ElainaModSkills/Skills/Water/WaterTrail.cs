using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KL.Dusts;
using KL.Dusts.Water;
using Terraria;
using Terraria.Audio;


namespace 伊蕾娜.ElainaModSkills.Skills.Water
{
    internal class WaterTrail : KLProjectile
    {
        Vector2[] oldPosition = new Vector2[100];

        Player player => Main.player[Projectile.owner];

        private Vector2 oldTargetCenter;
        public Vector2 TargetCenter;
        public int MaxLifeTime = 20;
        float RandRotation = 0;

        Vector2 RandOffset = Vector2.Zero;
        trailMode TrailMode = trailMode.straight;

        //水流因顶点的长度
        private float totalLength = 100f;
        
        //水流朝向
        public bool ReverseDirection = false;

        private int timeForDust = 0;

        private Vector2 towardToWaterBall;

        enum trailMode
        {
            //水流以直线流向目标位置
            straight,
            //水流以随机旋转角度流向目标位置
            random,
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
        }

        //可以给水球充能
        private int trailWater = 10;
        public override void SetDefaults()
        {
            MaxLifeTime = Main.rand.Next(30, 40);
            Projectile.timeLeft = MaxLifeTime;
            RandRotation = Main.rand.NextFloat(-0.9f, 0.9f);
            RandOffset = Main.rand.NextVector2Circular(20, 20);
            Projectile.tileCollide = false;

            Projectile.width = Projectile.height = 20;
            
            TrailMode = trailMode.random;
            timeForDust = 5;
            base.SetDefaults();
        }

        public override bool ShouldUpdatePosition()
        {
            return false;
        }

        public override void OnSpawn_AllClient()
        {
            if ((int)Projectile.ai[0] >= 0 && (int)Projectile.ai[0] < Main.projectile.Length)
            {
                if (Main.projectile[(int)Projectile.ai[0]].ModProjectile is WaterBall waterBall)
                {
                    if (waterBall.WaterBallRadius <= 295)
                    {
                        waterBall.WaterBallRadius += 5;
                    }
                    
                    towardToWaterBall = TargetCenter - waterBall.Projectile.Center;
                    towardToWaterBall.Normalize();
                }
            }

            oldTargetCenter = TargetCenter;
            InitTrailVertex();
            base.OnSpawn_AllClient();
        }

        void InitTrailVertex()
        {
            totalLength = 0;
            for (int i = 0; i < oldPosition.Length; i++)
            {
                oldPosition[i] = 确定拖尾顶点(i / (float)oldPosition.Length);
                if (TrailMode == trailMode.straight)
                {
                    totalLength = (TargetCenter - Projectile.Center).Length();
                }
                else if(i>0)
                {
                    totalLength += (oldPosition[i - 1] - oldPosition[i]).Length();
                }
            }
        }

        //水流需要参数:目标位置，拖尾模式，拖尾宽度
        //ReverseDirection可以控制水流方向
        public override void AI()
        {
            if (trailWater > 0)
            {
                trailWater--;
                if ((int)Projectile.ai[0] >= 0 && (int)Projectile.ai[0] < Main.projectile.Length)
                {
                    if (Main.projectile[(int)Projectile.ai[0]].ModProjectile is WaterBall waterBall)
                    {
                        if (waterBall.extraChargeWater <= 98f)
                        {
                            waterBall.extraChargeWater += 2f;
                        }
                    
                        towardToWaterBall = TargetCenter - waterBall.Projectile.Center;
                        towardToWaterBall.Normalize();
                    }
                }
            }

            
            //TargetCenter = Main.MouseWorld;
            if ((int)Projectile.ai[0] >= 0 && (int)Projectile.ai[0] < Main.projectile.Length)
            {
                if (Main.projectile[(int)Projectile.ai[0]].ModProjectile is WaterBall { state: WaterBall.State.Hold } waterBall)
                {
                    if (waterBall.Projectile.active)
                    {
                        TargetCenter = waterBall.Projectile.Center + towardToWaterBall * waterBall.WaterBallRadius * 0.3f;
                    }
                }
            }

            if (oldTargetCenter != TargetCenter)
            {
                //timeForDust = 10;
                oldTargetCenter = TargetCenter;
                InitTrailVertex();
            }
            
            base.AI();
        }
        

        public override bool PreDraw(ref Color lightColor)
        {
            DrawChargeTrail(oldPosition);

            return false;
        }
        

        void DrawChargeTrail(Vector2 []trailPos)
        {   
            Asset<Texture2D> trail = ModContent.Request<Texture2D>("KL/Effects/Tex/Trail/LightTrail2");
            Asset<Texture2D> trail2 = ModContent.Request<Texture2D>("KL/Effects/Tex/cellnoise");
            Asset<Texture2D> noise = ModContent.Request<Texture2D>("KL/Effects/Tex/Noise/5");
            Asset<Texture2D> noise2 = ModContent.Request<Texture2D>("KL/Effects/Tex/水波");

            if (trailPos is { Length: > 0 })
            {


                Vector2 move1 = trailPos[0]-trailPos[1];
                move1.Normalize();
                
                Vector2 move2 = trailPos[^2]-trailPos[^3];
                move2.Normalize();
                Vector2 attachPoint = Projectile.Center;


                int time2 = (int)MathHelper.Min(25f,Projectile.timeLeft);
                
                uint time = Main.GameUpdateCount;
                float timeEffect = MathHelper.Max(0, MathHelper.Lerp(0, 1, (time2) / 25f));
                
                float width = 3 * timeEffect;
                float width2 = 3* timeEffect;
                
                Vector2 imageScale = new Vector2(256f/totalLength, 1);

                Vector2 uTime = new Vector2(1-(time % 120) / 20f, 0);
                
                if (timeForDust++ >=5)
                {
                    timeForDust = 0;
                    KLBasicDust.SpawnDustsCircle(trailPos[0]+attachPoint + move1 * 5 +Main.rand.NextVector2Circular(width,width)*1.5f, ModContent.DustType<WaterDust3>(), 1,move1 * -3, 1.5f,
                        15, new Color(180, 230, 255, 255), new Vector2(0.3f)*width,width*5f);
                    
                    KLBasicDust.SpawnDustsCircle(trailPos[0]+attachPoint + move1 * 5+Main.rand.NextVector2Circular(width,width)*2.5f, ModContent.DustType<WaterDust>(), 2,move1 * -3, 1.5f,
                        15, new Color(180, 230, 255, 255), new Vector2(0.3f)*width,width*2f);
                    
                    
                    KLBasicDust.SpawnDustsCircle(trailPos[^2]+attachPoint +Main.rand.NextVector2Circular(width,width)*1.5f, ModContent.DustType<WaterDust3>(), 1,move2 *-3, 1.5f,
                        15, new Color(180, 230, 255, 255), new Vector2(0.3f)*width,width*5f);
                    
                    KLBasicDust.SpawnDustsCircle(trailPos[^2]+attachPoint +Main.rand.NextVector2Circular(width,width)*2.5f, ModContent.DustType<WaterDust>(), 2,move2 *-3, 1.5f,
                        15, new Color(180, 230, 255, 255), new Vector2(0.3f)*width,width*2f);
                }
                

                
                
                TrailEffect(noise.Value, trailPos, new Color(30, 180, 255, 255) , new Color(30, 180, 255, 255) ,
                    width, width2, blendState: 2, startAlpha:1f* timeEffect, endAlpha:1f* timeEffect, attachPoint: attachPoint,drawTimes:1,
                    uTime: uTime, imageScale: imageScale,
                    clipMask: noise.Value, threshold: 0.2f+(1-timeEffect), maskScale: imageScale, maskTime: uTime,
                    useRforAlpha:true);
                
                TrailEffect(noise2.Value, trailPos, new Color(30, 180, 255, 255) , new Color(30, 180, 255, 255),
                    width, width2, blendState: 2, startAlpha: 0.8f* timeEffect, endAlpha: 0.8f* timeEffect, attachPoint: attachPoint,drawTimes:1,
                    uTime: uTime, imageScale: imageScale,
                    clipMask: noise.Value, threshold: 0.2f+(1-timeEffect), maskScale: imageScale, maskTime: uTime,
                    useRforAlpha:true);
                


                TrailEffect(noise.Value, trailPos, Color.White * timeEffect, Color.White * timeEffect,
                    width, width2, blendState: 1, startAlpha: 0.8f, endAlpha: 0.8f, attachPoint: attachPoint,drawTimes:1,
                    uTime:uTime, imageScale: imageScale,
                    clipMask: noise.Value, threshold: 0.7f+(1-timeEffect), maskScale: imageScale,
                    maskTime: uTime);
                
                TrailEffect(trail.Value, oldPosition, Color.White * timeEffect, Color.White * timeEffect,
                    width*1.1f, width2*1.1f, blendState: 1, startAlpha: 1.5f, endAlpha: 1.5f, attachPoint: attachPoint,drawTimes:2,
                    clipMask: noise.Value, threshold: 0.0f, maskScale: imageScale, maskTime: uTime,
                    debugPoint: false); 
            }

            EndBeginDraw();
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
            List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            base.DrawBehind(index, behindNPCsAndTiles, behindNPCs, behindProjectiles, overPlayers, overWiresUI);
        }

        Vector2 确定拖尾顶点(float progress)
        {
            float xScale = 1;
            float yScale = 1;

            Vector2 startPosition = new Vector2(0);
            Vector2 endPosition =  TargetCenter - Projectile.Center;

            Vector2 toward = endPosition - startPosition;
            toward = toward.SafeNormalize(toward) * (endPosition - startPosition).Length() / 4f;

            if (TrailMode == trailMode.straight)
            {
                RandRotation = 0f;
                toward = toward.SafeNormalize(toward) * (endPosition - startPosition).Length() / 3f;
            }

            Vector2 引导点1 = startPosition + toward.RotatedBy(RandRotation);
            Vector2 引导点2 = startPosition + toward.RotatedBy(RandRotation * 0.5f) * 2f;


            Vector2 point1 = Vector2.Lerp(startPosition, 引导点1, progress);
            Vector2 point2 = Vector2.Lerp(引导点1, 引导点2, progress);
            Vector2 point3 = Vector2.Lerp(引导点2, endPosition, progress);

            Vector2 Point1 = Vector2.Lerp(point1, point2, progress);
            Vector2 Point2 = Vector2.Lerp(point2, point3, progress);

            Vector2 finalPosition = Vector2.Lerp(Point1, Point2, progress);

            return finalPosition;
        }
    }
}