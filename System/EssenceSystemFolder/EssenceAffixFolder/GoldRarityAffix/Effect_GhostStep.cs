using System;
using KL.Drawing;
using KL.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics;
using Terraria.Graphics.Renderers;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using NotImplementedException = System.NotImplementedException;

namespace 伊蕾娜.System.EssenceSystemFolder.EssenceAffixFolder.GoldRarityAffix;

public class Effect_GhostStep : IEssenceAffixEffect
{
    public static readonly Effect_GhostStep Instance = new();

    public void UpdateAccessory(Player player, in EssenceAffixEffectContext ctx)
    {
        // TODO: 触发逻辑还未实现
        player.GetModPlayer<GhostStepPlayer>().GhostStepDuration = (int)(ctx.GetLinearFactor() * 1.5f*60);
        player.GetModPlayer<GhostStepPlayer>().GhostStepDamageMultiplier = 1 + ctx.GetLinearFactor() * 0.25f;
        
    }

    public string GetDescription(in EssenceAffixEffectContext ctx)
    {
        float moveTimeRequired =  GhostStepPlayer.GhostStepNeedTime/60f; // 移动所需时间
        float ghostStepDuration = ctx.GetLinearFactor() * 1.5f; // 无敌时间
        float extraDamageMultiplier = 1 + ctx.GetLinearFactor() * 0.25f; //额外伤害倍率
        
        string moveTimeText = moveTimeRequired.ToString("0.0");
        string durationText = ghostStepDuration.ToString("0.0");
        string extraDamageText = extraDamageMultiplier.ToString("0.0");

        return Language.GetText("Mods.伊蕾娜.EssenceSystem.GhostStepDesc").WithFormatArgs(moveTimeText, durationText,extraDamageText)
            .Value;
    }

    class GhostStepPlayer : ModPlayer
    {
        private static bool _drawingAfterimages;
        
        private int GhostStepCD = 0;
        private int GhostStepTimer = 0;
        
        public bool InGhostStep => GhostStepCD > 0;
        
        public int GhostStepDuration = 0;
        public float GhostStepDamageMultiplier = 1;
        
        // 进入无敌时间需要固定三秒。
        public static float GhostStepNeedTime = 120;
        
        public bool needCancelGhostStep = false;
        
        public override void ResetEffects()
        {
            if (needCancelGhostStep)
            {
                CancelGhostStep();
                needCancelGhostStep = false;
            }
            GhostStepDuration = 0;
            GhostStepDamageMultiplier = 1;
            base.ResetEffects();
        }

        public override void UpdateEquips()
        {
            if (!InGhostStep)
            {
                if (Player.velocity.Length() > 0.1f)
                {
                    GhostStepTimer++;
                    if (GhostStepTimer >= GhostStepNeedTime)
                    {
                        ApplyGhostStep();
                    }
                }
                else
                {
                    GhostStepTimer = 0;
                }
            }
            else
            {
                GhostStepCD--;
                if (GhostStepCD <= 0)
                {
                    CancelGhostStep();
                }
            }
            base.UpdateEquips();
        }

        public override void DrawPlayer(Camera camera)
        {
            if (InGhostStep)
            {
                //PlayerEffectHelper.FaultEffect.Value.SetValue("MaxOffset", 0.325f);
                PlayerEffectHelper.FaultEffect.Value.SetValue("iTime", Main.GameUpdateCount % 1200 * 0.02f);
                PlayerEffectHelper.ApplyEffect(Main.LocalPlayer, PlayerEffectHelper.FaultEffect);
            }
            //PrintText(6);
            base.DrawPlayer(camera);
        }

        public override void OnHitAnything(float x, float y, Entity victim)
        {
            needCancelGhostStep = true;
            base.OnHitAnything(x, y, victim);
        }

        void ApplyGhostStep()
        {
            GhostStepCD = GhostStepDuration;
            GhostStepTimer = 0;
        }

        void CancelGhostStep()
        {
            if (!InGhostStep)return;
            GhostStepCD = 0;
            GhostStepTimer = 0;
        }

        public override bool CanBeHitByNPC(NPC npc, ref int cooldownSlot)
        {
            if (InGhostStep) return false;
            return base.CanBeHitByNPC(npc, ref cooldownSlot);
        }

        public override bool CanBeHitByProjectile(Projectile proj)
        {
            if (InGhostStep) return false;
            return base.CanBeHitByProjectile(proj);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (InGhostStep)
            {
                modifiers.FinalDamage *= GhostStepDamageMultiplier;
            }
            base.ModifyHitNPC(target, ref modifiers);
        }
    }
}