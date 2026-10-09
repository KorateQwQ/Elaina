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

public enum SkillBalanceKind { BasicAttack, Active }

/// <summary>所有技能共用的定价定义；BaseCooldown 对普攻表示完整攻击周期，HitCount 是整次预算的等额命中数。</summary>
public sealed record SkillBalanceEntry(string Id, string Name, SkillBalanceKind Kind, int ManaCost,
    double BaseCooldown, int HitCount, int UnlockLevel = 1, int LevelsPerPhase = 1, int UnlockStageCap = 5, double DpsRatio = 0, double ConversionRate = 0, double OccupySeconds = 0);

/// <summary>本地不可变配置；只接受 v2，不读取网页配装、等级、模拟结果作为游戏属性。</summary>
public sealed class SkillBalanceConfig
{
    public const int MaxJsonBytes = 32 * 1024;
    public const string Schema = "elaina-mage-balance-v2";
    public const string ResourcePath = "ElainaModSkills/Data/SkillBalance.json";
    private static readonly Regex IdPattern = new("^[A-Za-z][A-Za-z0-9_.-]{0,63}$", RegexOptions.CultureInvariant);

    public double RatePer20 { get; }
    public int CharacterPhaseSize { get; }
    public string ReferenceBasicAttackId { get; }
    public FrozenDictionary<string, SkillBalanceEntry> Skills { get; }

    private SkillBalanceConfig(double rate, int characterPhaseSize, string referenceBasicAttackId, IEnumerable<SkillBalanceEntry> skills)
    {
        RatePer20 = rate;
        CharacterPhaseSize = characterPhaseSize;
        ReferenceBasicAttackId = referenceBasicAttackId;
        Skills = skills.ToFrozenDictionary(x => x.Id, StringComparer.Ordinal);
    }

    public static SkillBalanceConfig Fallback => new(4, 5, "MagicMissileSkill",
    [
        new("MagicMissileSkill", "魔法飞弹", SkillBalanceKind.BasicAttack, 3, .3, 1, LevelsPerPhase: 2, DpsRatio: .8),
        new("WaterBallSkill", "水球魔法", SkillBalanceKind.Active, 100, 10, 1, ConversionRate: .8, OccupySeconds: 1)
    ]);

    public static bool TryParse(string json, out SkillBalanceConfig config, out string error)
    {
        config = null;
        error = null;
        try
        {
            if (Encoding.UTF8.GetByteCount(json) > MaxJsonBytes)
                throw new FormatException("配置超过 32 KiB，请保存参数而不是试算日志。");
            using JsonDocument document = JsonDocument.Parse(json.TrimStart('\uFEFF'));
            JsonElement root = document.RootElement;
            RequireObject(root, "配置");
            if (root.GetProperty("schema").GetString() != Schema)
                throw new FormatException("仅支持 elaina-mage-balance-v2，请使用新版编辑器保存。");
            JsonElement c = root.GetProperty("configuration");
            RequireObject(c, "configuration");
            Number(c, "version", 2, 2);
            double rate = Number(c, "rate", 0, 20);
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
                skills.Add(new(id, name, SkillBalanceKind.BasicAttack, Integer(row, "cost", 0, 10000),
                    Number(row, "interval", .05, 10), Integer(row, "shots", 1, 100), Integer(row, "unlockLevel", 1, 1000), Integer(row, "levelsPerPhase", 1, 10), Integer(row, "unlockStageCap", 1, 1000), DpsRatio: Number(row, "ratio", 0, 5)));
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
                skills.Add(new(id, name, SkillBalanceKind.Active, Integer(row, "cost", 0, 10000),
                    Number(row, "cd", .1, 600), Integer(row, "shots", 1, 100), Integer(row, "unlockLevel", 1, 1000), Integer(row, "levelsPerPhase", 1, 10), Integer(row, "unlockStageCap", 1, 1000),
                    ConversionRate: Number(row, "k", 0, 3), OccupySeconds: Number(row, "cast", 0, 60)));
            }
            config = new(rate, phaseSize, reference, skills);
            return true;
        }
        catch (Exception ex) when (ex is JsonException or FormatException or InvalidOperationException or KeyNotFoundException or ArgumentException)
        {
            error = ex.Message;
            return false;
        }
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

    private static void RequireObject(JsonElement value, string name)
    {
        if (value.ValueKind != JsonValueKind.Object) throw new FormatException(name + " 必须是对象。");
    }

    private static double Number(JsonElement value, string key, double min, double max)
    {
        JsonElement field = value.GetProperty(key);
        if (field.ValueKind != JsonValueKind.Number || !field.TryGetDouble(out double number)
            || !double.IsFinite(number) || number < min || number > max)
            throw new FormatException($"{key} 必须在 {min}..{max} 范围内。");
        return number;
    }

    private static int Integer(JsonElement value, string key, int min, int max)
    {
        double number = Number(value, key, min, max);
        if (number != Math.Truncate(number)) throw new FormatException(key + " 必须是整数。");
        return (int)number;
    }

    /// <summary>未取整的整次基础伤害；普攻与主动技能仅在定价公式上不同。</summary>
    public double GetSkillDamage(double baselineDps, SkillBalanceEntry skill)
    {
        double baseline = Math.Max(0, baselineDps);
        if (skill.Kind == SkillBalanceKind.BasicAttack)
            return baseline * skill.DpsRatio * skill.BaseCooldown;
        double referenceRatio = Skills[ReferenceBasicAttackId].DpsRatio;
        double extraRatio = (RatePer20 / 100 / 20) * skill.ManaCost * skill.BaseCooldown * skill.ConversionRate;
        return baseline * (referenceRatio * Math.Max(1d / 60, skill.OccupySeconds) + extraRatio);
    }

    /// <summary>按配置完整周期折算的基础 DPS；主动技能包含占手补偿，不是净额外 DPS。</summary>
    public double GetSkillDps(double baselineDps, SkillBalanceEntry skill) =>
        GetSkillDamage(baselineDps, skill) / skill.BaseCooldown;

    /// <summary>技能升级节点；角色门槛同时是该技能等级的伤害基准等级，不代替 Boss 解锁条件。</summary>
    public IEnumerable<int> GetSkillLevelNodes(SkillBalanceEntry skill, int maxCharacterLevel = 1000)
    {
        if (maxCharacterLevel < skill.UnlockLevel) yield break;
        int previous = skill.UnlockLevel;
        yield return previous;
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
            if (stream.Length > SkillBalanceConfig.MaxJsonBytes) throw new IOException("配置超过 32 KiB。");
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
