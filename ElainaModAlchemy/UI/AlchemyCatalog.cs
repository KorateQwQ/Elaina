using static 伊蕾娜.Localization.UIText;
using System;
using System.Collections.Generic;
using System.Linq;

namespace 伊蕾娜.ElainaModAlchemy.UI;

/// <summary>The notebook is presentation data; it does not grant recipes or interact with a player's inventory.</summary>
public sealed record AlchemyCatalogCategory(string Id, string NameKey, string EnglishName,
    string TitleKey, string DescriptionKey, string NoteKey)
{
    public string Name => CatalogText(NameKey);
    public string Title => CatalogText(TitleKey);
    public string Description => CatalogText(DescriptionKey);
    public string Note => CatalogText(NoteKey);
}

public sealed record AlchemyCatalogEffect(string LabelKey, string ValueKey, string Tone = "")
{
    public string Label => CatalogText(LabelKey);
    public string Value => CatalogText(ValueKey);
}

/// <summary>Alternative materials may be combined to fulfill Count.</summary>
public sealed record AlchemyCatalogIngredient(int Count, params string[] Choices);

public sealed record AlchemyCatalogMaterial(string Id, string NameKey, string EnglishName,
    string SourceKey, int Initial, string Art = null, string Entry = null)
{
    public string Name => CatalogText(NameKey);
    public string Source => CatalogText(SourceKey);
    public string IconPath => Art is null
        ? AlchemyCatalog.VanillaItemRoot + ItemType
        : AlchemyCatalog.AssetRoot + Art + "_Pixel";
    public int ItemType => AlchemyCatalog.ResolveItemType(Id);
}

public sealed record AlchemyCatalogRecipe
{
    public string Id { get; init; }
    private string _Name;
    public string Name { get => CatalogText(_Name); init => _Name = value; }
    public string EnglishName { get; init; }
    public string Category { get; init; }
    public string Art { get; init; }
    private string _Kind;
    public string Kind { get => CatalogText(_Kind); init => _Kind = value; }
    private string _Stage;
    public string Stage { get => CatalogText(_Stage); init => _Stage = value; }
    private string _Description;
    public string Description { get => CatalogText(_Description); init => _Description = value; }
    private string _Note;
    public string Note { get => CatalogText(_Note); init => _Note = value; }
    public int Rarity { get; init; }
    public string RarityName => CatalogText(Rarity switch { 0 => "Catalog.Common", 1 => "Catalog.Fine", 2 => "Catalog.Rare", _ => "Catalog.Exotic" });
    public int Yield { get; init; } = 1;
    public string OutputMaterial { get; init; }
    public string Material { get; init; }
    private string _Acquisition;
    public string Acquisition { get => CatalogText(_Acquisition); init => _Acquisition = value; }
    private string _Live;
    public string Live { get => CatalogText(_Live); init => _Live = value; }
    public int PixelWidth { get; init; }
    public int PixelHeight { get; init; }
    private bool _InitialUnlocked;
    // Ingredients are always known; only non-material entries can require research.
    public bool InitialUnlocked { get => IsMaterial || _InitialUnlocked; init => _InitialUnlocked = value; }
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
    public const string RecipeScrollPixelPath = AssetRoot + "Recipe_Pixel";
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
        new("potion", "Catalog.POTIONS", "POTIONS", "Catalog.PotionsForTheRoad", "Catalog.ReplenishYourMagicForTheJourneyAndBrewALittleCourageForBattle", "Catalog.TheRibbonsAroundTheBottlesHelpMeTellThemApart"),
        new("curio", "Catalog.CURIOS", "CURIOS", "Catalog.CuriosOfTheJourney", "Catalog.BottleShimmerSummonRainAndRevealDangersLurkingInTheDark", "Catalog.PerhapsAlchemyIsSimplyFindingNewPossibilitiesInOrdinaryThings"),
        new("food", "Catalog.CUISINE", "CUISINE", "Catalog.TheWitchSKitchen", "Catalog.WarmSoupAndSweetsGiveEvenTheLongestJourneyAMealToLookForwardTo", "Catalog.IMGoodAtMakingStewButDessertsAlwaysTemptMeMore"),
        new("material", "Catalog.INGREDIENTS", "INGREDIENTS", "Catalog.BagsAndIngredients", "Catalog.BrewBasicMixturesInTheCauldronAndBuyEverydayIngredientsFromMerch", "Catalog.KeepTrackOfWhereEachIngredientComesFromAndSpareYourselfTheNextRu"),
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
        new("water", "Catalog.BottledWater", "Bottled Water", "Catalog.GlassBottleWater", 36, null, null),
        new("moonglow", "Catalog.Moonglow", "Moonglow", "Catalog.JungleGatherAtNight", 9, null, null),
        new("star", "Catalog.FallenStar", "Fallen Star", "Catalog.FallsFromTheNightSky", 8, null, null),
        new("glowingmushroom", "Catalog.GlowingMushroom", "Glowing Mushroom", "Catalog.GlowingMushroomBiome", 12, null, null),
        new("daybloom", "Catalog.Daybloom", "Daybloom", "Catalog.SurfaceForestDaytime", 10, null, null),
        new("lesserhealing", "Catalog.LesserHealingPotion", "Lesser Healing Potion", "Catalog.SoldByTheMerchant", 3, null, null),
        new("regenerationpotion", "Catalog.RegenerationPotion", "Regeneration Potion", "Catalog.VanillaPotionCrafting", 3, null, null),
        new("magicpowerpotion", "Catalog.MagicPowerPotion", "Magic Power Potion", "Catalog.VanillaPotionCrafting", 3, null, null),
        new("archerypotion", "Catalog.ArcheryPotion", "Archery Potion", "Catalog.VanillaPotionCrafting", 3, null, null),
        new("summoningpotion", "Catalog.SummoningPotion", "Summoning Potion", "Catalog.VanillaPotionCrafting", 3, null, null),
        new("deathweed", "Catalog.Deathweed", "Deathweed", "Catalog.TheCorruptionOrCrimson", 7, null, null),
        new("blinkroot", "Catalog.Blinkroot", "Blinkroot", "Catalog.UndergroundDirtAndMud", 9, null, null),
        new("lens", "Catalog.Lens", "Lens", "Catalog.DroppedByDemonEyes", 4, null, null),
        new("stinger", "Catalog.Stinger", "Stinger", "Catalog.JungleHornetsAndSpikedJungleSlimes", 6, null, null),
        new("feather", "Catalog.Feather", "Feather", "Catalog.SkyHarpies", 5, null, null),
        new("powder", "Catalog.PurificationPowder", "Purification Powder", "Catalog.SoldByTheDryad", 30, null, null),
        new("glass", "Catalog.Glass", "Glass", "Catalog.FurnaceSand", 20, null, null),
        new("aetherblock", "Catalog.AetherBlock", "Aether Block", "Catalog.ObtainedFromShimmer", 3, null, null),
        new("obsidian", "Catalog.Obsidian", "Obsidian", "Catalog.WaterTouchingLava", 6, null, null),
        new("bone", "Catalog.Bone", "Bone", "Catalog.DroppedByDungeonEnemies", 15, null, null),
        new("raincloud", "Catalog.RainCloud", "Rain Cloud", "Catalog.FloatingIslands", 30, null, null),
        new("waterleaf", "Catalog.Waterleaf", "Waterleaf", "Catalog.DesertBloomsInRain", 4, null, null),
        new("honey", "Catalog.BottledHoney", "Bottled Honey", "Catalog.GlassBottleHoney", 4, null, null),
        new("wood", "Catalog.Wood", "Wood", "Catalog.ChopDownTrees", 60, null, null),
        new("gel", "Catalog.Gel", "Gel", "Catalog.DroppedBySlimes", 15, null, null),
        new("moondew", "Catalog.MoonDew", "Moon Dew", "Catalog.BottledWater1Moonglow1MoonDew3", 9, "MoonDew", "moondew"),
        new("flour", "Catalog.Flour", "Flour", "Catalog.SoldByTheMerchant", 8, "Flour", "flour"),
        new("sugar", "Catalog.Sugar", "Sugar", "Catalog.SoldByTheMerchant", 9, "Sugar", "sugar"),
        new("egg", "Catalog.Egg", "Egg", "Catalog.SoldByTheMerchant", 5, "Egg", "egg"),
        new("beef", "Catalog.Beef", "Beef", "Catalog.TravelingMerchantAlwaysStocked", 2, "Beef", "beef"),
        new("potato", "Catalog.Potato", "Potato", "Catalog.TravelingMerchantAlwaysStocked", 6, "Potato", "potato"),
        new("shimmer", "Catalog.BottledShimmer", "Bottled Shimmer", "Catalog.AetherDropperCollectFromShimmer", 2, "BottledShimmer", "shimmer"),
    };
    private static AlchemyCatalogRecipe[] CreateRecipes() => new AlchemyCatalogRecipe[]
    {
        new()
        {
            Id = "mana", Name = "Catalog.MoonDewElixir", EnglishName = "Moon Dew Elixir",
            Category = "potion", Art = "MoonDewElixir", Kind = "Catalog.PotionRecovery", Stage = "Catalog.AvailableFromTheStart",
            Description = "Catalog.MoonDewElixirDescription",
            Note = "Catalog.ThisTastesAwfulElaina", Rarity = 1, Yield = 3, InitialUnlocked = true,
            PixelWidth = 33, PixelHeight = 40,
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(1, "moondew"),
                new(1, "star"),
                new(1, "glowingmushroom"),
            },
            Effects = new AlchemyCatalogEffect[]
            {
                new("Catalog.Duration", "Catalog.4Min"),
                new("Catalog.RecoveryPerSecond", "Catalog.1OfMaximumMagicPower"),
                new("Catalog.UsageRestriction", "Catalog.ElainaOnly"),
            },
        },
        new()
        {
            Id = "painkiller", Name = "Catalog.Painkiller", EnglishName = "Painkiller",
            Category = "potion", Art = "Painkiller", Kind = "Catalog.PotionRecovery", Stage = "Catalog.RecipeFoundInJungleChests",
            Description = "Catalog.ProvidesContinuousLifeRegeneration",
            Note = "Catalog.TheRibbonsAroundTheBottlesHelpMeTellThemApart", Rarity = 1, Yield = 3,
            PixelWidth = 24, PixelHeight = 34,
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(1, "moondew"),
                new(1, "lesserhealing"),
                new(1, "daybloom"),
            },
            Effects = new AlchemyCatalogEffect[]
            {
                new("Catalog.Duration", "Catalog.60S"),
                new("Catalog.RecoveryPerSecond", "Catalog.1OfMaximumLife"),
            },
        },
        new()
        {
            Id = "bloodlust", Name = "Catalog.BloodthirstPotion", EnglishName = "Bloodthirst Potion",
            Category = "potion", Art = "BloodthirstPotion", Kind = "Catalog.PotionCombat", Stage = "Catalog.HardmodeBloodMoonEnemyDrop",
            Description = "Catalog.IncreasesDamageTakenButGrantsExtraMeleeDamageAndLifeSteal",
            Note = "Catalog.TheRibbonsAroundTheBottlesHelpMeTellThemApart", Rarity = 2, Yield = 1,
            PixelWidth = 30, PixelHeight = 40,
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(1, "regenerationpotion"),
                new(1, "deathweed"),
            },
            Effects = new AlchemyCatalogEffect[]
            {
                new("Catalog.Duration", "Catalog.4Min"),
                new("Catalog.MeleeLifeSteal", "+5%"),
                new("Catalog.MeleeDamage", "+10%"),
                new("Catalog.DamageTaken", "+20%", "bad"),
            },
        },
        new()
        {
            Id = "starpower", Name = "Catalog.StarPowerPotion", EnglishName = "Star Power Potion",
            Category = "potion", Art = "StarPowerPotion", Kind = "Catalog.PotionCombat", Stage = "Catalog.RecipeSoldByTheWizard",
            Description = "Catalog.IncreasesMaximumManaTheMoreYourManaExceedsYourLifeTheGreaterYour",
            Note = "Catalog.TheRibbonsAroundTheBottlesHelpMeTellThemApart", Rarity = 2, Yield = 1,
            PixelWidth = 31, PixelHeight = 36,
            Live = "starpower",
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(1, "magicpowerpotion"),
                new(1, "star"),
            },
            Effects = new AlchemyCatalogEffect[]
            {
                new("Catalog.Duration", "Catalog.4Min"),
                new("Catalog.MaximumMana", "+40"),
                new("Catalog.MaximumMagicDamageBonus", "+30%"),
                new("Catalog.RequiredManaLifeDifference", "300"),
            },
        },
        new()
        {
            Id = "focus", Name = "Catalog.ConcentrationPotion", EnglishName = "Concentration Potion",
            Category = "potion", Art = "ConcentrationPotion", Kind = "Catalog.PotionCombat", Stage = "Catalog.RecipeSoldByTheArmsDealerAtNight",
            Description = "Catalog.YourRangedAttacksDealMoreDamageTheCloserYouAimToTheTargetSCenter",
            Note = "Catalog.TheRibbonsAroundTheBottlesHelpMeTellThemApart", Rarity = 2, Yield = 1,
            PixelWidth = 19, PixelHeight = 38,
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(1, "archerypotion"),
                new(1, "lens"),
            },
            Effects = new AlchemyCatalogEffect[]
            {
                new("Catalog.Duration", "Catalog.4Min"),
                new("Catalog.DamageBonus", "+5% ~ +30%"),
                new("Catalog.MaximumAngle", "Catalog.30Degrees"),
            },
        },
        new()
        {
            Id = "resonance", Name = "Catalog.ResonancePotion", EnglishName = "Resonance Potion",
            Category = "potion", Art = "ResonancePotion", Kind = "Catalog.PotionCombat", Stage = "Catalog.RecipeSoldByTheWitchDoctorAtNight",
            Description = "Catalog.WhipHitsMarkEnemiesWithResonanceYourMinionsDealIncreasedDamageTo",
            Note = "Catalog.TheRibbonsAroundTheBottlesHelpMeTellThemApart", Rarity = 2, Yield = 1,
            PixelWidth = 31, PixelHeight = 39,
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(1, "summoningpotion"),
                new(1, "stinger"),
            },
            Effects = new AlchemyCatalogEffect[]
            {
                new("Catalog.Duration", "Catalog.4Min"),
                new("Catalog.MarkDuration", "Catalog.3S"),
                new("Catalog.MinionDamage", "+20%"),
            },
        },
        new()
        {
            Id = "featherlight", Name = "Catalog.FeatherlightPotion", EnglishName = "Featherlight Potion",
            Category = "potion", Art = "FeatherlightPotion", Kind = "Catalog.PotionUtility", Stage = "Catalog.RecipeFoundInUndergroundGoldChests",
            Description = "Catalog.MakesYouLightAsAFeatherIncreasingMovementFlightAndBroomSpeed",
            Note = "Catalog.TheRibbonsAroundTheBottlesHelpMeTellThemApart", Rarity = 1, Yield = 3,
            PixelWidth = 19, PixelHeight = 38,
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(1, "moondew"),
                new(1, "feather"),
                new(1, "daybloom"),
            },
            Effects = new AlchemyCatalogEffect[]
            {
                new("Catalog.Duration", "Catalog.4Min"),
                new("Catalog.SpeedBonus", "+20%"),
            },
        },
        new()
        {
            Id = "isolation", Name = "Catalog.PurificationDew", EnglishName = "Purification Dew",
            Category = "curio", Art = "PurificationDew", Kind = "Catalog.CurioUtility", Stage = "Catalog.RecipeSoldByTheDryadInHardmode",
            Description = "Catalog.ReleasesPurificationDewAtTheTargetLocationItFallsThroughBlocksAn",
            Note = "Catalog.PerhapsAlchemyIsSimplyFindingNewPossibilitiesInOrdinaryThings", Rarity = 2, Yield = 1,
            PixelWidth = 27, PixelHeight = 28,
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(1, "moondew"),
                new(10, "powder"),
                new(1, "daybloom"),
            },
            Effects = new AlchemyCatalogEffect[]
            {
                new("Catalog.AreaOfEffect", "Catalog.AllNearbyTiles"),
                new("Catalog.Properties", "Catalog.FallsThroughBlocks"),
            },
        },
        new()
        {
            Id = "dropper", Name = "Catalog.AetherDropper", EnglishName = "Aether Dropper",
            Category = "curio", Art = "AetherDropper", Kind = "Catalog.CurioUtility", Stage = "Catalog.RequiresAlchemyLv3",
            Description = "Catalog.CollectsShimmerIntoBottle",
            Note = "Catalog.PerhapsAlchemyIsSimplyFindingNewPossibilitiesInOrdinaryThings", Rarity = 2, Yield = 1,
            PixelWidth = 27, PixelHeight = 28,
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(10, "glass"),
                new(3, "aetherblock"),
            },
            Effects = new AlchemyCatalogEffect[]
            {
                new("Catalog.Capacity", "Catalog.1TileOfShimmer"),
                new("Catalog.Use", "Catalog.ConsumedAfterExtraction"),
            },
        },
        new()
        {
            Id = "mimic", Name = "Catalog.AshenFacsimileDust", EnglishName = "Ashen Facsimile Dust",
            Category = "curio", Art = "AshenFacsimileDust", Kind = "Catalog.CurioUtility", Stage = "Catalog.RequiresAlchemyLv3",
            Description = "Catalog.UsesShimmerSTransmutingPowerToReplaceSomeMissingCraftingMaterial",
            Note = "Catalog.PerhapsAlchemyIsSimplyFindingNewPossibilitiesInOrdinaryThings", Rarity = 2, Yield = 50,
            PixelWidth = 32, PixelHeight = 32,
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(1, "shimmer"),
                new(1, "obsidian"),
            },
            Effects = new AlchemyCatalogEffect[]
            {
                new("Catalog.MaterialRequirement", "Catalog.OwnAtLeast1OfEachRequiredMaterial"),
                new("Catalog.HowToUse", "Catalog.RightClickSelectedRecipeToCraftWithDust"),
            },
        },
        new()
        {
            Id = "trace", Name = "Catalog.RevealingDust", EnglishName = "Revealing Dust",
            Category = "curio", Art = "RevealingDust", Kind = "Catalog.CurioUtility", Stage = "Catalog.RequiresAlchemyLv2",
            Description = "Catalog.RevealsAllEnemiesAndTraps",
            Note = "Catalog.PerhapsAlchemyIsSimplyFindingNewPossibilitiesInOrdinaryThings", Rarity = 2, Yield = 3,
            PixelWidth = 32, PixelHeight = 32,
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(1, "blinkroot"),
                new(1, "lens"),
            },
            Effects = new AlchemyCatalogEffect[]
            {
                new("Catalog.RevealRadius", "1000"),
                new("Catalog.Duration", "Catalog.5Min"),
            },
        },
        new()
        {
            Id = "rain", Name = "Catalog.BottledRain", EnglishName = "Bottled Rain",
            Category = "curio", Art = "BottledRain", Kind = "Catalog.CurioUtility", Stage = "Catalog.RequiresAlchemyLv1",
            Description = "Catalog.ReleasesAPortableRainCloudThatSpeedsUpNearbyPlantGrowthAndMakesW",
            Note = "Catalog.PerhapsAlchemyIsSimplyFindingNewPossibilitiesInOrdinaryThings", Rarity = 1, Yield = 1,
            PixelWidth = 31, PixelHeight = 38,
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(1, "moondew"),
                new(1, "raincloud"),
                new(1, "waterleaf"),
            },
            Effects = new AlchemyCatalogEffect[]
            {
                new("Catalog.Duration", "Catalog.60S"),
            },
        },
        new()
        {
            Id = "bread", Name = "Catalog.HoneyBread", EnglishName = "Honey Bread",
            Category = "food", Art = "HoneyBread", Kind = "Catalog.FoodSupport", Stage = "Catalog.InitiallyUnlocked",
            InitialUnlocked = true,
            Description = "Catalog.HoneyDrizzledBreadOneOfElainaSFavoriteSweetProvisionsItsEffectsV",
            Note = "Catalog.IMGoodAtMakingStewButDessertsAlwaysTemptMeMore", Rarity = 1, Yield = 2,
            PixelWidth = 28, PixelHeight = 20,
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(1, "honey"),
                new(1, "sugar"),
                new(2, "flour"),
            },
            Effects = new AlchemyCatalogEffect[]
            {
                new("Catalog.Elaina", "Catalog.PlentySatisfiedAndHappy"),
                new("Catalog.OtherCharacters", "Catalog.WellFed"),
                new("Catalog.Duration", "Catalog.5Min"),
            },
        },
        new()
        {
            Id = "stew", Name = "Catalog.BeefStew", EnglishName = "Beef Stew",
            Category = "food", Art = "BeefStew", Kind = "Catalog.FoodSupport", Stage = "Catalog.InitiallyUnlocked",
            InitialUnlocked = true,
            Description = "Catalog.AWarmingBeefAndPotatoStewThatElainaMakesWellThoughItIsNotHerFavo",
            Note = "Catalog.IMGoodAtMakingStewButDessertsAlwaysTemptMeMore", Rarity = 1, Yield = 1,
            PixelWidth = 30, PixelHeight = 27,
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(1, "beef"),
                new(3, "potato"),
                new(5, "water"),
            },
            Effects = new AlchemyCatalogEffect[]
            {
                new("Catalog.FoodEffect", "Catalog.ExquisitelyStuffed"),
                new("Catalog.Duration", "Catalog.5Min"),
            },
        },
        new()
        {
            Id = "brulee", Name = "Catalog.CaramelBrulee", EnglishName = "Caramel Brulee",
            Category = "food", Art = "CaramelBrulee", Kind = "Catalog.FoodSupport", Stage = "Catalog.InitiallyUnlocked",
            InitialUnlocked = true,
            Description = "Catalog.CrackTheDelicateCaramelCrustToRevealASilkyCustardBeneath",
            Note = "Catalog.IMGoodAtMakingStewButDessertsAlwaysTemptMeMore", Rarity = 1, Yield = 1,
            PixelWidth = 28, PixelHeight = 21,
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(3, "sugar"),
                new(1, "egg"),
                new(1, "water"),
            },
            Effects = new AlchemyCatalogEffect[]
            {
                new("Catalog.FoodEffect", "Catalog.WellFed"),
                new("Catalog.Duration", "Catalog.5Min"),
            },
        },
        new()
        {
            Id = "moondew", Name = "Catalog.MoonDew", EnglishName = "Moon Dew",
            Category = "material", Art = "MoonDew", Kind = "Catalog.INGREDIENTS", Stage = "Catalog.InitiallyUnlocked",
            Description = "Catalog.ABaseMixtureMadeFromBottledWaterAndMoonglowReadyForAlchemy",
            Note = "Catalog.KeepTrackOfWhereEachIngredientComesFromAndSpareYourselfTheNextRu", Rarity = 0, Yield = 3, InitialUnlocked = true,
            PixelWidth = 20, PixelHeight = 28,
            OutputMaterial = "moondew",
            Ingredients = new AlchemyCatalogIngredient[]
            {
                new(1, "water"),
                new(1, "moonglow"),
            },
            Effects = new AlchemyCatalogEffect[]
            {
                new("Catalog.Use", "Catalog.BaseForPotions"),
            },
        },
        new()
        {
            Id = "flour", Name = "Catalog.Flour", EnglishName = "Flour",
            Category = "material", Art = "Flour", Kind = "Catalog.INGREDIENTS", Stage = "Catalog.Merchant",
            Description = "Catalog.FineWhiteBakingFlourPerfectForSweetDough",
            Note = "Catalog.KeepTrackOfWhereEachIngredientComesFromAndSpareYourselfTheNextRu", Rarity = 0, Yield = 1,
            PixelWidth = 20, PixelHeight = 26,
            Material = "flour",
            Acquisition = "Catalog.PurchaseFromTheMerchant",
            Effects = new AlchemyCatalogEffect[]
            {
                new("Catalog.UsedIn", "Catalog.HoneyBread"),
            },
        },
        new()
        {
            Id = "sugar", Name = "Catalog.Sugar", EnglishName = "Sugar",
            Category = "material", Art = "Sugar", Kind = "Catalog.INGREDIENTS", Stage = "Catalog.Merchant",
            Description = "Catalog.SweetensDoughAndFormsACaramelCrustWhenCooked",
            Note = "Catalog.KeepTrackOfWhereEachIngredientComesFromAndSpareYourselfTheNextRu", Rarity = 0, Yield = 1,
            PixelWidth = 24, PixelHeight = 22,
            Material = "sugar",
            Acquisition = "Catalog.PurchaseFromTheMerchant",
            Effects = new AlchemyCatalogEffect[]
            {
                new("Catalog.UsedIn", "Catalog.HoneyBreadCaramelBrulee"),
            },
        },
        new()
        {
            Id = "egg", Name = "Catalog.Egg", EnglishName = "Egg",
            Category = "material", Art = "Egg", Kind = "Catalog.INGREDIENTS", Stage = "Catalog.Merchant",
            Description = "Catalog.ABasicIngredientForCookingAndBaking",
            Note = "Catalog.KeepTrackOfWhereEachIngredientComesFromAndSpareYourselfTheNextRu", Rarity = 0, Yield = 1,
            PixelWidth = 18, PixelHeight = 24,
            Material = "egg",
            Acquisition = "Catalog.PurchaseFromTheMerchant",
            Effects = new AlchemyCatalogEffect[]
            {
                new("Catalog.UsedIn", "Catalog.CaramelBrulee"),
            },
        },
        new()
        {
            Id = "beef", Name = "Catalog.Beef", EnglishName = "Beef",
            Category = "material", Art = "Beef", Kind = "Catalog.INGREDIENTS", Stage = "Catalog.TravelingMerchant",
            Description = "Catalog.BeefSuitedToSlowCooking",
            Note = "Catalog.KeepTrackOfWhereEachIngredientComesFromAndSpareYourselfTheNextRu", Rarity = 0, Yield = 1,
            PixelWidth = 26, PixelHeight = 22,
            Material = "beef",
            Acquisition = "Catalog.AlwaysStockedByTheTravelingMerchantWhenHeVisits",
            Effects = new AlchemyCatalogEffect[]
            {
                new("Catalog.UsedIn", "Catalog.BeefStew"),
            },
        },
        new()
        {
            Id = "potato", Name = "Catalog.Potato", EnglishName = "Potato",
            Category = "material", Art = "Potato", Kind = "Catalog.INGREDIENTS", Stage = "Catalog.TravelingMerchant",
            Description = "Catalog.TurnsSoftWhenStewedSoakingUpTheFlavorOfTheBroth",
            Note = "Catalog.KeepTrackOfWhereEachIngredientComesFromAndSpareYourselfTheNextRu", Rarity = 0, Yield = 1,
            PixelWidth = 26, PixelHeight = 22,
            Material = "potato",
            Acquisition = "Catalog.AlwaysStockedByTheTravelingMerchantWhenHeVisits",
            Effects = new AlchemyCatalogEffect[]
            {
                new("Catalog.UsedIn", "Catalog.BeefStew"),
            },
        },
        new()
        {
            Id = "shimmer", Name = "Catalog.BottledShimmer", EnglishName = "Bottled Shimmer",
            Category = "material", Art = "BottledShimmer", Kind = "Catalog.INGREDIENTS", Stage = "Catalog.AetherDropper",
            Description = "Catalog.BottledShimmerDescription",
            Note = "Catalog.KeepTrackOfWhereEachIngredientComesFromAndSpareYourselfTheNextRu", Rarity = 2, Yield = 1,
            PixelWidth = 18, PixelHeight = 22,
            Material = "shimmer",
            Acquisition = "Catalog.CollectFromShimmerWithAnAetherDropper",
            Effects = new AlchemyCatalogEffect[]
            {
                new("Catalog.UsedIn", "Catalog.AshenFacsimileDust"),
                new("Catalog.Use", "Catalog.ReleasesOneTileAndIsConsumed"),
            },
        },
    };
}
