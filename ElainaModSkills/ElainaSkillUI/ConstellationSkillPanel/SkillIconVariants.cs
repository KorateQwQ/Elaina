using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI;

// All textures are owned by ModContent. Missing optional artwork is cached too.
internal sealed class SkillIconVariants
{
    private readonly Dictionary<string, Asset<Texture2D>> _uncolored = [];

    internal (Texture2D Texture, bool Uncolored) Resolve(Texture2D original, string path, bool uncolored)
    {
        if (!uncolored || string.IsNullOrWhiteSpace(path)) return (original, false);
        if (!_uncolored.TryGetValue(path, out var asset))
        {
            ModContent.RequestIfExists<Texture2D>(path, out asset, AssetRequestMode.ImmediateLoad);
            _uncolored[path] = asset;
        }
        return asset == null ? (original, false) : (asset.Value, true);
    }
}
