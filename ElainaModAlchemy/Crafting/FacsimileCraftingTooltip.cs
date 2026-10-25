using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace 伊蕾娜.ElainaModAlchemy.Crafting;

public sealed class FacsimileCraftingTooltip : GlobalItem
{
    internal static readonly Color HintColor = new(205, 175, 255);
    internal const string KeyPrefix = "Mods.伊蕾娜.Alchemy.Crafting.";
    internal const string LineName = "FacsimileSubstitution";
    internal const string MarkerSeparator = "  ";

    private const string StarsPath = "伊蕾娜/ElainaModAlchemy/UI/Assets/NotebookStars";
    private const string GlowPath = "伊蕾娜/ElainaModSkills/ElainaSkillUI/ConstellationPreview/Assets/Glow";
    private static Asset<Texture2D> stars, glow;

    internal static string Marker => Language.GetTextValue(KeyPrefix + "FacsimileCraftable");

    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
    {
        if (!FacsimileCraftingHoverSystem.TryGetHoveredRecipe(item, out Recipe recipe) ||
            !ModContent.GetInstance<FacsimileCraftingSystem>().TryGetQuote(recipe, out var quote) ||
            quote.Dust <= 0)
            return;

        // 文字留在物品名一行里，让原版按它计算提示框宽度；绘制时再把标记分开画。
        TooltipLine name = tooltips.Find(line => line.Mod == "Terraria" && line.Name == "ItemName");
        if (name != null)
            name.Text += MarkerSeparator + Marker;

        // 左键仍按原版制作；只有右键会消耗灰赝尘。
        tooltips.Add(new TooltipLine(Mod, LineName, Language.GetTextValue(
            KeyPrefix + "FacsimileRightClick", quote.Dust))
        {
            OverrideColor = HintColor
        });
    }

    public override void Unload()
    {
        stars = null;
        glow = null;
    }

    public override bool PreDrawTooltipLine(Item item, DrawableTooltipLine line, ref int yOffset)
    {
        var position = new Vector2(line.X, line.Y);
        float time = Main.GlobalTimeWrappedHourly;
        if (line.Mod == Mod.Name && line.Name == LineName)
        {
            DrawHint(line, line.Text, position, line.OverrideColor ?? line.Color, time, 0.35f);
            return false;
        }

        string marker = MarkerSeparator + Marker;
        if (line.Mod != "Terraria" || line.Name != "ItemName" || !line.Text.EndsWith(marker))
            return true;

        // 物品名保留原版稀有度颜色与闪烁，标记按同一明暗换成淡紫色。
        Color nameColor = line.OverrideColor ?? line.Color;
        string name = line.Text[..^marker.Length];
        ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, line.Font, name, position, nameColor,
            line.Rotation, line.Origin, line.BaseScale, line.MaxWidth, line.Spread);
        float offset = ChatManager.GetStringSize(line.Font, name + MarkerSeparator, line.BaseScale).X;
        DrawHint(line, Marker, position + new Vector2(offset, 0), HintColor * (nameColor.A / 255f), time, 0f);
        return false;
    }

    private static void DrawHint(DrawableTooltipLine line, string text, Vector2 position, Color color, float time, float phase)
    {
        ChatManager.DrawColorCodedStringShadow(Main.spriteBatch, line.Font, text, position, new Color(0, 0, 0, color.A),
            line.Rotation, line.Origin, line.BaseScale, line.MaxWidth, line.Spread);
        stars ??= ModContent.Request<Texture2D>(StarsPath, AssetRequestMode.ImmediateLoad);
        glow ??= ModContent.Request<Texture2D>(GlowPath, AssetRequestMode.ImmediateLoad);
        FacsimileTooltipEffect.Draw(Main.spriteBatch, line.Font, stars.Value, glow.Value, text, position, color,
            line.BaseScale, Main.UIScale, time, phase);
    }
}
