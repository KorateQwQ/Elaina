using KL.SkillSystem.SilkyUI;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader.IO;
using 伊蕾娜.ElainaAttribute;

namespace 伊蕾娜.ElainaModSkills.Skills.DollControl;

/// <summary>
/// 玩偶不在场时施法召唤熊玩偶；玩偶在场时左键改由 <see cref="DollCommandPlayer"/> 打开指令盘或标记挖掘区域，
/// 不再挥杖、不扣魔力。玩偶持续存在，直到“收回玩偶”指令、主人死亡或离开世界。
/// </summary>
[SkillUIInfo(State = 2, Pixels = 400)]
public class DollControlSkill : ElainaSkill
{
    protected override bool UsesLiveBalance => false;

    public override void Initialize()
    {
        base.Initialize();
        SetSummonCost();
    }

    public override void DeserializeData(TagCompound tag)
    {
        base.DeserializeData(tag);
        // 旧占位技能存档中有 60 秒的默认冷却。
        SetSummonCost();
    }

    private void SetSummonCost()
    {
        MagicPointCost = 20f;
        MaxCD = 1f;
    }

    public override bool CanUseSkill()
    {
        if (Main.netMode == NetmodeID.Server || !Player.active || Player.dead)
            return false;

        if (DollPetProjectile.FindOwned(Player) != null) return base.CanUseSkill();
        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            if (!Main.projectile[i].active) return base.CanUseSkill();
        }
        return false;
    }

    // 玩偶在场时左键属于指令盘与框选，魔杖不进入使用。
    public override bool BeginWandUse() => DollPetProjectile.FindOwned(Player) == null;

    public override bool PreUseSkill(IEntitySource source = null)
    {
        // 技能属于本地玩家；只有生成端支付魔力并创建弹幕，其他端使用原版同步。
        if (!CanUseSkill() || DollPetProjectile.FindOwned(Player) != null ||
            !Player.GetModPlayer<ElainaAttributeModPlayer>().ConsumeMagicPoint(MagicPointCost))
            return false;

        int index = Projectile.NewProjectile(source ?? Player.GetSource_Misc(nameof(DollControlSkill)),
            Player.Center, Vector2.Zero, ModContent.ProjectileType<KumaPetProjectile>(), 0, 0f, Player.whoAmI);
        if (index >= 0 && index < Main.maxProjectiles && Main.projectile[index].ModProjectile is KumaPetProjectile summoned)
        {
            summoned.RecallToOwner(Player);
            // 上一只玩偶意外消失时没来得及交还的收集物，由新召唤的玩偶交给主人。
            Player.GetModPlayer<DollCommandPlayer>().DeliverLoot(summoned.Projectile.Center, summoned.Projectile.GetSource_FromThis());
        }
        return base.PreUseSkill(source);
    }
}
