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

    private const int RainSpawnInterval = 3;
    private const int RainParticleLifeTime = 42;

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
        base.SetDefaults();
    }

    public override void AI()
    {
        Vector2 currentAnchor = Owner.VisualCenter().Floor();
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

        if (Main.netMode != NetmodeID.Server &&
            Main.GameUpdateCount % RainSpawnInterval == 0)
        {
            SpawnRainParticle();
        }

        VisualUnit.UpdateAll(rainParticles);
        base.AI();
    }

    private void SpawnRainParticle()
    {
        float spawnWidth = 28f;
        Vector2 spawnPosition = Projectile.Center + new Vector2(
            Main.rand.NextFloat(-spawnWidth, spawnWidth),
            -68f + Main.rand.NextFloat(-8f, 12f));
        Vector2 velocity = new(
            Main.rand.NextFloat(-1.85f, 1.85f),
            Main.rand.NextFloat(1.6f, 2.8f));

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
        Effect effect = AssetManager.GetEffect("伊蕾娜.Effects.Content.动态彩虹");
        Texture2D tex = AssetManager.GetTexture("伊蕾娜.ElainaModSkills.Skills.Heal.HealProj");
        Texture2D noise = AssetManager.GetTexture("KL.Effects.Tex.PerlinX");

        Vector2 scale = new Vector2(1, 1)*1.5f;
        EndBeginDraw(2);

        DrawInWorld(tex,Projectile.Center,color:Color.Black*0.3f,scale:scale*0.97f);
        
        EndBeginDraw(1,1);
        effect.SetValue("uTime",VisualTime%1200/60f);
        effect.SetValue("uColorInterval",0.3f);
        effect.SetValue("uBrightness",1.0f);
        effect.SetValue("baseColor",new Vector4(0.5f));
        
        effect.SetValue("uNoiseScale",new Vector2(0.3f, 0.3f));
        effect.SetValue("uNoiseSpeed",new Vector2(0.00f, -0.3f));
        effect.SetValue("uDistortionStrength",0.03f);
        effect.SetValue("uHeatVerticalStrength",0.2f);
        
        
        effect.SetTexture(1,noise);

        effect.Apply();
        DrawInWorld(tex,Projectile.Center,color:new Color(255,255,255,100),scale);
        
        //EndBeginDraw(1);
        VisualUnit.DrawAll(rainParticles);
        DrawInWorld(tex,Projectile.Center,color:new Color(255,255,255,155),scale*0.98f);

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
            float stretch = MathHelper.Clamp(Velocity2D.Length() * 1.8f, 2.5f, 7f);
            float streakLength = 0.04f + stretch * 0.005f;

            DrawInWorld(
                rainGlowTexture,
                Position2D,
                Color.Black,
                new Vector2(0.1f * particleScale),
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