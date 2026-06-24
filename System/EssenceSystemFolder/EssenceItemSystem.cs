using System;
using System.Linq;
using System.Reflection;
using KL.Extensions;
using KL.Utils;
using MonoMod.Cil;
using Terraria.GameContent;
using Terraria.UI;



namespace 伊蕾娜.System.EssenceSystemFolder;

public class EssenceItemSystem : ModSystem
{
    public override void Load()
    {
        IL_ItemSlot.AccessorySwap += IL_ItemSlot_AccessorySwap_AllowEssencesDuplicates;
        IL_ItemSlot.AccCheck_ForPlayer += IL_ItemSlot_AccCheck_ForPlayer_AllowEssencesDuplicates;
        base.Load();
    }

    public override void Unload()
    {
        IL_ItemSlot.AccessorySwap -= IL_ItemSlot_AccessorySwap_AllowEssencesDuplicates;
        IL_ItemSlot.AccCheck_ForPlayer -= IL_ItemSlot_AccCheck_ForPlayer_AllowEssencesDuplicates;

        _isTheSameAsCached = null;
        _miIsTheSameAsCached = null;

        base.Unload();
    }

    private static MethodInfo _miIsTheSameAsCached;
    private static Func<Item, Item, bool> _isTheSameAsCached;

    private static Func<Item, Item, bool> GetTerrariaIsTheSameAs()
    {
        if (_isTheSameAsCached != null)
            return _isTheSameAsCached;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        var methods = typeof(Item)
            .GetMethods(flags)
            .Where(m => m.Name == "IsTheSameAs" && m.ReturnType == typeof(bool))
            .ToArray();

        // tML/原版有可能是：bool IsTheSameAs(Item other)
        _miIsTheSameAsCached = methods.FirstOrDefault(m => {
            var p = m.GetParameters();
            return p.Length == 1 && p[0].ParameterType == typeof(Item);
        });

        // 也兼容：bool IsTheSameAs(Item other, bool ...)
        _miIsTheSameAsCached ??= methods.FirstOrDefault(m => {
            var p = m.GetParameters();
            return p.Length == 2 && p[0].ParameterType == typeof(Item) && p[1].ParameterType == typeof(bool);
        });

        if (_miIsTheSameAsCached == null)
        {
            // 极端兜底：至少别崩（但一般不会走到这里）
            _isTheSameAsCached = (a, b) => a.netID == b.netID && a.type == b.type;
            return _isTheSameAsCached;
        }
        
        var parameters = _miIsTheSameAsCached.GetParameters();

        if (parameters.Length == 1)
        {
            try
            {
                // open instance delegate: (Item a, Item b) => a.IsTheSameAs(b)
                _isTheSameAsCached = (Func<Item, Item, bool>)Delegate.CreateDelegate(typeof(Func<Item, Item, bool>), _miIsTheSameAsCached);
                return _isTheSameAsCached;
            }
            catch
            {
                // 如果运行环境不允许绑定 non-public，就退回 Invoke
            }
        }

        _isTheSameAsCached = (a, b) => {
            if (parameters.Length == 1)
                return (bool)_miIsTheSameAsCached.Invoke(a, new object[] { b });

            // parameters.Length == 2: 第二个 bool 传 false 作为默认行为（只用于“是否相同”的判断）
            return (bool)_miIsTheSameAsCached.Invoke(a, new object[] { b, false });
        };

        return _isTheSameAsCached;
    }

    private static bool IsTheSameAsExceptEssencesItem(Item a, Item b)
    {
        int essencesType = ModContent.ItemType<EssenceItem>();

        bool aIsEssences = a.type == essencesType;
        bool bIsEssences = b.type == essencesType;

        if (aIsEssences && bIsEssences)
        {
            //只有 BossName 与 BossState 都一致才视为“相同物品”
            if (a.ModItem is not EssenceItem ea || b.ModItem is not EssenceItem eb)
                return false;

            const float eps = 0.0001f;
            bool sameBossName = ea.BossName == eb.BossName;
            bool sameBossState = Math.Abs(ea.BossState - eb.BossState) <= eps;
            bool sameBoss = sameBossState && sameBossName;
            
            // 如果两个物品都有词条，且词条相同，则视为“相同物品”
            if (ea.Affix != null && eb.Affix != null&&ea.Affix.Effect != null && eb.Affix.Effect != null)
            {
                bool sameAffix = ea.Affix.Effect.SameAs(eb.Affix.Effect) ;
                return sameAffix || sameBoss;
            }
            return sameBoss;
        }

        return GetTerrariaIsTheSameAs()(a, b);
    }

    private void IL_ItemSlot_AccessorySwap_AllowEssencesDuplicates(ILContext il)
    {
        var c = new ILCursor(il);
        int patched = 0;

        while (c.TryGotoNext(MoveType.Before, i => i.MatchCallvirt<Item>("IsTheSameAs"))) {
            c.Remove();
            c.EmitDelegate<Func<Item, Item, bool>>(IsTheSameAsExceptEssencesItem);
            patched++;
        }

        if (patched == 0)
            Log("IL patch failed: no Item.IsTheSameAs calls found in ItemSlot.AccessorySwap");
    }

    private void IL_ItemSlot_AccCheck_ForPlayer_AllowEssencesDuplicates(ILContext il)
    {
        var c = new ILCursor(il);
        int patched = 0;

        while (c.TryGotoNext(MoveType.Before, i => i.MatchCallvirt<Item>("IsTheSameAs"))) {
            c.Remove();
            c.EmitDelegate<Func<Item, Item, bool>>(IsTheSameAsExceptEssencesItem);
            patched++;
        }
        
        if (patched == 0)
            Log("IL patch failed: no Item.IsTheSameAs calls found in ItemSlot.AccCheck_ForPlayer");
    }

    public static Dictionary<int,Color[]> BossMainColors = new Dictionary<int,Color[]>();
    
    //储存最后一个图片避免反复调用
    public static void MakeNewEssences(NPC npc)
    {
        if (!BossMainColors.ContainsKey(npc.type))
        {
            Texture2D testTexture = TextureAssets.Npc[npc.type].Value;
            BossMainColors.Add(npc.type, GetTopThreeColors(testTexture, npc.frame.Width, npc.frame.Height));
        }

        //KLGameStateManager.GetBossState(npc);
        Item item = Main.item[Item.NewItem(npc.GetSource_FromThis(), new Rectangle((int)npc.position.X,(int)npc.position.Y, npc.width, npc.height),
            ModContent.ItemType<EssenceItem>())];
        EssenceItem essenceItem = item.ModItem as EssenceItem;
        

        //PrintText($"当前boss阶段进度：{stateProgress} 最大稀有度：{maxRarity} 当前稀有度：{(int)(stateProgress*maxRarity)} ");
        essenceItem?.Initialize(BossMainColors[npc.type], npc.FullName, KLGameStateManager.GetBossState(npc));
        
        item.NetUpdate();
    }

    public override void PostDrawInterface(SpriteBatch spriteBatch)
    {
        //Main.LocalPlayer.HeldItem.damage = 1000000000;
        base.PostDrawInterface(spriteBatch);
    }

    /// <summary>
    ///  获取图片中前三个颜色，可以只获取第一帧的颜色，需要传入第一帧的大小。
    /// </summary>
    /// <param name="texture"></param>
    /// <param name="frameWidth"></param>
    /// <param name="frameHeight"></param>
    /// <returns></returns>
    public static Color[] GetTopThreeColors(Texture2D texture,int frameWidth = -1,int frameHeight = -1)
    {
        // 注意：GetData 必须读取整张贴图大小的数组，否则会炸。
        int width = frameWidth == -1 ? texture.Width : Math.Clamp(frameWidth, 1, texture.Width);
        int height = frameHeight == -1 ? texture.Height : Math.Clamp(frameHeight, 1, texture.Height);

        Color[] colors = new Color[texture.Width * texture.Height];
        texture.GetData(colors);

        if (colors == null || colors.Length == 0)
            return new[] { Color.Transparent, Color.Transparent, Color.Transparent };

        const int desiredColorCount = 3;

        //用 HSV 分桶（对“有颜色”的像素忽略明度V，只按H+S合并），避免同一主色因光影被拆散
        var buckets = new Dictionary<HsvKey, ColorAccumulator>();

        // 只遍历第一帧（默认第一帧在左上角：x=[0,width), y=[0,height)）
        for (int y = 0; y < height; y++)
        {
            int rowStart = y * texture.Width;
            for (int x = 0; x < width; x++)
            {
                var color = colors[rowStart + x];

                // 忽略几乎透明的像素
                if (color.A < 10)
                    continue;

                var key = SimplifyToHsvKey(color);
                if (buckets.TryGetValue(key, out var acc))
                {
                    acc.Add(color);
                    buckets[key] = acc;
                }
                else
                {
                    acc = new ColorAccumulator();
                    acc.Add(color);
                    buckets.Add(key, acc);
                }
            }
        }

        if (buckets.Count == 0)
            return new[] { Color.Transparent, Color.Transparent, Color.Transparent };

        //频次 TopN 作为候选，然后再做“多样性选择”（避免拿到多个不同深浅的同色）
        const int candidateCount = 120;
        var candidates = buckets
            .OrderByDescending(p => p.Value.Count)
            .Take(candidateCount)
            .Select(p => new ColorCandidate(p.Value.GetAverageColor(), p.Value.Count))
            .ToArray();

        //贪心：先取频次最高的，然后每次取与已选集合“最不相似且不太稀有”的
        var selected = new List<Color>(capacity: desiredColorCount) { candidates[0].Color };

        while (selected.Count < desiredColorCount && selected.Count < candidates.Length)
        {
            float bestScore = float.NegativeInfinity;
            Color bestColor = selected[0];

            for (int i = 0; i < candidates.Length; i++)
            {
                var c = candidates[i].Color;
                if (selected.Contains(c))
                    continue;

                float minDist = float.PositiveInfinity;
                for (int s = 0; s < selected.Count; s++)
                    minDist = Math.Min(minDist, HsvDistance(c, selected[s]));

                // 用 count^0.65（比 sqrt 更强调面积），避免“尾巴一小块稳定色”挤掉“身体大面积但有光影的主色”
                float score = (float)Math.Pow(candidates[i].Count, 0.65) * minDist;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestColor = c;
                }
            }

            if (selected.Contains(bestColor))
                break;

            selected.Add(bestColor);
        }

        // 至少返回3个颜色：不足则重复最后一个
        while (selected.Count < 3)
            selected.Add(selected[selected.Count - 1]);

        return new[] { selected[0], selected[1], selected[2] };
    }

    private readonly struct HsvKey : IEquatable<HsvKey>
    {
        public readonly int HBin;
        public readonly int SBin;
        public readonly int VBin;

        public HsvKey(int hBin, int sBin, int vBin)
        {
            HBin = hBin;
            SBin = sBin;
            VBin = vBin;
        }

        public bool Equals(HsvKey other) => HBin == other.HBin && SBin == other.SBin && VBin == other.VBin;
        public override bool Equals(object obj) => obj is HsvKey other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(HBin, SBin, VBin);
    }

    private struct ColorAccumulator
    {
        public long R;
        public long G;
        public long B;
        public int Count;

        public void Add(Color c)
        {
            R += c.R;
            G += c.G;
            B += c.B;
            Count++;
        }

        public Color GetAverageColor()
        {
            if (Count <= 0)
                return Color.Transparent;

            return new Color(
                (byte)(R / Count),
                (byte)(G / Count),
                (byte)(B / Count),
                255
            );
        }
    }

    private static HsvKey SimplifyToHsvKey(Color color)
    {
        RgbToHsv(color, out float h, out float s, out float v);

        // 低饱和度：当作灰度（需要靠 V 区分深浅）
        const float graySatThreshold = 0.18f;
        if (s < graySatThreshold)
        {
            // 灰度分 8 桶：0..7
            int vBin = (int)(v * 7f);
            vBin = Math.Clamp(vBin, 0, 7);
            return new HsvKey(0, 0, vBin);
        }

        // 有色：按 H + S 分桶，忽略 V（把光影的深浅都合到同一主色里）
        const float hueStepDeg = 15f; // 360/15 = 24 桶
        int hBin = (int)(h / hueStepDeg);
        hBin = Math.Clamp(hBin, 0, 23);

        // 饱和度分 6 桶：0..5
        int sBin = (int)(s * 6f);
        sBin = Math.Clamp(sBin, 0, 5);

        return new HsvKey(hBin, sBin, 0);
    }

    private readonly struct ColorCandidate
    {
        public readonly Color Color;
        public readonly int Count;

        public ColorCandidate(Color color, int count)
        {
            Color = color;
            Count = count;
        }
    }

    private static float HsvDistance(Color a, Color b)
    {
        RgbToHsv(a, out float ah, out float asat, out float av);
        RgbToHsv(b, out float bh, out float bsat, out float bv);

        float dh = HueDistanceDegrees(ah, bh) / 180f; // 0..1
        float ds = Math.Abs(asat - bsat);
        float dv = Math.Abs(av - bv);

        // 权重偏向色相，其次饱和度，最后亮度（黑/白主要靠 dv + ds 拉开）
        return 1.5f * dh + 1.0f * ds + 0.5f * dv;
    }

    private static float HueDistanceDegrees(float h1, float h2)
    {
        float d = Math.Abs(h1 - h2);
        return d > 180f ? 360f - d : d;
    }

    // RGB(0-255) -> HSV(H:0-360, S/V:0-1)
    private static void RgbToHsv(Color c, out float h, out float s, out float v)
    {
        float r = c.R / 255f;
        float g = c.G / 255f;
        float b = c.B / 255f;

        float max = Math.Max(r, Math.Max(g, b));
        float min = Math.Min(r, Math.Min(g, b));
        float delta = max - min;

        v = max;
        s = max <= 0f ? 0f : (delta / max);

        if (delta <= 0f)
        {
            h = 0f;
            return;
        }

        if (max == r)
            h = 60f * (((g - b) / delta) % 6f);
        else if (max == g)
            h = 60f * (((b - r) / delta) + 2f);
        else
            h = 60f * (((r - g) / delta) + 4f);

        if (h < 0f)
            h += 360f;
    }

    // 简化颜色，将相近的颜色归为一类
    private static Color SimplifyColor(Color color, int step = 32)
    {
        return new Color(
            (byte)(color.R / step * step),
            (byte)(color.G / step * step),
            (byte)(color.B / step * step),
            255
        );
    }
    
}