using System;
using System.Collections.Generic;
using System.Linq;

namespace 伊蕾娜.ElainaModAlchemy.UI;

/// <summary>The notebook is presentation data; it does not grant recipes or interact with a player's inventory.</summary>
public sealed record AlchemyCatalogCategory(string Id, string Name, string EnglishName,
    string Title, string Description, string Note);

public sealed record AlchemyCatalogEffect(string Label, string Value, string Tone = "");

/// <summary>Alternative materials may be combined to fulfill Count.</summary>
public sealed record AlchemyCatalogIngredient(int Count, params string[] Choices);

public sealed record AlchemyCatalogMaterial(string Id, string Name, string EnglishName,
    string Source, int Initial, string Art = null, string Entry = null)
{
    public string IconPath => AlchemyCatalog.PencilRoot +
        (Art is null ? "Materials/" + Id : Art) + "_Pencil";
    public int ItemType => AlchemyCatalog.ResolveItemType(Id);
}

public sealed record AlchemyCatalogRecipe
{
    public string Id { get; init; }
    public string Name { get; init; }
    public string EnglishName { get; init; }
    public string Category { get; init; }
    public string Art { get; init; }
    public string Kind { get; init; }
    public string Stage { get; init; }
    public string Description { get; init; }
    public string Note { get; init; }
    public int Rarity { get; init; }
    public string RarityName => Rarity switch { 0 => "普通", 1 => "精良", 2 => "稀有", _ => "珍奇" };
    public int Yield { get; init; } = 1;
    public string OutputMaterial { get; init; }
    public string Material { get; init; }
    public string Acquisition { get; init; }
    public string Live { get; init; }
    public int PixelWidth { get; init; }
    public int PixelHeight { get; init; }
    public IReadOnlyList<AlchemyCatalogIngredient> Ingredients { get; init; } = Array.Empty<AlchemyCatalogIngredient>();
    public IReadOnlyList<AlchemyCatalogEffect> Effects { get; init; } = Array.Empty<AlchemyCatalogEffect>();
    public string IconPath => AlchemyCatalog.PencilRoot + Art + "_Pencil";
    public string PixelPath => AlchemyCatalog.AssetRoot + Art + "_Pixel";
    public int ItemType => AlchemyCatalog.ResolveItemType(Id);
    public bool IsMaterial => Category == "material";
    public bool Craftable => Ingredients.Count > 0 && string.IsNullOrEmpty(Acquisition);
}

/// <summary>
/// Static copy of Tools/AlchemyPlan/plan.json and the supplied HTML's revised notebook.
/// Item IDs are resolved only after tModLoader has loaded content. Offline previews can
/// use every other field without referencing Terraria or loading any game assets.
/// </summary>
public static class AlchemyCatalog
{
    public const string AssetRoot = "伊蕾娜/ElainaModAlchemy/item/ExampleAssets/";
    // Derived exclusively from ExampleAssets. AlphaBlend needs premultiplied RGB;
    // retain the original straight-alpha PNGs for the HTML reference.
    public const string PencilRoot = "伊蕾娜/ElainaModAlchemy/UI/Assets/Pencil/";

    public static Func<string, int> ItemTypeResolver { get; set; }
    public static int ResolveItemType(string id) => ItemTypeResolver?.Invoke(id) ?? 0;

    public static IReadOnlyList<AlchemyCatalogCategory> Categories { get; } = new AlchemyCatalogCategory[]
    {
        new("potion", "魔药", "POTIONS", "随身的魔药", "为旅途补充魔力，为战斗调配一点勇气。", "药瓶上系着的丝带，是我认出它们的小记号。"),
        new("curio", "奇物", "CURIOS", "旅途中的奇物", "盛住微光，唤来细雨，也让暗处的危险显形。", "所谓炼金，大概就是替寻常事物发现另一种可能。"),
        new("food", "料理", "CUISINE", "魔女的厨房", "热汤与甜点，让漫长的旅途也有值得期待的一餐。", "我擅长炖菜，不过甜点总是更容易让我动心。"),
        new("material", "素材", "INGREDIENTS", "行囊与素材", "基础药液在锅中调制，日常食材向商人购入。", "把每一种材料的来处记清楚，下一次就不用翻遍行囊了。"),
    };

    // Filled from the checked-in plan below; no runtime JSON or HTML parsing is needed.
    public static IReadOnlyList<AlchemyCatalogMaterial> Materials { get; } = CreateMaterials();
    public static IReadOnlyList<AlchemyCatalogRecipe> Recipes { get; } = CreateRecipes();
    private static readonly Dictionary<string, AlchemyCatalogMaterial> MaterialsById = Materials.ToDictionary(m => m.Id, StringComparer.Ordinal);
    private static readonly Dictionary<string, AlchemyCatalogRecipe> RecipesById = Recipes.ToDictionary(r => r.Id, StringComparer.Ordinal);

    public static AlchemyCatalogMaterial GetMaterial(string id) => MaterialsById[id];
    public static AlchemyCatalogRecipe GetRecipe(string id) => RecipesById[id];

    private static AlchemyCatalogMaterial[] CreateMaterials() => new AlchemyCatalogMaterial[]
    {
        new("water", "瓶装水", "Bottled Water", "玻璃瓶 · 水源", 36, null, null),
        new("moonglow", "月光草", "Moonglow", "丛林 · 夜间采集", 9, null, null),
        new("star", "坠落之星", "Fallen Star", "夜空坠落", 8, null, null),
        new("glowingmushroom", "发光蘑菇", "Glowing Mushroom", "发光蘑菇生物群落", 12, null, null),
        new("daybloom", "太阳花", "Daybloom", "森林地表 · 白昼", 10, null, null),
        new("honeyblock", "蜂蜜块", "Honey Block", "蜂蜜与水接触", 6, null, null),
        new("deathweed", "死亡草", "Deathweed", "腐化或猩红之地", 7, null, null),
        new("vertebra", "椎骨", "Vertebra", "猩红敌怪掉落", 2, null, null),
        new("rottenchunk", "腐肉", "Rotten Chunk", "腐化敌怪掉落", 7, null, null),
        new("crystal", "水晶碎块", "Crystal Shard", "地下神圣之地", 8, null, null),
        new("blinkroot", "闪耀根", "Blinkroot", "地下泥土与土块", 9, null, null),
        new("lens", "晶状体", "Lens", "恶魔眼掉落", 4, null, null),
        new("mandible", "蚁狮上颚", "Antlion Mandible", "沙漠 · 蚁狮", 4, null, null),
        new("stinger", "毒刺", "Stinger", "丛林 · 蜜蜂与尖刺史莱姆", 6, null, null),
        new("feather", "羽毛", "Feather", "天空 · 鸟妖", 5, null, null),
        new("powder", "净化粉", "Purification Powder", "树妖出售", 30, null, null),
        new("glass", "玻璃", "Glass", "熔炉 · 沙块", 20, null, null),
        new("iron", "铁锭", "Iron Bar", "熔炉 · 铁矿", 3, null, null),
        new("lead", "铅锭", "Lead Bar", "熔炉 · 铅矿", 7, null, null),
        new("hallowed", "神圣锭", "Hallowed Bar", "机械 Boss 掉落", 3, null, null),
        new("obsidian", "黑曜石", "Obsidian", "水与熔岩接触", 6, null, null),
        new("pixiedust", "妖精尘", "Pixie Dust", "神圣之地 · 妖精", 5, null, null),
        new("bone", "骨头", "Bone", "地牢敌怪掉落", 15, null, null),
        new("cloud", "云块", "Cloud", "天空岛", 30, null, null),
        new("waterleaf", "幌菊", "Waterleaf", "沙漠 · 雨天开花", 4, null, null),
        new("honey", "蜂蜜瓶", "Bottled Honey", "玻璃瓶 · 蜂蜜", 4, null, null),
        new("wood", "木材", "Wood", "砍伐树木", 60, null, null),
        new("gel", "凝胶", "Gel", "史莱姆掉落", 15, null, null),
        new("moondew", "月露", "Moon Dew", "瓶装水×3＋月光草×1 → 月露×3", 9, "MoonDew", "moondew"),
        new("resin", "温香树脂", "Warm Resin", "木材×20＋凝胶×2 → 树脂×2", 4, "WarmResin", "resin"),
        new("flour", "面粉", "Flour", "商人出售", 8, "Flour", "flour"),
        new("sugar", "糖", "Sugar", "商人出售", 9, "Sugar", "sugar"),
        new("egg", "鸡蛋", "Egg", "商人出售", 5, "Egg", "egg"),
        new("beef", "牛肉", "Beef", "旅商 · 固定售卖", 2, "Beef", "beef"),
        new("potato", "土豆", "Potato", "旅商 · 固定售卖", 6, "Potato", "potato"),
        new("shimmer", "微光液滴", "Shimmer Droplet", "以太滴管 · 从微光中取得", 2, "ShimmerDroplet", "shimmer"),
    };
    private static AlchemyCatalogRecipe[] CreateRecipes() => new AlchemyCatalogRecipe[]
    {
        new()
        {
            Id = "mana", Name = "月露合剂", EnglishName = "Moon Dew Elixir",
            Category = "potion", Art = "MoonDewElixir", Kind = "专属补给 · 即时回复", Stage = "前期",
            Description = "把星光溶进月露，再用发光蘑菇稳定药液。仅供伊蕾娜饮用的专属魔力补给。",
            Note = "药瓶上系着的丝带，是我认出它们的小记号。", Rarity = 1, Yield = 3,
            PixelWidth = 20, PixelHeight = 28,
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(3, "moondew"),
                new(1, "star"),
                new(3, "glowingmushroom"),
            },
            Effects = new AlchemyCatalogEffect[]
            {
                new("专属魔力", "回复 100 点"),
                new("使用者", "伊蕾娜专属"),
            },
        },
        new()
        {
            Id = "painkiller", Name = "止痛药", EnglishName = "Painkiller",
            Category = "potion", Art = "Painkiller", Kind = "药片 · 持续恢复", Stage = "探索蜂巢后",
            Description = "太阳花与蜂蜜负责恢复，温香树脂让药效缓慢释放。服用后持续获得恢复效果。",
            Note = "药瓶上系着的丝带，是我认出它们的小记号。", Rarity = 1, Yield = 3,
            PixelWidth = 26, PixelHeight = 22,
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(1, "water"),
                new(1, "daybloom"),
                new(1, "honeyblock"),
                new(1, "resin"),
            },
            Effects = new AlchemyCatalogEffect[]
            {
                new("持续时间", "30 秒"),
                new("每秒恢复", "最大生命的 2%"),
                new("全程累计", "最大生命的 60%"),
            },
        },
        new()
        {
            Id = "bloodlust", Name = "嗜血药水", EnglishName = "Bloodthirst Potion",
            Category = "potion", Art = "BloodthirstPotion", Kind = "战斗药水 · 近战", Stage = "肉后解锁",
            Description = "以承受更多伤害为代价，唤醒近战攻击中的嗜血欲望。",
            Note = "药瓶上系着的丝带，是我认出它们的小记号。", Rarity = 2, Yield = 1,
            PixelWidth = 20, PixelHeight = 28,
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(1, "water"),
                new(1, "deathweed"),
                new(3, "vertebra", "rottenchunk"),
            },
            Effects = new AlchemyCatalogEffect[]
            {
                new("近战生命偷取", "+5%"),
                new("近战伤害", "+10%"),
                new("受到的伤害", "+20%", "bad"),
            },
        },
        new()
        {
            Id = "starpower", Name = "星力药水", EnglishName = "Star Power Potion",
            Category = "potion", Art = "StarPowerPotion", Kind = "战斗药水 · 魔法", Stage = "肉后初期",
            Description = "水晶放大魔力储备中的星光。魔力上限越高，获得的魔法伤害加成越多。",
            Note = "药瓶上系着的丝带，是我认出它们的小记号。", Rarity = 2, Yield = 1,
            PixelWidth = 20, PixelHeight = 28,
            Live = "starpower",
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(1, "moondew"),
                new(1, "star"),
                new(1, "deathweed"),
                new(2, "crystal"),
            },
            Effects = new AlchemyCatalogEffect[]
            {
                new("每 1 点魔力上限", "+0.05% 魔法伤害"),
            },
        },
        new()
        {
            Id = "focus", Name = "集中药水", EnglishName = "Concentration Potion",
            Category = "potion", Art = "ConcentrationPotion", Kind = "战斗药水 · 远程", Stage = "肉后解锁",
            Description = "你的远程攻击方向越接近目标中心，造成的伤害就越高。让每一发都落向瞄准的中心。",
            Note = "药瓶上系着的丝带，是我认出它们的小记号。", Rarity = 2, Yield = 1,
            PixelWidth = 20, PixelHeight = 28,
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(1, "water"),
                new(1, "blinkroot"),
                new(1, "lens"),
                new(1, "mandible"),
            },
            Effects = new AlchemyCatalogEffect[]
            {
                new("判定", "攻击方向与目标中心的夹角"),
                new("远程伤害", "最高 +30%"),
            },
        },
        new()
        {
            Id = "resonance", Name = "共鸣药水", EnglishName = "Resonance Potion",
            Category = "potion", Art = "ResonancePotion", Kind = "战斗药水 · 召唤", Stage = "探索丛林后",
            Description = "仆从与哨兵向鞭子标记的敌人集中攻击。持续攻击同一标记目标时，共鸣逐渐增强。",
            Note = "药瓶上系着的丝带，是我认出它们的小记号。", Rarity = 1, Yield = 1,
            PixelWidth = 20, PixelHeight = 28,
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(1, "water"),
                new(1, "moonglow"),
                new(1, "stinger"),
            },
            Effects = new AlchemyCatalogEffect[]
            {
                new("攻击鞭子标记目标", "伤害 +12%"),
                new("连续攻击 3 秒", "逐渐提高至 +24%"),
                new("切换标记目标", "重新积累"),
            },
        },
        new()
        {
            Id = "featherlight", Name = "轻羽药水", EnglishName = "Featherlight Potion",
            Category = "potion", Art = "FeatherlightPotion", Kind = "旅行药水 · 持续 8 分钟", Stage = "探索天空后",
            Description = "把羽毛的轻盈与日间上升的气流装进瓶中，让飞行与扫帚旅行更加轻快。",
            Note = "药瓶上系着的丝带，是我认出它们的小记号。", Rarity = 1, Yield = 1,
            PixelWidth = 20, PixelHeight = 28,
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(1, "water"),
                new(1, "blinkroot"),
                new(1, "feather"),
                new(1, "daybloom"),
            },
            Effects = new AlchemyCatalogEffect[]
            {
                new("飞行速度", "+20%"),
                new("扫帚速度", "+20%"),
                new("持续时间", "8 分钟"),
            },
        },
        new()
        {
            Id = "isolation", Name = "净土露滴", EnglishName = "Purification Dew",
            Category = "potion", Art = "PurificationDew", Kind = "净化药液 · 滴落使用", Stage = "肉后初期",
            Description = "将露滴滴在物块上，净化这一格以及它下方的所有物块，用于制造纵向隔离带。",
            Note = "药瓶上系着的丝带，是我认出它们的小记号。", Rarity = 2, Yield = 1,
            PixelWidth = 20, PixelHeight = 28,
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(1, "water"),
                new(10, "powder"),
                new(1, "daybloom"),
                new(1, "crystal"),
            },
            Effects = new AlchemyCatalogEffect[]
            {
                new("作用位置", "目标物块及其下方所有物块"),
                new("用途", "净化 · 制造隔离带"),
            },
        },
        new()
        {
            Id = "dropper", Name = "以太滴管", EnglishName = "Aether Dropper",
            Category = "curio", Art = "AetherDropper", Kind = "工具 · 可重复使用", Stage = "击败任意机械 Boss 后",
            Description = "神圣金属让容器能够稳定地保存微光。对着微光吸收一格，再次使用时释放一格。",
            Note = "所谓炼金，大概就是替寻常事物发现另一种可能。", Rarity = 2, Yield = 1,
            PixelWidth = 24, PixelHeight = 30,
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(10, "glass"),
                new(5, "iron", "lead"),
                new(3, "hallowed"),
            },
            Effects = new AlchemyCatalogEffect[]
            {
                new("容量", "1 格微光"),
                new("使用顺序", "先吸收，再释放"),
                new("附带用途", "取得微光液滴"),
            },
        },
        new()
        {
            Id = "mimic", Name = "灰赝尘", EnglishName = "Ashen Facsimile Dust",
            Category = "curio", Art = "AshenFacsimileDust", Kind = "炼金奇物 · 合成辅助", Stage = "取得微光后",
            Description = "借助微光的转化之力，为尚未齐备的材料补上缺失的一部分。",
            Note = "所谓炼金，大概就是替寻常事物发现另一种可能。", Rarity = 2, Yield = 50,
            PixelWidth = 28, PixelHeight = 26,
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(1, "shimmer"),
                new(1, "obsidian"),
                new(1, "star"),
            },
            Effects = new AlchemyCatalogEffect[]
            {
                new("材料前提", "每种所需材料至少持有 1 件"),
                new("数量前提", "总需求已满足至少 50%"),
                new("效果", "替代缺少的素材"),
            },
        },
        new()
        {
            Id = "trace", Name = "显迹尘", EnglishName = "Revealing Dust",
            Category = "curio", Art = "RevealingDust", Kind = "侦察奇物 · 撒出使用", Stage = "肉后 · 神圣之地",
            Description = "撒出闪耀的细尘，让一片区域内的敌对生物和陷阱显出清楚的标记。",
            Note = "所谓炼金，大概就是替寻常事物发现另一种可能。", Rarity = 2, Yield = 3,
            PixelWidth = 24, PixelHeight = 25,
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(1, "pixiedust"),
                new(1, "blinkroot"),
                new(3, "bone"),
            },
            Effects = new AlchemyCatalogEffect[]
            {
                new("标记对象", "区域内的敌对生物"),
                new("危险提示", "区域内的陷阱"),
            },
        },
        new()
        {
            Id = "rain", Name = "瓶中雨", EnglishName = "Bottled Rain",
            Category = "curio", Art = "BottledRain", Kind = "天气奇物 · 投掷使用", Stage = "探索天空后",
            Description = "摔开瓶子，释放一朵便携的小雨云。雨水照料附近的植物，也替旅人扑灭普通火焰。",
            Note = "所谓炼金，大概就是替寻常事物发现另一种可能。", Rarity = 1, Yield = 1,
            PixelWidth = 22, PixelHeight = 30,
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(1, "water"),
                new(10, "cloud"),
                new(1, "waterleaf"),
            },
            Effects = new AlchemyCatalogEffect[]
            {
                new("小雨云", "持续 45 秒"),
                new("覆盖区域", "附近约 20 格宽"),
                new("玩家", "熄灭普通着火"),
                new("幌菊与植物", "雨天条件开花 · 加速生长"),
            },
        },
        new()
        {
            Id = "bread", Name = "蜂蜜面包", EnglishName = "Honey Bread",
            Category = "food", Art = "HoneyBread", Kind = "料理 · 蜂蜜香甜", Stage = "基础料理",
            Description = "淋上蜂蜜的面包，是伊蕾娜喜欢的甜味补给。不同旅人食用时获得的效果有所不同。",
            Note = "我擅长炖菜，不过甜点总是更容易让我动心。", Rarity = 1, Yield = 2,
            PixelWidth = 28, PixelHeight = 20,
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(1, "honey"),
                new(1, "sugar"),
                new(2, "flour"),
            },
            Effects = new AlchemyCatalogEffect[]
            {
                new("伊蕾娜", "酒足饭饱 · 恢复 50 生命"),
                new("其他玩家", "吃得好 · 不回复生命"),
            },
        },
        new()
        {
            Id = "stew", Name = "炖菜", EnglishName = "Beef Stew",
            Category = "food", Art = "BeefStew", Kind = "料理 · 热气腾腾", Stage = "旅商食材",
            Description = "伊蕾娜擅长制作的牛肉土豆炖菜。炖得软烂又暖胃，不过并不是她最偏爱的食物。",
            Note = "我擅长炖菜，不过甜点总是更容易让我动心。", Rarity = 1, Yield = 1,
            PixelWidth = 30, PixelHeight = 27,
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(1, "beef"),
                new(3, "potato"),
                new(5, "water"),
            },
            Effects = new AlchemyCatalogEffect[]
            {
                new("食用效果", "很满意"),
            },
        },
        new()
        {
            Id = "brulee", Name = "焦糖布蕾", EnglishName = "Caramel Brulee",
            Category = "food", Art = "CaramelBrulee", Kind = "料理 · 焦糖甜点", Stage = "基础料理",
            Description = "轻轻敲开薄脆的焦糖外壳，下面是柔软细腻的蛋奶甜点。",
            Note = "我擅长炖菜，不过甜点总是更容易让我动心。", Rarity = 1, Yield = 1,
            PixelWidth = 28, PixelHeight = 21,
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(3, "sugar"),
                new(1, "egg"),
                new(1, "water"),
            },
            Effects = new AlchemyCatalogEffect[]
            {
                new("食用效果", "吃得好"),
            },
        },
        new()
        {
            Id = "moondew", Name = "月露", EnglishName = "Moon Dew",
            Category = "material", Art = "MoonDew", Kind = "新素材 · 装瓶基底", Stage = "前期",
            Description = "将月光草浸入瓶装水，得到可以直接使用的装瓶药液基底。",
            Note = "把每一种材料的来处记清楚，下一次就不用翻遍行囊了。", Rarity = 0, Yield = 3,
            PixelWidth = 20, PixelHeight = 28,
            OutputMaterial = "moondew",
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(3, "water"),
                new(1, "moonglow"),
            },
            Effects = new AlchemyCatalogEffect[]
            {
                new("设计用途", "魔力、星辰、空间类炼金"),
            },
        },
        new()
        {
            Id = "resin", Name = "温香树脂", EnglishName = "Warm Resin",
            Category = "material", Art = "WarmResin", Kind = "新素材 · 炼金锅熬制", Stage = "前期",
            Description = "将木材与凝胶在炼金锅中缓慢熬制，留下温润而微甜的树脂。",
            Note = "把每一种材料的来处记清楚，下一次就不用翻遍行囊了。", Rarity = 0, Yield = 2,
            PixelWidth = 25, PixelHeight = 24,
            OutputMaterial = "resin",
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(20, "wood"),
                new(2, "gel"),
            },
            Effects = new AlchemyCatalogEffect[]
            {
                new("设计用途", "药膏、熏香、扫帚养护"),
            },
        },
        new()
        {
            Id = "flour", Name = "面粉", EnglishName = "Flour",
            Category = "material", Art = "Flour", Kind = "新食材 · 商人售卖", Stage = "商人",
            Description = "细白的烘焙用面粉，适合揉成甜面团。",
            Note = "把每一种材料的来处记清楚，下一次就不用翻遍行囊了。", Rarity = 0, Yield = 1,
            PixelWidth = 20, PixelHeight = 26,
            Material = "flour",
            Acquisition = "向商人购买。",
            Effects = new AlchemyCatalogEffect[]
            {
                new("用于", "蜂蜜面包"),
            },
        },
        new()
        {
            Id = "sugar", Name = "糖", EnglishName = "Sugar",
            Category = "material", Art = "Sugar", Kind = "新食材 · 商人售卖", Stage = "商人",
            Description = "给面团带来甜味，熬煮后也能形成焦糖外壳。",
            Note = "把每一种材料的来处记清楚，下一次就不用翻遍行囊了。", Rarity = 0, Yield = 1,
            PixelWidth = 24, PixelHeight = 22,
            Material = "sugar",
            Acquisition = "向商人购买。",
            Effects = new AlchemyCatalogEffect[]
            {
                new("用于", "蜂蜜面包、焦糖布蕾"),
            },
        },
        new()
        {
            Id = "egg", Name = "鸡蛋", EnglishName = "Egg",
            Category = "material", Art = "Egg", Kind = "新食材 · 商人售卖", Stage = "商人",
            Description = "料理与烘焙的基础食材。",
            Note = "把每一种材料的来处记清楚，下一次就不用翻遍行囊了。", Rarity = 0, Yield = 1,
            PixelWidth = 18, PixelHeight = 24,
            Material = "egg",
            Acquisition = "向商人购买。",
            Effects = new AlchemyCatalogEffect[]
            {
                new("用于", "焦糖布蕾"),
            },
        },
        new()
        {
            Id = "beef", Name = "牛肉", EnglishName = "Beef",
            Category = "material", Art = "Beef", Kind = "新食材 · 旅商固定售卖", Stage = "旅商",
            Description = "适合长时间炖煮的牛肉。",
            Note = "把每一种材料的来处记清楚，下一次就不用翻遍行囊了。", Rarity = 0, Yield = 1,
            PixelWidth = 26, PixelHeight = 22,
            Material = "beef",
            Acquisition = "旅行商人到访时固定售卖。",
            Effects = new AlchemyCatalogEffect[]
            {
                new("用于", "炖菜"),
            },
        },
        new()
        {
            Id = "potato", Name = "土豆", EnglishName = "Potato",
            Category = "material", Art = "Potato", Kind = "新食材 · 旅商固定售卖", Stage = "旅商",
            Description = "炖煮后软糯绵密，能吸足肉汤的味道。",
            Note = "把每一种材料的来处记清楚，下一次就不用翻遍行囊了。", Rarity = 0, Yield = 1,
            PixelWidth = 26, PixelHeight = 22,
            Material = "potato",
            Acquisition = "旅行商人到访时固定售卖。",
            Effects = new AlchemyCatalogEffect[]
            {
                new("用于", "炖菜"),
            },
        },
        new()
        {
            Id = "shimmer", Name = "微光液滴", EnglishName = "Shimmer Droplet",
            Category = "material", Art = "ShimmerDroplet", Kind = "新素材 · 微光凝滴", Stage = "以太滴管",
            Description = "从微光中取得的一滴奇异液体，带着转化物质的微弱力量。",
            Note = "把每一种材料的来处记清楚，下一次就不用翻遍行囊了。", Rarity = 2, Yield = 1,
            PixelWidth = 20, PixelHeight = 28,
            Material = "shimmer",
            Acquisition = "使用以太滴管从微光中取得。",
            Effects = new AlchemyCatalogEffect[]
            {
                new("用于", "灰赝尘"),
            },
        },
    };
}
