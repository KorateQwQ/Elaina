using System;
using KL.Drawing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent.UI.ResourceSets;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaAttribute;

public class ElainaPlayerResourceDisplaySet : ModResourceDisplaySet
{
    private readonly ResourceBarState life = new(false, 41);
    private readonly ResourceBarState mana = new(true, 53);
    private ResourceBarDrawing drawing;
    private PlayerStatsSnapshot snapshot;
    private Player owner;
    private bool uniqueMana;
    private ulong lastTick;
    private float seconds, manaValue, manaMaximum;

    public override void PreDrawResources(PlayerStatsSnapshot snapshot)
    {
        this.snapshot = snapshot;
        if (Main.dedServ) return;
        drawing ??= new ResourceBarDrawing(
            name => ModContent.Request<Texture2D>("伊蕾娜/ElainaAttribute/ResourceBarAssets/" + name, AssetRequestMode.ImmediateLoad).Value,
            ModContent.Request<Effect>("伊蕾娜/Effects/Content/ResourceBarClip", AssetRequestMode.ImmediateLoad).Value);
        var player = Main.LocalPlayer;
        var attributes = player.GetModPlayer<ElainaAttributeModPlayer>();
        bool currentUnique = attributes.UniqueMagicEnabled;
        manaValue = currentUnique ? attributes.MagicPoint : snapshot.Mana;
        manaMaximum = currentUnique ? attributes.MaxMagicPoint : snapshot.ManaMax;
        ulong tick = Main.GameUpdateCount;
        bool reset = owner != player || tick < lastTick || tick - lastTick > 60;
        if (reset)
        {
            life.Reset(snapshot.Life, snapshot.LifeMax);
            mana.Reset(manaValue, manaMaximum);
            owner = player;
            seconds = 0;
        }
        else if (currentUnique != uniqueMana) mana.Reset(manaValue, manaMaximum);
        uniqueMana = currentUnique;
        float dt = reset || Main.gamePaused ? 0 : Math.Min(.1f, (tick - lastTick) / 60f);
        lastTick = tick;
        if (dt > 0)
        {
            seconds += dt;
            life.Update(snapshot.Life, snapshot.LifeMax, dt, seconds);
            mana.Update(manaValue, manaMaximum, dt, seconds + .7f);
        }
    }

    public override void DrawLife(SpriteBatch spriteBatch)
    {
        if (drawing == null) return;
        // The Resource Bars layer has already transformed screen dimensions and mouse input to UI units.
        var origin = ResourceBarDrawing.LifePosition(Main.screenWidth);
        Draw(spriteBatch, origin, false, life, seconds);
        Hover(spriteBatch, origin, ResourceBarDrawing.LifeHeight, $"HP: {snapshot.Life:0.#}/{snapshot.LifeMax:0.#}");
    }

    public override void DrawMana(SpriteBatch spriteBatch)
    {
        if (drawing == null) return;
        var origin = ResourceBarDrawing.ManaPosition(Main.screenWidth);
        Draw(spriteBatch, origin, true, mana, seconds + .7f);
        float recovery = uniqueMana
            ? Main.LocalPlayer.GetModPlayer<ElainaAttributeModPlayer>().GetMagicPointRecovery()
            : Main.LocalPlayer.GetModPlayer<ElainaMagicPointModPlayer>().NaturalManaRegenPerSecond;
        Hover(spriteBatch, origin, ResourceBarDrawing.ManaHeight, $"MP: {manaValue:0.#}/{manaMaximum:0.#}   + {recovery:0.#}/s");
    }

    private void Draw(SpriteBatch batch, Vector2 origin, bool isMana, ResourceBarState state, float time)
    {
        batch.End();
        var device = batch.GraphicsDevice;
        var blend = device.BlendState;
        var sampler = device.SamplerStates[0];
        var depth = device.DepthStencilState;
        var rasterizer = device.RasterizerState;
        var scissor = device.ScissorRectangle;
        var viewport = device.Viewport;
        try { drawing.Draw(batch, Main.UIScaleMatrix, origin, isMana, time, state); }
        finally
        {
            device.Viewport = viewport;
            device.ScissorRectangle = scissor;
            // Restore the vanilla Resource Bars batch contract for overlays and the mana callback.
            batch.Begin(SpriteSortMode.Deferred, blend, sampler, depth, rasterizer, null, Main.UIScaleMatrix);
        }
    }

    private static void Hover(SpriteBatch batch, Vector2 origin, float height, string text)
    {
        var area = new Rectangle((int)origin.X - 2, (int)origin.Y - 3, (int)ResourceBarDrawing.Length + 4, (int)height + 6);
        if (!area.Contains(Main.MouseScreen.ToPoint())) return;
        var font = FontManager.HarmonyOS_Sans_SC.Value;
        const float scale = .25f;
        var size = font.MeasureString(text) * scale;
        var position = new Vector2(Math.Clamp(origin.X, 4, Math.Max(4, Main.screenWidth - size.X - 4)), origin.Y + height + 5);
        batch.DrawString(font, text, position + Vector2.One, Color.Black * .8f, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
        batch.DrawString(font, text, position, Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
    }

    // Hover text above supports the custom magic pool; suppress vanilla's ordinary mana tooltip.
    public override bool PreHover(out bool hoveringLife) { hoveringLife = false; return false; }

    public override void Unload()
    {
        // ModContent owns textures and effects; never dispose those shared assets here.
        drawing = null;
        owner = null;
        life.Particles.Clear();
        mana.Particles.Clear();
    }
}
