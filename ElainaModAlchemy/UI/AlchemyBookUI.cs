using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SilkyUIFramework;
using SilkyUIFramework.Extensions;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationSkillPanel;

namespace 伊蕾娜.ElainaModAlchemy.UI;

public sealed partial class AlchemyNotebookPanel
{
    private readonly ConstellationBookMotion _book = new();
    private readonly ConstellationUIClock _bookClock = new();

    private void RequestBook(bool open)
    {
        if (_book.TargetOpen == open) return;
        _book.Request(open);
        _bookClock.Restart();
        StopDragging();
    }

    public override void HandleDraw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (_owner == null || _draw == null) return;
        var device = spriteBatch.GraphicsDevice;
        var matrix = SilkyUI.TransformMatrix;
        var viewport = device.Viewport;
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
            var resources = ModContent.GetInstance<AlchemyBookResources>();
            var front = resources.Cover(device, spriteBatch, pool, _draw);
            device.SetRenderTarget(page);
            device.Clear(Color.Transparent);
            device.ScissorRectangle = new Rectangle(0, 0, page.Width, page.Height);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
                DepthStencilState.None, global::SilkyUIFramework.SilkyUI.ScissorRasterizerState, null, matrix);
            pageBatchActive = true;
            // Native SUI keeps its original layout and clipping throughout the capture.
            base.DrawBodyCore(gameTime, spriteBatch);
            spriteBatch.End();
            pageBatchActive = false;
            device.RestoreRenderTargets(targets);
            device.Viewport = viewport;
            device.ScissorRectangle = scissor;
            Vector2 origin = Vector2.Transform(Bounds.Position, matrix);
            float scale = Vector2.TransformNormal(new Vector2(_scale, 0), matrix).Length();
            var dock = AlchemyBookLayout.DockBounds();
            Vector2 dockCenter = Vector2.Transform(new Vector2(dock.Center.X, dock.Center.Y), matrix);
            Vector2 dockSize = Vector2.TransformNormal(new Vector2(dock.Width, dock.Height), matrix);
            resources.Renderer(device).Draw(device, page, front, _draw.Texture(AlchemyBookArt.BackPath),
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
            pool.Return(page);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, global::SilkyUIFramework.SilkyUI.ScissorRasterizerState, null, matrix);
        }
    }
}

public sealed class AlchemyBookResources : ModSystem
{
    private ConstellationBookRenderer _renderer;
    private RenderTarget2D _cover;
    private RenderTargetPool _pool;
    internal ConstellationBookRenderer Renderer(GraphicsDevice device) => _renderer ??= new(device);

    internal Texture2D Cover(GraphicsDevice device, SpriteBatch batch, RenderTargetPool pool, AlchemyDrawing drawing)
    {
        if (_cover is { IsDisposed: false }) return _cover;
        _pool = pool;
        _cover = pool.Rent(1100, 800);
        device.SetRenderTarget(_cover);
        device.Clear(Color.Transparent);
        var previousBatch = drawing.Batch;
        var origin = drawing.Origin;
        float scale = drawing.Scale;
        drawing.Batch = batch;
        drawing.Origin = Vector2.Zero;
        drawing.Scale = 1;
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone);
        bool painted = false;
        try { AlchemyBookArt.PaintCover(drawing); painted = true; }
        finally
        {
            batch.End();
            drawing.Batch = previousBatch;
            drawing.Origin = origin;
            drawing.Scale = scale;
            if (!painted) { pool.Return(_cover); _cover = null; }
        }
        return _cover;
    }

    public override void Unload()
    {
        var renderer = _renderer;
        var cover = _cover;
        var pool = _pool;
        _renderer = null;
        _cover = null;
        _pool = null;
        if (renderer == null && cover == null) return;
        Main.QueueMainThreadAction(() =>
        {
            renderer?.Dispose();
            if (cover is { IsDisposed: false }) pool.Return(cover);
        });
    }
}
