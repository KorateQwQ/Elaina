using Terraria.ModLoader;

namespace 伊蕾娜.ElainaModSkills.Skills.Wind;

[Autoload(Side = ModSide.Client)]
internal sealed class WindVortexResources : ModSystem
{
    internal static Asset<Effect> Shader;
    internal static Asset<Texture2D> Noise;
    internal static Asset<Texture2D> Blade;
    internal static Asset<Texture2D> Haze;
    private string bladePath;
    public override void Load()
    {
        Shader = Mod.Assets.Request<Effect>("Effects/Content/WindVortex", AssetRequestMode.ImmediateLoad);
        Noise = Mod.Assets.Request<Texture2D>("ElainaModSkills/Skills/Wind/Assets/CoreNoise", AssetRequestMode.ImmediateLoad);
        Haze = Mod.Assets.Request<Texture2D>("ElainaModSkills/UICore/Assets/GlowDot", AssetRequestMode.ImmediateLoad);
        Reload();
    }
    private void Reload()
    {
        string path = WindVortexSettings.BladeTexture;
        if (path == bladePath) return;
        Blade = ModContent.Request<Texture2D>(path, AssetRequestMode.ImmediateLoad);
        bladePath = path;
    }
    public override void PostUpdateEverything() { if (Main.GameUpdateCount % 15 == 0) Reload(); }
    public override void Unload() { Shader = null; Haze = null; Noise = null; Blade = null; }
}
