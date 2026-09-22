using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace 伊蕾娜.ElainaModAlchemy.Crafting;

/// <summary>灰赝尘只影响当前玩家的本地合成偏好，不参与联机同步。</summary>
public sealed class FacsimileCraftingPlayer : ModPlayer
{
    public bool PreferDust { get; private set; }

    public override void Initialize() => PreferDust = false;

    internal void TogglePriority() => PreferDust = !PreferDust;

    public override void SaveData(TagCompound tag)
    {
        if (PreferDust)
            tag["PreferFacsimileDust"] = true;
    }

    public override void LoadData(TagCompound tag)
    {
        PreferDust = tag.TryGet("PreferFacsimileDust", out bool preferDust) && preferDust;
    }
}
