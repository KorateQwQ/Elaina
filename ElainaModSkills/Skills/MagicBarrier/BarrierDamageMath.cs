using System;

namespace 伊蕾娜.ElainaModSkills.Skills.MagicBarrier;

internal static class BarrierDamageMath
{
    internal static double GetReduction(int currentMaxLife, int lostMaxLife)
    {
        return lostMaxLife <= 0 ? 0d : lostMaxLife / (Math.Max(1d, currentMaxLife) + lostMaxLife);
    }

    internal static int GetRemainingDamage(int incomingDamage, double absorbedDamage)
    {
        // No rounding benefit without a successful Magic payment.
        if (absorbedDamage <= 0d)
        {
            return incomingDamage;
        }

        return (int)Math.Floor(Math.Max(0d, incomingDamage - absorbedDamage));
    }

}
