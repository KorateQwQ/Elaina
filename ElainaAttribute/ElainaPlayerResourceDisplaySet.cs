using System;
using KL.Drawing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.ResourceSets;
using Terraria.Localization;
using Terraria.ModLoader;
using 伊蕾娜.ElainaAttribute;

namespace 伊蕾娜.ElainaAttribute;

public class ElainaPlayerResourceDisplaySet : ModResourceDisplaySet
{
    private PlayerStatsSnapshot snapshot;
    private Rectangle lifeArea;
    private Rectangle manaArea;
    private LocalizedText lifeText;
    private LocalizedText manaText;

    private static readonly Color BorderColor = new Color(237, 240, 243);
    private static readonly Color BackgroundColor = new Color(35, 32, 42) * 0.85f;
    private static readonly Color LifeColor = new Color(220, 72, 108);
    private static readonly Color ManaColor = new Color(88, 128, 255);
    private static readonly Color LifeBackColor = new Color(93, 48, 66) * 0.9f;
    private static readonly Color ManaBackColor = new Color(43, 55, 96) * 0.9f;
    private static float ResourceDecaySpeed = 0.01f;
    private float lifeDecayPercent = -1f;
    private float manaDecayPercent = -1f;

    public override void SetStaticDefaults()
    {
        lifeText = this.GetLocalization(nameof(lifeText));
        manaText = this.GetLocalization(nameof(manaText));
    }

    public override void PreDrawResources(PlayerStatsSnapshot snapshot)
    {
        this.snapshot = snapshot;
        
    }

    public override void DrawLife(SpriteBatch spriteBatch)
    {
        ResourceDecaySpeed = 0.007f;
        DrawElainaIcon();

        Vector2 center = GetBaseCenter();
        string currentLifeText = $"{snapshot.Life:0.#}";
        string maxLifeText = $"{snapshot.LifeMax:0.#}";
        lifeArea = DrawBar(spriteBatch, center, new Vector2(285,15), snapshot.Life, snapshot.LifeMax, LifeBackColor, new Color(220, 102, 188,255), ref lifeDecayPercent);
        DrawCenteredText(spriteBatch, lifeText.Format(currentLifeText, maxLifeText), lifeArea, Color.White, 0.82f);
        
        Point mousePoint = Main.MouseScreen.ToPoint();
        
        DynamicSpriteFont font = FontManager.HarmonyOS_Sans_SC.Value;


        if (lifeArea.Contains(mousePoint))
        {
            float scale = 0.25f;
            string text = $"HP: {currentLifeText}/{maxLifeText}";
            Vector2 size = font.MeasureString(text) * scale;
            center = center - size * 0.5f;
            Main.spriteBatch.DrawString(font, text, center, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }

    public override void DrawMana(SpriteBatch spriteBatch)
    {
        bool UseUniqueMana = false;

        float currentMagicPoint = UseUniqueMana ?  Main.LocalPlayer.GetModPlayer<ElainaAttributeModPlayer>().MagicPoint : snapshot.Mana;
        float maxMagicPoint = UseUniqueMana ? Main.LocalPlayer.GetModPlayer<ElainaAttributeModPlayer>().MaxMagicPoint : snapshot.ManaMax;
        string currentMagicPointText = $"{currentMagicPoint:0.#}";
        string maxMagicPointText = $"{maxMagicPoint:0.#}";
        
        Vector2 center = GetBaseCenter() + new Vector2(0, 30f);
        manaArea = DrawBar(spriteBatch, center, new Vector2(285,15), currentMagicPoint, maxMagicPoint, ManaBackColor, new Color(100,210,255,255), ref manaDecayPercent);
        DrawCenteredText(spriteBatch, manaText.Format(currentMagicPointText, maxMagicPointText), manaArea, Color.White * 0.9f, 0.68f);
        
        Point mousePoint = Main.MouseScreen.ToPoint();
        DynamicSpriteFont font = FontManager.HarmonyOS_Sans_SC.Value;

        if (manaArea.Contains(mousePoint))
        {
            float scale = 0.25f;
            float magicPointRecovery = UseUniqueMana
                ? Main.LocalPlayer.GetModPlayer<ElainaAttributeModPlayer>().GetMagicPointRecovery()
                : Main.LocalPlayer.GetModPlayer<Elaina145ManaRegenPlayer>().NaturalManaRegenPerSecond;
            string text = $"MP: {currentMagicPointText}/{maxMagicPointText}   + {magicPointRecovery:0.#}/s";
            Vector2 size = font.MeasureString(text) * scale;
            center = center - size * 0.5f;
            Main.spriteBatch.DrawString(font, text, center, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
        //DrawExpBar(spriteBatch);
    }

    public void DrawExpBar(SpriteBatch spriteBatch)
    {
        Vector2 center = GetBaseCenter() + new Vector2(-50f, 40f);
        manaArea = DrawBar(spriteBatch, center, new Vector2(200,10), snapshot.Mana, snapshot.ManaMax, ManaBackColor, new Color(255,250,5,255));
        DrawCenteredText(spriteBatch, manaText.Format(snapshot.Mana, snapshot.ManaMax), manaArea, Color.White * 0.9f, 0.68f);
    }
    
    public override bool PreHover(out bool hoveringLife)
    {
        Point mousePoint = Main.MouseScreen.ToPoint();

        hoveringLife = false;
        
        return false;
        if (lifeArea.Contains(mousePoint))
        {
            hoveringLife = true;
            return true;
        }

        if (manaArea.Contains(mousePoint))
        {
            hoveringLife = false;
            return true;
        }

        hoveringLife = false;
        return false;
    }

    private static Vector2 GetBaseCenter()
    {
        return new Vector2(Main.screenWidth - 154f, 32f);
    }

    private static Rectangle DrawBar(SpriteBatch spriteBatch, Vector2 center, Vector2 size, float value, float maxValue, Color backColor, Color fillColor)
    {
        return DrawBar(spriteBatch, center, size, value, maxValue, backColor, fillColor, false, ref value);
    }

    private static Rectangle DrawBar(SpriteBatch spriteBatch, Vector2 center, Vector2 size, float value, float maxValue, Color backColor, Color fillColor, ref float decayPercent)
    {
        return DrawBar(spriteBatch, center, size, value, maxValue, backColor, fillColor, true, ref decayPercent);
    }

    private static Rectangle DrawBar(SpriteBatch spriteBatch, Vector2 center, Vector2 size, float value, float maxValue, Color backColor, Color fillColor, bool drawDecay, ref float decayPercent)
    {
        Vector2 topLeft = center - size * 0.5f;
        Rectangle borderRectangle = new Rectangle((int)topLeft.X, (int)topLeft.Y, (int)size.X, (int)size.Y);

        float percent = maxValue > 0 ? value / (float)maxValue : 0f;
        percent = MathHelper.Clamp(percent, 0f, 1f);
        if (drawDecay)
        {
            if (decayPercent < 0f || percent >= decayPercent)
            {
                decayPercent = percent;
            }
            else
            {
                decayPercent = MathHelper.Max(percent, decayPercent - ResourceDecaySpeed);
            }
        }
        
        
        EndBeginDrawUI();
        if (drawDecay && decayPercent > percent)
        {
            Vector2 decaySize = size-new Vector2(2);
            decaySize.X*=decayPercent;
            float decayOffset = size.X*(1-decayPercent)*0.5f;
            Color decayColor = Color.Lerp(fillColor, Color.White, 0.75f);
            //资源条减少后的衰减残影
            DrawCapsuleRectangle(center+new Vector2(-decayOffset,0),decaySize,decayColor,border:2.0f,filled:true,borderColor:Color.Black*0.0f,
                capsuleSharpness:0.7f,texture:null);
        }
        
        Vector2 size2 = size-new Vector2(2);
        size2.X*=percent;
        float offset =  size.X*(1-percent)*0.5f;
        //资源条内填充实际绘制
        DrawCapsuleRectangle(center+new Vector2(-offset,0),size2,fillColor,border:2.0f,filled:true,borderColor:Color.Black*0.0f,
            capsuleSharpness:0.7f,texture:null);
        
        //资源条外边框
        DrawCapsuleRectangle(center+new Vector2(0,0),size-new Vector2(2),fillColor,border:2.0f,filled:false,borderColor:Color.Black*0.5f,
            capsuleSharpness:0.7f,texture:null);
        DrawCapsuleRectangle(center+new Vector2(0,0),size,Color.White,border:1.0f,filled:false,borderColor:Color.White,
            capsuleSharpness:0.5f);

        float arrowSize = size.Y / 15;
        //DrawArrow(center + new Vector2(-size.X * 0.5f - 5f, 0f), -7f*arrowSize, 70f);
        //DrawArrow(center + new Vector2(size.X * 0.5f + 5f, 0f), 7f*arrowSize, 70f);
        if (fillColor == new Color(100, 210, 255, 255))
        {
            DrawCrossStar(center + new Vector2(-size.X * 0.5f-14 , -4f),new Vector2(15,18)*1,Color.White,0,0.0f,0.2f);
            DrawCrossStar(center + new Vector2(-size.X * 0.5f -8, 1f),new Vector2(14),Color.White,0,0.8f,0.2f);
        }
        
        //Main.spriteBatch = spriteBatch;

        return borderRectangle;
    }

    private static void DrawCenteredText(SpriteBatch spriteBatch, string text, Rectangle area, Color color, float scale)
    {
        scale = 0.4f;
        DynamicSpriteFont font = FontManager.HarmonyOS_Sans_SC.Value;
        Vector2 size = font.MeasureString(text) * scale;
        Vector2 position = area.Center.ToVector2() - size * 0.5f;

        //spriteBatch.DrawString(font, text, position + Vector2.One, Color.Black * 0.6f, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        //spriteBatch.DrawString(font, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    static void DrawElainaIcon()
    {
        Texture2D Elaina_Icon  =  ModContent.Request<Texture2D>("伊蕾娜/ElainaModSkills/ElainaSkillUI/Elaina_Icon3", AssetRequestMode.ImmediateLoad).Value;

        Vector2 center = GetBaseCenter();
        Color black =new Color(69,66,75);
        Vector2 offset = new Vector2(-210,18);
        DrawDiamond(center+offset,new Vector2(85),black,border:3,filled:true,borderColor:Color.White);

        DrawInScreen(Elaina_Icon,center+offset,scale: new Vector2(1.00f));
        DrawDiamond(center+offset,new Vector2(95),black,border:1.5f,filled:false,borderColor:Color.White);
        
    }

    private static void DrawArrow(Vector2 position, float size, float angleDegree)
    {
        Texture2D pixel = TextureAssets.MagicPixel.Value;
        Color color = Color.White;
        float direction = Math.Sign(size);
        float arrowSize = Math.Abs(size);
        float thickness = MathHelper.Clamp(arrowSize * 0.01f, 1f, 8f);
        float halfAngle = MathHelper.ToRadians(MathHelper.Clamp(angleDegree, 5f, 175f)) * 0.5f;
        float halfWidth = MathF.Cos(halfAngle) * arrowSize * 0.5f * direction;
        float halfHeight = MathF.Sin(halfAngle) * arrowSize;

        Vector2 tip = position + new Vector2(halfWidth, 0f);
        DrawLine(pixel, position + new Vector2(-halfWidth, -halfHeight), tip, color, thickness);
        DrawLine(pixel, position + new Vector2(-halfWidth, halfHeight), tip, color, thickness);
    }

    private static void DrawLine(Texture2D pixel, Vector2 start, Vector2 end, Color color, float thickness)
    {
        Vector2 line = end - start;
        Rectangle sourceRectangle = pixel.Frame();
        Vector2 origin = new Vector2(0f, sourceRectangle.Height * 0.5f);
        Vector2 scale = new Vector2(line.Length() / sourceRectangle.Width, thickness / sourceRectangle.Height);
        Main.spriteBatch.Draw(pixel, start, sourceRectangle, color, line.ToRotation(), origin, scale, SpriteEffects.None, 0f);
    }
    
}