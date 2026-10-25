using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
#if DEBUG
using System.IO;
#endif

namespace 伊蕾娜.ElainaModSkills;

/// <summary>扫帚参数来自 FlightSkill.flight；速度单位为像素/帧，时间单位为秒。</summary>
public sealed record FlightBalance(
    float BaseSpeed = 8f,
    float LearnedSpeedBonus = .10f,
    float SpeedBonusPerLevel = .05f,
    float BoostMultiplier = 1.5f,
    float BoostManaPerSecond = 12f,
    float HoldSeconds = .25f,
    float DashMultiplier = 2.5f,
    float DashManaCost = 25f,
    float DashSeconds = .4f,
    float DashCooldownSeconds = 1f,
    float HandlingPercent = 25f,
    float DashRecoverySeconds = .15f)
{
    public float Speed(int learnedLevel) => BaseSpeed * (learnedLevel <= 0 ? 1f
        : 1f + LearnedSpeedBonus + SpeedBonusPerLevel * (learnedLevel - 1));

    public static int Ticks(float seconds) => Math.Max(1, (int)Math.Ceiling(seconds * 60d - .00001));
}

public enum SkillBalanceKind { BasicAttack, Active }
public enum SkillGrowthMode { Phase, Interval }

/// <summary>所有技能共用的定价定义；BaseCooldown 对普攻表示完整攻击周期，HitCount 是整次预算的等额命中数。
/// RatioCurve 为空时普攻固定使用 DpsRatio，否则按角色等级在节点间线性插值、超出两端取端点。
/// GrowthMode 为 Interval 时，从 UnlockLevel 起每 LevelInterval 个角色等级新增一级，不使用阶段跨度或 LevelsPerPhase。
/// MaxLevel 为 0 时不设固定等级上限；正整数限制两种成长方式产生的技能等级数。
/// MaxCharges / InitialCharges 为主动技能的储存层数上限与新习得时的可用层数，普攻固定为 1。</summary>
public sealed record SkillBalanceEntry(string Id, string Name, SkillBalanceKind Kind, double ManaCost,
    double BaseCooldown, int HitCount, int UnlockLevel = 1, int LevelsPerPhase = 1, int UnlockStageCap = 5, double DpsRatio = 0, double ConversionRate = 0, double OccupySeconds = 0, double CdDamageRatio = 1,
    IReadOnlyList<(int Level, double Ratio)> RatioCurve = null, int MaxCharges = 1, int InitialCharges = 1, SkillGrowthMode GrowthMode = SkillGrowthMode.Phase, int LevelInterval = 5, double FreezeBuildup = 0, int MaxLevel = 0, FlightBalance Flight = null);

/// <summary>阶段推荐配装中的一个饰品；Id 与 Prefix 是网页键名（如 sorcerer / arcane），由 Loadout 映射到物品与前缀。</summary>
public sealed record SkillLoadoutItem(string Id, string Prefix);

/// <summary>第 Stage 个进度阶段的推荐配装与属性；Items 固定 7 项，只有前 Slots 项生效。
/// BaseMana 按魔力水晶 20 一档，MaxLife 400 以内按生命水晶 20 一档、之后按生命果 5 一档。</summary>
public sealed record SkillLoadout(int Stage, string Name, string Armor, int Slots, IReadOnlyList<SkillLoadoutItem> Items, int BaseMana, int MaxLife);

/// <summary>本地不可变配置；只接受 v2。网页的模拟配装、等级与结果不作为游戏属性，
/// 只有可选的 loadouts 供快捷配装面板按阶段换装。</summary>
public sealed class SkillBalanceConfig
{
    public const int MaxJsonBytes = 64 * 1024;
    public const string Schema = "elaina-mage-balance-v2";
    public const string ResourcePath = "ElainaModSkills/Data/SkillBalance.json";
    public const int LoadoutStages = 19;
    private static readonly Regex IdPattern = new("^[A-Za-z][A-Za-z0-9_.-]{0,63}$", RegexOptions.CultureInvariant);
    private static readonly Regex KeyPattern = new("^[A-Za-z][A-Za-z0-9_]{0,63}$", RegexOptions.CultureInvariant);

    public double RatePer20 { get; }
    public int CharacterPhaseSize { get; }
    public string ReferenceBasicAttackId { get; }
    /// <summary>主动技能每秒占手给予的基准 DPS 倍率；0 表示不做占手补偿，伤害全部来自魔力转换。</summary>
    public double OccupyRatio { get; }
    public FrozenDictionary<string, SkillBalanceEntry> Skills { get; }
    /// <summary>按阶段排列的推荐配装，下标为 Stage − 1；配置缺少 loadouts 时为空。</summary>
    public IReadOnlyList<SkillLoadout> Loadouts { get; }

    private SkillBalanceConfig(double rate, int characterPhaseSize, string referenceBasicAttackId, double occupyRatio, IEnumerable<SkillBalanceEntry> skills,
        IReadOnlyList<SkillLoadout> loadouts = null)
    {
        Loadouts = loadouts ?? [];
        RatePer20 = rate;
        CharacterPhaseSize = characterPhaseSize;
        ReferenceBasicAttackId = referenceBasicAttackId;
        OccupyRatio = occupyRatio;
        Skills = skills.ToFrozenDictionary(x => x.Id, StringComparer.Ordinal);
    }

    public static SkillBalanceConfig Fallback => new(4, 5, "MagicMissileSkill", 0,
    [
        new("MagicMissileSkill", "魔法飞弹", SkillBalanceKind.BasicAttack, 3, .3, 1, LevelsPerPhase: 2, DpsRatio: .6,
            RatioCurve: [(1, 1), (10, .9), (20, .8), (35, .7), (50, .6)]),
        new("WaterBallSkill", "水球术", SkillBalanceKind.Active, 100, 10, 1, UnlockLevel: 26, UnlockStageCap: 30, ConversionRate: 3.3, OccupySeconds: 1, CdDamageRatio: 1)
    ]);

    public static bool TryParse(string json, out SkillBalanceConfig config, out string error)
    {
        config = null;
        error = null;
        try
        {
            if (Encoding.UTF8.GetByteCount(json) > MaxJsonBytes)
                throw new FormatException("配置超过 64 KiB，请保存参数而不是试算日志。");
            using JsonDocument document = JsonDocument.Parse(json.TrimStart('\uFEFF'));
            JsonElement root = document.RootElement;
            RequireObject(root, "配置");
            if (root.GetProperty("schema").GetString() != Schema)
                throw new FormatException("仅支持 elaina-mage-balance-v2，请使用新版编辑器保存。");
            JsonElement c = root.GetProperty("configuration");
            RequireObject(c, "configuration");
            Number(c, "version", 2, 2);
            double rate = Number(c, "rate", 0, 20);
            double occupyRatio = OptionalNumber(c, "occupyRatio", 0, 5, 0);
            JsonElement progression = c.GetProperty("progression");
            RequireObject(progression, "progression");
            int phaseSize = Integer(progression, "phaseSize", 1, 20);
            List<SkillBalanceEntry> skills = [];
            HashSet<string> ids = new(StringComparer.Ordinal);
            JsonElement basics = c.GetProperty("basicAttacks");
            if (basics.ValueKind != JsonValueKind.Array || basics.GetArrayLength() is < 1 or > 32)
                throw new FormatException("basicAttacks 必须是 1–32 项的数组。");
            foreach (JsonElement row in basics.EnumerateArray())
            {
                var (id, name) = ReadIdentity(row, ids);
                skills.Add(new(id, name, SkillBalanceKind.BasicAttack, Number(row, "cost", 0, 10000),
                    Number(row, "interval", .05, 10), Integer(row, "shots", 1, 100), Integer(row, "unlockLevel", 1, 1000), Integer(row, "levelsPerPhase", 1, 10), Integer(row, "unlockStageCap", 1, 1000), DpsRatio: Number(row, "ratio", 0, 5),
                    RatioCurve: ReadRatioCurve(row), GrowthMode: ReadGrowthMode(row), LevelInterval: OptionalInteger(row, "levelInterval", 1, 1000, 5),
                    FreezeBuildup: OptionalNumber(row, "freezeBuildup", 0, 100, 0), MaxLevel: OptionalInteger(row, "maxLevel", 0, 1000, 0), Flight: ReadFlight(row)));
            }
            string reference = c.GetProperty("selectedBasicAttackId").GetString();
            if (reference == null || !ids.Contains(reference))
                throw new FormatException("参考普攻 ID 必须指向 basicAttacks 中的条目。");
            JsonElement actives = c.GetProperty("skills");
            if (actives.ValueKind != JsonValueKind.Array || actives.GetArrayLength() > 32)
                throw new FormatException("skills 必须是最多 32 项的数组。");
            foreach (JsonElement row in actives.EnumerateArray())
            {
                var (id, name) = ReadIdentity(row, ids);
                skills.Add(new(id, name, SkillBalanceKind.Active, Number(row, "cost", 0, 10000),
                    Number(row, "cd", .1, 600), Integer(row, "shots", 1, 100), Integer(row, "unlockLevel", 1, 1000), Integer(row, "levelsPerPhase", 1, 10), Integer(row, "unlockStageCap", 1, 1000),
                    ConversionRate: Number(row, "k", 0, 200), OccupySeconds: Number(row, "cast", 0, 60), CdDamageRatio: OptionalNumber(row, "cdRatio", 0, 1, 1),
                    MaxCharges: OptionalInteger(row, "maxCharges", 1, 99, 1), InitialCharges: OptionalInteger(row, "initialCharges", 0, 99, 1),
                    GrowthMode: ReadGrowthMode(row), LevelInterval: OptionalInteger(row, "levelInterval", 1, 1000, 5),
                    FreezeBuildup: OptionalNumber(row, "freezeBuildup", 0, 100, 0), MaxLevel: OptionalInteger(row, "maxLevel", 0, 1000, 0), Flight: ReadFlight(row)));
            }
            config = new(rate, phaseSize, reference, occupyRatio, skills, ReadLoadouts(c));
            return true;
        }
        catch (Exception ex) when (ex is JsonException or FormatException or InvalidOperationException or KeyNotFoundException or ArgumentException)
        {
            error = ex.Message;
            return false;
        }
    }

    private static FlightBalance ReadFlight(JsonElement row)
    {
        if (!row.TryGetProperty("flight", out var flight)) return null;
        RequireObject(flight, "flight");
        float Read(string key, double min, double max, float fallback) =>
            (float)OptionalNumber(flight, key, min, max, fallback);
        var value = new FlightBalance(
            Read("baseSpeed", 1, 40, 8), Read("learnedSpeedBonus", 0, 5, .1f),
            Read("speedBonusPerLevel", 0, 1, .05f), Read("boostMultiplier", 1, 3, 1.5f),
            Read("boostManaPerSecond", 0, 1000, 12), Read("holdSeconds", .05, 2, .25f),
            Read("dashMultiplier", 1, 5, 2.5f), Read("dashManaCost", 0, 10000, 25),
            Read("dashSeconds", 1d / 60, 1, .4f), Read("dashCooldownSeconds", .1, 60, 1),
            Read("handlingPercent", 1, 100, 25), Read("dashRecoverySeconds", 1d / 60, .5, .15f));
        if (value.DashCooldownSeconds < value.DashSeconds)
            throw new FormatException("冲刺冷却不能短于冲刺持续时间。");
        return value;
    }

    private static SkillGrowthMode ReadGrowthMode(JsonElement row)
    {
        if (!row.TryGetProperty("growthMode", out JsonElement value)) return SkillGrowthMode.Phase;
        if (value.ValueKind == JsonValueKind.String)
        {
            switch (value.GetString())
            {
                case "phase": return SkillGrowthMode.Phase;
                case "interval": return SkillGrowthMode.Interval;
            }
        }
        throw new FormatException("growthMode 必须是 phase（按阶段成长）或 interval（固定等级间隔）。");
    }

    private static (string Id, string Name) ReadIdentity(JsonElement row, HashSet<string> ids)
    {
        RequireObject(row, "技能");
        string id = row.GetProperty("id").GetString(), name = row.GetProperty("name").GetString();
        if (id == null || !IdPattern.IsMatch(id) || !ids.Add(id))
            throw new FormatException("普攻与主动技能的 ID 必须合法且全局唯一。");
        if (string.IsNullOrWhiteSpace(name) || name.Length > 64)
            throw new FormatException("技能名称不能为空或超过 64 字符。");
        return (id, name);
    }

    /// <summary>可选的 [[角色等级, 倍率], ...]；等级为严格递增整数，缺省或空数组表示固定倍率。</summary>
    private static (int Level, double Ratio)[] ReadRatioCurve(JsonElement row)
    {
        if (!row.TryGetProperty("ratioCurve", out JsonElement curve) || curve.ValueKind == JsonValueKind.Null) return [];
        if (curve.ValueKind != JsonValueKind.Array || curve.GetArrayLength() > 16)
            throw new FormatException("ratioCurve 必须是最多 16 项的数组。");
        var points = new (int Level, double Ratio)[curve.GetArrayLength()];
        int index = 0;
        foreach (JsonElement point in curve.EnumerateArray())
        {
            if (point.ValueKind != JsonValueKind.Array || point.GetArrayLength() != 2)
                throw new FormatException("ratioCurve 的每项必须是 [等级, 倍率]。");
            double level = point[0].ValueKind == JsonValueKind.Number ? point[0].GetDouble() : double.NaN;
            double ratio = point[1].ValueKind == JsonValueKind.Number ? point[1].GetDouble() : double.NaN;
            if (!(level >= 1 && level <= 1000) || level != Math.Truncate(level) || !(ratio >= 0 && ratio <= 5)
                || index > 0 && level <= points[index - 1].Level)
                throw new FormatException("ratioCurve 等级必须是 1..1000 的递增整数，倍率必须在 0..5 范围内。");
            points[index++] = ((int)level, ratio);
        }
        return points;
    }

    /// <summary>可选的 19 阶段配装；任一项不合法时整份配置被拒绝，与网页和 server.py 的校验一致。</summary>
    private static SkillLoadout[] ReadLoadouts(JsonElement c)
    {
        if (!c.TryGetProperty("loadouts", out JsonElement rows)) return [];
        if (rows.ValueKind != JsonValueKind.Array || rows.GetArrayLength() != LoadoutStages)
            throw new FormatException($"loadouts 必须是 {LoadoutStages} 项的数组。");
        var loadouts = new SkillLoadout[LoadoutStages];
        int stage = 0;
        foreach (JsonElement row in rows.EnumerateArray())
        {
            stage++;
            RequireObject(row, "配装");
            if (Integer(row, "stage", 1, LoadoutStages) != stage)
                throw new FormatException($"loadouts 第 {stage} 项的 stage 必须是 {stage}。");
            string name = row.GetProperty("name").GetString();
            if (string.IsNullOrWhiteSpace(name) || name.Length > 64)
                throw new FormatException("配装名称不能为空或超过 64 字符。");
            string armor = Key(row, "armor");
            int slots = Integer(row, "slots", 5, 7);
            JsonElement items = row.GetProperty("items");
            if (items.ValueKind != JsonValueKind.Array || items.GetArrayLength() != 7)
                throw new FormatException("配装饰品必须是 7 项的数组。");
            var list = new SkillLoadoutItem[7];
            int index = 0;
            foreach (JsonElement item in items.EnumerateArray())
            {
                RequireObject(item, "配装饰品");
                list[index++] = new(Key(item, "id"), Key(item, "prefix"));
            }
            int mana = Integer(row, "baseMana", 20, 200), life = Integer(row, "maxLife", 100, 500);
            if (mana % 20 != 0)
                throw new FormatException("基础魔力必须是 20 的倍数（魔力水晶）。");
            if (life % (life <= 400 ? 20 : 5) != 0)
                throw new FormatException("最大生命 400 以内按 20、之后按 5 一档。");
            loadouts[stage - 1] = new(stage, name, armor, slots, list, mana, life);
        }
        return loadouts;
    }

    private static string Key(JsonElement value, string key)
    {
        JsonElement field = value.GetProperty(key);
        string text = field.ValueKind == JsonValueKind.String ? field.GetString() : null;
        if (text == null || !KeyPattern.IsMatch(text)) throw new FormatException(key + " 必须是网页中的配装键名。");
        return text;
    }

    private static void RequireObject(JsonElement value, string name)
    {
        if (value.ValueKind != JsonValueKind.Object) throw new FormatException(name + " 必须是对象。");
    }

    private static double OptionalNumber(JsonElement value, string key, double min, double max, double fallback)
    {
        return value.TryGetProperty(key, out JsonElement field) ? Number(value, key, min, max) : fallback;
    }

    private static double Number(JsonElement value, string key, double min, double max)
    {
        JsonElement field = value.GetProperty(key);
        if (field.ValueKind != JsonValueKind.Number || !field.TryGetDouble(out double number)
            || !double.IsFinite(number) || number < min || number > max)
            throw new FormatException($"{key} 必须在 {min}..{max} 范围内。");
        return number;
    }

    private static int OptionalInteger(JsonElement value, string key, int min, int max, int fallback)
    {
        return value.TryGetProperty(key, out _) ? Integer(value, key, min, max) : fallback;
    }

    private static int Integer(JsonElement value, string key, int min, int max)
    {
        double number = Number(value, key, min, max);
        if (number != Math.Truncate(number)) throw new FormatException(key + " 必须是整数。");
        return (int)number;
    }

    /// <summary>普攻在指定角色等级的目标 DPS 倍率；无曲线时为固定 DpsRatio。</summary>
    public static double GetDpsRatio(SkillBalanceEntry skill, double characterLevel)
    {
        IReadOnlyList<(int Level, double Ratio)> curve = skill.RatioCurve;
        if (curve == null || curve.Count == 0) return skill.DpsRatio;
        if (characterLevel <= curve[0].Level) return curve[0].Ratio;
        for (int i = 1; i < curve.Count; i++)
        {
            if (characterLevel > curve[i].Level) continue;
            var (fromLevel, fromRatio) = curve[i - 1];
            var (toLevel, toRatio) = curve[i];
            return fromRatio + (toRatio - fromRatio) * (characterLevel - fromLevel) / (toLevel - fromLevel);
        }
        return curve[^1].Ratio;
    }

    /// <summary>未取整的整次基础伤害；characterLevel 是该技能等级映射出的伤害基准等级，只影响带曲线的普攻倍率。
    /// 主动技能的 CD 只按 CdDamageRatio 折算为伤害预算，实际冷却不变。</summary>
    public double GetSkillDamage(double baselineDps, SkillBalanceEntry skill, int characterLevel)
    {
        double baseline = Math.Max(0, baselineDps);
        if (skill.Kind == SkillBalanceKind.BasicAttack)
            return baseline * GetDpsRatio(skill, characterLevel) * skill.BaseCooldown;
        double extraRatio = (RatePer20 / 100 / 20) * skill.ManaCost * skill.BaseCooldown * skill.CdDamageRatio * skill.ConversionRate;
        return baseline * (OccupyRatio * Math.Max(1d / 60, skill.OccupySeconds) + extraRatio);
    }

    /// <summary>按配置完整周期折算的基础 DPS；主动技能包含 OccupyRatio 占手部分，不是净额外 DPS。</summary>
    public double GetSkillDps(double baselineDps, SkillBalanceEntry skill, int characterLevel) =>
        GetSkillDamage(baselineDps, skill, characterLevel) / skill.BaseCooldown;

    /// <summary>技能升级节点；角色门槛同时是该技能等级的伤害基准等级，不代替 Boss 解锁条件。</summary>
    public IEnumerable<int> GetSkillLevelNodes(SkillBalanceEntry skill, int maxCharacterLevel = 1000)
    {
        if (maxCharacterLevel < skill.UnlockLevel) yield break;
        int remaining = skill.MaxLevel > 0 ? skill.MaxLevel : int.MaxValue;
        int previous = skill.UnlockLevel;
        yield return previous;
        if (--remaining == 0) yield break;
        if (skill.GrowthMode == SkillGrowthMode.Interval)
        {
            for (long node = (long)skill.UnlockLevel + skill.LevelInterval; node <= maxCharacterLevel; node += skill.LevelInterval)
            {
                yield return (int)node;
                if (--remaining == 0) yield break;
            }
            yield break;
        }
        int stageStart = skill.UnlockLevel, stageEnd = skill.UnlockStageCap;
        if (stageEnd <= stageStart)
            stageEnd += (int)Math.Ceiling((stageStart - stageEnd + 1d) / CharacterPhaseSize) * CharacterPhaseSize;
        while (stageStart < maxCharacterLevel)
        {
            for (int i = 1; i <= skill.LevelsPerPhase; i++)
            {
                int node = stageStart + (int)Math.Ceiling((stageEnd - stageStart) * i / (double)skill.LevelsPerPhase);
                if (node > previous && node <= maxCharacterLevel)
                {
                    yield return node;
                    if (--remaining == 0) yield break;
                    previous = node;
                }
            }
            stageStart = stageEnd;
            stageEnd += CharacterPhaseSize;
        }
    }

    /// <summary>仅计算角色等级允许的技能等级上限；未满足首级门槛返回 0，不代表 Boss 条件已满足。</summary>
    public int GetSkillLevelCap(SkillBalanceEntry skill, int characterLevel)
    {
        int count = 0;
        foreach (int node in GetSkillLevelNodes(skill, characterLevel)) count++;
        return count;
    }

    /// <summary>将技能实际等级映射为伤害基准角色等级；未学习返回 0，超出配置节点范围时使用最后一档。</summary>
    public int GetSkillCharacterLevel(SkillBalanceEntry skill, int skillLevel)
    {
        if (skillLevel <= 0) return 0;
        int level = 0, characterLevel = 0;
        foreach (int node in GetSkillLevelNodes(skill))
        {
            characterLevel = node;
            if (++level >= skillLevel) break;
        }
        return characterLevel;
    }

    public static int ToHitDamage(double total, int hitCount = 1)
    {
        if (hitCount < 1) throw new ArgumentOutOfRangeException(nameof(hitCount));
        return total > 0 ? (int)Math.Clamp(Math.Floor(total / hitCount), 1, int.MaxValue) : 0;
    }
}

/// <summary>文件读取和校验与 tML 解耦；错误文件不覆盖最后一次成功的配置，下一次检测仍会重试。</summary>
public sealed class SkillBalanceFileStore(SkillBalanceConfig initial)
{
    public SkillBalanceConfig Current { get; private set; } = initial;
    public string LastError { get; private set; }
    private string acceptedJson;

    public bool Apply(string json)
    {
        if (json == acceptedJson) { LastError = null; return false; }
        if (!SkillBalanceConfig.TryParse(json, out SkillBalanceConfig next, out string error))
        {
            LastError = error;
            return false;
        }
        Current = next;
        acceptedJson = json;
        LastError = null;
        return true;
    }

#if DEBUG
    public bool Refresh(string path)
    {
        try
        {
            using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            if (stream.Length > SkillBalanceConfig.MaxJsonBytes) throw new IOException("配置超过 64 KiB。");
            using StreamReader reader = new(stream, Encoding.UTF8, true);
            return Apply(reader.ReadToEnd());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LastError = ex.Message;
            return false;
        }
    }
#endif
}
