using KL.SkillSystem.SilkyUI;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader.IO;
using 伊蕾娜.ElainaAttribute;

namespace 伊蕾娜.ElainaModSkills.Skills.DollControl;

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

        if (FindKuma() != null) return base.CanUseSkill();
        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            if (!Main.projectile[i].active) return base.CanUseSkill();
        }
        return false;
    }

    public override bool PreUseSkill(IEntitySource source = null)
    {
        // 技能属于本地玩家；只有生成端支付魔力并创建弹幕，其他端使用原版同步。
        if (!CanUseSkill() || !Player.GetModPlayer<ElainaAttributeModPlayer>().ConsumeMagicPoint(MagicPointCost))
            return false;

        KumaPetProjectile kuma = FindKuma();
        if (kuma != null)
        {
            kuma.RecallToOwner(Player);
        }
        else
        {
            int index = Projectile.NewProjectile(source ?? Player.GetSource_Misc(nameof(DollControlSkill)),
                Player.Center, Vector2.Zero, ModContent.ProjectileType<KumaPetProjectile>(), 0, 0f, Player.whoAmI);
            if (index >= 0 && index < Main.maxProjectiles && Main.projectile[index].ModProjectile is KumaPetProjectile summoned)
                summoned.RecallToOwner(Player);
        }
        return base.PreUseSkill(source);
    }

    private KumaPetProjectile FindKuma()
    {
        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            Projectile projectile = Main.projectile[i];
            if (projectile.active && projectile.owner == Player.whoAmI && projectile.ModProjectile is KumaPetProjectile kuma)
                return kuma;
        }
        return null;
    }
}
