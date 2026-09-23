namespace 伊蕾娜.ElainaModAlchemy.item;

/// <summary>
/// 用 PotionLiquid 着色器给空药水瓶绘制流动液体。
/// 瓶子贴图与液体遮罩必须同尺寸；遮罩 R 通道为液体可见度，G 通道为瓶内区域。
/// 流动纹理采样 KL 的 PerlinX 噪声图（s2）。
/// </summary>
public static class PotionLiquidRenderer
{
    private const string Folder = "伊蕾娜/ElainaModAlchemy/item/";

    /// <summary>月露合剂的默认液体色；可在具体药剂中微调。</summary>
    public static readonly Color MoonDewColor = new(106, 82, 224, 255);

    /// <summary>RuriPotionEmpty 瓶的液面范围（贴图 V 坐标）：X 为装满时的液面，Y 为瓶底。</summary>
    public static readonly Vector2 RuriLiquidRange = new(0.26f, 0.94f);

    private static Asset<Effect> effect;
    private static Asset<Texture2D> ruriBottle;
    private static Asset<Texture2D> ruriMask;

    public static Effect Effect => (effect ??= ModContent.Request<Effect>(Folder + "PotionLiquid", AssetRequestMode.ImmediateLoad)).Value;
    public static Texture2D RuriBottle => (ruriBottle ??= ModContent.Request<Texture2D>(Folder + "RuriPotionEmpty", AssetRequestMode.ImmediateLoad)).Value;
    public static Texture2D RuriMask => (ruriMask ??= ModContent.Request<Texture2D>(Folder + "RuriPotionLiquidMask", AssetRequestMode.ImmediateLoad)).Value;

    /// <summary>设置着色器参数并应用。须在 Immediate 模式的 SpriteBatch 中、绘制瓶子贴图之前调用。</summary>
    /// <param name="liquidColor">液体颜色，A 为液体不透明度。</param>
    /// <param name="fillLevel">液量，0 为空，1 为满。</param>
    /// <param name="liquidRange">液面范围，默认使用 <see cref="RuriLiquidRange"/>。</param>
    public static void Apply(Texture2D mask, Color liquidColor, float fillLevel, float time,
        float flowSpeed = 1f, float waveStrength = 1f, Vector2? liquidRange = null)
    {
        Effect fx = Effect;
        fx.Parameters["LiquidColor"].SetValue(liquidColor.ToVector4());
        fx.Parameters["FillLevel"].SetValue(fillLevel);
        fx.Parameters["Time"].SetValue(time);
        fx.Parameters["FlowSpeed"].SetValue(flowSpeed);
        fx.Parameters["WaveStrength"].SetValue(waveStrength);
        fx.Parameters["TextureSize"].SetValue(new Vector2(mask.Width, mask.Height));
        fx.Parameters["LiquidRange"].SetValue(liquidRange ?? RuriLiquidRange);

        GraphicsDevice device = Main.instance.GraphicsDevice;
        device.Textures[1] = mask;
        device.SamplerStates[1] = SamplerState.PointClamp;
        device.Textures[2] = PerLinNoiseX;
        device.SamplerStates[2] = SamplerState.LinearWrap;
        fx.CurrentTechnique.Passes[0].Apply();
    }

    /// <summary>绘制装有液体的琉璃瓶。inUI 为 true 时用于物品栏等 UI，否则用于世界绘制（position 为屏幕坐标）。</summary>
    public static void DrawRuri(Vector2 position, Color drawColor, float rotation, Vector2 origin, float scale,
        Color liquidColor, float fillLevel, bool inUI, float flowSpeed = 1f, float waveStrength = 1f)
    {
        DrawPotion(RuriBottle, RuriMask, position, drawColor, rotation, origin, scale,
            liquidColor, fillLevel, inUI, flowSpeed, waveStrength);
    }

    /// <summary>
    /// 使用指定瓶身与液体遮罩绘制一瓶动态药剂。调用方应在物品绘制钩子中使用，
    /// 以便药剂图标和世界掉落物都能共享同一套液体效果。
    /// </summary>
    public static void DrawPotion(Texture2D bottle, Texture2D mask, Vector2 position, Color drawColor,
        float rotation, Vector2 origin, float scale, Color liquidColor, float fillLevel, bool inUI,
        float flowSpeed = 1f, float waveStrength = 1f, Vector2? liquidRange = null)
    {
        if (inUI) EndBeginDrawUI(0, 1);
        else EndBeginDraw(0, 1);

        Apply(mask, liquidColor, fillLevel, Main.GlobalTimeWrappedHourly, flowSpeed, waveStrength, liquidRange);
        Main.spriteBatch.Draw(bottle, position, null, drawColor, rotation, origin, scale, SpriteEffects.None, 0f);

        if (inUI) EndBeginDrawUI();
        else EndBeginDraw();
    }
}
