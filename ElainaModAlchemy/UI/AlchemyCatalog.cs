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
    public string IconPath => Art is null
        ? AlchemyCatalog.VanillaItemRoot + ItemType
        : AlchemyCatalog.AssetRoot + Art + "_Pixel";
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
    public bool InitialUnlocked { get; init; }
    public IReadOnlyList<AlchemyCatalogIngredient> Ingredients { get; init; } = Array.Empty<AlchemyCatalogIngredient>();
    public IReadOnlyList<AlchemyCatalogEffect> Effects { get; init; } = Array.Empty<AlchemyCatalogEffect>();
    public string IconPath => PixelPath;
    public string LockedIconPath => AlchemyCatalog.MysteryPixelRoot + Art + "_Mystery_Pixel";
    public string PixelPath => AlchemyCatalog.AssetRoot + Art + "_Pixel";
    public int ItemType => AlchemyCatalog.ResolveItemType(Id);
    public bool IsMaterial => Category == "material";
    public bool Craftable => Ingredients.Count > 0 && string.IsNullOrEmpty(Acquisition);
}

/// <summary>
/// Static runtime catalog for the alchemy notebook and its current design data.
/// Item IDs are resolved only after tModLoader has loaded content. Offline previews can
/// use every other field without referencing Terraria or loading any game assets.
/// </summary>
public static class AlchemyCatalog
{
    public const string AssetRoot = "伊蕾娜/ElainaModAlchemy/item/ExampleAssets/";
    public const string VanillaItemRoot = "Terraria/Images/Item_";
    // Derived exclusively from ExampleAssets. AlphaBlend needs premultiplied RGB;
    // retain the original straight-alpha PNGs for the HTML reference.
    public const string PencilRoot = "伊蕾娜/ElainaModAlchemy/UI/Assets/Pencil/";
    public const string SilhouetteRoot = "伊蕾娜/ElainaModAlchemy/UI/Assets/Silhouettes/";
    public const string MysteryPixelRoot = "伊蕾娜/ElainaModAlchemy/UI/Assets/MysteryPixel/";

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
    public static IEnumerable<string> InitiallyUnlockedIds => Recipes.Where(r => r.InitialUnlocked).Select(r => r.Id);
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
        new("blinkroot", "闪耀根", "Blinkroot", "地下泥土与土块", 9, null, null),
        new("lens", "晶状体", "Lens", "恶魔眼掉落", 4, null, null),
        new("stinger", "毒刺", "Stinger", "丛林 · 蜜蜂与尖刺史莱姆", 6, null, null),
        new("feather", "羽毛", "Feather", "天空 · 鸟妖", 5, null, null),
        new("powder", "净化粉", "Purification Powder", "树妖出售", 30, null, null),
        new("glass", "玻璃", "Glass", "熔炉 · 沙块", 20, null, null),
        new("aetherblock", "以太块", "Aether Block", "微光中取得", 3, null, null),
        new("obsidian", "黑曜石", "Obsidian", "水与熔岩接触", 6, null, null),
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
            Category = "potion", Art = "MoonDewElixir", Kind = "魔药 · 回复", Stage = "初始直接解锁",
            Description = "融合了月与星尘的魔力药水，使用后为伊蕾娜储存 100 点备用魔力。无法在战斗中使用。",
            Note = "这东西的味道不好……——伊蕾娜", Rarity = 1, Yield = 3, InitialUnlocked = true,
            PixelWidth = 33, PixelHeight = 40,
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(3, "moondew"),
                new(3, "star"),
                new(1, "glowingmushroom"),
            },
            Effects = new AlchemyCatalogEffect[]
            {
                new("备用魔力", "储存 100 点"),
                new("使用限制", "无法在战斗中使用"),
            },
        },
        new()
        {
            Id = "painkiller", Name = "止痛药", EnglishName = "Painkiller",
            Category = "potion", Art = "Painkiller", Kind = "魔药 · 回复", Stage = "需要炼金等级 1",
            Description = "提供持续的生命恢复。",
            Note = "药瓶上系着的丝带，是我认出它们的小记号。", Rarity = 1, Yield = 3,
            PixelWidth = 32, PixelHeight = 34,
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(1, "moondew"),
                new(1, "daybloom"),
                new(1, "honeyblock"),
                new(1, "resin"),
            },
            Effects = new AlchemyCatalogEffect[]
            {
                new("持续时间", "60 秒"),
                new("每秒恢复", "最大生命的 1%"),
            },
        },
        new()
        {
            Id = "bloodlust", Name = "嗜血药水", EnglishName = "Bloodthirst Potion",
            Category = "potion", Art = "BloodthirstPotion", Kind = "魔药 · 战斗", Stage = "肉后血月敌人掉落",
            Description = "大幅提升受到的伤害，但你的近战攻击增加伤害并提供吸血效果。",
            Note = "药瓶上系着的丝带，是我认出它们的小记号。", Rarity = 2, Yield = 1,
            PixelWidth = 30, PixelHeight = 40,
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(1, "moondew"),
                new(1, "deathweed"),
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
            Category = "potion", Art = "StarPowerPotion", Kind = "魔药 · 战斗", Stage = "巫师出售配方",
            Description = "最大魔力增加；你的魔力值越高于生命值，魔法伤害越高。",
            Note = "药瓶上系着的丝带，是我认出它们的小记号。", Rarity = 2, Yield = 1,
            PixelWidth = 31, PixelHeight = 36,
            Live = "starpower",
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(1, "moondew"),
                new(1, "star"),
            },
            Effects = new AlchemyCatalogEffect[]
            {
                new("最大魔力", "+40"),
                new("最大魔法伤害", "+30%"),
                new("所需生命差值", "300"),
            },
        },
        new()
        {
            Id = "focus", Name = "集中药水", EnglishName = "Concentration Potion",
            Category = "potion", Art = "ConcentrationPotion", Kind = "魔药 · 战斗", Stage = "夜晚军火商出售配方",
            Description = "你的远程攻击方向距离目标中心越近，造成的伤害就越高。",
            Note = "药瓶上系着的丝带，是我认出它们的小记号。", Rarity = 2, Yield = 1,
            PixelWidth = 19, PixelHeight = 38,
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(1, "moondew"),
                new(1, "lens"),
            },
            Effects = new AlchemyCatalogEffect[]
            {
                new("伤害增幅", "+5% ~ +30%"),
                new("最大判定角度", "30 度"),
            },
        },
        new()
        {
            Id = "resonance", Name = "共鸣药水", EnglishName = "Resonance Potion",
            Category = "potion", Art = "ResonancePotion", Kind = "魔药 · 战斗", Stage = "夜晚巫医出售配方",
            Description = "鞭子命中敌人后赋予共鸣标记，你的召唤物攻击被共鸣的敌人时伤害增加。",
            Note = "药瓶上系着的丝带，是我认出它们的小记号。", Rarity = 1, Yield = 1,
            PixelWidth = 31, PixelHeight = 39,
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(1, "moondew"),
                new(1, "stinger"),
            },
            Effects = new AlchemyCatalogEffect[]
            {
                new("标记时间", "3 秒"),
                new("召唤物伤害", "+20%"),
            },
        },
        new()
        {
            Id = "featherlight", Name = "轻羽药水", EnglishName = "Featherlight Potion",
            Category = "potion", Art = "FeatherlightPotion", Kind = "魔药 · 功能", Stage = "需要炼金等级 2",
            Description = "使你变得身轻如燕，移动速度、飞行速度和扫帚速度增加。",
            Note = "药瓶上系着的丝带，是我认出它们的小记号。", Rarity = 1, Yield = 3,
            PixelWidth = 19, PixelHeight = 38,
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(1, "moondew"),
                new(1, "feather"),
                new(1, "daybloom"),
            },
            Effects = new AlchemyCatalogEffect[]
            {
                new("速度增幅", "+20%"),
            },
        },
        new()
        {
            Id = "isolation", Name = "净土露滴", EnglishName = "Purification Dew",
            Category = "potion", Art = "PurificationDew", Kind = "魔药 · 功能", Stage = "肉后树妖出售配方",
            Description = "使用后在目标位置滴下净土露滴，其会下落穿透物块并净化附近所有物块。",
            Note = "药瓶上系着的丝带，是我认出它们的小记号。", Rarity = 2, Yield = 1,
            PixelWidth = 19, PixelHeight = 34,
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(1, "moondew"),
                new(10, "powder"),
                new(1, "daybloom"),
            },
            Effects = new AlchemyCatalogEffect[]
            {
                new("作用范围", "附近所有物块"),
                new("特性", "下落时穿透物块"),
            },
        },
        new()
        {
            Id = "dropper", Name = "以太滴管", EnglishName = "Aether Dropper",
            Category = "curio", Art = "AetherDropper", Kind = "奇物 · 功能", Stage = "需要炼金等级 3",
            Description = "可以吸收并重新释放微光。",
            Note = "所谓炼金，大概就是替寻常事物发现另一种可能。", Rarity = 2, Yield = 1,
            PixelWidth = 19, PixelHeight = 34,
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(10, "glass"),
                new(3, "aetherblock"),
            },
            Effects = new AlchemyCatalogEffect[]
            {
                new("容量", "1 格微光"),
            },
        },
        new()
        {
            Id = "mimic", Name = "灰赝尘", EnglishName = "Ashen Facsimile Dust",
            Category = "curio", Art = "AshenFacsimileDust", Kind = "奇物 · 功能", Stage = "需要炼金等级 3",
            Description = "借助微光的转化之力，为尚未齐备的材料补上缺失的一部分。",
            Note = "所谓炼金，大概就是替寻常事物发现另一种可能。", Rarity = 2, Yield = 50,
            PixelWidth = 32, PixelHeight = 32,
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(1, "shimmer"),
                new(1, "obsidian"),
            },
            Effects = new AlchemyCatalogEffect[]
            {
                new("材料前提", "每种所需材料至少持有 1 件"),
                new("数量前提", "总需求已满足至少 50%"),
                new("优先级", "通过收藏优先替代原材料"),
            },
        },
        new()
        {
            Id = "trace", Name = "显迹尘", EnglishName = "Revealing Dust",
            Category = "curio", Art = "RevealingDust", Kind = "奇物 · 功能", Stage = "需要炼金等级 2",
            Description = "照亮所有敌人与陷阱。",
            Note = "所谓炼金，大概就是替寻常事物发现另一种可能。", Rarity = 2, Yield = 3,
            PixelWidth = 24, PixelHeight = 25,
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(1, "blinkroot"),
                new(3, "bone"),
            },
            Effects = new AlchemyCatalogEffect[]
            {
                new("照亮半径", "1500"),
                new("持续时间", "120 秒"),
            },
        },
        new()
        {
            Id = "rain", Name = "瓶中雨", EnglishName = "Bottled Rain",
            Category = "curio", Art = "BottledRain", Kind = "奇物 · 功能", Stage = "需要炼金等级 1",
            Description = "释放一朵便携雨云，增快附近所有植物的生长，让水叶草开花。",
            Note = "所谓炼金，大概就是替寻常事物发现另一种可能。", Rarity = 1, Yield = 1,
            PixelWidth = 31, PixelHeight = 38,
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(1, "moondew"),
                new(10, "cloud"),
                new(1, "waterleaf"),
            },
            Effects = new AlchemyCatalogEffect[]
            {
                new("持续时间", "60 秒"),
            },
        },
        new()
        {
            Id = "bread", Name = "蜂蜜面包", EnglishName = "Honey Bread",
            Category = "food", Art = "HoneyBread", Kind = "料理 · 辅助", Stage = "基础料理",
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
            Category = "food", Art = "BeefStew", Kind = "料理 · 辅助", Stage = "旅商食材",
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
            Category = "food", Art = "CaramelBrulee", Kind = "料理 · 辅助", Stage = "基础料理",
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
            Category = "material", Art = "MoonDew", Kind = "素材", Stage = "初始解锁",
            Description = "用瓶装水与月光草制成的药液基底，可直接用于炼金。",
            Note = "把每一种材料的来处记清楚，下一次就不用翻遍行囊了。", Rarity = 0, Yield = 3, InitialUnlocked = true,
            PixelWidth = 20, PixelHeight = 28,
            OutputMaterial = "moondew",
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(3, "water"),
                new(1, "moonglow"),
            },
            Effects = new AlchemyCatalogEffect[]
            {
                new("用途", "作为药液基底"),
            },
        },
        new()
        {
            Id = "resin", Name = "温香树脂", EnglishName = "Warm Resin",
            Category = "material", Art = "WarmResin", Kind = "素材", Stage = "初始解锁",
            Description = "木材和凝胶熬制成的温润树脂。",
            Note = "把每一种材料的来处记清楚，下一次就不用翻遍行囊了。", Rarity = 0, Yield = 2, InitialUnlocked = true,
            PixelWidth = 25, PixelHeight = 24,
            OutputMaterial = "resin",
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(20, "wood"),
                new(2, "gel"),
            },
            Effects = new AlchemyCatalogEffect[]
            {
                new("用途", "作为药液辅材"),
            },
        },
        new()
        {
            Id = "flour", Name = "面粉", EnglishName = "Flour",
            Category = "material", Art = "Flour", Kind = "素材", Stage = "商人",
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
            Category = "material", Art = "Sugar", Kind = "素材", Stage = "商人",
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
            Category = "material", Art = "Egg", Kind = "素材", Stage = "商人",
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
            Category = "material", Art = "Beef", Kind = "素材", Stage = "旅商",
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
            Category = "material", Art = "Potato", Kind = "素材", Stage = "旅商",
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
            Category = "material", Art = "ShimmerDroplet", Kind = "素材", Stage = "以太滴管",
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
