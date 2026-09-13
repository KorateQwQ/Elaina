using System;
using KL.Drawing;
using KL.Extensions;
using KL.Utils;
using Terraria.GameContent;
using Terraria.ID;

namespace 伊蕾娜.ElainaModSkills.Skills.Heal;

public class HealProj : ElainaBasicProjectile
{
    private readonly List<VisualUnit> rainParticles = new();
    private Vector2 lastParticleAnchor;
    private static Texture2D rainStreakTexture;
    private static Texture2D rainGlowTexture;

    private int RainSpawnInterval => 3;
    private int RainParticleLifeTime => 42;
    private int OrbitingGlowCount => 3;
    private float OrbitingGlowHeight => 92f;
    private float OrbitingGlowRadius => 48f;
    private float OrbitingGlowVerticalScale => 0.5f;
    private float OrbitingGlowScale => 0.26f;
    private float OrbitingGlowRotationSpeed => 0.045f;
    private float TransitionDuration => 60f;
    private float RainbowPhaseStart => 0.3f;
    private float RainbowRevealSoftness => 0.12f;

    public override void Load()
    {
        if (Main.netMode != NetmodeID.Server)
        {
            rainStreakTexture = ModContent.Request<Texture2D>(
                "KL/Effects/Tex/Line", AssetRequestMode.ImmediateLoad).Value;
            rainGlowTexture = ModContent.Request<Texture2D>(
                "KL/Effects/Tex/LightBloom", AssetRequestMode.ImmediateLoad).Value;
        }

        base.Load();
    }

    public override void Unload()
    {
        rainStreakTexture = null;
        rainGlowTexture = null;
    }

    public override void SetDefaults()
    {
        Projectile.hide = true;
        Projectile.timeLeft = 300;
        base.SetDefaults();
    }

    public override void OnSpawn_AllClient()
    {
        base.OnSpawn_AllClient();
    }

    public override void AI()
    {
        if (!TryGetTarget(out Player target))
        {
            Projectile.Kill();
            return;
        }

        Projectile.localAI[0] = MathF.Min(Projectile.localAI[0] + 1f, TransitionDuration);
        Vector2 currentAnchor = target.VisualCenter().Floor()+new Vector2(0,-target.gravDir*10);
        Vector2 anchorDelta = currentAnchor - lastParticleAnchor;
        Projectile.Center = currentAnchor;

        if (lastParticleAnchor != Vector2.Zero)
        {
            for (int i = 0; i < rainParticles.Count; i++)
            {
                rainParticles[i].Position3D += new Vector3(anchorDelta, 0f);
            }
        }

        lastParticleAnchor = currentAnchor;

        float transitionProgress = GetTransitionProgress();
        float rainbowProgress = GetRainbowProgress(transitionProgress);
        if (Main.netMode != NetmodeID.Server &&
            rainbowProgress > 0f && Projectile.timeLeft > TransitionDuration &&
            Main.GameUpdateCount % RainSpawnInterval == 0)
        {
            SpawnRainParticle();
        }

        VisualUnit.UpdateAll(rainParticles);
        base.AI();
    }

    private void SpawnRainParticle()
    {
        if (!TryGetTarget(out Player target))
        {
            return;
        }

        float spawnWidth = 18f;
        Vector2 spawnPosition = Projectile.Center + new Vector2(
            Main.rand.NextFloat(-spawnWidth, spawnWidth),
            -68f*target.gravDir + Main.rand.NextFloat(-8f, 12f));
        Vector2 velocity = new(
            Main.rand.NextFloat(-1.85f, 1.85f),
            Main.rand.NextFloat(1.6f, 2.8f)*target.gravDir);

        VisualUnit.Spawn(
            rainParticles,
            new RainbowRainParticle(
                spawnPosition,
                velocity,
                Main.rand.NextFloat(),
                0.3f+Main.rand.NextFloat(0.7f, 1.25f),
                RainParticleLifeTime+20 + Main.rand.Next(-5, 8)),
            Projectile);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        if (!TryGetTarget(out Player target))
        {
            return false;
        }

        Effect effect = AssetManager.GetEffect("伊蕾娜.Effects.Content.动态彩虹");
        Texture2D tex = AssetManager.GetTexture("伊蕾娜.ElainaModSkills.Skills.Heal.HealProj");
        Texture2D noise = AssetManager.GetTexture("KL.Effects.Tex.PerlinX");
        float transitionProgress = GetTransitionProgress();
        float glowProgress = MathHelper.SmoothStep(0f, 1f, transitionProgress);
        float rainbowProgress = GetRainbowProgress(transitionProgress);

        Vector2 scale = new Vector2(1, target.gravDir)*1.5f;
        EndBeginDraw(2,1);
        effect.SetValue("uTime",VisualTime%1200/60f);
        effect.SetValue("uColorInterval",0.3f);
        effect.SetValue("uBrightness",1.0f);
        effect.SetValue("baseColor",new Vector4(0.8f));
        
        effect.SetValue("uNoiseScale",new Vector2(0.3f, 0.3f));
        effect.SetValue("uNoiseSpeed",new Vector2(0.00f, -0.3f));
        effect.SetValue("uDistortionStrength",0.03f);
        effect.SetValue("uHeatVerticalStrength",0.2f);
        effect.SetValue("uRevealProgress", rainbowProgress);
        effect.SetValue("uRevealSoftness", RainbowRevealSoftness);
        effect.SetTexture(1, noise);
        effect.Apply();
        DrawInWorld(tex,Projectile.Center,color:new Color(255,255,255,255),scale);

        EndBeginDraw(0);
        DrawOrbitingHealGlows(target, glowProgress);

        EndBeginDraw();
        VisualUnit.DrawAll(rainParticles);
        //DrawInWorld(tex,Projectile.Center,color:new Color(255,255,255,155),scale*0.98f);

        return false;
    }

    private float GetTransitionProgress()
    {
        float revealProgress = MathHelper.Clamp(Projectile.localAI[0] / TransitionDuration, 0f, 1f);
        float fadeProgress = MathHelper.Clamp(Projectile.timeLeft / TransitionDuration, 0f, 1f);
        return MathF.Min(revealProgress, fadeProgress);
    }

    private float GetRainbowProgress(float transitionProgress)
    {
        float progress = MathHelper.Clamp(
            (transitionProgress - RainbowPhaseStart) / (1f - RainbowPhaseStart),
            0f,
            1f);
        return MathHelper.SmoothStep(0f, 1f, progress);
    }

    private void DrawOrbitingHealGlows(Player target, float glowProgress)
    {
        Texture2D glowTexture = AssetManager.GetTexture(
            "伊蕾娜.ElainaModSkills.Skills.Heal.HealGlow");
        Vector2 orbitCenter = Projectile.Center +
                              new Vector2(0f, -OrbitingGlowHeight * target.gravDir);
        float baseRotation = VisualTime * OrbitingGlowRotationSpeed;

        for (int i = 0; i < OrbitingGlowCount; i++)
        {
            float rotation = baseRotation + MathHelper.TwoPi * i / OrbitingGlowCount;
            Vector2 orbitOffset = new(
                MathF.Cos(rotation) * OrbitingGlowRadius * glowProgress,
                MathF.Sin(rotation) * OrbitingGlowRadius * OrbitingGlowVerticalScale *
                target.gravDir * glowProgress);

            float pulse = 1f + MathF.Sin(baseRotation * 2f + i * MathHelper.TwoPi / OrbitingGlowCount) * 0.08f;
            float animatedScale = OrbitingGlowScale * pulse * glowProgress;
            DrawInWorld(
                glowTexture,
                orbitCenter + orbitOffset,
                new Color(0,0,0,255) * glowProgress,
                new Vector2(animatedScale),
                0f);

            DrawInWorld(
                glowTexture,
                orbitCenter + orbitOffset,
                new Color(255,200,230,0)*0.7f * glowProgress,
                new Vector2(animatedScale),
                0f);
            DrawInWorld(
                glowTexture,
                orbitCenter + orbitOffset,
                new Color(255,160,230,0)*0.7f * glowProgress,
                new Vector2(animatedScale * 1.02f),
                0f);
        }
    }

    private bool TryGetTarget(out Player target)
    {
        int targetIndex = (int)Projectile.ai[0];
        if (targetIndex >= 0 && targetIndex < Main.maxPlayers &&
            Main.player[targetIndex] is { active: true, dead: false } candidate)
        {
            target = candidate;
            return true;
        }

        target = null;
        return false;
    }

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers,
        List<int> overWiresUI)
    {
        overPlayers.Add(index);
        base.DrawBehind(index, behindNPCsAndTiles, behindNPCs, behindProjectiles, overPlayers, overWiresUI);
    }

    public override void OnKill(int timeLeft)
    {
        VisualUnit.KillAll(rainParticles);
        base.OnKill(timeLeft);
    }

    private sealed class RainbowRainParticle : VisualUnit
    {
        private readonly float hue;
        private readonly float particleScale;
        private readonly float horizontalVelocity;
        private readonly float swayPhase;

        public RainbowRainParticle(
            Vector2 position,
            Vector2 velocity,
            float hue,
            float particleScale,
            int timeLeft)
            : base(position, velocity, 0, timeLeft)
        {
            this.hue = hue;
            this.particleScale = particleScale;
            horizontalVelocity = velocity.X;
            swayPhase = Main.rand.NextFloat(0f, MathHelper.TwoPi);
            Scale = particleScale;
        }

        public override void Update()
        {
            if (!Active)
            {
                return;
            }

            // Gravity makes each drop fall naturally; the sine term adds a soft
            // side-to-side shimmer instead of perfectly straight rain.
            Velocity3D.Y = MathHelper.Clamp(Velocity3D.Y + 0.045f, 0f, 4.2f);
            Velocity3D.X = horizontalVelocity + MathF.Sin((Timer + swayPhase) * 0.14f) * 0.35f;
            base.Update();

            if (!Active)
            {
                return;
            }

            float lifeProgress = MathHelper.Clamp(
                (Timer - Delay) / (float)Math.Max(TimeLeft, 1), 0f, 1f);
            float fadeIn = MathHelper.Clamp(Timer / 5f, 0f, 1f);
            float fadeOut = 1f - MathHelper.SmoothStep(0.65f, 1f, lifeProgress);
            Alpha = fadeIn * fadeOut;
        }

        public override void Draw()
        {
            if (rainStreakTexture == null || rainGlowTexture == null)
            {
                return;
            }

            float animatedHue = (hue + Main.GlobalTimeWrappedHourly * 0.08f + Timer * 0.004f) % 1f;
            Color particleColor = Main.hslToRgb(animatedHue, 0.95f, 0.68f) * (Alpha * 0.9f);
            float lifeProgress = MathHelper.Clamp(
                (Timer - Delay) / (float)Math.Max(TimeLeft, 1), 0f, 1f);
            float glowAlpha = 1f - lifeProgress;
            Color glowColor = Main.hslToRgb(animatedHue, 0.85f, 0.72f) * glowAlpha;
            glowColor.A = 0;
            float stretch = MathHelper.Clamp(Velocity2D.Length() * 1.8f, 2.5f, 7f);
            float streakLength = 0.04f + stretch * 0.005f;

            DrawInWorld(
                rainGlowTexture,
                Position2D,
                Color.Black*glowAlpha,
                new Vector2(0.12f * particleScale),
                0f);
            
            for (int i = 0; i < 3; i++)
            {
                DrawInWorld(
                    rainGlowTexture,
                    Position2D,
                    glowColor*(1-i*0.2f),
                    new Vector2(0.1f * particleScale),
                    0f);
            }
        }
    }
}
