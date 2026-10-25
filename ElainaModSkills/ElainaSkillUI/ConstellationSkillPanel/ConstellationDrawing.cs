using static 伊蕾娜.Localization.UIText;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using KL.Drawing;
using KL.Drawing.Snippets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using SilkyUIFramework;
using SilkyUIFramework.Graphics2D;
using Terraria.GameContent;
using Terraria;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationSkillPanel;

// Coordinates here are design pixels. The same scale/offset is used for SUI hit regions.
// Original and uncolored artwork are ModContent-owned; no runtime texture copies are created.
internal sealed class ConstellationDrawing
{
    internal const string Root = "伊蕾娜/ElainaModSkills/ElainaSkillUI/ConstellationPreview/Assets/";
    internal static readonly Color Ink = new(238, 233, 245);
    internal static readonly Color Muted = new(178, 168, 195);
    internal static readonly Color Lavender = new(202, 178, 237);
    internal static readonly Color Gold = new(230, 206, 165);
    internal static readonly Color LineColor = new(76, 62, 88);
    internal static readonly Color DetailText = new(201, 187, 211); // .description
    internal static readonly Color DetailLabel = new(159, 143, 174); // .stat > span
    internal static readonly Color DetailValue = new(230, 215, 239); // .stat strong
    internal static readonly Color GrowthLabel = new(188, 168, 207);
    internal static readonly Color GrowthNext = new(180, 211, 198);
    internal static readonly Color DetailRule = new Color(183, 152, 204) * (37 / 255f);
    internal static readonly Color NodePlate = new(39, 32, 51);
    // Tools/SkillPanelRedesign/index.html: quiet labels, dotted rules and the detail's value colors.
    internal static readonly Color Muted2 = new(158, 150, 174);
    internal static readonly Color Faint = new(108, 101, 121);
    internal static readonly Color Mint = new(178, 222, 200);
    internal static readonly Color Shortfall = new(226, 128, 150);
    internal static readonly Color SlotGold = new(227, 193, 141);
    internal static readonly Color Heading = new(196, 180, 210);
    internal static readonly Color SerifLabel = new(213, 197, 215);
    internal static readonly Color DotColor = new Color(236, 229, 247) * .32f;
    // Skill nodes (Tools/SkillFramePreview/perch.html): ring radius, icon scale inside it and
    // the butterfly perch on the upper left of the ring, in unzoomed world pixels / degrees.
    internal const float NodeRadius = 32, PerchAngle = -125;
    internal static Vector2 Polar(float radius, float degrees)
        => new(radius * MathF.Cos(MathHelper.ToRadians(degrees)), radius * MathF.Sin(MathHelper.ToRadians(degrees)));
    // The sprite body sits high in its cell; this center puts it on the ring line.
    internal static readonly Vector2 PerchOffset = Polar(NodeRadius + 2, PerchAngle);
    private readonly Dictionary<string, Texture2D> _textures = [];
    private readonly SkillIconVariants _skillIcons = new();
    private readonly Dictionary<string, ConstellationToggleAnimation> _toggles = [];
    private readonly Dictionary<string, ConstellationToggleAnimation> _buttons = [];
    private ConstellationTween _filterMarker;
    private static DynamicSpriteFont GetFont(bool serif) => serif
        ? FontManager.NotoSerifSC.Value : FontManager.HarmonyOS_Sans_SC.Value;
    private readonly Dictionary<string, int[]> _icons;
    private readonly Dictionary<(int Aspect, int Waist, bool Filled), Rectangle[]> _stars = [];
    private sealed class StarRegion
    {
        public int Aspect { get; set; }
        public int Waist { get; set; }
        public bool Filled { get; set; }
        public int[][] Levels { get; set; }
    }
    internal SpriteBatch Batch;
    // Batch's transform matrix, for the SDF clipping that bypasses the batch.
    internal Matrix Transform = Matrix.Identity;
    internal Vector2 Origin;
    internal float Scale = 1;

    internal ConstellationDrawing(Mod mod)
    {
        const string path = "ElainaModSkills/ElainaSkillUI/ConstellationPreview/Assets/";
        _icons = JsonSerializer.Deserialize<Dictionary<string, int[]>>(mod.GetFileBytes(path + "Icons.json"));
        foreach (var star in JsonSerializer.Deserialize<StarRegion[]>(mod.GetFileBytes(path + "Stars.json")))
        {
            var levels = new Rectangle[star.Levels.Length];
            for (int i = 0; i < levels.Length; i++)
            {
                int[] r = star.Levels[i];
                levels[i] = new Rectangle(r[0], r[1], r[2], r[3]);
            }
            _stars.Add((star.Aspect, star.Waist, star.Filled), levels);
        }
    }

    internal Texture2D Texture(string name)
        => LoadTexture(name is "ButterflyMask" or "BookCoverFront" or "BookCoverBack" or "NotebookSurface" or "StudyBook" or "ToggleTrack" or "ToggleOutline" or "ToggleThumb" or "UpgradeFill" or "UpgradeOutline" or "PrimaryActionFill" or "PrimaryActionTrim"
            or "PortraitNight"
            ? "伊蕾娜/ElainaModSkills/ElainaSkillUI/ConstellationSkillPanel/Assets/" + name : Root + name);

    private Texture2D LoadTexture(string path)
    {
        if (!_textures.TryGetValue(path, out var result))
            _textures[path] = result = ModContent.Request<Texture2D>(path, AssetRequestMode.ImmediateLoad).Value;
        return result;
    }

    internal Vector2 At(float x, float y) => Origin + new Vector2(x, y) * Scale;
    internal void Image(string name, float x, float y, float width, float height, Color? tint = null, float rotation = 0)
    {
        var tex = Texture(name);
        Batch.Draw(tex, At(x + width / 2, y + height / 2), null, tint ?? Color.White,
            rotation, new Vector2(tex.Width, tex.Height) / 2,
            new Vector2(width / tex.Width, height / tex.Height) * Scale, SpriteEffects.None, 0);
    }

    // NotebookSurface is drawn 1:1 over the 1100×800 design plane, so the same rectangle
    // re-covers drawn lines with the exact page texture instead of an approximate solid color.
    internal void SurfacePatch(int x, int y, int width, int height, float alpha = 1)
        => Batch.Draw(Texture("NotebookSurface"), At(x, y), new Rectangle(x, y, width, height), Color.White * alpha,
            0, Vector2.Zero, Scale, SpriteEffects.None, 0);

    // The redesign's CSS mask on the sky (44px top, 30px bottom, 14px left, 10px right): the map's
    // content fades into the page by re-covering 1px strips with the page texture itself.
    internal void SkyEdgeFade(Rectangle sky)
    {
        const int top = 44, bottom = 30, left = 14, right = 10;
        for (int i = 0; i < top; i++) SurfacePatch(sky.X, sky.Y + i, sky.Width, 1, 1 - (i + .5f) / top);
        for (int i = 0; i < bottom; i++) SurfacePatch(sky.X, sky.Bottom - 1 - i, sky.Width, 1, 1 - (i + .5f) / bottom);
        for (int i = 0; i < left; i++) SurfacePatch(sky.X + i, sky.Y, 1, sky.Height, 1 - (i + .5f) / left);
        for (int i = 0; i < right; i++) SurfacePatch(sky.Right - 1 - i, sky.Y, 1, sky.Height, 1 - (i + .5f) / right);
    }

    // Background stars of the sky, each with its own breathing and occasional glint (ConstellationSky).
    internal void SkyStars(ConstellationSky sky)
    {
        foreach (var star in ConstellationSky.Stars)
        {
            float twinkle = sky.Twinkle(star), opacity = star.Opacity * twinkle;
            Image("Glow", star.Center.X - 3, star.Center.Y - 3, 6, 6, new Color(198, 161, 253) * (.4f * opacity));
            Dot(star.Center, star.Size / 2, new Color(221, 212, 248) * opacity);
            if (!star.Glints) continue;
            var (alpha, scale, rotation) = sky.Glint(star);
            if (alpha <= 0) continue;
            float size = star.GlintSize * scale, halo = size * .9f;
            Image("Glow", star.Center.X - halo, star.Center.Y - halo, halo * 2, halo * 2, new Color(214, 190, 250) * (.35f * alpha));
            CrossStar(star.Center, new Vector2(size), new Color(240, 232, 252) * (.85f * alpha), .2f, rotation: rotation);
        }
        if (sky.CurrentMeteor is not { } meteor) return;
        var (head, tail, fade) = ConstellationSky.MeteorPose(meteor);
        // The tail is a gradient from transparent to the bright head.
        const int segments = 12;
        for (int i = 0; i < segments; i++)
            Line(Vector2.Lerp(tail, head, i / (float)segments), Vector2.Lerp(tail, head, (i + 1f) / segments),
                Color.Lerp(new Color(226, 210, 250), new Color(246, 240, 255), (i + .5f) / segments) * ((i + .5f) / segments * fade), 1.2f);
        Image("Glow", head.X - 5, head.Y - 5, 10, 10, new Color(214, 190, 250) * (.7f * fade));
        Dot(head, 1, Color.White * fade);
    }

    // Design rectangle of the cropped portrait texture (build_art.py --game prints it).
    private static readonly (float X, float Y, float Width, float Height) PortraitArea = (428, 188, 322.16f, 576.29f);

    // Elaina holding the notebook (night treatment), printed on the page under the links and nodes;
    // a soft glow on the open book and the motes rising from it. Fades are baked into the texture.
    internal void Portrait(ConstellationSky sky)
    {
        var (x, y, width, height) = PortraitArea;
        Image("PortraitNight", x, y, width, height, Color.White * .3f);
        Vector2 book = ConstellationSky.Book;
        Image("Glow", book.X - 90, book.Y - 52, 180, 92, new Color(214, 190, 250) * .16f);
        foreach (var mote in sky.Motes)
        {
            float alpha = ConstellationSky.MoteOpacity(mote);
            if (mote.Spark > 0) CrossStar(mote.Position, new Vector2(mote.Spark), new Color(238, 228, 252) * alpha, .2f);
            else Dot(mote.Position, mote.Radius, new Color(229, 217, 245) * alpha);
        }
    }

    internal void Box(float x, float y, float w, float h, Color color)
        => Batch.Draw(TextureAssets.MagicPixel.Value, At(x, y), new Rectangle(0, 0, 1, 1), color,
            0, Vector2.Zero, new Vector2(w, h) * Scale, SpriteEffects.None, 0);

    internal void AdvanceToggleAnimations(float seconds)
    {
        foreach (var animation in _toggles.Values) animation.Advance(seconds);
        foreach (var animation in _buttons.Values) animation.Advance(seconds);
        _filterMarker?.Advance(seconds);
    }

    internal void ClearToggleAnimations() { _toggles.Clear(); _buttons.Clear(); _filterMarker = null; }

    internal void FilterMarker(string filter)
    {
        int index = filter switch { "ready" => 1, "locked" => 2, "hidden" => 3, _ => 0 };
        _filterMarker ??= new ConstellationTween(index, .18f);
        _filterMarker.SetTarget(index);
        // Tabs are 180 wide (40 dot steps), so the lit dots always land on the rule's dots.
        ActiveFilterMarker(new Vector2(116 + _filterMarker.Value * 180, 145.5f));
    }

    private float ButtonHover(string id, bool enabled, bool hover, bool pressed = false)
    {
        if (!_buttons.TryGetValue(id, out var motion)) _buttons[id] = motion = new ConstellationToggleAnimation(false);
        motion.SetTarget(enabled && pressed, enabled && hover);
        return motion.Hover;
    }

    internal void UpgradeButton(string id, bool enabled, bool hover, bool pressed)
        => PillFrame(0, 0, 276, 35, false, enabled, ButtonHover(id, enabled, hover, pressed));

    // Detail actions: a large ghost pill; the main action has a gold stroke and two sparks.
    private void PillFrame(float x, float y, float width, float height, bool gold, bool enabled, float highlight)
    {
        float alpha = enabled ? 1 : .4f;
        if (gold)
        {
            if (highlight > 0) Shadow(x, y, width, height, height / 2, new Color(195, 162, 118) * (.18f * highlight), 14);
            RoundRect(x, y, width, height, height / 2, new Color(185, 155, 87) * ((.1f + .1f * highlight) * alpha),
                SlotGold * ((.6f + .35f * highlight) * alpha), 1);
        }
        else RoundRect(x, y, width, height, height / 2, Ink * (.06f * highlight * alpha), Ink * ((.26f + .29f * highlight) * alpha), 1);
    }

    // `tail` is the slot key, or the study cost when `studyCost` is set.
    internal void ActionPill(string id, float x, float y, float width, string label, string tail, bool studyCost,
        bool gold, bool enabled, bool hover)
    {
        const float height = 36;
        float highlight = ButtonHover(id, enabled, hover), alpha = enabled ? 1 : .4f;
        PillFrame(x, y, width, height, gold, enabled, highlight);
        Color text = (gold ? new Color(240, 222, 186) : Color.Lerp(new Color(222, 212, 232), Ink, highlight)) * alpha;
        if (gold)
        {
            Spark(new Vector2(x + 18.5f, y + height / 2), 9, text * .55f);
            Spark(new Vector2(x + width - 18.5f, y + height / 2), 9, text * .55f);
        }
        float labelWidth = Measure(label, 13, 3), tailWidth = 0;
        if (!string.IsNullOrEmpty(tail))
            tailWidth = 8 + (studyCost ? 16 + LatinWidth(tail, 12) : LatinWidth(tail, 12, .5f));
        // Fit long localized labels between the sparks.
        float room = width - (gold ? 64 : 28) - tailWidth, fit = Math.Min(1, room / Math.Max(1, labelWidth));
        float start = x + (width - labelWidth * fit - tailWidth) / 2;
        Text(label, start, y + 10.5f, 13 * fit, text, spacing: 3 * fit);
        if (tailWidth == 0) return;
        float tailX = start + labelWidth * fit + 8;
        if (studyCost)
        {
            Image("StudyBook", tailX, y + 11.5f, 13, 13, text * .8f);
            LatinText(tail, tailX + 16, y + 11, 12, text * .8f);
        }
        else LatinText(tail, tailX, y + 11, 12, text * .85f, spacing: .5f);
    }

    // Bottom-bar tools: 22px ghost pills; `on` is the filled (active) state.
    internal void GhostButton(float x, float y, float width, string label, bool hover, bool on = false)
    {
        if (on) RoundRect(x, y, width, 22, 11, Ink);
        else RoundRect(x, y, width, 22, 11, Ink * (hover ? .05f : 0), Ink * (hover ? .45f : .18f), 1);
        Text(label, x + width / 2, y + 5.5f, 9.5f, on ? new Color(44, 40, 54) : hover ? Ink : Muted2, .5f, 1.4f);
    }

    // Hit areas of the bottom-bar tools (the pill is drawn 2px inside, 22px tall). The layout
    // editor's save / cancel / reset replace the debug tools while editing.
    internal static Rectangle ToolButton(string kind, bool editing)
    {
        var (x, width) = editing
            ? kind switch { "save" => (597, 42), "cancel" => (646, 42), "reset" => (695, 64), _ => (526, 64) }
            : kind switch
            {
                "points" => (597, 53), "level" => (657, 42), "max-level" => (706, 53),
                "cap" => (766, 49), "master-skills" => (822, 64), "reset-skills" => (893, 42), _ => (526, 64)
            };
        return new Rectangle(x, 753, width, 26);
    }

    // Rounded track with a 1px-inset fill (the header XP and detail level bars).
    internal void ProgressBar(float x, float y, float width, float progress, Color fill)
    {
        RoundRect(x, y, width, 5, 2.5f, new Color(8, 6, 14) * .55f);
        if (progress > 0) RoundRect(x + 1, y + 1, Math.Max(3, (width - 2) * Math.Min(1, progress)), 3, 1.5f, fill);
    }

    // SUI's SDF passes draw straight to the device (see SkillIcon): flush, draw, reopen the batch.
    internal void RoundRect(float x, float y, float width, float height, float radius, Color fill,
        Color border = default, float borderWidth = 0)
    {
        if (width <= 0 || height <= 0) return;
        Batch.End();
        var states = Batch.BackupStates();
        var corner = new Vector4(Math.Min(radius, Math.Min(width, height) / 2) * Scale);
        if (borderWidth > 0)
            SDFRectangle.DrawWithBorder(At(x, y), new Vector2(width, height) * Scale, corner, fill, borderWidth * Scale, border, Transform);
        else SDFRectangle.DrawWithoutBorder(At(x, y), new Vector2(width, height) * Scale, corner, fill, Transform);
        states.Begin();
    }

    // A CSS-like blurred shadow of the rectangle: half strength at its edge, fading `blur` outward.
    internal void Shadow(float x, float y, float width, float height, float radius, Color color, float blur)
    {
        Batch.End();
        var states = Batch.BackupStates();
        SDFRectangle.DrawShadow(At(x - blur, y - blur), new Vector2(width + blur * 2, height + blur * 2) * Scale,
            new Vector4((radius + blur) * Scale), color, blur * 2 * Scale, Transform);
        states.Begin();
    }

    // The redesign's dotted rule: 1.4px round dots every 4.5px.
    internal void Dots(float x1, float y1, float x2, float y2, Color color) => Dots(new(x1, y1), new(x2, y2), _ => color);

    internal void Dots(Vector2 a, Vector2 b, Func<float, Color> color, float gap = 4.5f)
    {
        float length = Vector2.Distance(a, b);
        for (float p = 0; p <= length + .01f; p += gap)
        {
            float t = length > 0 ? Math.Min(1, p / length) : 0;
            Dot(Vector2.Lerp(a, b, t), .7f, color(t));
        }
    }

    // Vertical dotted divider with a larger dot at each end.
    internal void DotSeparator(float x, float top, float bottom)
    {
        Dots(x, top + 6, x, bottom - 6, DotColor);
        Dot(new Vector2(x, top + 2), 2, new Color(236, 229, 247) * .5f);
        Dot(new Vector2(x, bottom - 2), 2, new Color(236, 229, 247) * .5f);
    }

    internal void Spark(Vector2 center, float size, Color color) => CrossStar(center, new Vector2(size), color, .2f);

    // Three sparks set into a dotted rule where two rules meet.
    internal void Trio(Vector2 center, Color color)
    {
        Spark(center, 13.33f, color);
        Spark(center - new Vector2(14.33f, 0), 4.67f, color);
        Spark(center + new Vector2(14.33f, 0), 4.67f, color);
    }

    // Detail icon frame: a hairline square with open corners and one small star, no plate.
    internal void DetailIconFrame(Vector2 center, bool maxed, bool unlearned)
    {
        const float h = 33, gap = 4;
        Color tone = maxed ? Gold : Lavender;
        float a = unlearned ? .5f : 1;
        Color line = tone * (.32f * a);
        Line(center + new Vector2(-h + gap, -h), center + new Vector2(h - gap, -h), line);
        Line(center + new Vector2(-h + gap, h), center + new Vector2(h - gap, h), line);
        Line(center + new Vector2(-h, -h + gap), center + new Vector2(-h, h - gap), line);
        Line(center + new Vector2(h, -h + gap), center + new Vector2(h, h - gap), line);
        foreach (var corner in new[] { new Vector2(h, -h), new Vector2(-h, h), new Vector2(h, h) })
            Dot(center + corner, .9f, tone * (.45f * a));
        CrossStar(center - new Vector2(h), new Vector2(8 * .976f, 8), maxed ? Gold * .95f : Ink * (.9f * a), .28f);
    }

    internal void Toggle(string id, float x, float y, bool on, bool enabled, bool hover)
    {
        if (!_toggles.TryGetValue(id, out var animation))
            _toggles[id] = animation = new ConstellationToggleAnimation(on);
        animation.SetTarget(on, enabled && hover);
        float value = animation.Position, highlight = animation.Hover, alpha = enabled ? 1 : .4f;
        Color track = Color.Lerp(new Color(31, 27, 40), new Color(164, 130, 188) * (66 / 255f), value);
        Color border = Color.Lerp(new Color(142, 120, 166) * (107 / 255f), new Color(203, 177, 222) * (156 / 255f), value);
        Color thumb = Color.Lerp(new Color(153, 135, 167), new Color(225, 206, 242), value);
        Image("ToggleTrack", x, y, 37, 21, Color.Lerp(track, new Color(103, 78, 128) * .6f, highlight * .22f) * alpha);
        Image("ToggleOutline", x, y, 37, 21, Color.Lerp(border, Lavender, highlight * .35f) * alpha);
        Image("ToggleThumb", x + 3 + value * 16, y + 3, 15, 15, Color.Lerp(thumb, Ink, highlight * .2f) * alpha);
    }

    internal void Gradient(float x, float y, float width, float height, Color left, Color right)
    {
        // A small number of design-pixel strips; no generated textures or render targets.
        int count = Math.Max(1, (int)MathF.Ceiling(width));
        for (int i = 0; i < count; i++)
            Box(x + i * width / count, y, width / count, height, Color.Lerp(left, right, (i + .5f) / count));
    }

    internal void ActiveFilterMarker(Vector2 center)
    {
        // The tab rule's dots under the active tab light up (bright middle, fading ends), with a
        // long thin flash laid straight on the rule. Glow.png already contains premultiplied RGB.
        Image("Glow", center.X - 72, center.Y - 7, 144, 14, new Color(192, 154, 220) * .16f);
        Image("Glow", center.X - 24, center.Y - 6, 48, 12, new Color(207, 157, 250) * .32f);
        Dots(center - new Vector2(63, 0), center + new Vector2(63, 0), t =>
        {
            float peak = 1 - Math.Abs(t * 2 - 1);
            return Color.Lerp(new Color(226, 206, 240), new Color(240, 228, 252), peak) * peak;
        });
        // Baked at its own 34:12 aspect; squashing the square star blurred and hardened it.
        CrossStar(center, new Vector2(34, 12), new Color(244, 234, 252), .2f);
    }

    internal void Line(Vector2 a, Vector2 b, Color color, float width = 1)
    {
        var delta = b - a;
        if (delta.LengthSquared() < .001f) return;
        Batch.Draw(TextureAssets.MagicPixel.Value, Origin + a * Scale, new Rectangle(0, 0, 1, 1), color,
            MathF.Atan2(delta.Y, delta.X), new Vector2(0, .5f), new Vector2(delta.Length(), width) * Scale,
            SpriteEffects.None, 0);
    }

    internal void Dash(Vector2 a, Vector2 b, Color color, float width = 1, float dash = 4, float gap = 5)
    {
        float length = Vector2.Distance(a, b);
        if (length < .01f) return;
        var dir = (b - a) / length;
        for (float p = 0; p < length; p += dash + gap)
            Line(a + dir * p, a + dir * Math.Min(p + dash, length), color, width);
    }

    // Stretch only the middle of a preblurred stroke: cap falloff stays round at any length.
    private void ProfileLine(string textureName, Vector2 a, Vector2 b, float width, float padding,
        float zoom, Color color)
    {
        Vector2 delta = b - a;
        float length = delta.Length();
        if (length < .01f) return;
        var texture = Texture(textureName);
        Vector2 direction = delta / length;
        float rotation = MathF.Atan2(delta.Y, delta.X);
        float cap = padding * 2 * zoom, height = (width + padding * 2) * zoom;
        void Slice(Rectangle source, float offset, float span)
            => Batch.Draw(texture, Origin + (a + direction * offset) * Scale, source, color, rotation,
                new Vector2(0, source.Height / 2f),
                new Vector2(span / source.Width, height / source.Height) * Scale, SpriteEffects.None, 0);
        if (length < cap)
        {
            Slice(new Rectangle(0, 0, texture.Width, texture.Height), -padding * zoom,
                length + padding * 2 * zoom);
            return;
        }
        int half = texture.Width / 2;
        Slice(new Rectangle(0, 0, half, texture.Height), -padding * zoom, cap);
        if (length > cap)
            Slice(new Rectangle(half, 0, 1, texture.Height), padding * zoom, length - cap);
        Slice(new Rectangle(half, 0, texture.Width - half, texture.Height), length - padding * zoom, cap);
    }

    internal void SkillLink(Vector2 a, Vector2 b, string status, bool related, bool dim, float zoom)
    {
        bool learned = status == "learned", ready = status == "available", hidden = status == "hidden";
        float opacity = dim ? .09f : related ? 1 : ready ? .65f : hidden ? .35f : 1;
        Color color = (related ? new Color(228, 201, 152) : learned ? new Color(191, 172, 216)
            : ready ? new Color(189, 157, 218) : new Color(102, 89, 115)) * opacity;
        float width = related ? 1.8f : learned ? 1.2f : 1;
        string core = related ? "LinkRelated" : learned ? "LinkLearned" : "LinkLocked";
        Vector2 delta = b - a;
        float length = delta.Length();
        if (length < .01f) return;
        Vector2 direction = delta / length;
        if (ready || hidden)
        {
            float dash = (hidden ? 2 : 4) * zoom, gap = (hidden ? 7 : 5) * zoom;
            // SVG drop-shadow follows the dashed alpha mask; do not draw a solid halo underneath.
            if (related)
            {
                var texture = Texture("LinkRelatedDashGlow");
                float rotation = MathF.Atan2(delta.Y, delta.X);
                for (float p = 0; p < length; p += dash + gap)
                {
                    float span = Math.Min(dash, length - p);
                    Batch.Draw(texture, Origin + (a + direction * (p + span / 2)) * Scale, null,
                        new Color(227, 192, 145) * (opacity / 3), rotation,
                        new Vector2(texture.Width, texture.Height) / 2,
                        new Vector2((span + 18 * zoom) / texture.Width, 19.8f * zoom / texture.Height) * Scale,
                        SpriteEffects.None, 0);
                }
            }
            for (float p = 0; p < length; p += dash + gap)
                ProfileLine(core, a + direction * p, a + direction * Math.Min(p + dash, length), width, 1.5f, zoom, color);
        }
        else
        {
            if (related || learned)
                ProfileLine(related ? "LinkRelatedGlow" : "LinkLearnedGlow", a, b, width, related ? 9 : 12, zoom,
                    (related ? new Color(227, 192, 145) : new Color(185, 157, 222)) * (opacity / 3));
            ProfileLine(core, a, b, width, 1.5f, zoom, color);
        }
    }

    internal void Ring(Vector2 center, float rx, float ry, Color color, float width = 1, bool dashed = false, float rotation = 0)
    {
        const int segments = 160;
        Vector2 Point(int i)
        {
            float angle = i * MathHelper.TwoPi / segments;
            var v = new Vector2(MathF.Cos(angle) * rx, MathF.Sin(angle) * ry);
            float c = MathF.Cos(rotation), s = MathF.Sin(rotation);
            return center + new Vector2(v.X * c - v.Y * s, v.X * s + v.Y * c);
        }
        for (int i = 0; i < segments; i++)
            if (!dashed || i % 4 == 0) Line(Point(i), Point(i + 1), color, width);
    }

    // Circular arc from `from` degrees, sweeping `sweep` degrees clockwise on screen.
    internal void Arc(Vector2 center, float radius, float from, float sweep, Color color, float width = 1)
    {
        if (sweep <= 0 || radius <= 0) return;
        int segments = Math.Max(2, (int)MathF.Ceiling(sweep / 360 * 160));
        Vector2 previous = center + Polar(radius, from);
        for (int i = 1; i <= segments; i++)
        {
            Vector2 next = center + Polar(radius, from + sweep * i / segments);
            Line(previous, next, color, width);
            previous = next;
        }
    }

    internal void Dot(Vector2 center, float radius, Color color)
        => Image("Disc", center.X - radius, center.Y - radius, radius * 2, radius * 2, color);

    // The small hashed constellation outside a node ring. `traced` is the time since the butterfly
    // last landed or beat its wings here: links light up one by one from the first star (0.2s each),
    // every star flashes as it is reached, then the bright links fade back to hairlines.
    internal void NodeConstellation(Vector2 center, float zoom, ConstellationStar[] stars, Color color, float alpha,
        bool selected, float time, float traced)
    {
        const float segment = .2f, flash = .4f, hold = .15f, fade = .5f;
        Vector2 Local(ConstellationStar s) => Polar(NodeRadius + s.Distance, s.Angle);
        // Links stop short of both stars by their radius + 1.5.
        void Link(int i, float progress, Color tint, float width)
        {
            ConstellationStar from = stars[stars[i].From], to = stars[i];
            Vector2 a = Local(from), b = Local(to);
            float length = Vector2.Distance(a, b);
            if (length < .01f) return;
            float start = (from.Size + 1.5f) / length, end = 1 - (to.Size + 1.5f) / length;
            Line(center + Vector2.Lerp(a, b, start) * zoom,
                center + Vector2.Lerp(a, b, start + (end - start) * progress) * zoom, tint, width);
        }
        Span<float> reach = stackalloc float[stars.Length];
        for (int i = 0; i < stars.Length; i++)
        {
            reach[i] = i == 0 ? 0 : reach[stars[i].From] + segment;
            if (i > 0) Link(i, 1, color * (alpha * .45f), .8f);
        }
        for (int i = 1; i < stars.Length; i++)
        {
            float u = Math.Clamp((traced - reach[stars[i].From]) / segment, 0, 1);
            float light = 1 - Math.Clamp((traced - reach[i] - hold) / fade, 0, 1);
            if (u > 0 && light > 0)
                Link(i, ConstellationNodeEffects.EaseOut(u), new Color(255, 248, 236) * (light * .95f * alpha), 1.5f);
        }
        for (int i = 0; i < stars.Length; i++)
        {
            var star = stars[i];
            float f = (traced - reach[i]) / flash;
            float pulse = f >= 0 && f < 1 ? 1 + (star.Size > 2 ? .7f : 1.3f) * MathF.Sin(MathHelper.Pi * f)
                : 1 + (selected ? .1f * MathF.Sin(time * 2.6f + i * 1.9f) : 0);
            float size = star.Size * (selected ? 1.12f : 1) * pulse * zoom;
            Vector2 p = center + Local(star) * zoom;
            if (star.Size > 2)
            {
                float glow = size * 2.3f;
                Image("Glow", p.X - glow, p.Y - glow, glow * 2, glow * 2, new Color(255, 244, 220) * (.36f * alpha));
                CrossStar(p, new Vector2(size * 2), (selected ? new Color(255, 244, 220) : color) * alpha, .2f);
            }
            else Dot(p, size, color * (alpha * .85f));
        }
    }

    // Gold arcs grow both ways from the perch after landing, with a brighter crest at the perch,
    // one outward pulse and a ripple where the butterfly touched. A left node keeps a fading ring.
    internal void NodeSelection(Vector2 center, float zoom, float amount, bool landed, float sinceLanding,
        float time, bool flutter, float alpha = 1)
    {
        float r = NodeRadius * zoom;
        if (!landed)
        {
            if (amount > 0) Ring(center, r, r, Gold * (amount * alpha), 1.6f);
            return;
        }
        float progress = ConstellationNodeEffects.EaseOut(sinceLanding / .55f);
        Arc(center, r, PerchAngle, 180 * progress, Gold * alpha, 1.6f);
        Arc(center, r, PerchAngle - 180 * progress, 180 * progress, Gold * alpha, 1.6f);
        float crest = progress * Math.Min(1, .65f + .2f * MathF.Sin(time * 2.1f) + (flutter ? .25f : 0)) * alpha;
        float span = 24 * progress;
        Arc(center, r, PerchAngle - span, span * 2, Gold * (crest * .3f), 4.5f);
        Arc(center, r, PerchAngle - span, span * 2, new Color(255, 248, 236) * crest, 2.2f);
        float pulse = sinceLanding / .6f;
        if (pulse < 1)
        {
            float radius = (NodeRadius + ConstellationNodeEffects.EaseOut(pulse) * 11) * zoom;
            Ring(center, radius, radius, Gold * (.55f * (1 - pulse) * alpha));
        }
        float ripple = sinceLanding / .7f;
        if (ripple < 1)
        {
            float radius = (2 + ConstellationNodeEffects.EaseOut(ripple) * 13) * zoom;
            Ring(center + Polar(NodeRadius, PerchAngle) * zoom, radius, radius,
                new Color(245, 236, 255) * (.8f * (1 - ripple) * alpha));
        }
    }

    internal void Icon(string name, Vector2 center, float size, Color color)
    {
        if (!_icons.TryGetValue(name, out var r)) r = _icons["star"];
        Batch.Draw(Texture("Icons"), Origin + center * Scale, new Rectangle(r[0], r[1], r[2], r[3]),
            color, 0, new Vector2(r[2], r[3]) / 2, size * Scale / r[2], SpriteEffects.None, 0);
    }

    // `round` crops the icon to the circle inscribed in its `size` square.
    internal void SkillIcon(ConstellationNode skill, Vector2 center, float size, Color color, bool hidden = false,
        bool uncolored = false, bool round = false)
    {
        if (hidden)
        {
            // The reference's crescent and four-point star, with a transparent cutout.
            // Scanline spans avoid painting an opaque circle over the retained starfield.
            float radius = size * .35f;
            var moon = center + new Vector2(-size * .08f, size * .06f);
            var cut = moon + new Vector2(size * .17f, -size * .15f);
            const int rows = 40;
            float step = radius * 2 / rows;
            for (int i = 0; i < rows; i++)
            {
                float y = moon.Y - radius + (i + .5f) * step;
                float half = MathF.Sqrt(Math.Max(0, radius * radius - (y - moon.Y) * (y - moon.Y)));
                float left = moon.X - half, right = moon.X + half;
                float dy = y - cut.Y;
                if (Math.Abs(dy) < radius)
                    right = Math.Min(right, cut.X - MathF.Sqrt(radius * radius - dy * dy));
                if (right > left) Box(left, y - step / 2, right - left, step, color * .8f);
            }
            CrossStar(center + new Vector2(size * .3f, -size * .3f), new Vector2(size * .35f), color, .2f);
            return;
        }
        var icon = _skillIcons.Resolve(skill.Skill.SkillIcon.Value, skill.ModSkill.ConstellationUncoloredIconPath, uncolored);
        var texture = icon.Texture;
        if (uncolored && !icon.Uncolored) color *= .65f;
        float factor = size / Math.Max(texture.Width, texture.Height);
        // Keep authored transparency for uncolored art; original artwork retains its additive tint.
        Color tint = icon.Uncolored ? color : new Color(color.R, color.G, color.B, (byte)0);
        var pivot = new Vector2(texture.Width, texture.Height) / 2;
        if (!round)
        {
            Batch.Draw(texture, Origin + center * Scale, null, tint, 0, pivot, factor * Scale, SpriteEffects.None, 0);
            return;
        }
        // SUI's SDF sample pass draws straight to the device: flush what is queued (which leaves the
        // batch's state on the device), clip, then reopen the batch so later sprites layer on top.
        Batch.End();
        var states = Batch.BackupStates();
        Vector2 extent = pivot * 2 * factor * Scale;
        SDFRectangle.SampleVersion(texture, Origin + center * Scale - extent / 2, extent, Vector2.Zero, Vector2.One,
            new Vector4(Math.Min(extent.X, extent.Y) / 2), tint, Transform);
        states.Begin();
    }

    internal string[] WrapParagraph(string text, float width, float size)
        => WrapUIParagraph(text, width, value => Measure(value, size));

    internal void Frame(float x, float y, float width, float height, Color color)
    {
        Line(new Vector2(x, y), new Vector2(x + width, y), color);
        Line(new Vector2(x + width, y), new Vector2(x + width, y + height), color);
        Line(new Vector2(x + width, y + height), new Vector2(x, y + height), color);
        Line(new Vector2(x, y + height), new Vector2(x, y), color);
    }

    internal void Tooltip(string text, Vector2 pointer, Vector2 bounds, Rectangle? anchor = null)
    {
        float maxWidth = Math.Min(340, bounds.X - 44);
        var lines = WrapParagraph(ConstellationRequirements.Localize(text), maxWidth, 11);
        int capacity = Math.Max(1, (int)((bounds.Y - 44) / 18));
        if (lines.Length > capacity)
        {
            Array.Resize(ref lines, capacity);
            lines[^1] = LocalizeUI("MoreRequirements");
        }
        float width = 20;
        foreach (string line in lines) width = Math.Max(width, Measure(line, 11) + 20);
        width = Math.Min(width, maxWidth + 20);
        float height = lines.Length * 18 + 14;
        var p = ConstellationTooltipPlacement.Place(new Vector2(width, height), pointer, bounds, anchor);
        RoundRect(p.X, p.Y, width, height, 6, new Color(26, 22, 35) * .96f, new Color(236, 229, 247) * .18f, 1);
        for (int i = 0; i < lines.Length; i++) Text(lines[i], p.X + 10, p.Y + 7 + i * 18, 11, Ink);
    }

    // Term card ("notebook slip"): the page's dotted inner frame with star corners, the term's icon
    // and serif name, its rich-text explanation, then optional fixed values as detail-style stat columns.
    internal void TermTooltip(KLTerm term, Vector2 pointer, Vector2 bounds, Rectangle? anchor = null)
    {
        const float width = 272, pad = 18, rule = pad + 30, bodyTop = rule + 12;
        var stats = term.Stats?.Invoke() ?? [];
        var body = new ConstellationRichText(term.Description(), width - pad * 2, 12, 21, DetailText);
        float statTop = bodyTop + body.Height + 10;
        float height = (stats.Length > 0 ? statTop + 29 + 44 : bodyTop + body.Height) + pad - 2;
        var p = ConstellationTooltipPlacement.Place(new Vector2(width, height), pointer, bounds, anchor);
        var origin = Origin;
        Origin = At(p.X, p.Y);
        Shadow(0, 8, width, height, 4, Color.Black * .55f, 16);
        RoundRect(0, 0, width, height, 3, new Color(26, 22, 36) * .98f, Lavender * .3f, 1);
        float l = 6.5f, r = width - 6.5f, t = 6.5f, b = height - 6.5f;
        Dots(l + 9, t, r - 9, t, DotColor * .8f); Dots(l + 9, b, r - 9, b, DotColor * .8f);
        Dots(l, t + 9, l, b - 9, DotColor * .8f); Dots(r, t + 9, r, b - 9, DotColor * .8f);
        foreach (var corner in new[] { new Vector2(l, t), new Vector2(r, t), new Vector2(r, b), new Vector2(l, b) })
            CrossStar(corner, new Vector2(7 * .976f, 7), Lavender * .8f, .28f);
        var icon = ModContent.Request<Texture2D>(term.IconPath, AssetRequestMode.ImmediateLoad).Value;
        Batch.Draw(icon, At(pad, pad + 2), null, Color.White, 0, Vector2.Zero, 18f / Math.Max(icon.Width, icon.Height) * Scale, SpriteEffects.None, 0);
        Text(term.Name(), pad + 25, pad - 1, 17, term.Color, spacing: 1, serif: true);
        Text(LocalizeUI("TermTag"), width - pad, pad + 5, 9, Muted2, 1, 2);
        Dots(pad, rule, width - pad, rule, DotColor);
        var bodyOrigin = Origin;
        Origin = At(pad, bodyTop);
        body.Draw(this);
        Origin = bodyOrigin;
        if (stats.Length > 0)
        {
            Dots(pad, statTop + 14.5f, width - pad, statTop + 14.5f, DotColor);
            float y = statTop + 29, column = (width - pad * 2) / 2;
            for (int i = 0; i < Math.Min(2, stats.Length); i++)
            {
                var (label, value, unit) = stats[i];
                float x = pad + i * (column + 18);
                if (i > 0) Dots(pad + column, y + 2, pad + column, y + 40, DotColor);
                Text(label, x, y, 10, Muted2, spacing: 2);
                LatinText(value, x, y + 16, 22, Ink);
                float unitLeft = x + LatinWidth(value, 22) + 4;
                if (unit.All(c => c is >= ' ' and <= '~')) LatinText(unit, unitLeft, y + 26.5f, 11, Muted, spacing: .5f);
                else Text(unit, unitLeft, y + 26, 10, Muted, spacing: 1);
            }
        }
        Origin = origin;
    }

    // Offline KL DrawCrossStar output; all shapes and prefiltered sizes share one texture.
    internal void CrossStar(Vector2 center, Vector2 size, Color color, float waist = .2f, bool filled = true, float rotation = 0)
    {
        if (size.X <= 0 || size.Y <= 0) return;
        var key = ((int)MathF.Round(size.X / size.Y * 1000),
            (int)MathF.Round(Math.Clamp(waist, .05f, .49f) * 1000), filled);
        Rectangle source = StarTile(_stars[key], Math.Max(size.X, size.Y) * 128f / 112);
        float extent = Math.Max(size.X, size.Y) * 128f / 112;
        Batch.Draw(Texture("Stars"), Origin + center * Scale, source, color, rotation,
            new Vector2(source.Width, source.Height) / 2, extent * Scale / source.Width, SpriteEffects.None, 0);
    }

    // PNG assets have no automatic mipmaps. Select a prefiltered tile close to screen size.
    private Rectangle StarTile(Rectangle[] levels, float extent)
    {
        float pixels = extent * Scale * Main.UIScale;
        int level = 0;
        while (level + 1 < levels.Length && pixels < MathF.Sqrt(levels[level].Width * levels[level + 1].Width)) level++;
        return levels[level];
    }

    // 1px vertical strips colored by x, clipped to rounded corners on the left and/or right.
    private void Strips(float x, float y, float width, float height, float leftRadius, float rightRadius, Func<float, Color> color)
    {
        int count = Math.Max(1, (int)MathF.Ceiling(width));
        float step = width / count;
        for (int i = 0; i < count; i++)
        {
            float mid = x + (i + .5f) * step, inset = 0;
            float Round(float radius, float distance)
                => radius - MathF.Sqrt(Math.Max(0, radius * radius - (radius - distance) * (radius - distance)));
            if (mid - x < leftRadius) inset = Round(leftRadius, mid - x);
            else if (x + width - mid < rightRadius) inset = Round(rightRadius, x + width - mid);
            Box(x + i * step, y + inset, step, height - inset * 2, color(mid));
        }
    }

    private static Color Stops(float t, params (float At, Color Color)[] stops)
    {
        t = Math.Clamp(t, 0, 1);
        for (int i = 1; i < stops.Length; i++)
            if (t <= stops[i].At)
                return Color.Lerp(stops[i - 1].Color, stops[i].Color, (t - stops[i - 1].At) / Math.Max(.0001f, stops[i].At - stops[i - 1].At));
        return stops[^1].Color;
    }

    // Approved hardcover: a dark cover (11px spine on the left), a page block offset 10px to the
    // lower right with uneven dark seams, a bookmark ribbon and a soft drop shadow.
    internal void NotebookPage(float width, float height)
    {
        const float block = 10;
        Shadow(-6, 18, width + 22, height + 10, 8, Color.Black * .6f, 16);
        float diagonal = width * width + height * height;
        Strips(-11, -5, width + block + 15, height + block + 9, 5, 5, x => Color.Lerp(new Color(29, 25, 39), new Color(16, 14, 23),
            Math.Clamp((x * width + height / 2 * height) / diagonal, 0, 1)));
        Strips(-11, -5, 11, height + block + 9, 5, 0, x => Stops((x + 11) / 11,
            (0, new Color(14, 12, 20)), (.45f, new Color(50, 42, 66)), (1, new Color(19, 16, 27))));
        RoundRect(-11, -5, width + block + 15, height + block + 9, 5, Color.Transparent, Lavender * .14f, 1);
        // Ribbon, hanging from between the leaves with a swallowtail end.
        for (float x = 962; x < 974; x += .5f)
        {
            float notch = 6 * (1 - Math.Abs(x + .25f - 968) / 6);
            Box(x, height - 8, .5f, 40 - notch, Stops((x + .25f - 962) / 12,
                (0, new Color(79, 63, 102)), (.45f, new Color(110, 90, 142)), (1, new Color(74, 59, 96))));
        }
        Line(new Vector2(965.5f, height - 8), new Vector2(965.5f, height + 28), new Color(236, 220, 255) * .16f, .8f);
        CrossStar(new Vector2(968, height + 20), new Vector2(5.5f * .976f, 5.5f), Gold * .75f, .28f);
        // Page block: lighter right face, darker bottom face.
        for (float k = 0; k < block; k += .5f)
        {
            Box(width + k, k, .5f, height + .5f, Stops((k + .25f) / block,
                (0, new Color(66, 57, 86)), (.6f, new Color(92, 81, 114)), (1, new Color(84, 74, 105))));
            Box(0, height + k, width + k, .5f, Stops((k + .25f) / block,
                (0, new Color(48, 41, 63)), (.6f, new Color(66, 57, 85)), (1, new Color(60, 52, 78))));
        }
        foreach (var (k, a) in new[] { (1.3f, .34f), (2.4f, .16f), (3.2f, .28f), (4.6f, .14f), (5.5f, .3f), (6.9f, .18f), (7.8f, .26f), (9f, .2f) })
        {
            Color seam = new Color(18, 14, 27) * a;
            Line(new Vector2(width + k, k), new Vector2(width + k, height + k), seam, .6f);
            Line(new Vector2(width + k, height + k), new Vector2(10, height + k), seam, .6f);
            Line(new Vector2(10, height + k), new Vector2(0, height + k * 1.08f), seam, .6f);
        }
        Gradient(0, height, 18, block, new Color(10, 9, 17) * .55f, Color.Transparent);
        Color edge = Lavender * .4f;
        Line(new Vector2(width, 0), new Vector2(width + block, block), edge);
        Line(new Vector2(width + block, block), new Vector2(width + block, height + block - 2), edge);
        Line(new Vector2(width + block, height + block - 2), new Vector2(width + block - 2, height + block), edge);
        Line(new Vector2(width + block - 2, height + block), new Vector2(0, height + block), edge);
        Box(0, height, width, 2.5f, new Color(10, 8, 16) * .35f);
        Box(width, 1.25f, 2.5f, height + 1.25f, new Color(10, 8, 16) * .35f);

        Image("NotebookSurface", 0, 0, width, height);
        Frame(.5f, .5f, width - 1, height - 1, Lavender * .34f);
        Gradient(0, 0, 14, height, new Color(10, 9, 17) * .55f, Color.Transparent);
        // Dotted inner frame: corners give way to star ornaments, a small star on each side.
        float l = 12.5f, r = width - 12.5f, t = 12.5f, b = height - 12.5f, mid = height / 2;
        const float g = 16;
        Dots(l + g, t, r - g, t, DotColor); Dots(l + g, b, r - g, b, DotColor);
        Dots(l, t + g, l, mid - 10, DotColor); Dots(l, mid + 10, l, b - g, DotColor);
        Dots(r, t + g, r, mid - 10, DotColor); Dots(r, mid + 10, r, b - g, DotColor);
        CrossStar(new Vector2(l, mid), new Vector2(6.224f, 8), Lavender * .85f, .25f);
        CrossStar(new Vector2(r, mid), new Vector2(6.224f, 8), Lavender * .85f, .25f);
        StarCorner(new Vector2(l, t), 1, 1); StarCorner(new Vector2(r, t), -1, 1);
        StarCorner(new Vector2(r, b), -1, -1); StarCorner(new Vector2(l, b), 1, -1);
    }

    // A four-point star on the corner, a quarter arc around it joining the two dotted edges.
    private void StarCorner(Vector2 corner, int sx, int sy)
    {
        Image("Glow", corner.X - 9, corner.Y - 9, 18, 18, new Color(214, 186, 240) * .32f);
        float from = (sx, sy) switch { (1, 1) => 0, (-1, 1) => 90, (-1, -1) => 180, _ => 270 };
        Arc(corner, 14, from, 90, Lavender * .42f);
        CrossStar(corner, new Vector2(11 * .976f, 11), new Color(236, 226, 247), .28f);
        Dot(corner + new Vector2(sx, sy) * 15.5f, 1, Lavender * .55f);
    }

    internal void SelectionButterfly(Vector2 node, int frame, float zoom)
    {
        var texture = Texture("ButterflyMask");
        int cell = texture.Width / 4;
        var source = new Rectangle(frame % 4 * cell, frame / 4 * cell, cell, cell);
        Vector2 center = node + PerchOffset * zoom;
        Image("Glow", center.X - 19 * zoom, center.Y - 19 * zoom, 38 * zoom, 38 * zoom,
            new Color(183, 162, 234) * .1f);
        Batch.Draw(texture, At(center.X, center.Y), source, new Color(229, 217, 245) * .9f,
            MathHelper.ToRadians(-18), new Vector2(cell / 2f), 34f / cell * zoom * Scale, SpriteEffects.None, 0);
    }

    // Open corners may rotate independently of the rectangular, unmasked artwork.
    internal void Corners(Vector2 center, float size, float arm, Color color, float rotation = 0)
    {
        Vector2 Transform(Vector2 v) => center + Vector2.Transform(v, Matrix.CreateRotationZ(rotation));
        foreach (int x in new[] { -1, 1 })
        foreach (int y in new[] { -1, 1 })
        {
            var corner = new Vector2(x * size / 2, y * size / 2);
            Line(Transform(corner), Transform(corner - new Vector2(x * arm, 0)), color);
            Line(Transform(corner), Transform(corner - new Vector2(0, y * arm)), color);
        }
    }

    internal void FittedText(string text, float x, float y, float width, float size, Color color,
        float align = 0, bool serif = false, float spacing = 0)
    {
        float measured = Measure(text, size, spacing, serif);
        float fit = measured > width ? width / measured : 1;
        Text(text, x, y, size * fit, color, align, spacing * fit, serif);
    }

    // Use the font's full-width Chinese advance as one design em, not a fixed atlas resolution.
    private static float FontScale(DynamicSpriteFont font, float size) => size / Math.Max(1f, font.MeasureString("国").X);

    // Gelasio is baked at 36pt / 48px; it contains ASCII only, so do not measure a CJK em.
    internal float SlotLabelWidth(string text, float size)
        => FontManager.Gelasio.Value.MeasureString(text).X * size / 48f;

    internal void SlotLabel(string text, float x, float y, float size, Color color)
        => Batch.DrawString(FontManager.Gelasio.Value, text, At(x, y), color, 0, Vector2.Zero,
            size / 48f * Scale, SpriteEffects.None, 0);

    // Masthead counterpart of character-progression in the approved level-preview HTML.
    // Share the actual drawing with the FNA fixture; only the fixture substitutes player data.
    internal void PlayerLevel(int currentLevel, int maxLevel, int experience, int experienceMax)
    {
        const float sealX = 823, left = 868, right = 999, width = right - left;

        // A single quiet diamond repeats the notebook tooling without enclosing another panel.
        Corners(new Vector2(sealX, 41), 36, 18, new Color(205, 177, 138) * (66 / 255f), MathHelper.PiOver4);
        LatinText("LV.", sealX, 19, 8, new Color(188, 167, 141), .5f, 2);
        string level = currentLevel.ToString();
        float levelSize = Math.Min(28, 28 * 56 / Math.Max(1, LatinWidth(level, 28)));
        LatinText(level, sealX, 27 + (28 - levelSize) * .5f, levelSize, new Color(240, 222, 186), .5f);

        string cap = maxLevel.ToString(), capLabel = LocalizeUI("LevelCap");
        Color capColor = new(184, 167, 192);
        float capLabelLeft = right - LatinWidth(cap, 10) - 5 - Measure(capLabel, 10, 1);
        FittedText(LocalizeUI("CharacterLevel"), left, 23, capLabelLeft - left - 8, 11, SerifLabel, serif: true, spacing: 1);
        Text(capLabel, capLabelLeft, 24, 10, capColor, spacing: 1);
        LatinText(cap, right, 23, 10, capColor, 1);

        Text(LocalizeUI("Experience"), left, 44, 9, new Color(169, 154, 182), spacing: .5f);
        string experienceText = $"{experience} / {experienceMax}";
        float experienceWidth = width - Measure(LocalizeUI("Experience"), 9, .5f) - 10;
        float experienceSize = Math.Min(10, 10 * experienceWidth / Math.Max(1, LatinWidth(experienceText, 10)));
        LatinText(experienceText, right, 42, experienceSize, new Color(203, 189, 211), 1);
        float progress = experienceMax > 0 ? MathHelper.Clamp(experience / (float)experienceMax, 0, 1) : 0;
        ProgressBar(left, 61, width, progress, Lavender * .8f);
    }

    internal float LatinWidth(string text, float size, float spacing = 0)
        => SlotLabelWidth(text, size) + Math.Max(0, text.Length - 1) * spacing;

    internal void LatinText(string text, float x, float y, float size, Color color, float align = 0, float spacing = 0)
    {
        float start = x - LatinWidth(text, size, spacing) * align;
        if (spacing == 0) { SlotLabel(text, start, y, size, color); return; }
        foreach (char c in text)
        {
            string glyph = c.ToString();
            SlotLabel(glyph, start, y, size, color);
            start += SlotLabelWidth(glyph, size) + spacing;
        }
    }

    internal void NotebookTitle(float x, float y)
        => FittedText(LocalizeUI("NotebookTitle"), x, y, 420, 30, new Color(240, 231, 244), spacing: 3, serif: true);

    internal float Measure(string text, float size, float spacing = 0, bool serif = false)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        var font = GetFont(serif);
        float factor = FontScale(font, size);
        if (spacing == 0) return font.MeasureString(text).X * factor;
        float width = 0;
        foreach (char c in text)
            width += font.MeasureString(c.ToString()).X * factor + spacing;
        return Math.Max(0, width - spacing);
    }

    internal void Text(string text, float x, float y, float size, Color color,
        float align = 0, float spacing = 0, bool serif = false)
    {
        if (string.IsNullOrEmpty(text)) return;
        var font = GetFont(serif);
        float factor = FontScale(font, size);
        float start = x - Measure(text, size, spacing, serif) * align;
        if (spacing == 0)
        {
            Batch.DrawString(font, text, At(start, y), color, 0, Vector2.Zero,
                factor * Scale, SpriteEffects.None, 0);
            return;
        }
        foreach (char c in text)
        {
            string glyph = c.ToString();
            Batch.DrawString(font, glyph, At(start, y), color, 0, Vector2.Zero,
                factor * Scale, SpriteEffects.None, 0);
            start += font.MeasureString(glyph).X * factor + spacing;
        }
    }

    internal float Paragraph(string text, float x, float y, float width, float size, Color color,
        float lineHeight = 24)
    {
        foreach (string line in WrapParagraph(text, width, size))
        {
            Text(line, x, y, size, color);
            y += lineHeight;
        }
        return y;
    }
}
