using System;
using System.Collections.Generic;
using System.Text;
using Terraria.Utilities;

namespace 伊蕾娜.System.EssenceSystemFolder.EssenceAffixFolder;

/// <summary>
/// 词条定义层（Definition Layer）
/// - 这里描述“有什么词条、词条属于哪一类、能不能出、怎么 roll”
/// - 不负责存档/网络同步，也不直接修改玩家属性
/// </summary>
public static class EssenceAffixRegistry
{
    private static readonly Dictionary<string, EssenceAffixDef> _defsByKey = new(StringComparer.Ordinal);
    private static readonly List<EssenceAffixDef> _defsByRuntimeId = new();

    /// <summary>
    /// 调试用：传入 state（BossState/等级），输出当前命中的“稀有度权重档位”以及各稀有度的概率。
    /// 注意：这里展示的是“权重表的理论概率”。实际 roll 时，如果某个稀有度池子里没有任何可用词条，
    /// 会被置 0 并重抽（最终概率会发生变化）。
    /// </summary>
    public static string DebugDescribeRarityChances(float state)
    {
        var s = (int)MathF.Floor(state);

        // 找到当前命中的档位（最后一个满足 MinState 的）
        var tierMinState = RarityWeightTiers[0].MinState;
        var weights = RarityWeightTiers[0].Weights;
        for (var i = 1; i < RarityWeightTiers.Length; i++)
        {
            if (s >= RarityWeightTiers[i].MinState)
            {
                tierMinState = RarityWeightTiers[i].MinState;
                weights = RarityWeightTiers[i].Weights;
            }
            else
            {
                break;
            }
        }

        var total =
            Math.Max(0, weights.White) +
            Math.Max(0, weights.Blue) +
            Math.Max(0, weights.Purple) +
            Math.Max(0, weights.Gold) +
            Math.Max(0, weights.Prismatic);

        static string Percent(int w, int totalWeight)
        {
            if (totalWeight <= 0)
                return "0%";

            var p = (float)Math.Max(0, w) / totalWeight;
            return (p * 100f).ToString("0.##") + "%";
        }

        var sb = new StringBuilder(128);
        sb.Append("当前state为").Append(state.ToString("0.##"))
            .Append("（向下取整=").Append(s).Append("），命中档位MinState=").Append(tierMinState).Append('。');

        sb.Append("\n白色稀有度概率：").Append(Percent(weights.White, total));
        sb.Append("\n蓝色稀有度概率：").Append(Percent(weights.Blue, total));
        sb.Append("\n紫色稀有度概率：").Append(Percent(weights.Purple, total));
        sb.Append("\n金色稀有度概率：").Append(Percent(weights.Gold, total));
        sb.Append("\n棱彩稀有度概率：").Append(Percent(weights.Prismatic, total));

        sb.Append("\n（总权重=").Append(total).Append("）");
        return sb.ToString();
    }

    /// <summary>
    /// 按 Key 索引的定义表（Key 必须稳定，用于存档/网络同步）。
    /// </summary>
    public static IReadOnlyDictionary<string, EssenceAffixDef> DefsByKey => _defsByKey;

    /// <summary>
    /// 运行时顺序表（仅用于内存优化/数组索引，不要用于存档）。
    /// </summary>
    public static IReadOnlyList<EssenceAffixDef> DefsByRuntimeId => _defsByRuntimeId;

    public static void Clear()
    {
        _defsByKey.Clear();
        _defsByRuntimeId.Clear();
    }

    public static void Register(EssenceAffixDef def)
    {
        if (def == null)
            throw new ArgumentNullException(nameof(def));

        if (string.IsNullOrWhiteSpace(def.Key))
            throw new ArgumentException("词条 Key 不能为空（且应当稳定，用于存档）", nameof(def));

        if (_defsByKey.ContainsKey(def.Key))
            throw new ArgumentException($"重复注册词条：{def.Key}", nameof(def));

        def.RuntimeId = _defsByRuntimeId.Count;
        _defsByRuntimeId.Add(def);
        _defsByKey.Add(def.Key, def);
    }

    public static bool TryGet(string key, out EssenceAffixDef def) => _defsByKey.TryGetValue(key, out def);

    public static bool TryGet(int runtimeId, out EssenceAffixDef def)
    {
        if ((uint)runtimeId < (uint)_defsByRuntimeId.Count)
        {
            def = _defsByRuntimeId[runtimeId];
            return true;
        }

        def = null!;
        return false;
    }

    /// <summary>
    /// 稀有度权重
    /// - 这里用 BossState 当作“等级”。你可以自行把 BossState 映射为你想要的阶段/等级。
    /// - MinState 表示：当 BossState >= MinState 时，使用该档位权重（取最后一个满足条件的档位）。
    /// - 权重为 0 表示该稀有度完全不会出现。
    /// </summary>
    private static RarityWeightTier[] RarityWeightTiers =>
    new[]
    {
        // 兜底：任何时候至少能抽到 White（避免全 0 导致死）
        new RarityWeightTier(minState: 0, new EssenceRarityWeights(
            white: 100, blue: 0, purple: 0, gold: 0, prismatic: 0)),

        //史莱姆王
        new RarityWeightTier(minState: 1, new EssenceRarityWeights(
            white: 100, blue: 0, purple: 0, gold: 0, prismatic: 0)),
        //克眼
        new RarityWeightTier(minState: 2, new EssenceRarityWeights(
            white: 90, blue: 10, purple: 0, gold: 0, prismatic: 0)),
        //邪恶boss
        new RarityWeightTier(minState: 3, new EssenceRarityWeights(
            white: 70, blue: 30, purple: 0, gold: 0, prismatic: 0)),
        //蜂王
        new RarityWeightTier(minState: 4, new EssenceRarityWeights(
            white: 60, blue: 40, purple: 0, gold: 0, prismatic: 0)),
        //骷髅王，此时允许出现紫色（暴击，召唤栏相关）
        new RarityWeightTier(minState: 5, new EssenceRarityWeights(
            white: 50, blue: 40, purple: 10, gold: 0, prismatic: 0)),
        
        //肉山, 此时允许出现金色（生命偷取，伤害减免，无敌等特殊能力）
        new RarityWeightTier(minState: 7, new EssenceRarityWeights(
            white: 40, blue: 33, purple: 25, gold: 2, prismatic: 0)),
        //双子魔眼
        new RarityWeightTier(minState: 9, new EssenceRarityWeights(
            white: 30, blue: 30, purple: 35, gold: 5, prismatic: 0)),
        //世花
        new RarityWeightTier(minState: 12, new EssenceRarityWeights(
            white: 20, blue: 30, purple: 35, gold: 15, prismatic: 0)),
        //光女, 此时允许出现棱彩
        new RarityWeightTier(minState: 15, new EssenceRarityWeights(
            white: 10, blue: 20, purple: 45, gold: 24, prismatic: 1)),
        //月总后概率
        new RarityWeightTier(minState: 18, new EssenceRarityWeights(
            white: 10, blue: 15, purple: 40, gold: 33, prismatic: 3)),
    };

    private static EssenceRarityWeights GetRarityWeights(float bossState)
    {
        // BossState 可能是 float，但我们按“阶段/等级”来用它：>= 哪个档位就用哪个档位。
        // 如果你后续想要更细的映射，可以在这里改成你自己的规则（比如按某些 BossFlag 计算）。
        var state = (int)MathF.Floor(bossState);

        var weights = RarityWeightTiers[0].Weights;
        for (var i = 1; i < RarityWeightTiers.Length; i++)
        {
            if (state >= RarityWeightTiers[i].MinState)
                weights = RarityWeightTiers[i].Weights;
            else
                break;
        }

        return weights;
    }

    private static EssenceAffixRarity PickRarity(UnifiedRandom rand, Span<int> weightsByRarity)
    {
        var total = 0;
        for (var i = 0; i < weightsByRarity.Length; i++)
            total += Math.Max(0, weightsByRarity[i]);

        if (total <= 0)
            return EssenceAffixRarity.White;

        var roll = rand.Next(total);
        for (var i = 0; i < weightsByRarity.Length; i++)
        {
            var w = Math.Max(0, weightsByRarity[i]);
            if (w <= 0)
                continue;

            if (roll < w)
                return (EssenceAffixRarity)i;

            roll -= w;
        }

        // 理论上不会走到这里
        return EssenceAffixRarity.White;
    }

    private static bool TryPickRandomAffix(
        UnifiedRandom rand,
        in EssenceAffixRollContext ctx,
        EssenceAffixRarity rarity,
        out EssenceAffixDef def)
    {
        // 水塘抽样（reservoir sampling）：单次遍历、零临时分配，从所有满足条件的元素中等概率选 1 个。
        // 这样就不需要 Where/ToList，也不需要临时 candidates List。
        def = null!;
        var seen = 0;

        for (var i = 0; i < _defsByRuntimeId.Count; i++)
        {
            var d = _defsByRuntimeId[i];
            if (d.Rarity != rarity)
                continue;

            if (!d.CanRoll(ctx))
                continue;

            seen++;
            if (rand.Next(seen) == 0)
                def = d;
        }

        return seen > 0;
    }

    /// <summary>
    /// 从注册表中随机抽取一个满足条件的词条定义。
    /// - 如果传入 rarity：只在指定稀有度池子里抽。
    /// - 如果不传 rarity：将根据 BossState（等级）先抽“稀有度”，再去该稀有度池子里抽具体词条。
    /// </summary>
    public static bool TryRollRandomAffix(
        UnifiedRandom rand,
        in EssenceAffixRollContext ctx,
        out EssenceAffixDef def,
        EssenceAffixRarity? rarity = null)
    {
        // 1) 外部指定了稀有度：严格按指定稀有度抽
        if (rarity != null)
            return TryPickRandomAffix(rand, ctx, rarity.Value, out def);

        // 2) 未指定稀有度：按 BossState 权重表先抽稀有度
        var tierWeights = GetRarityWeights(ctx.BossState);

        // 注意：目前稀有度一共 5 档：White/Blue/Purple/Gold/Prismatic
        Span<int> weightsByRarity = stackalloc int[5];
        weightsByRarity[(int)EssenceAffixRarity.White] = tierWeights.White;
        weightsByRarity[(int)EssenceAffixRarity.Blue] = tierWeights.Blue;
        weightsByRarity[(int)EssenceAffixRarity.Purple] = tierWeights.Purple;
        weightsByRarity[(int)EssenceAffixRarity.Gold] = tierWeights.Gold;
        weightsByRarity[(int)EssenceAffixRarity.Prismatic] = tierWeights.Prismatic;

        // 如果某个稀有度池子里没有任何满足条件的词条，就把它的权重置 0 再重抽，最多尝试 5 次。
        for (var attempt = 0; attempt < weightsByRarity.Length; attempt++)
        {
            var pickedRarity = PickRarity(rand, weightsByRarity);
            if (TryPickRandomAffix(rand, ctx, pickedRarity, out def))
                return true;

            weightsByRarity[(int)pickedRarity] = 0;
        }

        def = null!;
        return false;
    }
}

/// <summary>
/// 某个“BossState >= MinState”时使用的稀有度权重档位。
/// </summary>
internal readonly struct RarityWeightTier
{
    public readonly int MinState;
    public readonly EssenceRarityWeights Weights;

    public RarityWeightTier(int minState, EssenceRarityWeights weights)
    {
        MinState = minState;
        Weights = weights;
    }
}

/// <summary>
/// 单个档位下，各稀有度的权重。
/// </summary>
internal readonly struct EssenceRarityWeights
{
    public readonly int White;
    public readonly int Blue;
    public readonly int Purple;
    public readonly int Gold;
    public readonly int Prismatic;

    public EssenceRarityWeights(int white, int blue, int purple, int gold, int prismatic)
    {
        White = white;
        Blue = blue;
        Purple = purple;
        Gold = gold;
        Prismatic = prismatic;
    }
}

/// <summary>
/// 用于词条“是否可出现/如何 roll”的上下文（后续你可以逐步加字段）
/// </summary>
public readonly struct EssenceAffixRollContext
{
    public readonly string BossName;
    public readonly float BossState;

    public EssenceAffixRollContext(string bossName, float bossState)
    {
        BossName = bossName;
        BossState = bossState;
    }
}

/// <summary>
/// 五个不同稀有度的池子，根据基质本身的state来动态分配池子选取的权重。比如十级的基质，出现Red池子的概率是15%，在Red池中随机挑选一个词条。
/// </summary>
public enum EssenceAffixRarity : byte
{
    White,
    Blue,
    Purple,
    Gold,
    Prismatic
}


/// <summary>
/// 单条词条定义（静态定义）
/// </summary>
public sealed class EssenceAffixDef
{
    public EssenceAffixDef(string key, EssenceAffixRarity rarity)
    {
        Key = key;
        Rarity = rarity;
    }

    /// <summary>
    /// 稳定存档键（例如："alpha_melee_speed"）。
    /// </summary>
    public string Key { get; }
    
    /// <summary>
    /// 运行时索引（仅用于内存优化，不要用于存档/网络同步）。
    /// </summary>
    public int RuntimeId { get; internal set; } = -1;

    /// <summary>
    /// 随机池类型。
    /// </summary>
    public EssenceAffixRarity Rarity { get; init; }

    /// <summary>
    /// 该词条的具体效果实现（可选）。
    /// </summary>
    public IEssenceAffixEffect Effect { get; init; } = NoEssenceAffixEffect.Instance;

    /// <summary>
    /// 生成条件（比如：只在月后/只在特定 BossState 之后/有概率出现等）
    /// </summary>
    public Func<EssenceAffixRollContext, bool> CanRoll { get; init; } = static _ => true;

    /// <summary>
    /// 生成 roll（最小实现：给一个 0..1 的指示值）。
    /// 组合词条/离散档位以后可以把返回值扩展为 TagCompound 或自定义结构。
    /// </summary>
    public Func<UnifiedRandom, EssenceAffixRollContext, float> Roll01 { get; init; } = static (rand, _) => rand.NextFloat(0.8f,1.2f);
}