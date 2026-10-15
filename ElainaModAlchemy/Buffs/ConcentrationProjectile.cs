using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace 伊蕾娜.ElainaModAlchemy.Buffs;

/// <summary>保留最初的射击方向，避免追踪、反弹或命中后的零速度把集中判定变成必定满额。</summary>
public sealed class ConcentrationProjectile : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    private bool hasAim;
    private Vector2 origin;
    private Vector2 direction;

    public override void OnSpawn(Projectile projectile, IEntitySource source)
    {
        if (source is EntitySource_Parent { Entity: Projectile parent } && parent.owner == projectile.owner)
        {
            var inherited = parent.GetGlobalProjectile<ConcentrationProjectile>();
            if (inherited.hasAim)
            {
                hasAim = true;
                origin = inherited.origin;
                direction = inherited.direction;
                return;
            }
        }

        if (!projectile.CountsAsClass(DamageClass.Ranged) || projectile.velocity.LengthSquared() < 0.0001f) return;
        hasAim = true;
        origin = projectile.Center;
        direction = Vector2.Normalize(projectile.velocity);
    }

    public float DamageBonus(Vector2 targetCenter)
    {
        if (!hasAim) return 0f;
        Vector2 toTarget = targetCenter - origin;
        if (toTarget.LengthSquared() < 0.0001f) return 0.3f;

        float cosine = MathHelper.Clamp(Vector2.Dot(direction, Vector2.Normalize(toTarget)), -1f, 1f);
        float angle = MathF.Acos(cosine);
        float maximumAngle = MathHelper.ToRadians(30f);
        // 最大判定角度 30 度：边缘 +5%，正对中心 +30%，范围外不触发。
        if (angle > maximumAngle + 0.00001f) return 0f;
        return MathHelper.Lerp(0.3f, 0.05f, MathHelper.Clamp(angle / maximumAngle, 0f, 1f));
    }

    public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
    {
        bitWriter.WriteBit(hasAim);
        if (!hasAim) return;
        binaryWriter.WriteVector2(origin);
        binaryWriter.WriteVector2(direction);
    }

    public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
    {
        hasAim = bitReader.ReadBit();
        if (!hasAim) return;
        origin = binaryReader.ReadVector2();
        direction = binaryReader.ReadVector2();
    }
}
