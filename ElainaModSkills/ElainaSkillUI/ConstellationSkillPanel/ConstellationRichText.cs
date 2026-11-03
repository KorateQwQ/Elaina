using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using KL.Drawing;
using KL.Drawing.Snippets;
using Microsoft.Xna.Framework;
using ReLogic.Graphics;
using SilkyUIFramework.Helper;
using Terraria.GameContent;
using Terraria.GameContent.UI.Chat;
using Terraria.UI.Chat;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationSkillPanel;

// Preserve KL snippets and the actual font while specifying the HTML line-height.
// Text is laid out with the font baked for the on-screen size, so wrapping matches what is drawn.
internal sealed class ConstellationRichText
{
    private readonly List<List<TextSnippet>> _lines = [];
    private readonly float _scale, _size, _device, _lineHeight;
    private readonly ConstellationFont _pixels;
    // Registered KL terms (icon plus same-colored name) in paragraph coordinates, for hover explanations.
    internal readonly List<(Rectangle Area, KLTerm Term)> Terms = [];
    internal float Height => _lines.Count * _lineHeight;
    // Widest line in design units, so tooltips can shrink to their content.
    internal float Width { get; private set; }

    // `device` is the drawing's device pixels per design unit; rebuild the paragraph when it changes.
    internal ConstellationRichText(string text, float width, float size, float lineHeight, Color color, ConstellationDrawing drawing)
    {
        size = ConstellationDrawing.Legible(size);
        var font = FontManager.HarmonyOS_Sans_SC.Value;
        _scale = size / Math.Max(1, font.MeasureString("国").X);
        _size = size;
        _device = drawing.Device;
        _pixels = drawing.Sans;
        _lineHeight = lineHeight;
        var source = new List<TextSnippet>().Parse(text, color).ConvertPlainSnippet();
        var line = new List<TextSnippet>();
        float used = 0;
        void Commit()
        {
            FindTerms(line, font, color);
            _lines.Add(line);
            Width = Math.Max(Width, used);
            line = []; used = 0;
        }
        void Add(TextSnippet snippet, float reserve = 0)
        {
            float advance = (snippet.UniqueDraw(true, out _, null) ? snippet.GetStringLength(font) * _scale
                : TextWidth(snippet.Text, snippet.Scale)) + font.CharacterSpacing * snippet.Scale * _scale;
            if (line.Count > 0 && used + advance + reserve > width)
            {
                Commit();
                // The space that overflowed a line must not indent the next one.
                if (snippet is not KLTextureSnippet && string.IsNullOrWhiteSpace(snippet.Text)) return;
            }
            line.Add(snippet); used += advance;
        }
        foreach (var snippet in source)
        {
            if (snippet is KLTextureSnippet texture)
                texture.SetFontScale(font.LineSpacing / (float)Math.Max(1, FontAssets.MouseText.Value.LineSpacing), 10.5f);
            // Keep a term's icon and its (short) name together on one line.
            if (snippet is KLTextureSnippet { Path: var path } && KLTerms.Find(path) != null)
            {
                int next = source.IndexOf(snippet) + 1;
                Add(snippet, next < source.Count ? TextWidth(source[next].Text) + font.CharacterSpacing * _scale : 0);
                continue;
            }
            if (snippet is not PlainTagHandler.PlainSnippet) { Add(snippet); continue; }
            foreach (Match match in Regex.Matches(snippet.Text, @"[A-Za-z0-9]+(?:['’-][A-Za-z0-9]+)*|[^\r]"))
            {
                string token = match.Value;
                if (token == "\n") { Commit(); continue; }
                if (line.Count == 0 && string.IsNullOrWhiteSpace(token)) continue;
                int next = match.Index + match.Length;
                float reserve = next < snippet.Text.Length && "，。！？；：、”」）%".Contains(snippet.Text[next])
                    ? TextWidth(snippet.Text[next].ToString()) + font.CharacterSpacing * _scale : 0;
                var part = snippet.Copy(token);
                if (TextWidth(token, part.Scale) <= width) Add(part, reserve);
                else foreach (char c in token) Add(snippet.Copy(c.ToString()));
            }
        }
        if (line.Count > 0) Commit();
    }

    // Design-unit width of plain text, measured with the font drawn at this on-screen size.
    private float TextWidth(string text, float snippetScale = 1)
        => _pixels.Measure(text, _size * snippetScale * _device) / _device;

    // Mirrors SnippetModule.DrawText advances in design units: character spacing plus each snippet's width.
    private void FindTerms(List<TextSnippet> line, DynamicSpriteFont font, Color baseColor)
    {
        float x = 0, start = 0;
        KLTerm term = null;
        Color? name = null;
        void Close(float end)
        {
            if (term != null && name != null)
                Terms.Add((new Rectangle((int)MathF.Floor(start), (int)MathF.Floor(_lines.Count * _lineHeight),
                    (int)MathF.Ceiling(end - start), (int)MathF.Ceiling(_lineHeight)), term));
            term = null; name = null;
        }
        foreach (var snippet in line)
        {
            float width = snippet.UniqueDraw(true, out var size, null, default, default, _scale * snippet.Scale)
                ? size.X : TextWidth(snippet.Text, snippet.Scale);
            if (snippet is KLTextureSnippet texture)
            {
                Close(x);
                term = KLTerms.Find(texture.Path);
                start = x;
            }
            else if (term != null)
            {
                name ??= snippet.Color == baseColor ? null : snippet.Color;
                if (name != snippet.Color) Close(x);
            }
            x += width + font.CharacterSpacing * _scale * snippet.Scale;
        }
        Close(x);
    }

    // Same advances as SnippetModule.DrawText, with text measured and drawn by the on-screen size font.
    internal void Draw(ConstellationDrawing drawing)
    {
        var font = FontManager.HarmonyOS_Sans_SC.Value;
        var pixels = _pixels;
        float inset = Math.Max(0, (_lineHeight - font.LineSpacing * _scale) / 2);
        for (int i = 0; i < _lines.Count; i++)
        {
            float x = 0;
            foreach (var snippet in _lines[i])
            {
                snippet.Update();
                var color = Color.FromNonPremultiplied(snippet.GetVisibleColor().ToVector4());
                var position = drawing.At(x, inset + i * _lineHeight);
                float width;
                if (snippet.UniqueDraw(false, out var size, drawing.Batch, position, color, _scale * snippet.Scale * drawing.Scale))
                    width = size.X / drawing.Scale;
                else
                {
                    pixels.Draw(drawing.Batch, snippet.Text, position, color, _size * snippet.Scale * _device, drawing.Transform);
                    width = TextWidth(snippet.Text, snippet.Scale);
                }
                x += width + font.CharacterSpacing * _scale * snippet.Scale;
            }
        }
    }
}
