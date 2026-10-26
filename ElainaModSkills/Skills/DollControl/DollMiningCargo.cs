using System;

namespace 伊蕾娜.ElainaModSkills.Skills.DollControl;

public sealed partial class DollCommandPlayer
{
    // 物品总数仍保存在角色背包；此表只记录活着的哪只玩偶负责搬运，不写入存档。
    private readonly Dictionary<DollPetProjectile, Dictionary<int, int>> miningCarriers = [];

    private bool LiveCarrier(DollPetProjectile worker) => worker != null && worker.Projectile.active
        && worker.Projectile.owner == Player.whoAmI && worker.Projectile.ModProjectile == worker && !worker.IsDismissing;

    private void AssignMiningCargo(DollPetProjectile worker, int type, int stack)
    {
        if (!LiveCarrier(worker) || stack <= 0) return;
        if (!miningCarriers.TryGetValue(worker, out var cargo)) miningCarriers.Add(worker, cargo = []);
        cargo[type] = (int)Math.Min(int.MaxValue, (long)cargo.GetValueOrDefault(type) + stack);
    }

    internal bool HasMiningCargo(DollPetProjectile worker) => LiveCarrier(worker)
        && miningCarriers.TryGetValue(worker, out var cargo) && cargo.Count > 0;

    private int DeliverableMiningLoot(int type, int total, DollPetProjectile worker)
    {
        if (worker == null) return total; // 显式重新召唤/恢复存档时统一交还。
        long reservedByOthers = 0;
        foreach (var (carrier, cargo) in miningCarriers)
            if (carrier != worker && LiveCarrier(carrier)) reservedByOthers += cargo.GetValueOrDefault(type);
        return (int)Math.Max(0, total - reservedByOthers);
    }
}
