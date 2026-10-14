using static 伊蕾娜.Localization.UIText;
using System.Collections.Frozen;
using System.Collections.Generic;
using Terraria.ID;
using Terraria.ModLoader;
using 伊蕾娜.Items.accessories;

namespace 伊蕾娜.ElainaModSkills.Loadout;

/// <summary>Tools/MageBalance 配装页键名到原版物品与前缀的映射；网页新增键时在这里同步添加，未知键在换装时跳过。</summary>
public static class LoadoutItems
{
    /// <summary>与网页 periods 一致：第 k 阶段为击败对应 Boss 后，角色等级上限 5k。</summary>
    public static string[] StageLabels =>
    [
        LocalizeUI("LoadoutStages.Stage1"),
        LocalizeUI("LoadoutStages.Stage2"),
        LocalizeUI("LoadoutStages.Stage3"),
        LocalizeUI("LoadoutStages.Stage4"),
        LocalizeUI("LoadoutStages.Stage5"),
        LocalizeUI("LoadoutStages.Stage6"),
        LocalizeUI("LoadoutStages.Stage7"),
        LocalizeUI("LoadoutStages.Stage8"),
        LocalizeUI("LoadoutStages.Stage9"),
        LocalizeUI("LoadoutStages.Stage10"),
        LocalizeUI("LoadoutStages.Stage11"),
        LocalizeUI("LoadoutStages.Stage12"),
        LocalizeUI("LoadoutStages.Stage13"),
        LocalizeUI("LoadoutStages.Stage14"),
        LocalizeUI("LoadoutStages.Stage15"),
        LocalizeUI("LoadoutStages.Stage16"),
        LocalizeUI("LoadoutStages.Stage17"),
        LocalizeUI("LoadoutStages.Stage18"),
        LocalizeUI("LoadoutStages.Stage19")
    ];

    /// <summary>头、身、腿；0 表示该部位保留玩家原有装备（法袍类套装不需要护腿）。</summary>
    public static readonly FrozenDictionary<string, int[]> Armors = new Dictionary<string, int[]>
    {
        ["wizardAmethyst"] = [ItemID.WizardHat, ItemID.AmethystRobe, 0],
        ["magicTopaz"] = [ItemID.MagicHat, ItemID.TopazRobe, 0],
        ["magicDiamond"] = [ItemID.MagicHat, ItemID.DiamondRobe, 0],
        ["wizardDiamond"] = [ItemID.WizardHat, ItemID.DiamondRobe, 0],
        ["meteor"] = [ItemID.MeteorHelmet, ItemID.MeteorSuit, ItemID.MeteorLeggings],
        ["jungle"] = [ItemID.JungleHat, ItemID.JungleShirt, ItemID.JunglePants],
        ["magicDiamondMeteor"] = [ItemID.MagicHat, ItemID.DiamondRobe, ItemID.MeteorLeggings],
        ["cobalt"] = [ItemID.CobaltHat, ItemID.CobaltBreastplate, ItemID.CobaltLeggings],
        ["palladium"] = [ItemID.PalladiumHeadgear, ItemID.PalladiumBreastplate, ItemID.PalladiumLeggings],
        ["mythril"] = [ItemID.MythrilHood, ItemID.MythrilChainmail, ItemID.MythrilGreaves],
        ["orichalcum"] = [ItemID.OrichalcumHeadgear, ItemID.OrichalcumBreastplate, ItemID.OrichalcumLeggings],
        ["adamantite"] = [ItemID.AdamantiteMask, ItemID.AdamantiteBreastplate, ItemID.AdamantiteLeggings],
        ["titanium"] = [ItemID.TitaniumHeadgear, ItemID.TitaniumBreastplate, ItemID.TitaniumLeggings],
        ["forbidden"] = [ItemID.AncientBattleArmorHat, ItemID.AncientBattleArmorShirt, ItemID.AncientBattleArmorPants],
        ["forbiddenMix"] = [ItemID.AdamantiteMask, ItemID.AdamantiteBreastplate, ItemID.AncientBattleArmorPants],
        ["hallowed"] = [ItemID.HallowedHeadgear, ItemID.HallowedPlateMail, ItemID.HallowedGreaves],
        ["chlorophyte"] = [ItemID.ChlorophyteHeadgear, ItemID.ChlorophytePlateMail, ItemID.ChlorophyteGreaves],
        ["hallowedChloroApprentice"] = [ItemID.HallowedHeadgear, ItemID.ChlorophytePlateMail, ItemID.ApprenticeTrousers],
        ["spectre"] = [ItemID.SpectreMask, ItemID.SpectreRobe, ItemID.SpectrePants],
        ["spectreDarkArtist"] = [ItemID.SpectreMask, ItemID.SpectreRobe, ItemID.ApprenticeAltPants],
        ["darkArtist"] = [ItemID.ApprenticeAltHead, ItemID.ApprenticeAltShirt, ItemID.ApprenticeAltPants],
        ["nebula"] = [ItemID.NebulaHelmet, ItemID.NebulaBreastplate, ItemID.NebulaLeggings],
    }.ToFrozenDictionary();

    public static readonly FrozenDictionary<string, int> Accessories = new Dictionary<string, int>
    {
        ["sorcerer"] = ItemID.SorcererEmblem,
        ["avenger"] = ItemID.AvengerEmblem,
        ["celestial"] = ItemID.CelestialEmblem,
        ["band"] = ItemID.BandofStarpower,
        ["regen"] = ItemID.ManaRegenerationBand,
        ["cuffs"] = ItemID.MagicCuffs,
        ["celestialCuffs"] = ItemID.CelestialCuffs,
        ["flower"] = ItemID.ManaFlower,
        ["naturesGift"] = ItemID.NaturesGift,
        ["destroyer"] = ItemID.DestroyerEmblem,
        ["eyeGolem"] = ItemID.EyeoftheGolem,
        ["celestialStone"] = ItemID.CelestialStone,
    }.ToFrozenDictionary();

    public static readonly FrozenDictionary<string, int> Prefixes = new Dictionary<string, int>
    {
        ["none"] = 0,
        ["arcane"] = PrefixID.Arcane,
        ["menacing"] = PrefixID.Menacing,
        ["warding"] = PrefixID.Warding,
        ["lucky"] = PrefixID.Lucky,
    }.ToFrozenDictionary();

    /// <summary>原版饰品查表；模组物品的 type 在加载后才确定，不能放进静态表，在这里单独解析。</summary>
    public static bool TryGetAccessory(string id, out int type)
    {
        if (id == "elainaHat")
        {
            type = ModContent.ItemType<ElainaHat>();
            return true;
        }
        return Accessories.TryGetValue(id, out type);
    }

    /// <summary>“机动／生存（占位）”与“空槽”：换装时保留该槽位原有饰品。</summary>
    public static bool KeepsSlot(string id) => id is "utility" or "empty";
}
