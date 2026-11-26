using System;

namespace 伊蕾娜.ElainaModSkills.Skills.Ice;

// 相对确认施法的游戏帧；所有客户端和服务器使用同一排程，避免镜头影响命中时刻。
public static class ShardbindSequence
{
    public const int TightenTicks = 21;
    public const int FrostTicks = 54;
    public const int LockTicks = TightenTicks + FrostTicks;
    public const int LaunchTick = LockTicks;
    public static int SealTick => LaunchTick - 6;
    public const int VolleySpan = 28;
    public const int ChargeLeadTicks = 24;
    public const int SettledTick = LaunchTick + VolleySpan + 8;
    public const int FirstImpactTick = LaunchTick + 3;
    public const int LastImpactTick = LaunchTick + VolleySpan + 3;
    public const int ChargeStartTick = 92;
    public const int BurstTick = ChargeStartTick + 72;
    private static readonly int[] shotOffsets = BuildShotOffsets();

    public static int ShotOffset(int index) => shotOffsets[index];

    private static int[] BuildShotOffsets()
    {
        int[] slots = [0, 2, 3, 6, 7, 9, 12, 13, 16, 18, 19, 23, 24, 26, VolleySpan];
        // 独立固定种子便于比较昼夜与不同目标；只打乱发射，不影响生成、布局和粒子随机源。
        var random = new global::System.Random(7319);
        for (int i = slots.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (slots[i], slots[j]) = (slots[j], slots[i]);
        }
        return slots;
    }
}
