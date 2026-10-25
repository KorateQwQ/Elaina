using System;
using System.Collections.Generic;
using KL.Drawing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaModAlchemy.Purification;

/// <summary>ai[0] 起始行，ai[1] 经过 tick，ai[2] 使用者。露滴归服务器所有。</summary>
public sealed class PurificationDewDrop : ModProjectile
{
    internal const float FallSpeed = 8;
    private int nextRow = -1;
    private int mapStartRow, mapRowCount;
    private Action bloomDraw;
    public override string Texture => "伊蕾娜/ElainaModAlchemy/item/ExampleAssets/PurificationDew_Pixel";
    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 320;
    public override void SetDefaults()
    {
        Projectile.width = 48; Projectile.height = 16;
        Projectile.aiStyle = -1; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.penetrate = -1; Projectile.timeLeft = 18000;
        Projectile.friendly = Projectile.hostile = false;
        Projectile.netImportant = true;
    }
    public override bool? CanDamage() => false;
    public override bool? CanCutTiles() => false;
    public override bool ShouldUpdatePosition() => false;

    public override void AI()
    {
        if (Main.netMode == NetmodeID.Server && Projectile.owner != Main.maxPlayers)
        { Projectile.Kill(); return; } // 客户端自行生成的同类型弹幕不能修改世界。
        int x = (int)(Projectile.Center.X / 16);
        int start = (int)Projectile.ai[0];
        if (x < 2 || x >= Main.maxTilesX - 2 || start < 1 || start >= Main.maxTilesY - 1)
        { if (Main.netMode != NetmodeID.MultiplayerClient) Projectile.Kill(); return; }
        float bottom = (Main.maxTilesY - 2) * 16 + 8;
        Projectile.Center = new Vector2(x * 16 + 8, Math.Min(start * 16 + 8 + Projectile.ai[1] * FallSpeed, bottom));
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            if (nextRow < 0) nextRow = start;
            int row = Math.Min((int)(Projectile.Center.Y / 16), Main.maxTilesY - 2);
            // 正常每两 tick 一行，每格只访问一次；异常位置跃迁也限制本 tick 的处理量。
            if (nextRow <= row)
            {
                PurifyRow(x, nextRow);
                if (mapRowCount == 0) mapStartRow = nextRow;
                mapRowCount++;
                nextRow++;
                if (Main.netMode == NetmodeID.SinglePlayer || mapRowCount >= PurificationDewMapSystem.RowsPerPacket)
                    FlushMapStrip();
            }
            if (nextRow >= Main.maxTilesY - 1) { Projectile.Kill(); return; }
            if ((int)Projectile.ai[1] % 60 == 0) Projectile.netUpdate = true;
        }
        Projectile.ai[1]++;
        Projectile.timeLeft = 18000; // 生命由到达世界底部控制，晚加入不从地表重新净化。
        Lighting.AddLight(Projectile.Center,new Vector3(1)*2f);

    }

    public override void OnKill(int timeLeft) => FlushMapStrip();

    private void FlushMapStrip()
    {
        if (mapRowCount == 0) return;
        PurificationProtectionSystem.SyncStrip((int)(Projectile.Center.X / 16), mapStartRow, mapRowCount);
        PurificationDewMapSystem.RevealStrip((int)Projectile.ai[2], (int)(Projectile.Center.X / 16), mapStartRow, mapRowCount);
        mapRowCount = 0;
    }

    internal static void PurifyRow(int centerX, int row)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        PurificationProtectionSystem.ProtectRow(centerX, row);
        for (int x = centerX - 1; x <= centerX + 1; x++)
            if (WorldGen.InWorld(x, row, 1))
            {
                PurificationProtectionSystem.CleanTile(x, row);
                // size=0 必须逐格调用，size=1 会把上下行也纳入；不额外发送重复地形包。
                WorldGen.Convert(x, row, BiomeConversionID.Purity, 0, tiles: true, walls: false);
            }
    }

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
        List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
    {
        if (Main.dedServ || Projectile.ai[1] <= 0
            || !PurificationDewRenderer.OnScreen(Projectile.Center - Main.screenPosition,
                Main.GameViewMatrix.TransformationMatrix, Main.graphics.GraphicsDevice.Viewport)
            || PurificationDewVisualSystem.GetRenderer() == null) return;
        // 与魔力飞弹共享同层、同时机的一次 Bloom，不为每道露滴创建全屏后处理。
        LayerDrawRequestSystem.RequestBloom("Elaina:PurificationDew", LayerDrawRequestSystem.DrawTargetLayer.Projectiles,
            LayerDrawRequestSystem.DrawTiming.After, bloomDraw ??= DrawBloom);
    }

    private void DrawBloom()
    {
        if (!Projectile.active) return;
        PurificationDewVisualSystem.GetRenderer()?.Draw(Main.spriteBatch,
            Projectile.Center - Main.screenPosition, Projectile.ai[1], Main.GameViewMatrix.TransformationMatrix);
        DrawHelper.EndBeginDraw();
    }

    public override bool PreDraw(ref Color lightColor)
    {
        if (PurificationDewVisualSystem.GetRenderer() == null)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Color.White,
                0, texture.Size() * .5f, 1, SpriteEffects.None);
        }
        return false;
    }
}
