using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace 伊蕾娜.炼金;

/// <summary>Legacy save compatibility for old spell proficiency; no alchemy runtime remains here.</summary>
public sealed class 炼金modplayer : ModPlayer
{
    private TagCompound _legacy = new();
    public int 炼金最大lv;
    public override void LoadData(TagCompound tag)
    {
        _legacy = tag;
        炼金最大lv = tag.GetInt("炼金最大lv");
    }
    public override void SaveData(TagCompound tag)
    {
        foreach (var pair in _legacy) tag[pair.Key] = pair.Value;
        tag["炼金最大lv"] = 炼金最大lv;
    }
}
