using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace 伊蕾娜.ElainaModSkills.Skills.DollControl;

public sealed partial class DollCommandPlayer
{
    // 同一目标只能被一只玩偶认领；物块、墙、放盆各自独立，(true,true) 留给照料花盆。认领仅在主人端维护。
    private readonly Dictionary<(Point Point, bool Wall, bool Planter), DollPetProjectile> workClaims = [];

    private bool IsClaimed(Point point, bool wall, bool planter, DollPetProjectile worker)
    {
        var key = (point, wall, planter);
        if (!workClaims.TryGetValue(key, out var assigned)) return false;
        // 弹幕被顶掉、死亡或槽位被复用时，旧认领不能阻挡其他玩偶接手。
        if (!assigned.Projectile.active || assigned.Projectile.owner != Player.whoAmI
            || assigned.Projectile.ModProjectile != assigned)
        {
            workClaims.Remove(key);
            return false;
        }
        return assigned != worker;
    }

    private void ClaimTarget(Point point, bool wall, bool planter, DollPetProjectile worker)
    {
        if (worker != null) workClaims[(point, wall, planter)] = worker;
    }

    internal void ReleaseTarget(Point point, bool wall, bool planter, DollPetProjectile worker)
    {
        var key = (point, wall, planter);
        if (workClaims.TryGetValue(key, out var assigned) && assigned == worker) workClaims.Remove(key);
    }
}
