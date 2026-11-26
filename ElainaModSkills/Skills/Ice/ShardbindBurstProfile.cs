using System;
using Microsoft.Xna.Framework;

namespace 伊蕾娜.ElainaModSkills.Skills.Ice;

// 冰凌锁终爆使用三组冰锥碎屑，复用预制图集、运动、旋转和淡出绘制。
internal static class ShardbindBurstProfile
{
    internal static IceShardPart[] Create(int seed, float direction)
    {
        int groups = ShardbindVisualSettings.ShardGroups;
        var parts = new IceShardPart[27 * groups];
        for (int group = 0; group < groups; group++)
        {
            var source = IceShardVisuals.Create(seed + group * 701, direction + MathHelper.ToRadians(ShardbindVisualSettings.ShardAngle) + group * MathHelper.TwoPi / groups);
            for (int i = 0; i < source.Length; i++)
            {
                var part = source[i];
                float scale = part.Variant < 9 ? ShardbindVisualSettings.ShardSize : ShardbindVisualSettings.ShardSparkSize;
                parts[group * source.Length + i] = part with
                {
                    Speed = part.Speed * ShardbindVisualSettings.ShardSpeed,
                    Length = part.Length * scale,
                    Width = part.Width * scale,
                    Life = Math.Min(part.Life * ShardbindVisualSettings.ShardLifeScale, ShardbindVisualSettings.ShardMaxLifeSeconds)
                };
            }
        }
        return parts;
    }
}
