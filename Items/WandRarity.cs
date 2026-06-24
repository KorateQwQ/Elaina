using System;

namespace 伊蕾娜.Items;

public class WandRarity : ModRarity
{
    public override Color RarityColor =>GetWandColor();

    static Color GetWandColor()
    {
        float 渐变 = 60 - (float)Main.GameUpdateCount % 120;//60到 -60
        Color c2 = Color.Lerp( new(255, 48, 215, 255), new(255, 100, 239, 255), (float)Math.Abs(渐变) / 60f);
        return c2;
    }
    public override int GetPrefixedRarity(int offset, float valueMult) {
        //不受前缀影响，魔杖也不可以打前缀。
        return Type; 
    }
}