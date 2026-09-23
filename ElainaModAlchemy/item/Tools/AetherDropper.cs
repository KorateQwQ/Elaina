namespace 伊蕾娜.ElainaModAlchemy.item.Tools;

/// <summary>以太滴管：炼金物品，具体效果待后续实现。</summary>
public sealed class AetherDropper : AlchemyItem
{
    public override string EntryId => "dropper";

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.maxStack = 1;
    }
}
