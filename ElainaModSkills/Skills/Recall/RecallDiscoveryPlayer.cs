using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace 伊蕾娜.ElainaModSkills.Skills.Recall;

/// <summary>个人探索经历不会因药水消耗、丢弃或技能重置而清除。</summary>
public sealed class RecallDiscoveryPlayer : ModPlayer
{
    public bool HasObtainedReturnPotion { get; private set; }

    public override void Initialize() => HasObtainedReturnPotion = false;

    public void Observe(Item item)
    {
        if (item != null && !item.IsAir && item.type == ItemID.PotionOfReturn)
            HasObtainedReturnPotion = true;
    }

    public override void SaveData(TagCompound tag) => tag[nameof(HasObtainedReturnPotion)] = HasObtainedReturnPotion;
    public override void LoadData(TagCompound tag) => HasObtainedReturnPotion = tag.GetBool(nameof(HasObtainedReturnPotion));
    public override void OnEnterWorld() => CheckPossessions();
    public override void PostUpdate() => CheckPossessions();

    private void CheckPossessions()
    {
        if (HasObtainedReturnPotion) return;
        // 补认旧存档中的个人储物；查看公共箱子不算获得物品。
        Check(Player.inventory);
        Check(Player.bank.item);
        Check(Player.bank2.item);
        Check(Player.bank3.item);
        Check(Player.bank4.item);
        if (Player.whoAmI == Main.myPlayer) Observe(Main.mouseItem);
    }

    private void Check(Item[] items)
    {
        if (HasObtainedReturnPotion) return;
        foreach (Item item in items)
        {
            Observe(item);
            if (HasObtainedReturnPotion) return;
        }
    }
}

public sealed class RecallDiscoveryItem : GlobalItem
{
    public override bool AppliesToEntity(Item entity, bool lateInstantiation) => entity.type == ItemID.PotionOfReturn;

    public override bool OnPickup(Item item, Player player)
    {
        player.GetModPlayer<RecallDiscoveryPlayer>().Observe(item);
        return true;
    }

    public override void OnConsumeItem(Item item, Player player) => player.GetModPlayer<RecallDiscoveryPlayer>().Observe(item);
}
