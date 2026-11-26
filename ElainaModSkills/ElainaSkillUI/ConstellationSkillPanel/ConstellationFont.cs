using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria.ModLoader;
using 伊蕾娜.ElainaModSkills.UICore;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationSkillPanel;

// Game assets stay owned by ModContent; the same raster font renderer is linked by FNA previews.
internal sealed class ConstellationFont(Mod mod, string family, Asset<DynamicSpriteFont> master, int minimum, int maximum)
    : UIRasterFont(family, master.Value, minimum, maximum, mod.GetFileBytes,
        path => ModContent.Request<Texture2D>("伊蕾娜/" + path, AssetRequestMode.ImmediateLoad).Value);
