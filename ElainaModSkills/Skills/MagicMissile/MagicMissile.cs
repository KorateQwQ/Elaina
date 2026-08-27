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
        effect ??= ModContent.Request<Effect>("伊蕾娜/Effects/Content/MagicMissileEffect", AssetRequestMode.ImmediateLoad)
            .Value;
        waterNoise ??= ModContent.Request<Texture2D>("伊蕾娜/Effects/Tex/水波", AssetRequestMode.ImmediateLoad);
        headClip ??= ModContent.Request<Texture2D>("KL/Effects/Tex/射灯", AssetRequestMode.ImmediateLoad);
        trail ??= ModContent.Request<Texture2D>("KL/Effects/Tex/Trail/LightTrail");

        //projectile.ignoreWater = true;//无视水
        Projectile.friendly = true; //可以攻击敌人
        Projectile.ownerHitCheck = false;
        Projectile.penetrate = 1; // 穿透数量
        Projectile.DamageType = ModContent.GetInstance<ElainaBasicDamage>();
        //InfusionElement = ElementType.Fire;

        Projectile.tileCollide = false; //瓷砖碰撞
        //projectile.timeLeft=30;
        //projectile.extraUpdates=1;
        Projectile.width = 5;
        Projectile.height = 5;
        Projectile.damage = 15;
        Projectile.timeLeft = 300;
        //Projectile.extraUpdates = ;
        Projectile.alpha = 0;

        TrailLength = 15;
        scale = 0.0f;
        totalAlpha = 0;

        base.SetDefaults();
    }

    public override void OnSpawn_AllClient()
    {
        Vector2 toward = Projectile.velocity.SafeNormalize(Projectile.velocity);

        KLBasicDust.SpawnDust(Projectile.Center + toward * 30f, ModContent.DustType<CircleBurst>(), toward * 0.1f, 20,
            new Color(255, 120, 239, 255), new Vector2(0.3f, 0.7f));
        KLBasicDust.SpawnDust(Projectile.Center + toward * 30f, ModContent.DustType<CircleBurst>(), toward * 0.1f, 20,
            new Color(255, 120, 239, 255), new Vector2(0.3f, 0.7f));

        base.OnSpawn_AllClient();
    }

    public override void OnSpawn(IEntitySource source)
    {
        base.OnSpawn(source);
    }

    public override void AI()
    {
        Projectile.rotation = Projectile.velocity.ToRotation();
        Lighting.AddLight(Projectile.Center, 伊蕾娜.skillColor[伊蕾娜.SkillType.MagicMissile].ToVector3());
        //Projectile.velocity = Vector2.Zero;
        //TraceTarget();
        count++;
        if (totalAlpha < 1) totalAlpha += 0.1f;
        if (scale < 0.7) scale += 0.1f;

        if (count > 10)
        {
            Projectile.tileCollide = true;
            NPC target = null;
            Projectile.ai[0] = Projectile.FindTarget(searchAngle: 120);

            if (Projectile.ai[0] >= 0)
            {
                target = Main.npc[(int)Projectile.ai[0]];
                Projectile.TraceTargetPosition(target.Center, 20, 0.03f);
            }
        }

        //TraceTarget();
        base.AI();
    }

    public void TraceTarget()
    {
        NPC target = null;
        Projectile.ai[0] = Projectile.FindTargetWithLineOfSight(1000f);

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
            Projectile.velocity *= 20f;
        }
    }

    void DrawFlower(float rotation = 0)
    {
        Texture2D flower = AssetManager.GetTexture("伊蕾娜.ElainaModSkills.Skills.MagicMissile.Flo_1");
        Texture2D flowerEdge = AssetManager.GetTexture("伊蕾娜.ElainaModSkills.Skills.MagicMissile.Flower_Edge");


        DrawInWorld(flower, Main.MouseWorld, new Color(255, 255, 255, 255), scale: new Vector2(1f), rotation: rotation);
        //DrawInWorld(flowerEdge,Main.MouseWorld,new Color(255, 255, 255,255),scale:new Vector2(0.5f),rotation:rotation);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Color borderColor = new Color(0, 0, 0, 255);
        base.PreDraw(ref lightColor);
        if (trail != null && OldCenter.Length > 2 && OldCenter != null)
        {
            if (DrawSystem.GetShouldBloom())
            {
                TrailEffect(TextureAssets.MagicPixel.Value, OldCenter, borderColor,
                    borderColor,
                    2, 0f, startAlpha: 1f, endAlpha: -0.5f, drawTimes: 1,
                    uTime: new Vector2(1 - (count % 120) / 30f, 0),
                    blendState: 2);
            }


            TrailEffect(trail.Value, OldCenter, GetColor(new Color(255, 160, 239, 255)),
                new Color(255, 160, 239, 255) * 0f,
                8, 2f, startAlpha: DrawSystem.GetShouldBloom() ? 1.3f : 0f, endAlpha: 3f, drawTimes: 1,
                uTime: new Vector2(1 - (count % 120) / 30f, 0),
                blendState: 1);
        }

        float clipValue = 0.4f;

        Vector2 time = new Vector2((count % 120) / 40f, 0);

        effect.Parameters["uTime"].SetValue(time);
        effect.Parameters["clipValue"].SetValue(clipValue);
        effect.Parameters["clipValue2"].SetValue(0.2f);

        effect.Parameters["Edge"].SetValue(0.00f);
        effect.Parameters["EdgeColor"].SetValue(borderColor.ToVector4());
        effect.Parameters["imageColor"].SetValue(new Vector4(new Vector3(0, 0, 0) * 1.0f, 1));

        Main.graphics.GraphicsDevice.Textures[1] = waterNoise.Value;
        Main.graphics.GraphicsDevice.Textures[2] = headClip.Value;


        EndBeginDraw(2, shader: effect, ss: SamplerState.LinearWrap, adjustToScreen: true);

        Vector2 move = new Vector2(1, 0).RotatedBy(Projectile.rotation) * 35f;

        Vector2 borderScale = new Vector2(0.32f, 0.37f);
        Vector2 borderOffset = new Vector2(1.09f);
        /*Main.spriteBatch.Draw(waterNoise.Value, Projectile.Center + move*borderOffset- Main.screenPosition, waterNoise.Value.GetRec(),
            borderColor, Projectile.rotation, new Vector2(waterNoise.Size().X,waterNoise.Size().Y/2f), new Vector2(3.5f,2.2f)* borderScale * scale, 0, 0);

        Main.spriteBatch.Draw(waterNoise.Value, Projectile.Center + move*borderOffset- Main.screenPosition, waterNoise.Value.GetRec(),
            borderColor, Projectile.rotation, new Vector2(waterNoise.Size().X,waterNoise.Size().Y/2f), new Vector2(5.7f,0.8f)* borderScale* scale, 0, 0);

        EndBeginDraw(2,shader:effect,ss:SamplerState.LinearWrap,adjustToScreen:true);

        effect.Parameters["Edge"].SetValue(0.01f);
        effect.Parameters["imageColor"].SetValue(new Vector4(new Vector3(1,0.5f,0.8f)*(DrawSystem.GetShouldBloom()?2.5f:1.5f),1.0f));

        Main.spriteBatch.Draw(waterNoise.Value, Projectile.Center + move - Main.screenPosition, waterNoise.Value.GetRec(),
            new Color(255,255,255,255), Projectile.rotation, new Vector2(waterNoise.Size().X,waterNoise.Size().Y/2f), new Vector2(3.5f,2.2f)* 0.3f * scale, 0, 0);

        Main.spriteBatch.Draw(waterNoise.Value, Projectile.Center + move- Main.screenPosition, waterNoise.Value.GetRec(),
            new Color(255,255,255,255), Projectile.rotation, new Vector2(waterNoise.Size().X,waterNoise.Size().Y/2f), new Vector2(5.7f,0.8f)* 0.3f* scale, 0, 0);*/
        //if(trail!= null && OldCenter!=null)TrailEffect(trail.Value,OldCenter,new Color(255, 255, 255,255),Color.White*0f,5,0.1f,drawTimes:3,uTime:new Vector2(1-(count % 120) / 30f,0));

        DrawMagicMissile();
        EndBeginDraw();
        return false;
    }

    void DrawMagicMissile()
    {
        Texture2D waterNoise = ModContent.Request<Texture2D>("KL/Effects/Tex/Noise/1", AssetRequestMode.ImmediateLoad)
            .Value;
        Texture2D waterNoise2 =
            ModContent.Request<Texture2D>("KL/Effects/Tex/水波", AssetRequestMode.ImmediateLoad).Value;
        Texture2D waterNoise3 = ModContent
            .Request<Texture2D>("KL/Effects/Tex/Noise/Eff_Noise_11", AssetRequestMode.ImmediateLoad).Value;

        Texture2D ball = ModContent.Request<Texture2D>("KL/Effects/Tex/射灯_alpha", AssetRequestMode.ImmediateLoad).Value;
        Vector2 move = new Vector2(1, 0).RotatedBy(Projectile.rotation);
        Vector2 totalMove = move * -57;
        Vector2 scale = new Vector2(0.35f, 0.40f);

        EndBeginDraw(2, 1);
        RadialDissolve(new Vector4(new Vector3(0.0f), 1.0f), waterNoise, 0.2f,
            new Vector2((float)VisualTime % 360 / 120f, 0), new Vector2(1), 0.58f, 0.52f, -25f,
            sweepDirection: new Vector2(1, 0),
            imageTex: waterNoise3, internalTextureOffset: new Vector2((float)VisualTime % 360 / 40f, 0),
            internalTextureScale: new Vector2(1.5f));

        DrawInWorld(ball, Projectile.Center + totalMove, Color.White, new Vector2(1.0f, 0.5f) * scale * 1.00f,
            Projectile.rotation);

        RadialDissolve(new Vector4(new Vector3(0.0f), 1.0f), waterNoise, 0.2f,
            new Vector2((float)VisualTime % 360 / 120f, 0), new Vector2(1), 0.58f, 0.52f, -25f,
            sweepDirection: new Vector2(1, 0),
            imageTex: waterNoise2, internalTextureOffset: new Vector2((float)VisualTime % 360 / 40f, 0),
            internalTextureScale: new Vector2(1.5f));

        DrawInWorld(ball, Projectile.Center + move * 10 + totalMove, Color.White,
            new Vector2(0.8f, 0.3f) * scale * 1.00f, Projectile.rotation);

        if(!DrawSystem.GetShouldBloom()) EndBeginDraw(1, 1);

        RadialDissolve(new Vector4(new Vector3(1, 0.5f, 0.8f) * (DrawSystem.GetShouldBloom() ? 7.5f : 1.5f), 1.0f),
            waterNoise, 0.2f,
            new Vector2((float)VisualTime % 360 / 120f, 0), new Vector2(1), 0.58f, 0.52f, -25f,
            sweepDirection: new Vector2(1, 0),
            imageTex: waterNoise3, internalTextureOffset: new Vector2((float)VisualTime % 360 / 40f, 0),
            internalTextureScale: new Vector2(1.5f));

        DrawInWorld(ball, Projectile.Center + totalMove, Color.White, new Vector2(1.0f, 0.5f) * scale,
            Projectile.rotation);

        RadialDissolve(new Vector4(new Vector3(1, 0.5f, 0.8f) * (DrawSystem.GetShouldBloom() ? 3.5f : 1.5f), 1.0f),
            waterNoise, 0.2f,
            new Vector2((float)VisualTime % 360 / 120f, 0), new Vector2(1), 0.58f, 0.52f, -25f,
            sweepDirection: new Vector2(1, 0),
            imageTex: waterNoise2, internalTextureOffset: new Vector2((float)VisualTime % 360 / 40f, 0),
            internalTextureScale: new Vector2(1.5f));

        DrawInWorld(ball, Projectile.Center + move * 10 + totalMove, Color.White, new Vector2(0.8f, 0.3f) * scale,
            Projectile.rotation);
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float width = 50;
        Vector2 dir = new Vector2(1, 0).RotatedBy(Projectile.rotation);
        return AABBvLineCollision(targetHitbox, Projectile.Center - dir * width / 2,
            Projectile.Center + dir * width / 2, width);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        bool fullMark = target.GetGlobalNPC<MagicMissileNpcMark>().ApplyMark(target);
        if (fullMark)
        {
            //Main.player[Projectile.owner].GetModPlayer<ElainaAttributeModPlayer>().RegenPercentMagicPoint(15,true);
            Projectile.damage *= 2;
            Projectile.Damage();
        }

        base.OnHitNPC(target, hit, damageDone);
    }

    public override void OnKill(int timeLeft)
    {
        Vector2 velocity = Projectile.velocity.SafeNormalize(Projectile.velocity);
        KLBasicDust.SpawnDustsCircle(Projectile.Center, ModContent.DustType<ShockBlackDust>(), 7, -velocity * 10.1f,
            2.14f, 10, new Color(180, 50, 180, 255), new Vector2(0.3f, 0.1f), 0, 10, new Vector2(0.05f, 0.03f), 3);


        KLBasicDust.SpawnDust(Projectile.Center, ModContent.DustType<BurstPoint>(),
            Main.rand.NextVector2Circular(0.1f, 0.1f),
            lifeTime: 12, color: new Color(180, 50, 180, 255), scale: new Vector2(1.3f));

        KLBasicDust.SpawnDust(Projectile.Center, ModContent.DustType<BurstPoint>(),
            Main.rand.NextVector2Circular(0.1f, 0.1f),
            lifeTime: 12, color: new Color(255, 150, 239, 0), scale: new Vector2(1.3f));
        KLBasicDust.SpawnDust(Projectile.Center, ModContent.DustType<BurstPoint>(),
            Main.rand.NextVector2Circular(0.1f, 0.1f),
            lifeTime: 12, color: new Color(255, 150, 239, 0), scale: new Vector2(1.3f));

        KLBasicDust.SpawnDustsCircle(Projectile.Center, ModContent.DustType<ShockDust>(), 5, -velocity * 10.1f,
            2.14f, 15, new Color(255, 120, 239, 255), new Vector2(0.2f, 0.1f), 0, 10, new Vector2(0.05f, 0.03f), 3);


        KLBasicDust.SpawnDustsCircle(Projectile.Center, ModContent.DustType<LineSparkle>(), 5, -velocity * 10.1f,
            3.14f, 15, new Color(255, 120, 239, 0), new Vector2(1, 0.3f), 0, 0, new Vector2(0.5f, 0f), 3);

        KLBasicDust.SpawnDustsCircle(Projectile.Center, ModContent.DustType<LineSparkle>(), 10, -velocity * 10.1f,
            3.14f, 15, new Color(255, 120, 239, 0), new Vector2(1, 0.3f), 0, 20, new Vector2(0.5f, 0f), 3);

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
                flowers[i] = AssetManager.GetTexture($"伊蕾娜.ElainaModSkills.Skills.MagicMissile.Flo_{i + 1}",
                    AssetRequestMode.ImmediateLoad);
            }

            base.Load();
        }

        public bool ApplyMark(NPC npc)
        {
            markSpawnFrames[markCount] = Main.GameUpdateCount;
            markCount++;
            if (markCount >= 5)
            {
                KLBasicDust.SpawnDust(npc.Center, ModContent.DustType<FloDust>(), Vector2.Zero, 15,
                    new Color(255, 255, 255, 200));
                markCount = 0;
                return true;
            }

            return false;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (markCount > 0) Lighting.AddLight(npc.Center, new Color(255, 120, 239, 0).ToVector3());
            return base.PreDraw(npc, spriteBatch, screenPos, drawColor);
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            for (int i = 0; i < markCount; i++)
            {
                float fadeProgress = MathHelper.Clamp((Main.GameUpdateCount - markSpawnFrames[i]) / 15f, 0f, 1f);
                float scale = KLMathF.ClampLerp(2f, 1f, (Main.GameUpdateCount - markSpawnFrames[i]) / 15f);

                DrawInWorld(flowers[i], npc.Center, new Color(255, 255, 255, 200) * fadeProgress,
                    scale: new Vector2(1f) * scale);
            }

            //DrawInWorld(flower,npc.Center,new Color(255, 255, 255,200));
            base.PostDraw(npc, spriteBatch, screenPos, drawColor);
        }
    }
}