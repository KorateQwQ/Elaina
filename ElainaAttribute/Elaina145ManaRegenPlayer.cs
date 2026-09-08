using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.ID;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace 伊蕾娜.ElainaAttribute;

/// <summary>
/// 给伊蕾娜暂存一套 1.4.5 风格的原版魔力回复逻辑。
/// 不包含空蓝施法与 slowMagicUse，只替换自然回蓝与回蓝延迟表现。
/// </summary>
public class Elaina145ManaRegenPlayer : ModPlayer
{
    public bool EnableElaina145ManaRegen => true;

    private int nebulaManaCounter;
    private readonly int[] naturalManaGainHistory = new int[60];
    private int naturalManaGainHistoryIndex;
    private int naturalManaGainHistoryCount;
    private int naturalManaGainHistoryTotal;
    private int pendingExternalManaGain;
    private int buffsManaBefore;
    private int miscEffectsManaBefore;

    public int NaturalManaGainThisTick { get; private set; }

    public float NaturalManaRegenPerSecond => naturalManaGainHistoryCount == 0
        ? 0f
        : naturalManaGainHistoryTotal * 60f / naturalManaGainHistoryCount;

    public override void PreUpdateBuffs()
    {
        buffsManaBefore = Player.statMana;
    }

    public override void PostUpdateBuffs()
    {
        RecordExternalManaGain(Player.statMana - buffsManaBefore);
    }

    public override void Load()
    {
        On_Player.UpdateManaRegen += On_Player_UpdateManaRegen;
        On_Player.UpdateEquips += On_PlayerOnUpdateEquips;
        IL_Player.Update += IL_Player_Update;
        On_Player.ItemCheck += On_Player_ItemCheck;
    }

    private void On_PlayerOnUpdateEquips(On_Player.orig_UpdateEquips orig, Player self, int i)
    {
        Elaina145ManaRegenPlayer modPlayer =
            self.GetModPlayer<Elaina145ManaRegenPlayer>();
        int manaBefore = self.statMana;

        orig(self,i);
        // 记录整个 UpdateEquips 阶段的实际回蓝，包括满蓝时的回复量。
        modPlayer.RecordExternalManaGain(self.statMana - manaBefore);
    }

    public override void Unload()
    {
        On_Player.UpdateManaRegen -= On_Player_UpdateManaRegen;
        On_Player.UpdateEquips -= On_PlayerOnUpdateEquips;
        IL_Player.Update -= IL_Player_Update;
        On_Player.ItemCheck -= On_Player_ItemCheck;
    }
    
    private static void IL_Player_Update(ILContext il)
    {
        var cursor = new ILCursor(il);
        if (!cursor.TryGotoNext(MoveType.Before, instruction => instruction.MatchCall("Terraria.ModLoader.PlayerLoader", "PostUpdateMiscEffects")))
            return;

        cursor.Emit(OpCodes.Dup);
        cursor.EmitDelegate<Action<Player>>(BeginMiscEffectsManaTracking);
        cursor.GotoNext(MoveType.After, instruction => instruction.MatchCall("Terraria.ModLoader.PlayerLoader", "PostUpdateMiscEffects"));
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Action<Player>>(EndMiscEffectsManaTracking);
    }

    private static void BeginMiscEffectsManaTracking(Player player)
    {
        player.GetModPlayer<Elaina145ManaRegenPlayer>().miscEffectsManaBefore = player.statMana;
    }

    private static void EndMiscEffectsManaTracking(Player player)
    {
        RecordPositiveManaDifference(player, player.GetModPlayer<Elaina145ManaRegenPlayer>().miscEffectsManaBefore);
    }

    private static void RecordPositiveManaDifference(Player player, int manaBefore)
    {
        Elaina145ManaRegenPlayer modPlayer = player.GetModPlayer<Elaina145ManaRegenPlayer>();
        modPlayer.RecordExternalManaGain(Math.Max(0, player.statMana - manaBefore));
    }

    /// <summary>
    /// 记录由装备、技能等系统直接产生的实际回蓝量。
    /// 会在当前帧的自然回蓝统计结算时合并进去。
    /// </summary>
    public void RecordExternalManaGain(int manaGain)
    {
        if (EnableElaina145ManaRegen && Player.GetModPlayer<ElainaModplayer>().Elaina)
            pendingExternalManaGain += Math.Max(0, manaGain);
    }

    private void On_Player_ItemCheck(On_Player.orig_ItemCheck orig, Player self)
    {
        bool wasUsingManaItem = false;
        Item item = self.inventory[self.selectedItem];
        if (EnableElaina145ManaRegen && self.GetModPlayer<ElainaModplayer>().Elaina && self.itemAnimation > 0 && item.mana > 0)
        {
            wasUsingManaItem = !(self.spaceGun && (item.type == ItemID.SpaceGun || item.type == ItemID.LaserRifle || item.type == ItemID.MeteorStaff || item.type == ItemID.ZapinatorGray));
        }

        orig(self);

        if (wasUsingManaItem)
            ApplyManaRegenerationDelay145(self);
    }

    private void On_Player_UpdateManaRegen(On_Player.orig_UpdateManaRegen orig, Player self)
    {
        NaturalManaGainThisTick = 0;
        if (!EnableElaina145ManaRegen || !self.GetModPlayer<ElainaModplayer>().Elaina)
        {
            orig(self);
            return;
        }

        Elaina145ManaRegenPlayer modPlayer = self.GetModPlayer<Elaina145ManaRegenPlayer>();
        if (self.dead)
        {
            modPlayer.ResetNaturalManaRegenHistory();
            modPlayer.pendingExternalManaGain = 0;
            modPlayer.NaturalManaGainThisTick = 0;
            UpdateManaRegen145(self);
            return;
        }

        int actualNaturalManaGain = UpdateManaRegen145(self);
        modPlayer.NaturalManaGainThisTick = actualNaturalManaGain;
        modPlayer.RecordNaturalManaGain(actualNaturalManaGain + modPlayer.pendingExternalManaGain);
        modPlayer.pendingExternalManaGain = 0;
    }

    private void ResetNaturalManaRegenHistory()
    {
        Array.Clear(naturalManaGainHistory, 0, naturalManaGainHistory.Length);
        naturalManaGainHistoryIndex = 0;
        naturalManaGainHistoryCount = 0;
        naturalManaGainHistoryTotal = 0;
    }

    private void RecordNaturalManaGain(int manaGain)
    {
        int gain = Math.Max(0, manaGain);
        if (naturalManaGainHistoryCount == naturalManaGainHistory.Length)
            naturalManaGainHistoryTotal -= naturalManaGainHistory[naturalManaGainHistoryIndex];
        else
            naturalManaGainHistoryCount++;

        naturalManaGainHistory[naturalManaGainHistoryIndex] = gain;
        naturalManaGainHistoryTotal += gain;
        naturalManaGainHistoryIndex = (naturalManaGainHistoryIndex + 1) % naturalManaGainHistory.Length;
    }

    private int UpdateManaRegen145(Player player)
    {
        bool isUsingItem = player.itemAnimation > 0 || player.reuseDelay > 0;
        Elaina145ManaRegenPlayer modPlayer = player.GetModPlayer<Elaina145ManaRegenPlayer>();
        ApplyNebulaBuffMana145(modPlayer, player);
        int actualNaturalManaGain = 0;

        if (player.manaRegenDelay > 0f)
        {
            player.manaRegenDelay--;
            player.manaRegenDelay -= player.manaRegenDelayBonus;
            if (IsStandingStillForManaRegen(player) || player.grappling[0] >= 0 || player.manaRegenBuff)
                player.manaRegenDelay--;
            if (player.usedArcaneCrystal)
                player.manaRegenDelay -= 0.05f;
        }

        bool canRegenMana = player.manaRegenDelay <= 0f;
        bool isShortDelayRegen = false;
        if (!canRegenMana && player.manaRegenDelay < 4f)
        {
            canRegenMana = true;
            isShortDelayRegen = true;
        }

        int minManaRegen = 2;
        if (canRegenMana)
        {
            int baseManaRegen = player.statManaMax2 / 3 + player.manaRegenBonus + 1;
            player.manaRegen = baseManaRegen;
            if (IsStandingStillForManaRegen(player) || player.grappling[0] >= 0 || player.manaRegenBuff)
                player.manaRegen += baseManaRegen;
            if (player.usedArcaneCrystal)
                player.manaRegen += player.statManaMax2 / 50;

            float manaRatio = player.statMana / (float)player.statManaMax2;
            float minManaScale = player.manaRegenBuff ? 1f : 0.5f;
            float manaScale = minManaScale + (1f - minManaScale) * manaRatio;

            if (isShortDelayRegen)
                manaScale *= 0.05f;

            player.manaRegen = (int)(player.manaRegen * manaScale);
            if (player.manaRegen < minManaRegen)
                player.manaRegen = minManaRegen;
        }
        else
        {
            player.manaRegen = 0;
        }

        player.manaRegenCount += player.manaRegen;
        while (player.manaRegenCount >= 120)
        {
            bool shouldShowFullManaEffect = false;
            player.manaRegenCount -= 120;
            // 每完成一个自然回蓝周期都记录一次，满蓝时也记录。
            actualNaturalManaGain++;
            if (player.statMana < player.statManaMax2)
            {
                player.statMana++;
                shouldShowFullManaEffect = true;
            }

            if (isUsingItem)
                shouldShowFullManaEffect = false;

            if (player.statMana >= player.statManaMax2)
            {
                if (player.whoAmI == Main.myPlayer && shouldShowFullManaEffect)
                {
                    SoundEngine.PlaySound(SoundID.MaxMana);
                    for (int i = 0; i < 5; i++)
                    {
                        int dustIndex = Dust.NewDust(player.position, player.width, player.height, 45, Alpha: byte.MaxValue, Scale: Main.rand.Next(20, 26) * 0.1f);
                        Main.dust[dustIndex].noLight = true;
                        Main.dust[dustIndex].noGravity = true;
                        Main.dust[dustIndex].velocity *= 0.5f;
                    }
                }

                player.statMana = player.statManaMax2;
            }
        }

        return actualNaturalManaGain;
    }

    public static void ApplyManaRegenerationDelay145(Player player)
    {
        player.manaRegenDelay = (int)(((1f - player.statMana / (float)player.statManaMax2) * 60f * 4f + 45f) * 0.7f);
        if (player.manaRegenBuff && player.manaRegenDelay > 20f)
            player.manaRegenDelay = 20f;

        int delayWithMana = 4;
        int maxDelay = 4;
        if (player.manaRegenDelay > maxDelay)
            player.manaRegenDelay = maxDelay;
        if (player.statMana <= 0)
            return;
        player.manaRegenDelay = delayWithMana;
        //PrintText("延迟"+player.manaRegenDelay);
    }

    private static int ApplyNebulaBuffMana145(Elaina145ManaRegenPlayer modPlayer, Player player)
    {
        if (player.nebulaLevelMana > 0)
        {
            int interval = 6;
            modPlayer.nebulaManaCounter += player.nebulaLevelMana;
            if (modPlayer.nebulaManaCounter < interval)
                return 0;

            modPlayer.nebulaManaCounter -= interval;
            if (player.statMana >= player.statManaMax2)
                return 0;

            player.statMana++;
            if (player.statMana >= player.statManaMax2)
                player.statMana = player.statManaMax2;
            return 1;
        }
        else
        {
            modPlayer.nebulaManaCounter = 0;
            return 0;
        }
    }

    private static bool IsStandingStillForManaRegen(Player player)
    {
        return player.IsStandingStillForSpecialEffects;
    }
}