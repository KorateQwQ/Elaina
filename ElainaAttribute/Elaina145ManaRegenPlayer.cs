using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.ID;

namespace 伊蕾娜.ElainaAttribute;

/// <summary>
/// 给伊蕾娜暂存一套 1.4.5 风格的原版魔力回复逻辑。
/// 不包含空蓝施法与 slowMagicUse，只替换自然回蓝与回蓝延迟表现。
/// </summary>
public class Elaina145ManaRegenPlayer : ModPlayer
{
    public const bool EnableElaina145ManaRegen = true;

    private int nebulaManaCounter;

    public override void Load()
    {
        On_Player.UpdateManaRegen += On_Player_UpdateManaRegen;
    }

    public override void Unload()
    {
        On_Player.UpdateManaRegen -= On_Player_UpdateManaRegen;
    }

    private void On_Player_UpdateManaRegen(On_Player.orig_UpdateManaRegen orig, Player self)
    {
        if (!EnableElaina145ManaRegen || !self.GetModPlayer<ElainaModplayer>().Elaina)
        {
            orig(self);
            return;
        }

        if (self.manaRegenDelay > 4f)
            ApplyManaRegenerationDelay145(self);
        UpdateManaRegen145(self);
    }

    private static void UpdateManaRegen145(Player player)
    {
        bool isUsingItem = player.itemAnimation > 0 || player.reuseDelay > 0;
        ApplyNebulaBuffMana145(player.GetModPlayer<Elaina145ManaRegenPlayer>(), player);

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
    }

    private static void ApplyNebulaBuffMana145(Elaina145ManaRegenPlayer modPlayer, Player player)
    {
        if (player.nebulaLevelMana > 0)
        {
            int interval = 6;
            modPlayer.nebulaManaCounter += player.nebulaLevelMana;
            if (modPlayer.nebulaManaCounter < interval)
                return;

            modPlayer.nebulaManaCounter -= interval;
            player.statMana++;
            if (player.statMana >= player.statManaMax2)
                player.statMana = player.statManaMax2;
        }
        else
        {
            modPlayer.nebulaManaCounter = 0;
        }
    }

    private static bool IsStandingStillForManaRegen(Player player)
    {
        return player.IsStandingStillForSpecialEffects;
    }
}