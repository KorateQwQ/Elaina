using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace 伊蕾娜.ElainaModSkills.UICore;

// FNA SpriteBatch state, shared by the live UI and the standalone renderer.
internal static class UIBatch
{
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "transformMatrix")]
    internal static extern ref Matrix Transform(SpriteBatch batch);
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "sortMode")]
    private static extern ref SpriteSortMode Sort(SpriteBatch batch);
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "blendState")]
    private static extern ref BlendState Blend(SpriteBatch batch);
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "samplerState")]
    internal static extern ref SamplerState Sampler(SpriteBatch batch);
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "depthStencilState")]
    private static extern ref DepthStencilState Depth(SpriteBatch batch);
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "rasterizerState")]
    private static extern ref RasterizerState Rasterizer(SpriteBatch batch);
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "customEffect")]
    private static extern ref Effect Effect(SpriteBatch batch);

    internal static float Scale(SpriteBatch batch) => new Vector2(Transform(batch).M11, Transform(batch).M12).Length();
    internal readonly record struct State(SpriteSortMode Sort, BlendState Blend, SamplerState Sampler,
        DepthStencilState Depth, RasterizerState Rasterizer, Effect Effect, Matrix Transform, Rectangle Scissor)
    {
        internal void Begin(SpriteBatch batch, Effect effect = null, bool replaceEffect = false, SamplerState sampler = null)
        {
            batch.GraphicsDevice.ScissorRectangle = Scissor;
            batch.Begin(replaceEffect ? SpriteSortMode.Immediate : Sort, Blend, sampler ?? Sampler, Depth, Rasterizer,
                replaceEffect ? effect : Effect, Transform);
        }
    }
    internal static State Capture(SpriteBatch batch) => new(Sort(batch), Blend(batch), Sampler(batch), Depth(batch),
        Rasterizer(batch), Effect(batch), Transform(batch), batch.GraphicsDevice.ScissorRectangle);
}
