using Microsoft.Xna.Framework;
using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Terraria.ID;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaModSkills;

public class DpsHelper : ModSystem
{
    private static SkillDpsCurve[] _skillDpsCurves;
    
    static string path => "伊蕾娜/ElainaModSkills/Data/SkillDpsCurves.json";
    
    
    
    private enum SkillCurveType
    {
        Linear,
        SmoothStep,
        CustomBezier,
    }

    private class SkillDpsCurve
    {
        public string Name { get; set; }
        public SkillCurveType Curve { get; set; }
        [JsonPropertyName("s1")]
        public float S1 { get; set; }
        [JsonPropertyName("s18")]
        public float S18 { get; set; }
        public SkillCurveKey[] Keys { get; set; } = [];
    }

    private class SkillCurveKey
    {
        public float BossState { get; set; }
        public float Dps { get; set; }
        public float InState { get; set; }
        public float InDps { get; set; }
        public float OutState { get; set; }
        public float OutDps { get; set; }
    }
    

    public static int GetSkillDamage(string skillTypeName, float bossState)
    {
        SkillDpsCurve curve = GetSkillDpsCurve(skillTypeName);
        return curve == null ? 0 : CalcDps(curve, bossState);
    }

    public override void Unload()
    {
        _skillDpsCurves = null;
    }

    private static SkillDpsCurve GetSkillDpsCurve(string skillTypeName)
    {
        SkillDpsCurve[] curves = LoadSkillDpsCurves();
        return curves.FirstOrDefault(curve => SameSkillName(curve.Name, skillTypeName));
    }

    private static SkillDpsCurve[] LoadSkillDpsCurves()
    {
        if (_skillDpsCurves != null) {
            return _skillDpsCurves;
        }
        
        if (!ModContent.FileExists(path)||Main.netMode== NetmodeID.Server) {
            _skillDpsCurves = [];
            return _skillDpsCurves;
        }

        string json = Encoding.UTF8.GetString(ModContent.GetFileBytes(path));
        JsonSerializerOptions options = new JsonSerializerOptions {
            PropertyNameCaseInsensitive = true,
        };
        options.Converters.Add(new JsonStringEnumConverter());
        _skillDpsCurves = JsonSerializer.Deserialize<SkillDpsCurve[]>(json, options) ?? [];
        return _skillDpsCurves;
    }

    private static int CalcDps(SkillDpsCurve skill, float bossState)
    {
        bossState = MathF.Max(bossState, 0f);
        
        if (skill.Curve != SkillCurveType.CustomBezier) {
            return CalcFixedDps(skill, bossState);
        }

        SkillCurveKey[] keys = skill.Keys.OrderBy(key => key.BossState).ToArray();
        if (keys.Length == 0) {
            return CalcFixedDps(skill, bossState);
        }
        if (bossState <= keys[0].BossState) {
            return (int)MathF.Round(keys[0].Dps);
        }
        if (bossState >= keys[^1].BossState) {
            return (int)MathF.Round(keys[^1].Dps);
        }

        for (int i = 0; i < keys.Length - 1; i++) {
            SkillCurveKey start = keys[i];
            SkillCurveKey end = keys[i + 1];
            if (bossState < start.BossState || bossState > end.BossState) {
                continue;
            }

            float lo = 0f;
            float hi = 1f;
            for (int step = 0; step < 28; step++) {
                float mid = (lo + hi) / 2f;
                float x = Cubic(start.BossState, start.BossState + start.OutState, end.BossState + end.InState, end.BossState, mid);
                if (x < bossState) {
                    lo = mid;
                }
                else {
                    hi = mid;
                }
            }

            float t = (lo + hi) / 2f;
            float dps = Cubic(start.Dps, start.Dps + start.OutDps, end.Dps + end.InDps, end.Dps, t);
            return (int)MathF.Round(dps);
        }

        return 0;
    }

    private static int CalcFixedDps(SkillDpsCurve skill, float bossState)
    {
        bossState = MathF.Max(bossState, 0f);
        float progress = bossState / 18f;
        float amount = skill.Curve switch {
            SkillCurveType.Linear => progress,
            SkillCurveType.SmoothStep => SmoothStep(progress),
            _ => progress,
        };
        return (int)MathF.Round(Lerp(skill.S1, skill.S18, amount));
    }

    private static float SmoothStep(float amount)
    {
        amount = MathHelper.Clamp(amount, 0f, 1f);
        return amount * amount * (3f - 2f * amount);
    }

    private static float Lerp(float start, float end, float amount)
    {
        return start + (end - start) * amount;
    }

    private static float Cubic(float a, float b, float c, float d, float t)
    {
        float u = 1f - t;
        return u * u * u * a + 3f * u * u * t * b + 3f * u * t * t * c + t * t * t * d;
    }

    private static bool SameSkillName(string left, string right)
    {
        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }
    
}