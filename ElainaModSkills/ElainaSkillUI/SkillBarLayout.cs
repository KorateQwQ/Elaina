using System;
using System.IO;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using Terraria;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI;

/// <summary>
/// 战斗技能栏的位置存档。属于整个模组的客户端偏好，所有角色、所有世界共用，
/// 不写进 ModPlayer；文件放在 tML 的 ModConfigs 目录下。
/// </summary>
internal static class SkillBarLayout
{
    /// <summary>默认停在屏幕右下角附近。</summary>
    public static Vector2 DefaultAnchor => new(.88f, .94f);

    private static Vector2? _anchor;

    private static string FilePath => Path.Combine(Main.SavePath, "ModConfigs", "伊蕾娜_SkillBarLayout.json");

    /// <summary>
    /// 技能栏在屏幕可移动范围内的相对位置（0~1，即 SUI 的 alignment）。
    /// 用比例而非像素保存，切换分辨率或界面缩放后技能栏仍完整留在屏幕内。
    /// </summary>
    public static Vector2 Anchor => _anchor ??= Load();

    /// <summary>把技能栏左上角放到 <paramref name="topLeft"/>（UI 坐标），超出屏幕的部分会被夹回。</summary>
    public static void MoveTo(Vector2 topLeft, Vector2 screen, Vector2 size)
    {
        Vector2 room = screen - size;
        _anchor = new Vector2(
            room.X > 0 ? Math.Clamp(topLeft.X / room.X, 0, 1) : DefaultAnchor.X,
            room.Y > 0 ? Math.Clamp(topLeft.Y / room.Y, 0, 1) : DefaultAnchor.Y);
    }

    public static void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            var json = new JObject { ["AnchorX"] = Anchor.X, ["AnchorY"] = Anchor.Y };
            File.WriteAllText(FilePath, json.ToString());
        }
        catch (Exception e)
        {
            ModContent.GetInstance<global::伊蕾娜.伊蕾娜>().Logger.Warn($"技能栏位置保存失败：{e.Message}");
        }
    }

    private static Vector2 Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return DefaultAnchor;
            var json = JObject.Parse(File.ReadAllText(FilePath));
            float x = json.Value<float>("AnchorX");
            float y = json.Value<float>("AnchorY");
            if (!float.IsFinite(x) || !float.IsFinite(y)) return DefaultAnchor;
            return new Vector2(Math.Clamp(x, 0, 1), Math.Clamp(y, 0, 1));
        }
        catch (Exception e)
        {
            ModContent.GetInstance<global::伊蕾娜.伊蕾娜>().Logger.Warn($"技能栏位置读取失败，使用默认位置：{e.Message}");
            return DefaultAnchor;
        }
    }
}
