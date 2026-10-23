using System;
using KL.Drawing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent.UI.ResourceSets;
using Terraria.ModLoader;
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
            portraitDrawing.Draw(batch, ui, centre, portrait, life.Value, seconds);
        });
        Hover(spriteBatch, origin, ResourceBarDrawing.LifeHeight, $"HP: {snapshot.Life:0.#}/{snapshot.LifeMax:0.#}");
        if (PortraitHudDrawing.Contains(centre, Main.MouseScreen))
            Tooltip(spriteBatch, new Vector2(centre.X - PortraitHudDrawing.Outer, centre.Y + PortraitHudDrawing.Outer + 15),
                $"Lv {level}/{levelCap}   EXP {experience}/{experienceNeed}");
    }

    public override void DrawMana(SpriteBatch spriteBatch)
    {
        if (drawing == null) return;
        var origin = ResourceBarDrawing.ManaPosition(Main.screenWidth);
        Draw(spriteBatch, (batch, ui) => drawing.Draw(batch, ui, origin, true, seconds + .7f, mana));
        float recovery = uniqueMana
            ? Main.LocalPlayer.GetModPlayer<ElainaAttributeModPlayer>().GetMagicPointRecovery()
            : Main.LocalPlayer.GetModPlayer<ElainaMagicPointModPlayer>().NaturalManaRegenPerSecond;
        Hover(spriteBatch, origin, ResourceBarDrawing.ManaHeight, $"MP: {manaValue:0.#}/{manaMaximum:0.#}   + {recovery:0.#}/s");
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

    private static void Hover(SpriteBatch batch, Vector2 origin, float height, string text)
    {
        var area = new Rectangle((int)origin.X - 2, (int)origin.Y - 3, (int)ResourceBarDrawing.Length + 4, (int)height + 6);
        if (area.Contains(Main.MouseScreen.ToPoint())) Tooltip(batch, new Vector2(origin.X, origin.Y + height + 5), text);
    }

    private static void Tooltip(SpriteBatch batch, Vector2 anchor, string text)
    {
        var font = FontManager.HarmonyOS_Sans_SC.Value;
        const float scale = .25f;
        var size = font.MeasureString(text) * scale;
        var position = new Vector2(Math.Clamp(anchor.X, 4, Math.Max(4, Main.screenWidth - size.X - 4)), anchor.Y);
        batch.DrawString(font, text, position + Vector2.One, Color.Black * .8f, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
        batch.DrawString(font, text, position, Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
    }

    // Hover text above supports the custom magic pool; suppress vanilla's ordinary mana tooltip.
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
