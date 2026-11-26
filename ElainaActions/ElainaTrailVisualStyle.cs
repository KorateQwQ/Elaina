using System;
using KL.Drawing;
using KL.Extensions;
using KL.Utils;
using Terraria.DataStructures;
using Terraria.GameContent;

namespace 伊蕾娜.ElainaActions;

public static class ElainaTrailVisualStyle
{
    private static Texture2D defaultTrailTexture;
    private static Texture2D defaultStarLineTexture;
    private static Texture2D defaultStarCrossTexture;

    private static Texture2D starHaloTexture;
    private static Texture2D GetStarHaloTexture() => starHaloTexture ??=
        ModContent.Request<Texture2D>("伊蕾娜/ElainaActions/Assets/WandStarHalo", AssetRequestMode.ImmediateLoad).Value;

    public static Color DefaultWandTrailColor = new(255, 160, 239, 255);
    public static Color DefaultSlashTrailColor = new(255, 100, 255, 255);
    public static Color DefaultStarColor => new(255, 180, 255, 255);
    public static Color WaterWandColor => new(100, 180, 255, 255);
    public static Color FireWandColor => new(255, 150, 40, 255);
    public static float DefaultStarIntensity => 2.0f;
    public static float DefaultTrailMaxWidth = 5f;
    public static float DefaultTrailEndWidth = 0f;
    public static float DefaultTrailFadeInEnd = 0.2f;
    public static float DefaultTrailFadeOutStart = 0.8f;
    public static float DefaultStarFadeInEnd = 0.2f;
    public static float DefaultStarFadeOutStart = 0.8f;
    public static float DefaultStarBlinkMin = 0.5f;
    public static float DefaultStarBlinkMax = 1f;
    public static float DefaultStarBlinkCycle = 60f;
    public static Vector2 DefaultStarCrossScale = new(0.05f, 0.03f);
    public static Vector2 DefaultStarLineScale = new(0.2f, 0.2f);
    public static Vector2 DefaultStarLineScale2 = new(0.1f, 0.2f);
    public static float DefaultStarLineRotation = MathHelper.PiOver2;

    public static Texture2D GetTrailTexture(Texture2D texture = null)
    {
        if (texture != null)
        {
            return texture;
        }

        defaultTrailTexture ??= ModContent.Request<Texture2D>("KL/Effects/Tex/Trail/LightTrail", AssetRequestMode.ImmediateLoad).Value;
        return defaultTrailTexture;
    }

    public static Texture2D GetStarLineTexture(Texture2D texture = null)
    {
        if (texture != null)
        {
            return texture;
        }

        defaultStarLineTexture ??= ModContent.Request<Texture2D>("KL/Effects/Tex/Sparkle/ShotLine", AssetRequestMode.ImmediateLoad).Value;
        return defaultStarLineTexture;
    }

    public static Texture2D GetStarCrossTexture(Texture2D texture = null)
    {
        if (texture != null)
        {
            return texture;
        }

        defaultStarCrossTexture ??= ModContent.Request<Texture2D>("KL/Effects/Tex/Sparkle/T_StartFlare001", AssetRequestMode.ImmediateLoad).Value;
        return defaultStarCrossTexture;
    }

    public static float GetActionAlpha(float actionProgress, float? fadeInEnd = null, float? fadeOutStart = null)
    {
        float fadeIn = fadeInEnd ?? DefaultTrailFadeInEnd;
        float fadeOut = fadeOutStart ?? DefaultTrailFadeOutStart;
        float entrance = fadeIn > 0f ? SmoothOpacity(actionProgress / fadeIn) : 1f;
        float exit = fadeOut < 1f ? SmoothOpacity((1f - actionProgress) / (1f - fadeOut)) : 1f;
        return entrance * exit;
    }

    internal static float SmoothOpacity(float progress)
    {
        float t = MathHelper.Clamp(progress, 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    public static void DrawTrail(Vector2[] points, Vector2 attachPoint, float attachRotation, float actionProgress,
        Color trailColor, float trailMaxWidth, float trailEndWidth, float? fadeInEnd = null, float? fadeOutStart = null,
        Texture2D texture = null, bool debugPoint = false, int blendState = 0)
    {
        DrawTrail(points, null, attachPoint, attachRotation, actionProgress, trailColor, trailMaxWidth, trailEndWidth,
            fadeInEnd, fadeOutStart, texture, debugPoint, blendState);
    }

    public static void DrawTrail(Vector2[] points, float[] widths, Vector2 attachPoint, float attachRotation, float actionProgress,
        Color trailColor, float trailMaxWidth, float trailEndWidth, float? fadeInEnd = null, float? fadeOutStart = null,
        Texture2D texture = null, bool debugPoint = false, int blendState = 0)
    {
        texture = GetTrailTexture(texture);
        if (texture == null || points == null || points.Length < 2)
        {
            return;
        }

        DrawTrailOpacity(points, widths, attachPoint, attachRotation, trailColor, trailMaxWidth, trailEndWidth,
            GetActionAlpha(actionProgress, fadeInEnd, fadeOutStart), texture, debugPoint, blendState);
    }

    internal static void DrawTrailOpacity(Vector2[] points, float[] widths, Vector2 attachPoint, float attachRotation,
        Color trailColor, float trailMaxWidth, float trailEndWidth, float opacity,
        Texture2D texture = null, bool debugPoint = false, int blendState = 0)
    {
        float alpha = MathHelper.Clamp(opacity, 0f, 1f);
        if (alpha <= 0f || points == null || points.Length < 2)
            return;
        // LightTrail 的 PNG 已含横截面亮度。只预乘时间透明度；KL 在顶点中预乘尾部透明度。
        bool premultiplied = blendState == 0;
        float brightness = premultiplied ? alpha : 1f;
        TrailEffect(GetTrailTexture(texture), points, widths,
            new Vector4(trailColor.ToVector3() * (3.5f * brightness), alpha),
            new Vector4(trailColor.ToVector3() * (5.5f * brightness), alpha),
            trailMaxWidth * 0.5f, trailEndWidth, startAlpha: 1f, endAlpha: 0f,
            attachPoint: attachPoint, attachRotation: attachRotation, blendState: blendState,
            debugPoint: debugPoint, premultiplyOpacity: premultiplied);
    }

    internal static void RequestTrail(Action drawTrail, bool useBloom = true, bool behindPlayer = false)
    {
        bool foreground = useBloom && !behindPlayer;
        RequestVisual(useBloom, "Elaina:ActionTrail",
            foreground ? LayerDrawRequestSystem.DrawTargetLayer.OverPlayers : LayerDrawRequestSystem.DrawTargetLayer.Projectiles,
            foreground ? LayerDrawRequestSystem.DrawTiming.Before : LayerDrawRequestSystem.DrawTiming.After, () =>
            {
                drawTrail();
                EndBeginDraw();
            }, order: LayerDrawRequestSystem.TrailOrder);
    }

    private static void RequestVisual(bool useBloom, string id, LayerDrawRequestSystem.DrawTargetLayer layer,
        LayerDrawRequestSystem.DrawTiming timing, Action draw, int order)
    {
        if (useBloom) LayerDrawRequestSystem.RequestBloom(id, layer, timing, draw, order);
        else LayerDrawRequestSystem.Request(id, layer, timing, draw, order);
    }

    public static void DrawWandTrail(Vector2[] points, float actionProgress)
    {
        DrawTrail(points, Vector2.Zero, 0f, actionProgress, DefaultWandTrailColor, DefaultTrailMaxWidth,
            DefaultTrailEndWidth, DefaultTrailFadeInEnd, DefaultTrailFadeOutStart);
    }

    public static void DrawSlashTrail(Vector2[] points, Vector2 attachPoint, float attachRotation, float actionProgress,
        bool debugPoint = false)
    {
        DrawTrail(points, attachPoint, attachRotation, actionProgress, DefaultSlashTrailColor, DefaultTrailMaxWidth,
            DefaultTrailEndWidth, DefaultTrailFadeInEnd, DefaultTrailFadeOutStart, debugPoint: debugPoint);
    }

    public static void DrawDefaultStar(ref PlayerDrawSet drawInfo, Vector2 starCenter, float actionProgress)
    {
        DrawStar(ref drawInfo, starCenter, actionProgress, DefaultStarColor, DefaultStarFadeInEnd,
            DefaultStarFadeOutStart, DefaultStarBlinkMin, DefaultStarBlinkMax, DefaultStarBlinkCycle,
            DefaultStarCrossScale, DefaultStarLineScale, DefaultStarLineScale2, DefaultStarLineRotation);
    }

    public static void DrawStar(ref PlayerDrawSet drawInfo, Vector2 starCenter, float actionProgress,
        Color starColor, float? fadeInEnd = null, float? fadeOutStart = null,
        float? blinkMin = null, float? blinkMax = null, float? blinkCycle = null,
        Vector2? crossScale = null, Vector2? lineScale = null, Vector2? lineScale2 = null,
        float? lineRotation = null, Texture2D crossTexture = null, Texture2D lineTexture = null)
    {
        if (drawInfo.shadow != 0f)
        {
            return;
        }

        QueueStar(starCenter, actionProgress, starColor, fadeInEnd, fadeOutStart, blinkMin, blinkMax,
            blinkCycle, crossScale, lineScale, lineScale2, lineRotation, crossTexture, lineTexture,
            true, LayerDrawRequestSystem.DustOrder + 1);
    }

    public static void RequestStar(Vector2 starCenter, float actionProgress,
        Color starColor, float? fadeInEnd = null, float? fadeOutStart = null,
        float? blinkMin = null, float? blinkMax = null, float? blinkCycle = null,
        Vector2? crossScale = null, Vector2? lineScale = null, Vector2? lineScale2 = null,
        float? lineRotation = null, Texture2D crossTexture = null, Texture2D lineTexture = null, bool drawHalo = true)
    {
        QueueStar(starCenter, actionProgress, starColor, fadeInEnd, fadeOutStart, blinkMin, blinkMax,
            blinkCycle, crossScale, lineScale, lineScale2, lineRotation, crossTexture, lineTexture,
            drawHalo, LayerDrawRequestSystem.DustOrder);
    }

    private static void QueueStar(Vector2 starCenter, float actionProgress, Color starColor,
        float? fadeInEnd, float? fadeOutStart, float? blinkMin, float? blinkMax, float? blinkCycle,
        Vector2? crossScale, Vector2? lineScale, Vector2? lineScale2, float? lineRotation,
        Texture2D crossTexture, Texture2D lineTexture, bool drawHalo, int order)
    {
        crossTexture = GetStarCrossTexture(crossTexture);
        lineTexture = GetStarLineTexture(lineTexture);
        if (crossTexture == null || lineTexture == null)
        {
            return;
        }

        crossScale ??= DefaultStarCrossScale;
        lineScale ??= DefaultStarLineScale;
        lineScale2 ??= DefaultStarLineScale2;

        float totalAlpha = GetActionAlpha(actionProgress, fadeInEnd, fadeOutStart);
        float blinkAlpha = PingPongWave(blinkMin ?? DefaultStarBlinkMin, blinkMax ?? DefaultStarBlinkMax,
            blinkCycle ?? DefaultStarBlinkCycle, Main.timeForVisualEffects, PingPongWaveType.SmoothStep);
        // Keep HDR RGB in a shader uniform; Color/DrawData would clamp it to 0..1.
        Vector4 color = new Vector4(starColor.ToVector3() * DefaultStarIntensity, starColor.A / 255f)
            * totalAlpha * blinkAlpha;
        Vector2 drawPosition = starCenter - Main.screenPosition;

        DrawData cross = new DrawData(crossTexture, drawPosition, null, Color.White, 0f, crossTexture.Origin(),
            crossScale.Value, SpriteEffects.None, 0) { ignorePlayerRotation = true };
        DrawData line = new DrawData(lineTexture, drawPosition, null, Color.White * 0.5f, 0f, lineTexture.Origin(),
            lineScale.Value, SpriteEffects.None, 0) { ignorePlayerRotation = true };
        DrawData rotatedLine = new DrawData(lineTexture, drawPosition, null, Color.White * 0.5f,
            lineRotation ?? DefaultStarLineRotation, lineTexture.Origin(), lineScale2.Value,
            SpriteEffects.None, 0) { ignorePlayerRotation = true };

        // 全组的星芒共用一个普通 Immediate 批次，每支杖依次绘制亮芯和柔光。
        LayerDrawRequestSystem.Request("Elaina:ActionStar", LayerDrawRequestSystem.DrawTargetLayer.OverPlayers,
            LayerDrawRequestSystem.DrawTiming.After, context =>
            {
                if (context.IsFirst) EndBeginDraw(0, 1);
                ReColorEffect(color);
                cross.Draw(Main.spriteBatch);
                line.Draw(Main.spriteBatch);
                rotatedLine.Draw(Main.spriteBatch);
                if (drawHalo && DrawSystem.GetShouldBloom())
                {
                    Texture2D halo = GetStarHaloTexture();
                    float pulse = totalAlpha * blinkAlpha;
                    ReColorEffect(new Vector4(starColor.ToVector3() * pulse * pulse, 0f));
                    Main.spriteBatch.Draw(halo, drawPosition, null, Color.White, 0f, halo.Origin(),
                        1f, SpriteEffects.None, 0f);
                }
                if (context.IsLast) EndBeginDraw();
            }, order: order);
    }
}
