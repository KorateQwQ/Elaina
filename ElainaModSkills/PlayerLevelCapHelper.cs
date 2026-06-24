using System;

namespace 伊蕾娜.ElainaModSkills;

public static class PlayerLevelCapHelper
{
    public static int GetLevelCap(float bossState)
    {
        bossState = MathF.Max(bossState, 0f);

        float fullState = MathF.Floor(bossState);
        float fraction = bossState - fullState;
        int levelCap = (int)fullState * 5;

        if (fraction >= 0.5f)
        {
            levelCap += 3;
        }

        return Math.Max(levelCap, 1);
    }
}
