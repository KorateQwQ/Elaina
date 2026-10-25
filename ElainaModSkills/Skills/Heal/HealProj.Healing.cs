using System;
using Terraria.ID;

namespace 伊蕾娜.ElainaModSkills.Skills.Heal;

public partial class HealProj
{
    private const int HealingSeconds = HealSkill.HealingSeconds;
    private const int HealingInterval = 60;
    private const int HealingLifetime = HealingSeconds * HealingInterval;

    // ai[0]: total healing; ai[1]: target player; ai[2]: elapsed ticks.
    // Each endpoint advances the same projectile AI; no extra healing packets are sent.
    public override void SetDefaults()
    {
        base.SetDefaults();
        Projectile.hide = true;
        Projectile.timeLeft = HealingLifetime;
        Projectile.extraUpdates = 0;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
    }

    public override bool? CanDamage() => false;

    private bool UpdateHealing(out Player target)
    {
        if (!TryGetTarget(out target) || !float.IsFinite(Projectile.ai[0]) ||
            !float.IsFinite(Projectile.ai[2]) || Projectile.ai[2] < 0 || Projectile.ai[2] >= HealingLifetime)
        {
            Projectile.Kill();
            return false;
        }

        int elapsed = (int)++Projectile.ai[2];
        // A newly received projectile can already have elapsed AI time. Do not give it a fresh ten seconds.
        // Terraria decrements timeLeft after AI; keep the final (600th) tick available for the tenth heal.
        Projectile.timeLeft = Math.Min(Projectile.timeLeft, HealingLifetime - elapsed + 1);
        if (elapsed % HealingInterval != 0) return true;

        int totalHealing = (int)Math.Clamp((double)Projectile.ai[0], 0, int.MaxValue);
        int pulse = elapsed / HealingInterval;
        // Distribute any integer remainder across the ten pulses; 100 produces exactly 10 each second.
        int amount = (int)((long)totalHealing * pulse / HealingSeconds
            - (long)totalHealing * (pulse - 1) / HealingSeconds);
        int restored = Math.Min(amount, Math.Max(0, target.statLifeMax2 - target.statLife));
        if (restored <= 0) return true;

        target.statLife += restored;
        if (Main.netMode != NetmodeID.Server) target.HealEffect(restored, broadcast: false);
        return true;
    }

    private bool TryGetTarget(out Player target)
    {
        float index = Projectile.ai[1];
        if (float.IsFinite(index) && index >= 0 && index < Main.maxPlayers && index == MathF.Truncate(index)
            && Main.player[(int)index] is { active: true, dead: false } candidate)
        {
            target = candidate;
            return true;
        }
        target = null;
        return false;
    }
}
