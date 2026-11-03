using System;
using KL.Drawing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.UI.ResourceSets;
using Terraria.ModLoader;
using 伊蕾娜.Config;
using 伊蕾娜.System;

namespace 伊蕾娜.ElainaAttribute;

public class ElainaPlayerResourceDisplaySet : ModResourceDisplaySet
{
    private readonly ResourceBarState life = new(false, 41);
    private readonly ResourceBarState mana = new(true, 53);
    private readonly PortraitHudState portrait = new();
    private ResourceBarDrawing drawing;
    private PortraitHudDrawing portraitDrawing;
    private PlayerStatsSnapshot snapshot;
    private Player owner;
    private bool uniqueMana;
    private ulong lastTick;
    private float seconds, manaValue, manaMaximum;
    private int level, experience, experienceNeed, levelCap;

    public override void PreDrawResources(PlayerStatsSnapshot snapshot)
    {
        this.snapshot = snapshot;
        if (Main.dedServ) return;
        static Texture2D Texture(string name) =>
            ModContent.Request<Texture2D>("伊蕾娜/ElainaAttribute/ResourceBarAssets/" + name, AssetRequestMode.ImmediateLoad).Value;
        drawing ??= new ResourceBarDrawing(Texture,
            ModContent.Request<Effect>("伊蕾娜/Effects/Content/ResourceBarClip", AssetRequestMode.ImmediateLoad).Value);
        portraitDrawing ??= new PortraitHudDrawing(Texture, FontManager.Gelasio.Value);
        var player = Main.LocalPlayer;
        var progress = player.GetModPlayer<ElainaStatePlayer>();
        level = progress.GetLevel();
        experience = progress.GetExperience();
        experienceNeed = progress.GetExperienceToNextLevel();
        levelCap = progress.GetMaxLevel();
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
            portrait.Reset(level, experience, experienceNeed, levelCap);
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
            portrait.Update(level, experience, experienceNeed, levelCap, dt);
        }
    }

    public override void DrawLife(SpriteBatch spriteBatch)
    {
        if (drawing == null) return;
        // The Resource Bars layer has already transformed screen dimensions and mouse input to UI units.
        var origin = ResourceBarDrawing.LifePosition(Main.screenWidth);
        var centre = PortraitHudDrawing.Position(Main.screenWidth);
        Draw(spriteBatch, (batch, ui) =>
        {
            drawing.Draw(batch, ui, origin, false, seconds, life);
            Value(batch, ui, origin, false, snapshot.Life, snapshot.LifeMax);
            portraitDrawing.Draw(batch, ui, centre, portrait, life.Value, seconds);
        });
    }

    public override void DrawMana(SpriteBatch spriteBatch)
    {
        if (drawing == null) return;
        var origin = ResourceBarDrawing.ManaPosition(Main.screenWidth);
        Draw(spriteBatch, (batch, ui) =>
        {
            drawing.Draw(batch, ui, origin, true, seconds + .7f, mana);
            Value(batch, ui, origin, true, manaValue, manaMaximum);
        });
    }

    private static void Value(SpriteBatch batch, Matrix ui, Vector2 origin, bool isMana, float value, float max)
    {
        if (!ModContent.GetInstance<BasicConfig>().ShowResourceValues) return;
        ResourceBarDrawing.DrawValue(batch, ui, origin, isMana, FontManager.NotoSerifSC.Value, $"{value:0}/{max:0}");
    }

    private static void Draw(SpriteBatch batch, Action<SpriteBatch, Matrix> draw)
    {
        batch.End();
        var device = batch.GraphicsDevice;
        var blend = device.BlendState;
        var sampler = device.SamplerStates[0];
        var depth = device.DepthStencilState;
        var rasterizer = device.RasterizerState;
        var scissor = device.ScissorRectangle;
        var viewport = device.Viewport;
        try { draw(batch, Main.UIScaleMatrix); }
        finally
        {
            device.Viewport = viewport;
            device.ScissorRectangle = scissor;
            // Restore the vanilla Resource Bars batch contract for overlays and the mana callback.
            batch.Begin(SpriteSortMode.Deferred, blend, sampler, depth, rasterizer, null, Main.UIScaleMatrix);
        }
    }

    // The bars have no hover readout; suppress vanilla's resource tooltip as well.
    public override bool PreHover(out bool hoveringLife) { hoveringLife = false; return false; }

    public override void Unload()
    {
        // ModContent owns textures and effects; never dispose those shared assets here.
        drawing = null;
        portraitDrawing = null;
        owner = null;
        life.Particles.Clear();
        mana.Particles.Clear();
        portrait.Sparks.Clear();
    }
}
