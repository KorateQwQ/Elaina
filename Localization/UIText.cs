using Terraria.Localization;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace 伊蕾娜.Localization;

// Resolve on use so UI text follows language changes without reloading the mod.
internal static class UIText
{
    internal static string LocalizeUI(string key, params object[] args)
        => Language.GetTextValue("Mods.伊蕾娜.UI." + key, args);

    internal static string CatalogText(string value)
        => value != null && value.StartsWith("Catalog.", global::System.StringComparison.Ordinal) ? LocalizeUI(value) : value;

    internal static string[] WrapUIParagraph(string text, float width, Func<string, float> measure)
    {
        var lines = new List<string>();
        string line = "";
        void Append(string token)
        {
            if (line.Length > 0 && measure(line + token) > width)
            {
                if (line.Length > 1 && "，。！？；：、”」）".Contains(token))
                {
                    lines.Add(line[..^1]); line = line[^1].ToString();
                }
                else { lines.Add(line.TrimEnd()); line = ""; }
            }
            if (line.Length > 0 || !string.IsNullOrWhiteSpace(token)) line += token;
        }
        foreach (Match match in Regex.Matches(text ?? "", @"[A-Za-z0-9]+(?:['’-][A-Za-z0-9]+)*|[^\r]"))
        {
            string token = match.Value;
            if (token == "\n") { lines.Add(line.TrimEnd()); line = ""; continue; }
            if (measure(token) <= width) Append(token);
            else foreach (char c in token) Append(c.ToString());
        }
        if (line.Length > 0 || lines.Count == 0) lines.Add(line.TrimEnd());
        return lines.ToArray();
    }
}
