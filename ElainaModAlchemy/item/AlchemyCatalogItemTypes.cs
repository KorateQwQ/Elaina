using Terraria.ID;
using Terraria.ModLoader;
using 伊蕾娜.ElainaModAlchemy.item.Curios;
using 伊蕾娜.ElainaModAlchemy.item.Foods;
using 伊蕾娜.ElainaModAlchemy.item.Materials;
using 伊蕾娜.ElainaModAlchemy.item.Potions;
using 伊蕾娜.ElainaModAlchemy.item.Tools;
using 伊蕾娜.ElainaModAlchemy.UI;

namespace 伊蕾娜.ElainaModAlchemy.item;

/// <summary>Keeps game content lookups out of the standalone notebook data model.</summary>
public sealed class AlchemyCatalogItemTypes : ModSystem
{
    public override void PostSetupContent() => AlchemyCatalog.ItemTypeResolver = Resolve;
    public override void Unload() => AlchemyCatalog.ItemTypeResolver = null;

    private static int Resolve(string id) => id switch
    {
        "water" => ItemID.BottledWater,
        "moonglow" => ItemID.Moonglow,
        "star" => ItemID.FallenStar,
        "glowingmushroom" => ItemID.GlowingMushroom,
        "daybloom" => ItemID.Daybloom,
        "honeyblock" => ItemID.HoneyBlock,
        "deathweed" => ItemID.Deathweed,
        "vertebra" => ItemID.Vertebrae,
        "rottenchunk" => ItemID.RottenChunk,
        "crystal" => ItemID.CrystalShard,
        "blinkroot" => ItemID.Blinkroot,
        "lens" => ItemID.Lens,
        "mandible" => ItemID.AntlionMandible,
        "stinger" => ItemID.Stinger,
        "feather" => ItemID.Feather,
        "powder" => ItemID.PurificationPowder,
        "glass" => ItemID.Glass,
        "iron" => ItemID.IronBar,
        "lead" => ItemID.LeadBar,
        "hallowed" => ItemID.HallowedBar,
        "obsidian" => ItemID.Obsidian,
        "pixiedust" => ItemID.PixieDust,
        "bone" => ItemID.Bone,
        "cloud" => ItemID.Cloud,
        "waterleaf" => ItemID.Waterleaf,
        "honey" => ItemID.BottledHoney,
        "wood" => ItemID.Wood,
        "gel" => ItemID.Gel,
        "mana" => ModContent.ItemType<MoonDewElixir>(),
        "painkiller" => ModContent.ItemType<Painkiller>(),
        "bloodlust" => ModContent.ItemType<BloodthirstPotion>(),
        "starpower" => ModContent.ItemType<StarPowerPotion>(),
        "focus" => ModContent.ItemType<ConcentrationPotion>(),
        "resonance" => ModContent.ItemType<ResonancePotion>(),
        "featherlight" => ModContent.ItemType<FeatherlightPotion>(),
        "isolation" => ModContent.ItemType<PurificationDew>(),
        "dropper" => ModContent.ItemType<AetherDropper>(),
        "mimic" => ModContent.ItemType<AshenFacsimileDust>(),
        "trace" => ModContent.ItemType<RevealingDust>(),
        "rain" => ModContent.ItemType<BottledRain>(),
        "bread" => ModContent.ItemType<HoneyBread>(),
        "stew" => ModContent.ItemType<BeefStew>(),
        "brulee" => ModContent.ItemType<CaramelBrulee>(),
        "moondew" => ModContent.ItemType<MoonDew>(),
        "resin" => ModContent.ItemType<WarmResin>(),
        "flour" => ModContent.ItemType<Flour>(),
        "sugar" => ModContent.ItemType<Sugar>(),
        "egg" => ModContent.ItemType<Egg>(),
        "beef" => ModContent.ItemType<Beef>(),
        "potato" => ModContent.ItemType<Potato>(),
        "shimmer" => ModContent.ItemType<ShimmerDroplet>(),
        _ => ItemID.None,
    };
}
