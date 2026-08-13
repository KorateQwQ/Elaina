using System.IO;
using KL.DamageSystem;
using KL.DamageSystem.ElementalDamageClass;
using KL.Drawing;
using KL.Dusts;
using KL.Dusts.Burst;
using KL.Extensions;
using KL.Utils;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using 伊蕾娜.Dusts;
using 伊蕾娜.ElainaAttribute;
using 伊蕾娜.ElainaModSkills.ElainaDamageClass;

namespace 伊蕾娜.ElainaModSkills.Skills.MagicMissile;

public class MagicMissile : KLProjectile
{
    private int count = 0;
    private bool draw = false;

    private float scale = 0.5f;
    private float totalAlpha = 0;
    Effect effect;
    Asset<Texture2D> waterNoise;
    Asset<Texture2D> headClip;
    Asset<Texture2D> trail;


    public override void SetDefaults()
    {
        effect ??= ModContent.Request<Effect>("伊蕾娜/Effects/Content/MagicMissileEffect", AssetRequestMode.ImmediateLoad).Value;
        waterNoise ??= ModContent.Request<Texture2D>("伊蕾娜/Effects/Tex/水波", AssetRequestMode.ImmediateLoad);
        headClip ??= ModContent.Request<Texture2D>("KL/Effects/Tex/射灯", AssetRequestMode.ImmediateLoad);
        trail ??= ModContent.Request<Texture2D>("KL/Effects/Tex/Trail/LightTrail");
        
        //projectile.ignoreWater = true;//无视水
        Projectile.friendly = true;//可以攻击敌人
        Projectile.ownerHitCheck = false;
        Projectile.penetrate = 1; // 穿透数量
        Projectile.DamageType = ModContent.GetInstance<ElainaBasicDamage>();
        //InfusionElement = ElementType.Fire;
        
        Projectile.tileCollide = true;//瓷砖碰撞
        //projectile.timeLeft=30;
        //projectile.extraUpdates=1;
        Projectile.width = 5;
        Projectile.height = 5;
        Projectile.damage = 15;
        Projectile.timeLeft = 300;
        //Projectile.extraUpdates = ;
        Projectile.alpha = 0;

        TrailLength = 30;
        scale = 0.7f;
        totalAlpha = 0;

        base.SetDefaults();
    }

    public override void OnSpawn_AllClient()
    {
        Vector2 toward = Projectile.velocity.SafeNormalize(Projectile.velocity);

        KLBasicDust.SpawnDust(Projectile.Center+ toward*30f,ModContent.DustType<CircleBurst>(), toward*0.1f, 20, new Color(255, 120, 239,255), new Vector2(0.3f,0.7f));
        KLBasicDust.SpawnDust(Projectile.Center+ toward*30f,ModContent.DustType<CircleBurst>(), toward*0.1f, 20, new Color(255, 120, 239,255), new Vector2(0.3f,0.7f));

        base.OnSpawn_AllClient();
    }

    public override void OnSpawn(IEntitySource source)
    {
        base.OnSpawn(source);
    }

    public override void AI()
    {
        Projectile.rotation = Projectile.velocity.ToRotation(); 
        Lighting.AddLight(Projectile.Center,伊蕾娜.skillColor[伊蕾娜.SkillType.MagicMissile].ToVector3());
        //Projectile.velocity = Vector2.Zero;
        //TraceTarget();
        count++;
        if (totalAlpha < 1) totalAlpha += 0.1f;
        
        base.AI();
    }

    public void TraceTarget()
    {
        NPC target = null;
        Projectile.ai[0] = Projectile.FindTargetWithLineOfSight(500f);
        
        if (Projectile.ai[0] >= 0) target = Main.npc[(int)Projectile.ai[0]];
        if (target != null && target.active && !target.friendly && !target.dontTakeDamage)
        {
            Vector2 targetVec = target.Center - Projectile.Center;
            targetVec.Normalize();
            // 目标向量是朝向目标的大小为20的向量
            targetVec *= 65f;
            float 渐进 = MathHelper.Lerp(1, 1, Projectile.timeLeft / 800f);
            targetVec *= 渐进;
            // 朝向npc的单位向量*20 + 3.33%偏移量
            Projectile.velocity = (Projectile.velocity * 15f + targetVec) / 15f;
            Projectile.velocity.Normalize();
            Projectile.velocity *= 40f;
        }
    }

    void DrawFlower(float rotation = 0)
    {
        Texture2D flower = AssetManager.GetTexture("伊蕾娜.ElainaModSkills.Skills.MagicMissile.Flo_1");
        Texture2D flowerEdge = AssetManager.GetTexture("伊蕾娜.ElainaModSkills.Skills.MagicMissile.Flower_Edge");


        DrawInWorld(flower,Main.MouseWorld,new Color(255, 255, 255,255),scale:new Vector2(1f),rotation:rotation);
        //DrawInWorld(flowerEdge,Main.MouseWorld,new Color(255, 255, 255,255),scale:new Vector2(0.5f),rotation:rotation);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        base.PreDraw(ref lightColor);
        if(trail!= null&&OldCenter.Length>2 && OldCenter!=null)TrailEffect(trail.Value,OldCenter,GetColor(new Color(255, 160, 239,100)),new Color(255, 160, 239,0)*0f,
            8,2f,startAlpha:2.5f,endAlpha:0f,drawTimes:1,uTime:new Vector2(1-(count % 120) / 30f,0),blendState:0);
        
        float clipValue = 0.3f;

        Vector2 time = new Vector2( (count % 120) / 40f,0);

        effect.Parameters["uTime"].SetValue(time);
        effect.Parameters["clipValue"].SetValue(clipValue);
        effect.Parameters["clipValue2"].SetValue(0f);

        effect.Parameters["Edge"].SetValue(0f);
        effect.Parameters["EdgeColor"].SetValue(new Vector4(0));
        effect.Parameters["imageColor"].SetValue(new Vector4(new Vector3(1,0.5f,0.8f)*1.4f,1)* totalAlpha);
        
        Main.graphics.GraphicsDevice.Textures[1] = waterNoise.Value;
        Main.graphics.GraphicsDevice.Textures[2] = headClip.Value;  

        
        EndBeginDraw(0,shader:effect,ss:SamplerState.LinearWrap,adjustToScreen:true);

        Vector2 move = new Vector2(1,0).RotatedBy(Projectile.rotation)*35f;

        Main.spriteBatch.Draw(waterNoise.Value, Projectile.Center + move - Main.screenPosition, waterNoise.Value.GetRec(),
            new Color(150,150,150,255), Projectile.rotation, new Vector2(waterNoise.Size().X,waterNoise.Size().Y/2f), new Vector2(3.5f,2.2f)* 0.3f * scale, 0, 0);

        Main.spriteBatch.Draw(waterNoise.Value, Projectile.Center + move- Main.screenPosition, waterNoise.Value.GetRec(),
            new Color(100,100,100,255), Projectile.rotation, new Vector2(waterNoise.Size().X,waterNoise.Size().Y/2f), new Vector2(5.7f,0.8f)* 0.3f* scale, 0, 0);
        
        Main.spriteBatch.Draw(waterNoise.Value, Projectile.Center + move - Main.screenPosition, waterNoise.Value.GetRec(),
            new Color(255,255,255,0), Projectile.rotation, new Vector2(waterNoise.Size().X,waterNoise.Size().Y/2f), new Vector2(3.5f,2.2f)* 0.3f * scale, 0, 0);

        Main.spriteBatch.Draw(waterNoise.Value, Projectile.Center + move- Main.screenPosition, waterNoise.Value.GetRec(),
            new Color(255,255,255,0), Projectile.rotation, new Vector2(waterNoise.Size().X,waterNoise.Size().Y/2f), new Vector2(5.7f,0.8f)* 0.3f* scale, 0, 0);
        //if(trail!= null && OldCenter!=null)TrailEffect(trail.Value,OldCenter,new Color(255, 255, 255,255),Color.White*0f,5,0.1f,drawTimes:3,uTime:new Vector2(1-(count % 120) / 30f,0));
        EndBeginDraw();
        return false;
    }
    
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return (new Vector2(targetHitbox.X, targetHitbox.Y) - Projectile.Center).Length() < 50;
        return base.Colliding(projHitbox, targetHitbox);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        bool fullMark = target.GetGlobalNPC<MagicMissileNpcMark>().ApplyMark(target);
        if (fullMark)
        {
            Main.player[Projectile.owner].GetModPlayer<ElainaAttributeModPlayer>().RegenPercentMagicPoint(15,true);
            Projectile.damage*=2;
            Projectile.Damage();
        }
        base.OnHitNPC(target, hit, damageDone);
    }

    public override void OnKill(int timeLeft)
    {
        Vector2 velocity = Projectile.velocity.SafeNormalize(Projectile.velocity);
        
        KLBasicDust.SpawnDust(Projectile.Center,ModContent.DustType<BurstPoint>(),Main.rand.NextVector2Circular(0.1f,0.1f),lifeTime:12,color:new Color(255, 150, 239,0),scale:new Vector2(1));
        KLBasicDust.SpawnDust(Projectile.Center,ModContent.DustType<BurstPoint>(),Main.rand.NextVector2Circular(0.1f,0.1f),lifeTime:12,color:new Color(255, 150, 239,0),scale:new Vector2(1.3f));

        KLBasicDust.SpawnDustsCircle(Projectile.Center, ModContent.DustType<LineSparkle>(), 5, -velocity*10.1f, 
            3.14f,20, new Color(255, 120, 239,0),Vector2.One,0,10,new Vector2(0.5f,0f),7);
        
        KLBasicDust.SpawnDustsCircle(Projectile.Center, ModContent.DustType<LineSparkle>(), 10, -velocity*10.1f, 
            3.14f,20, new Color(255, 120, 239,0),Vector2.One,0,50,new Vector2(0.5f,0f),7);
        
        base.OnKill(timeLeft);
    }

    class MagicMissileNpcMark : GlobalNPC
    {
        public override bool InstancePerEntity => true;
        static Texture2D[] flowers = new Texture2D[5];
        
        int markCount = 0;
        ulong[] markSpawnFrames = new ulong[5];
        public override void Load()
        {
            for (int i = 0; i < 5; i++)
            {
                flowers[i] = AssetManager.GetTexture($"伊蕾娜.ElainaModSkills.Skills.MagicMissile.Flo_{i+1}",AssetRequestMode.ImmediateLoad);
            }
            base.Load();
        }

        public bool ApplyMark(NPC npc)
        {
            markSpawnFrames[markCount] = Main.GameUpdateCount;
            markCount++;
            if(markCount>=5)
            {
                KLBasicDust.SpawnDust(npc.Center,ModContent.DustType<FloDust>(),Vector2.Zero,15,new Color(255, 255, 255,200));
                markCount = 0;
                return true;
            }

            return false;
        }
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            return base.PreDraw(npc, spriteBatch, screenPos, drawColor);
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            for (int i = 0; i < markCount; i++)
            {
                float fadeProgress = MathHelper.Clamp((Main.GameUpdateCount - markSpawnFrames[i]) / 15f, 0f, 1f);
                float scale = KLMathF.ClampLerp(2f, 1f,(Main.GameUpdateCount - markSpawnFrames[i]) / 15f);
                
                DrawInWorld(flowers[i],npc.Center,new Color(255, 255, 255,200) * fadeProgress,scale:new Vector2(1f)*scale);
            }

            //DrawInWorld(flower,npc.Center,new Color(255, 255, 255,200));
            base.PostDraw(npc, spriteBatch, screenPos, drawColor);
        }
    }
}