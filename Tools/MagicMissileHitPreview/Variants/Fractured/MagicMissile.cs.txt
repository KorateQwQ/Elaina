using System.IO;
using KL.DamageSystem;
using KL.DamageSystem.ElementalDamageClass;
using KL.Drawing;
using KL.Dusts;
using KL.Extensions;
using KL.Utils;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using 伊蕾娜.ElainaAttribute;
using 伊蕾娜.ElainaModSkills.ElainaDamageClass;

namespace 伊蕾娜.ElainaModSkills.Skills.MagicMissile;

public class MagicMissile : KLProjectile
{
    /// <summary>被动溅射伤害倍率（基于飞弹单发伤害）。</summary>
    private const float SplashDamageMultiplier = 2f;
    /// <summary>被动溅射半径（像素，约10格）。</summary>
    private const float SplashRadius = 160f;

    private int count = 0;
    private bool draw = false;
    private TrailManager.TrailHandle managedTrail;
    private MagicMissileTrailVisuals trailVisuals;
    // 撞墙时沿入射方向找回的接触点与入射角；为空表示命中敌人或寿命结束
    private Vector2? wallContact;
    private float wallImpactRotation;
    protected override bool SampleTrailInDrawBehind => true;

    private float scale = 0.5f;
    private float totalAlpha = 0;
    // 本次命中触发了花印爆发时的目标中心（仅命中方客户端）
    private Vector2? burstCenter;

    private static Texture2D dissolveNoise;
    private static Texture2D waterRipple;
    private static Texture2D innerNoise;
    private static Texture2D headClip;
    private static Texture2D trail;
    private static MagicMissileTrailNoise trailNoise;
    private static Texture2D flower;
    private static Texture2D headStamp;
    private static Texture2D headNoise;
    private static Effect trailEffect;


    public override void Load()
    {
        if (Main.netMode != NetmodeID.Server)
        {
            dissolveNoise = AssetManager.GetTexture("KL/Effects/Tex/Noise/1");
            waterRipple = AssetManager.GetTexture("KL/Effects/Tex/水波");
            innerNoise = AssetManager.GetTexture("KL/Effects/Tex/Noise/Eff_Noise_11");
            headClip = AssetManager.GetTexture("KL/Effects/Tex/射灯_alpha");
            trail = AssetManager.GetTexture("KL/Effects/Tex/Trail/LightTrail");
            trailNoise = new MagicMissileTrailNoise(AssetManager.GetTexture("KL/Effects/Tex/PerlinX"));
            trailEffect = AssetManager.GetEffect("伊蕾娜/Effects/Content/MagicMissileTrail");
            flower = AssetManager.GetTexture("伊蕾娜.ElainaModSkills.Skills.MagicMissile.Flo_1");
            // 直接请求：缺失时报错，而不是返回 null 让头部静默不画
            headStamp = ModContent.Request<Texture2D>("KL/Effects/Tex/Sparkle/T_Vfx_Stamp_HitSword_22", AssetRequestMode.ImmediateLoad).Value;
            headNoise = ModContent.Request<Texture2D>("KL/Effects/Tex/Noise/5", AssetRequestMode.ImmediateLoad).Value;
        }
        base.Load();
    }

    public override void SetDefaults()
    {


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

        TrailSampleCount = 24;
        TrailMaxLength = 320f;
        managedTrail = null;
        trailVisuals = null;
        wallContact = null;
        wallImpactRotation = 0;
        scale = 0.0f;
        totalAlpha = 0;
        burstCenter = null;

        base.SetDefaults();
    }

    public override void OnSpawn_AllClient()
    {
        using var measurement = RenderPerformanceDiagnostics.MeasureSkill("MagicMissile.Spawn");
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
        if (scale < 1) scale += 0.1f;

        if (count > 10)
        {
            Projectile.tileCollide = true;
            NPC target = null;
            Projectile.ai[0] = Projectile.FindTarget(searchAngle: 180);

            if (Projectile.ai[0] >= 0)
            {
                target = Main.npc[(int)Projectile.ai[0]];
                Projectile.TraceTargetPosition(target.Center, Projectile.velocity.Length(), 0.08f);
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
        DrawInWorld(flower, Main.MouseWorld, new Color(255, 255, 255, 255), scale: new Vector2(1f), rotation: rotation);
        //DrawInWorld(flowerEdge,Main.MouseWorld,new Color(255, 255, 255,255),scale:new Vector2(0.5f),rotation:rotation);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        base.PreDraw(ref lightColor);

        return false;
    }

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
        List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
    {
        base.DrawBehind(index, behindNPCsAndTiles, behindNPCs, behindProjectiles, overPlayers, overWiresUI);
        if (trail != null && OldCenter is { Length: > 1 })
        {
            trailVisuals ??= new MagicMissileTrailVisuals(Projectile.identity * 397 ^ Projectile.owner);
            trailVisuals.TextureOffset = 1f - (Main.GameUpdateCount % 120) / 30f;
            TrailManager.CreateOrUpdateTrail(
                ref managedTrail,
                OldCenter,
                lifetime: MagicMissileTrailVisuals.FadeTicks,
                trailVisuals,
                DrawManagedTrail,
                layer: LayerDrawRequestSystem.DrawTargetLayer.Projectiles,
                LayerDrawRequestSystem.DrawTiming.After,
                bloom: true, shrinkPathOnExpire: false);
        }
        LayerDrawRequestSystem.RequestBloom("Elaina:MagicMissile", LayerDrawRequestSystem.DrawTargetLayer.Projectiles,
            LayerDrawRequestSystem.DrawTiming.After, DrawVisual);
    }

    private void DrawVisual()
    {
        DrawMagicMissile();
        EndBeginDraw();
    }

    private static void DrawManagedTrail(MagicMissileTrailVisuals visual, Vector2[] points, float remaining)
    {
        // 长度缩短由 MagicMissileTrailVisuals 截掉尾端几何完成（提交时关闭了 TrailManager 的线性缩短）
        if (points.Length < 2)
            return;

        Main.spriteBatch.End();
        try
        {
            Matrix transform = Matrix.CreateTranslation(-Main.screenPosition.X, -Main.screenPosition.Y, 0) * Main.Transform;
            visual.Draw(Main.graphics.GraphicsDevice, trailEffect, trail, trailNoise, points, remaining, transform,
                DrawSystem.GetBlendStateForBloom(BlendState.Additive), Main.GameViewMatrix.TransformationMatrix.M11);
            // 飞行中由飞弹本体盖住拖尾头部；消失后改由消融的爆点遮住截断端口
            if (visual.Expiring)
                visual.DrawHead(Main.graphics.GraphicsDevice, trailEffect, headStamp, headNoise, points[0], remaining, transform,
                    DrawSystem.GetBlendStateForBloom(BlendState.AlphaBlend));
        }
        finally
        {
            BeginTrailBatch();
        }
    }

    private static void BeginTrailBatch() =>
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
            DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);

    void DrawMagicMissile()
    {
        Texture2D ball = headClip;
        Vector2 move = new Vector2(1, 0).RotatedBy(Projectile.rotation);
        Vector2 totalMove = move * -57*scale;
        Vector2 scale2 = new Vector2(0.35f, 0.40f)*scale;

        EndBeginDraw(2, 1);

        RadialDissolve(new Vector4(new Vector3(1, 0.4f, 0.9f) * 8.5f, 1.0f),
            dissolveNoise, 0.2f,
            new Vector2((float)VisualTime % 360 / 120f, 0), new Vector2(1), 0.58f, 0.52f, -25f,
            sweepDirection: new Vector2(1, 0),
            imageTex: innerNoise, internalTextureOffset: new Vector2((float)VisualTime % 360 / 40f, 0),
            internalTextureScale: new Vector2(1.5f));

        DrawInWorld(ball, Projectile.Center + totalMove, Color.White, new Vector2(1.0f, 0.5f) * scale2,
            Projectile.rotation);

        RadialDissolve(new Vector4(new Vector3(1, 0.7f, 0.8f) *4.5f, 1.0f),
            dissolveNoise, 0.2f,
            new Vector2((float)VisualTime % 360 / 120f, 0), new Vector2(1), 0.58f, 0.52f, -25f,
            sweepDirection: new Vector2(1, 0),
            imageTex: waterRipple, internalTextureOffset: new Vector2((float)VisualTime % 360 / 40f, 0),
            internalTextureScale: new Vector2(1.5f));

        DrawInWorld(ball, Projectile.Center + move * 5 + totalMove, Color.White, new Vector2(0.9f, 0.3f) * scale2,
            Projectile.rotation);
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float width = 50;
        Vector2 dir = new Vector2(1, 0).RotatedBy(Projectile.rotation);
        return AABBvLineCollision(targetHitbox, Projectile.Center - dir * width / 2,
            Projectile.Center + dir * width / 2, width);
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        // 魔法飞弹无视敌人防御，避免前期低伤害被防御吃掉大半
        modifiers.ScalingArmorPenetration += 1f;
        base.ModifyHitNPC(target, ref modifiers);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        // 被动：同一目标累计命中5次后，对周围其他敌人造成一次溅射伤害
        if (target.GetGlobalNPC<MagicMissileNpcMark>().ApplyMark(target))
        {
            SplashDamage(target);
            // 命中后飞弹随即消失，由 OnKill 在目标中心播放花印爆发
            burstCenter = target.Center;
        }

        base.OnHitNPC(target, hit, damageDone);
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        // 原版在此之后才按滑动后的速度移动并 Kill，此时 Center 仍是本次移动前的位置
        wallContact = MagicMissileTrailVisuals.WallContact(Projectile.Center, oldVelocity, Projectile.velocity);
        wallImpactRotation = oldVelocity.ToRotation();
        return base.OnTileCollide(oldVelocity);
    }

    private void SplashDamage(NPC mainTarget)
    {
        int damage = (int)(Projectile.damage * SplashDamageMultiplier);
        foreach (NPC npc in Main.ActiveNPCs)
        {
            // 溅射不对主目标生效
            if (npc.whoAmI == mainTarget.whoAmI || npc.friendly || npc.dontTakeDamage)
                continue;
            if (Vector2.Distance(npc.Center, mainTarget.Center) > SplashRadius)
                continue;

            int hitDirection = npc.Center.X >= mainTarget.Center.X ? 1 : -1;
            NPC.HitModifiers modifiers = npc.GetIncomingStrikeModifiers(Projectile.DamageType, hitDirection);
            modifiers.ScalingArmorPenetration += 1f;
            NPC.HitInfo hitInfo = modifiers.ToHitInfo(damage, false, 0f);
            npc.StrikeNPC(hitInfo);
            if (Main.netMode != NetmodeID.SinglePlayer)
                NetMessage.SendStrikeNPC(npc, hitInfo);

            // 被溅射的敌人从主目标方向受击
            MagicMissileHitDust.Emit(npc.Center, (npc.Center - mainTarget.Center).ToRotation());
        }
    }

    public override void OnKill(int timeLeft)
    {
        Vector2 end = wallContact ?? Projectile.Center;
        // 撞墙后速度已被改成沿墙滑动甚至归零，命中特效与星芒按入射方向
        float angle = wallContact.HasValue ? wallImpactRotation : Projectile.velocity.ToRotation();
        if (!Main.dedServ && trail != null)
        {
            // 碰撞可能发生在下一次 DrawBehind 之前，先补末段，否则命中点与旧拖尾之间会空一截。
            Vector2[] finalPath = MagicMissileTrailVisuals.FinishPath(OldCenter, end, TrailMaxLength);
            trailVisuals ??= new MagicMissileTrailVisuals(Projectile.identity * 397 ^ Projectile.owner);
            trailVisuals.Expiring = true;
            trailVisuals.HeadRotation = angle;
            trailVisuals.TextureOffset = 1f - (Main.GameUpdateCount % 120) / 30f;
            TrailManager.CreateOrUpdateTrail(ref managedTrail, finalPath, MagicMissileTrailVisuals.FadeTicks,
                trailVisuals, DrawManagedTrail, layer: LayerDrawRequestSystem.DrawTargetLayer.Projectiles,
                timing: LayerDrawRequestSystem.DrawTiming.After, bloom: true, shrinkPathOnExpire: false);
        }
        if (burstCenter is Vector2 center)
        {
            MagicMissileHitDust.Emit(center, angle, burst: true);
        }
        else
        {
            MagicMissileHitDust.Emit(end, angle);
        }

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
                // 花印爆发特效由飞弹的 OnKill 播放
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
