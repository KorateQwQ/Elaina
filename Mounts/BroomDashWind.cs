using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace 伊蕾娜.Mounts;

/// <summary>纯视觉弹幕：只在成功冲刺时由拥有者生成一次，使用原版弹幕同步。
/// ai[0] 冲刺方向角，ai[1] 冲刺速度，ai[2] 冲刺 tick 数。</summary>
public sealed class BroomDashWind : ModProjectile
{
    public override string Texture => "伊蕾娜/Mounts/Assets/BroomDashWind";
    // 弹幕停在起点，拖尾却跟随人物到冲刺终点，剔除边距需覆盖整段冲刺。
    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1600;

    // 骑乘中心的逐 tick 采样，最新点在 [0]；仅客户端本地记录，不参与同步。
    private readonly Vector2[] trail = new Vector2[BroomDashWindRenderer.TrailSamples + 1];
    private int trailCount;
    private bool trailFrozen;

    private int DashTicks => (int)Projectile.ai[2];

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.aiStyle = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.hide = true;
        // 实际寿命由 AI 按冲刺时长结束，这里只是上限。
        Projectile.timeLeft = 120;
    }

    public override bool? CanDamage() => false;
    public override bool ShouldUpdatePosition() => false;

    public override void AI()
    {
        if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers
            || !Main.player[Projectile.owner].active || Main.player[Projectile.owner].dead
            || !BroomAccessSystem.CanRide(Main.player[Projectile.owner])
            || Projectile.localAI[0] >= BroomDashWindRenderer.Lifetime(DashTicks))
        {
            Projectile.Kill();
            return;
        }
        if (!Main.dedServ) SampleTrail(Main.player[Projectile.owner]);
        Projectile.localAI[0]++;
    }

    // 冲刺时速度锁定在冲刺方向，路径必为直线；冲刺结束（或撞墙、远端延迟导致方向已变）后不再追加新点，
    // 拖尾留在这条直线上向终点收拢，不随转向弯折。
    private void SampleTrail(Player owner)
    {
        if (Projectile.localAI[0] == 0) PushTrail(Projectile.Center);
        if (trailFrozen) return;
        Vector2 direction = Projectile.ai[0].ToRotationVector2();
        if (Projectile.localAI[0] + 1 >= DashTicks || Vector2.Dot(owner.velocity, direction) < Projectile.ai[1] * .8f)
        {
            trailFrozen = true;
            return;
        }
        PushTrail(owner.MountedCenter);
    }

    private void PushTrail(Vector2 point)
    {
        int count = Math.Min(trailCount, trail.Length - 1);
        for (int i = count; i > 0; i--) trail[i] = trail[i - 1];
        trail[0] = point;
        trailCount = count + 1;
    }

    // 双环与拖尾都在人物身后，画在玩家层之下，气流带从身体后方露出。
    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
        List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) => behindProjectiles.Add(index);

    public override bool PreDraw(ref Color lightColor)
    {
        if (Main.dedServ) return false;
        Main.spriteBatch.End();
        try
        {
            BroomDashVisualSystem.Draw(Projectile.Center, Projectile.ai[0].ToRotationVector2(),
                Projectile.localAI[0], Projectile.ai[1], trail.AsSpan(0, trailCount), DashTicks);
        }
        finally
        {
            // BasicEffect 修改过设备 shader，先立即应用原版 sprite shader，再恢复延迟批次。
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
        }
        return false;
    }
}

public sealed class BroomDashVisualSystem : ModSystem
{
    private static BroomDashWindRenderer renderer;
    private static Asset<Texture2D> wind;

    internal static void Draw(Vector2 origin, Vector2 direction, float age, float speed,
        ReadOnlySpan<Vector2> trail, int dashTicks)
    {
        var device = Main.instance.GraphicsDevice;
        renderer ??= new BroomDashWindRenderer(device);
        wind ??= ModContent.Request<Texture2D>("伊蕾娜/Mounts/Assets/BroomDashWind", AssetRequestMode.ImmediateLoad);
        Matrix view = Matrix.CreateTranslation(-Main.screenPosition.X, -Main.screenPosition.Y, 0)
            * Main.GameViewMatrix.TransformationMatrix;
        renderer.DrawWake(device, wind.Value, origin, direction, age, speed, view);
        renderer.DrawTrail(device, wind.Value, trail, age, dashTicks, view);
    }

    public override void Unload()
    {
        var old = renderer;
        renderer = null;
        wind = null;
        if (old != null) Main.QueueMainThreadAction(old.Dispose);
    }
}
