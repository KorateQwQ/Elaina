using System;
using Microsoft.Xna.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaModSkills.Skills.MagicBarrier;

public class MagicBarrierLifeRegenSystem : ModSystem
{
    public override void Load()
    {
        IL_Player.UpdateLifeRegen += ModifyLifeRegenDamage;
    }

    public override void Unload()
    {
        IL_Player.UpdateLifeRegen -= ModifyLifeRegenDamage;
    }

    private static void ModifyLifeRegenDamage(ILContext il)
    {
        ILCursor cursor = new(il);
        int patchedHits = 0;
        while (cursor.TryGotoNext(MoveType.Before,
            instruction => instruction.MatchSub(),
            instruction => instruction.MatchStfld<Player>(nameof(Player.statLife))))
        {
            // Stack: player, current life, tick damage. Replace only the damage so
            // vanilla still owns tick timing, counter consumption and death handling.
            VariableDefinition remainingDamage = new(il.Import(typeof(int)));
            il.Body.Variables.Add(remainingDamage);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<int, Player, int>>((damage, player) =>
                player.GetModPlayer<MagicBarrierSkill.MagicBarrierModPlayer>().ResolveLifeRegenHit(damage));
            cursor.Emit(OpCodes.Dup);
            cursor.Emit(OpCodes.Stloc, remainingDamage);
            cursor.Index += 2; // sub; stfld statLife

            if (!cursor.TryGotoNext(MoveType.Before, instruction =>
                instruction.MatchCall<CombatText>(nameof(CombatText.NewText))
                && instruction.Operand is MethodReference method
                && method.Parameters.Count == 5
                && method.Parameters[2].ParameterType.MetadataType == MetadataType.Int32))
            {
                throw new InvalidOperationException("Magic Barrier could not locate the life-regen damage text.");
            }

            // Replace the matching vanilla number with the remaining damage, or
            // suppress it when blocked. Blue shield feedback is emitted by the resolver.
            cursor.Remove();
            cursor.Emit(OpCodes.Ldloc, remainingDamage);
            cursor.EmitDelegate<Func<Rectangle, Color, int, bool, bool, int, int>>(
                (area, color, originalDamage, dramatic, dot, remaining) => remaining > 0
                    ? CombatText.NewText(area, color, remaining, dramatic, dot)
                    : -1);
            patchedHits++;
        }

        if (patchedHits == 0)
        {
            throw new InvalidOperationException("Magic Barrier could not locate any life-regen damage ticks.");
        }
    }
}
