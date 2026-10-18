using System;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaModSkills.Skills.MagicBarrier;

public class MagicBarrierLifeRegenSystem : ModSystem
{
    public override void Load()
    {
        IL_Player.UpdateLifeRegen += ModifyLifeRegen;
    }

    public override void Unload()
    {
        IL_Player.UpdateLifeRegen -= ModifyLifeRegen;
    }

    private static void ModifyLifeRegen(ILContext il)
    {
        ILCursor cursor = new(il);
        // Clamp the final rate after vanilla and mod regen effects, immediately before
        // lifeRegenCount += lifeRegen. This also covers starvation's late override.
        if (!cursor.TryGotoNext(MoveType.Before,
            instruction => instruction.MatchLdarg(0),
            instruction => instruction.MatchLdarg(0),
            instruction => instruction.MatchLdfld<Player>(nameof(Player.lifeRegenCount)),
            instruction => instruction.MatchLdarg(0),
            instruction => instruction.MatchLdfld<Player>(nameof(Player.lifeRegen)),
            instruction => instruction.MatchAdd(),
            instruction => instruction.MatchStfld<Player>(nameof(Player.lifeRegenCount))))
        {
            throw new InvalidOperationException("Magic Barrier could not locate life-regen accumulation.");
        }

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Action<Player>>(player =>
            player.GetModPlayer<MagicBarrierSkill.MagicBarrierModPlayer>().ApplyLifeRegenFloor());
    }
}
