using System;
using KL.Drawing;
using 伊蕾娜.ElainaModSkills.UICore;

namespace 伊蕾娜.ElainaModSkills.Skills.Wind;

public class WindTornado : ElainaBasicProjectile
{
    private const int Lifetime = 300;
    private const float ReleaseTime = Lifetime / 60f - WindVortexDrawing.CloseSeconds;
    private WindVortexDrawing renderer;
    private RenderTarget2D scene;
    private float age, rotation, distortion, textureTravel;

    public override void SetDefaults()
    {
        InfusionElement = new(KL.DamageSystem.ElementType.Wind, SkillHitBalance.WindTornadoBuildup);
        Projectile.timeLeft = Lifetime;
        base.SetDefaults();
    }

    public override void AI()
    {
        if (!Main.dedServ) {
            float next = (Lifetime - Projectile.timeLeft + 1) / 60f;
            float step = CoreTime(next) - CoreTime(age);
            rotation += step * WindVortexSettings.RotationSpeed;
            distortion += step * WindVortexSettings.DistortionFrequency;
            textureTravel += (next - age) * WindVortexSettings.TextureFlow;
            age = next;
        }
        base.AI();
    }

    private static float CoreTime(float t)
    {
        float end = Math.Clamp((t - ReleaseTime) / WindVortexDrawing.CloseSeconds, 0, 1);
        return t + end * end * .92f;
    }

    public override bool PreDraw(ref Color lightColor) => false;

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
        List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
    {
        const LayerDrawRequestSystem.DrawTargetLayer layer = LayerDrawRequestSystem.DrawTargetLayer.OverPlayers;
        const LayerDrawRequestSystem.DrawTiming timing = LayerDrawRequestSystem.DrawTiming.Before;
        LayerDrawRequestSystem.Request("Elaina:WindVortexScene", layer, timing, RememberScene);
        LayerDrawRequestSystem.RequestBloom("Elaina:WindVortex", layer, timing, DrawVortex);
        base.DrawBehind(index, behindNPCsAndTiles, behindNPCs, behindProjectiles, overPlayers, overWiresUI);
    }

    private void RememberScene()
    {
        var targets = Main.instance.GraphicsDevice.GetRenderTargets();
        scene = targets.Length == 1 ? targets[0].RenderTarget as RenderTarget2D : null;
    }

    private void DrawVortex()
    {
        var batch = Main.spriteBatch;
        var state = UIBatch.Capture(batch);
        batch.End();
        var device = batch.GraphicsDevice;
        var targets = device.GetRenderTargets();
        var background = targets.Length == 1 && targets[0].RenderTarget != scene ? scene : null;
        var center = Projectile.Center - Main.screenPosition;
        var transform = Matrix.CreateTranslation(center.X, center.Y, 0) * state.Transform;
        renderer ??= new WindVortexDrawing();
        renderer.Draw(device, WindVortexResources.Shader.Value, WindVortexResources.Blade.Value, WindVortexResources.Noise.Value,
            WindVortexResources.Haze.Value, background, transform, age, ReleaseTime, distortion, rotation, textureTravel, Projectile.identity * 31);
        // Release the sampled scene before KL binds it as its composite destination.
        for (int i = 0; i < 4; i++) if (device.Textures[i] == background) device.Textures[i] = null;
        state.Begin(batch);
    }
}
