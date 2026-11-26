using System;
using Terraria.Localization;

namespace 伊蕾娜.Localization;

/// <summary>技能描述中伤害与治疗数值的着色类型。</summary>
public enum SkillValueKind
{
    Fire,
    Ice,
    Water,
    Magic,
    Heal,
}

/// <summary>
/// 技能描述里的伤害/治疗数值：数值连同类型名整段着色（如“96水元素伤害”），作为格式参数传入描述。
/// 颜色只在这里维护，中英文 HJSON 不写色值；用法见 Localization/README.md。
/// </summary>
internal static class SkillValueText
{
    private static string Hex(SkillValueKind kind) => kind switch
    {
        SkillValueKind.Fire => "FFB45C",
        SkillValueKind.Ice => "B5F1FF",
        SkillValueKind.Water => "5FA2FF",
        SkillValueKind.Magic => "D9B8FF",
        SkillValueKind.Heal => "8CEB6E",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    /// <summary>整段伤害短语，如“96水元素伤害”/“96 Water damage”。</summary>
    internal static string Damage(int value, SkillValueKind kind) => kind == SkillValueKind.Heal
        ? throw new ArgumentOutOfRangeException(nameof(kind), kind, "Use Heal for healing values.")
        : Colorize(Language.GetTextValue($"Mods.伊蕾娜.SkillInfo.Values.{kind}Damage", value), kind);

    /// <summary>整段治疗短语，如“100生命”/“100 life”。</summary>
    internal static string Heal(int value) =>
        Colorize(Language.GetTextValue("Mods.伊蕾娜.SkillInfo.Values.Heal", value), SkillValueKind.Heal);

    /// <summary>仅着色数值，用于句中已说明伤害类型的位置（如“伤害改为X”）。</summary>
    internal static string Number(int value, SkillValueKind kind) => Colorize(value.ToString(), kind);

    private static string Colorize(string text, SkillValueKind kind) => $"[c/{Hex(kind)}:{text}]";
}
