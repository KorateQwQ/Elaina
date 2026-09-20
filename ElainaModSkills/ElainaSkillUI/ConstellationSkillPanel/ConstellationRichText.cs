using System;
using System.Collections.Generic;
using KL.Drawing;
using KL.Drawing.Snippets;
using Microsoft.Xna.Framework;
using SilkyUIFramework.Components;
using SilkyUIFramework.Helper;
using Terraria.GameContent;
using Terraria.GameContent.UI.Chat;
using Terraria.UI.Chat;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationSkillPanel;

// Preserve KL snippets and the actual font while specifying the HTML line-height.
internal sealed class ConstellationRichText
{
    private readonly List<SnippetModule> _lines = [];
    private readonly float _scale, _lineHeight;
    internal float Height => _lines.Count * _lineHeight;

    internal ConstellationRichText(string text, float width, float size, float lineHeight, Color color)
    {
        var font = FontManager.HarmonyOS_Sans_SC.Value;
        _scale = size / Math.Max(1, font.MeasureString("国").X);
        _lineHeight = lineHeight;
        var source = new List<TextSnippet>().Parse(text, color).ConvertPlainSnippet();
        var line = new List<TextSnippet>();
        float used = 0;
        void Commit()
        {
            var module = new SnippetModule();
            module.UpdateProperties(font, float.MaxValue, 0);
            module.FromSnippets(line);
            _lines.Add(module);
            line = []; used = 0;
        }
        void Add(TextSnippet snippet, float reserve = 0)
        {
            float advance = (snippet.GetStringLength(font) + font.CharacterSpacing * snippet.Scale) * _scale;
            if (line.Count > 0 && used + advance + reserve > width) Commit();
            line.Add(snippet); used += advance;
        }
        foreach (var snippet in source)
        {
            if (snippet is KLTextureSnippet texture)
                texture.SetFontScale(font.LineSpacing / (float)Math.Max(1, FontAssets.MouseText.Value.LineSpacing), 10.5f);
            if (snippet is not PlainTagHandler.PlainSnippet) { Add(snippet); continue; }
            for (int i = 0; i < snippet.Text.Length; i++)
            {
                char c = snippet.Text[i];
                if (c == '\r') continue;
                if (c == '\n') { Commit(); continue; }
                float reserve = i + 1 < snippet.Text.Length && "，。！？；：、”」）".Contains(snippet.Text[i + 1])
                    ? (font.MeasureString(snippet.Text[i + 1].ToString()).X + font.CharacterSpacing) * _scale : 0;
                Add(snippet.Copy(c.ToString()), reserve);
            }
        }
        if (line.Count > 0) Commit();
    }

    internal void Draw(ConstellationDrawing drawing)
    {
        var font = FontManager.HarmonyOS_Sans_SC.Value;
        float inset = Math.Max(0, (_lineHeight - font.LineSpacing * _scale) / 2);
        for (int i = 0; i < _lines.Count; i++)
            _lines[i].DrawText(drawing.Batch, font, drawing.At(0, inset + i * _lineHeight), Color.White,
                0, Vector2.Zero, new Vector2(_scale * drawing.Scale), out _);
    }
}
