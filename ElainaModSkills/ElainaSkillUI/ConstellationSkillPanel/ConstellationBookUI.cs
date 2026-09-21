using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SilkyUIFramework;
using SilkyUIFramework.Elements;
using SilkyUIFramework.Extensions;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationSkillPanel;

public sealed partial class ConstellationSkillPanel
{
    private readonly ConstellationBookMotion _book = new();
    private readonly ConstellationUIClock _bookClock = new();

    private void RequestBook(bool open)
    {
        if (_book.TargetOpen == open) return;
        _book.Request(open);
        // Reverse from the last displayed pose, without charging the preceding interval to the new direction.
        _bookClock.Restart();
    }

    internal static bool BookMoving => TryPanel(out var panel) && panel.Enabled && panel._book.Moving;
    internal static float BookDockGlow => TryPanel(out var panel) && panel.Enabled ? panel._book.DockGlow : 0;
    private static bool TryPanel(out ConstellationSkillPanel panel)
    {
        panel = null;
        return !Main.dedServ && !Main.gameMenu && SilkyUISystem.ServiceProvider != null
            && SilkyUIRenderSystem.Instance.TryGetInstance(out panel);
    }

    // Keep the root hittable during flight so transformed content cannot leak input to the game.
    public override UIView GetElementAt(Vector2 mousePosition) => _book.Moving ? this : base.GetElementAt(mousePosition);

    public override void HandleDraw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (_state == null) return;
        var device = spriteBatch.GraphicsDevice;
        var matrix = SilkyUI.TransformMatrix;
        var viewport = device.Viewport;
        // The backdrop belongs to the screen, not to the moving book.
        spriteBatch.Draw(TextureAssets.MagicPixel.Value,
            new Rectangle(0, 0, (int)Math.Ceiling(viewport.Width / Main.UIScale), (int)Math.Ceiling(viewport.Height / Main.UIScale)),
            new Color(8, 10, 18) * (.72f * _book.BackdropAlpha));
        if (!_book.Moving) { base.HandleDraw(gameTime, spriteBatch); return; }

        var pool = (RenderTargetPool)SilkyUISystem.ServiceProvider.GetService(typeof(RenderTargetPool));
        var page = pool.Rent(viewport.Width, viewport.Height);
        spriteBatch.End();
        var targets = device.GetRenderTargets();
        var scissor = device.ScissorRectangle;
        var blend = device.BlendState;
        var depth = device.DepthStencilState;
        var rasterizer = device.RasterizerState;
        var sampler = device.SamplerStates[0];
        bool pageBatchActive = false;
        try
        {
            var resources = ModContent.GetInstance<ConstellationBookResources>();
            var front = resources.Cover(device, spriteBatch, pool, _draw);
            device.SetRenderTarget(page);
            device.Clear(Color.Transparent);
            device.ScissorRectangle = new Rectangle(0, 0, page.Width, page.Height);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
                DepthStencilState.None, global::SilkyUIFramework.SilkyUI.ScissorRasterizerState, null, matrix);
            pageBatchActive = true;
            // Drawing and native child clipping use their original, untransformed SUI coordinates.
            base.DrawBodyCore(gameTime, spriteBatch);
            spriteBatch.End();
            pageBatchActive = false;
            device.RestoreRenderTargets(targets);
            device.Viewport = viewport;
            device.ScissorRectangle = scissor;
            Vector2 origin = Vector2.Transform(Bounds.Position + _origin, matrix);
            float scale = Vector2.TransformNormal(new Vector2(_scale, 0), matrix).Length();
            var dock = ConstellationBookButton.DockBounds();
            Vector2 dockCenter = Vector2.Transform(new Vector2(dock.Center.X, dock.Center.Y), matrix);
            Vector2 dockSize = Vector2.TransformNormal(new Vector2(dock.Width, dock.Height), matrix);
            resources.Renderer(device).Draw(device, page, front, _draw.Texture("BookCoverBack"),
                TextureAssets.MagicPixel.Value, _book, origin, scale, dockCenter, dockSize);
        }
        finally
        {
            if (pageBatchActive) spriteBatch.End();
            device.RestoreRenderTargets(targets);
            device.Viewport = viewport;
            device.ScissorRectangle = scissor;
            device.BlendState = blend;
            device.DepthStencilState = depth;
            device.RasterizerState = rasterizer;
            device.SamplerStates[0] = sampler;
            // Flush every use before returning this target; SUI owns its eventual disposal.
            pool.Return(page);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, global::SilkyUIFramework.SilkyUI.ScissorRasterizerState, null, matrix);
        }
    }
}

// These resources span worlds. Only the custom BasicEffect is owned here; the cover is a pool lease.
public sealed class ConstellationBookResources : ModSystem
{
    private ConstellationBookRenderer _renderer;
    private RenderTarget2D _cover;
    private RenderTargetPool _pool;
    internal ConstellationBookRenderer Renderer(GraphicsDevice device) => _renderer ??= new(device);
    internal Texture2D Cover(GraphicsDevice device, SpriteBatch batch, RenderTargetPool pool, ConstellationDrawing drawing)
    {
        if (_cover is { IsDisposed: false }) return _cover;
        _pool = pool;
        _cover = pool.Rent(1100, 800);
        device.SetRenderTarget(_cover);
        device.Clear(Color.Transparent);
        var previousBatch = drawing.Batch; var origin = drawing.Origin; float scale = drawing.Scale;
        drawing.Batch = batch; drawing.Origin = Vector2.Zero; drawing.Scale = 1;
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone);
        bool painted = false;
        try
        {
            ConstellationBookRenderer.PaintCover(drawing);
            painted = true;
        }
        finally
        {
            batch.End();
            drawing.Batch = previousBatch; drawing.Origin = origin; drawing.Scale = scale;
            if (!painted) { pool.Return(_cover); _cover = null; }
        }
        return _cover;
    }

    public override void Unload()
    {
        var renderer = _renderer; var cover = _cover; var pool = _pool;
        _renderer = null; _cover = null; _pool = null;
        if (renderer == null && cover == null) return;
        Main.QueueMainThreadAction(() =>
        {
            renderer?.Dispose();
            if (cover is { IsDisposed: false }) pool.Return(cover);
        });
    }
}
