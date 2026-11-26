using System;
using System.Collections.Generic;
using System.IO;
using KL.DamageSystem;
using KL.Drawing;
using KL.Drawing.ThreeD;
using KL.Dusts;
using KL.Dusts.Burst;
using KL.Dusts.Glow;
using KL.Dusts.Smoke;
using KL.Extensions;
using KL.SkillSystem.SilkyUI;
using KL.Utils;
using Terraria.ID;


namespace 伊蕾娜.ElainaModSkills.Skills.Ice;
public class ShardbindProj : ElainaBasicProjectile
{
    private readonly List<VisualUnit> units = new();

    private bool initialized;
    private readonly HashSet<int> damagedEntities = new();
    private uint targetSpawnSerial;
    private bool damagePulse;
    private float damageRadius;
    private int totalDamage;
    private bool exploded;
    private bool targetLost;
    private Vector2? strikeCenter;
    private Vector2 visualCenter;
    private int sealTick;

    private const float SphereRadius = 200f;
    private const float IceConeSpeed = 58f;

    private int time = 0;
    internal int ElapsedTicks => time;
    internal ReticlePresentation ReticlePresentation { get; set; }
    internal float ReticleRadius { get; private set; } = 43.2f;

    private int burstTime => IceCycleBalance.LockBurstFrame;

    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2000;

    public void BindTarget(NPC target, uint selectedSpawnSerial)
    {
        Projectile.ai[0] = target.whoAmI + 1;
        Projectile.ai[2] = target.type;
        targetSpawnSerial = selectedSpawnSerial;
        ReticleRadius = ShardbindReticle.Radius(target);
        Projectile.Center = target.Center;
        Projectile.netUpdate = true;
        if (!Main.dedServ && Projectile.owner == Main.myPlayer)
            ReticlePresentation = IceConeTargetSystem.TakeHover(target);
    }

    public NPC LockedTarget
    {
        get
        {
            if (targetLost) return null;
            int index = (int)Projectile.ai[0] - 1;
            if (index < 0 || index >= Main.maxNPCs) return null;
            NPC target = Main.npc[index];
            if (target is not { active: true, life: > 0 } || target.type != (int)Projectile.ai[2]) return null;
            uint serial = target.GetGlobalNPC<IceboundNPC>().SpawnSerial;
            if (targetSpawnSerial == 0 && serial != 0) targetSpawnSerial = serial;
            return targetSpawnSerial != 0 && serial != 0 && serial != targetSpawnSerial ? null : target;
        }
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(targetSpawnSerial);
        writer.Write(targetLost);
        writer.Write(ReticleRadius);
        writer.Write(strikeCenter.HasValue);
        if (strikeCenter is Vector2 center) { writer.Write(center.X); writer.Write(center.Y); }
        base.SendExtraAI(writer);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        targetSpawnSerial = reader.ReadUInt32();
        bool lost = reader.ReadBoolean();
        ReticleRadius = reader.ReadSingle();
        Vector2? center = reader.ReadBoolean() ? new Vector2(reader.ReadSingle(), reader.ReadSingle()) : null;
        if (Main.netMode != NetmodeID.Server)
        {
            strikeCenter ??= center;
            if (lost && !targetLost) IceboundNPC.ReleaseGroup(Projectile);
            targetLost |= lost;
        }
        base.ReceiveExtraAI(reader);
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 20;
        Projectile.friendly = false;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = ShardbindVisualSettings.ControllerLifetime;
        Projectile.alpha = 0;
        Projectile.hide = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        InfusionElement = ElementType.Ice;

        //ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 5000;
        base.SetDefaults();
    }

    public override void AI()
    {
        NPC target = LockedTarget;
        bool controlsSeal = Main.netMode != NetmodeID.MultiplayerClient;
        if (target == null && controlsSeal)
            LoseTarget();

        if (!initialized) sealTick = ShardbindSequence.SealTick;
        // 伤害由施法客户端逐帧结算，收到服务器定点/脱锁包时不能跳过本地的穿刺帧。
        int nextTime = initialized && !controlsSeal && HasAuthority() ? time + 1 : Math.Max(time, (int)Projectile.ai[1]) + 1;
        if (!controlsSeal && !strikeCenter.HasValue && nextTime >= sealTick)
        {
            if (time >= sealTick - 1)
            {
                Projectile.timeLeft = ShardbindVisualSettings.ControllerLifetime - time;
                return;
            }
            nextTime = sealTick - 1;
        }
        if (!controlsSeal && strikeCenter.HasValue) nextTime = Math.Max(nextTime, sealTick);
        time = nextTime;
        Projectile.ai[1] = time;
        Projectile.timeLeft = Math.Min(Projectile.timeLeft, Math.Max(1, ShardbindVisualSettings.ControllerLifetime - time));
        Vector2 center = strikeCenter ?? Projectile.Center;
        if (!strikeCenter.HasValue && target != null)
            center = target.Center;
        bool sealing = controlsSeal && !strikeCenter.HasValue && time >= sealTick;
        Projectile.Center = center;
        if (strikeCenter.HasValue && target != null && !InsideImpact(target, center))
        {
            if (controlsSeal) LoseTarget();
            else IceboundNPC.ReleaseGroup(Projectile);
            target = null;
        }
        if ((sealing || strikeCenter.HasValue) && target != null && time < burstTime)
            IceboundNPC.HoldGroup(target, Projectile, burstTime - time + 2);
        if (sealing)
        {
            center = target?.Center ?? center;
            strikeCenter = center;
            Projectile.Center = center;
        }
        if (initialized && center != visualCenter)
            foreach (VisualUnit visual in units)
                if (visual is IceCone3DUnit cone) cone.MoveWithTarget(center - visualCenter);
        visualCenter = center;
        if (sealing && Main.netMode == NetmodeID.Server)
        {
            if (target != null) NetMessage.SendData(MessageID.SyncNPC, number: target.whoAmI);
            NetMessage.SendData(MessageID.SyncProjectile, number: Projectile.whoAmI);
        }
        InitializeUnits();
        foreach (VisualUnit visual in units)
            if (visual is IceCone3DUnit cone)
            {
                cone.CatchUp(time - 1);
                cone.ChargeProgress = ShardbindVisuals.Charge(time);
            }
        VisualUnit.UpdateAll(units);

        foreach (VisualUnit visual in units)
        {
            if (visual is not IceCone3DUnit cone) continue;
            if (!cone.ArrivedThisUpdate || time >= burstTime) continue;
            if (!Main.dedServ) ShakeShardImpact(cone);
            ApplyDamagePulse(IceCycleBalance.LockShardDamage(totalDamage, cone.Index, units.Count), IceCycleBalance.LockImpactRadius);
            if (target != null && (!target.active || target.life <= 0))
            {
                if (controlsSeal) LoseTarget();
                else IceboundNPC.ReleaseGroup(Projectile);
                target = null;
            }
            if (HasAuthority()) Projectile.netUpdate = true;
        }

        if (time >= burstTime && !exploded)
        {
            exploded = true;
            ApplyDamagePulse(IceCycleBalance.LockFinalDamage(totalDamage), IceCycleBalance.LockRadius);
            IceboundNPC.ReleaseGroup(Projectile);
            if (HasAuthority()) Projectile.netUpdate = true;
            if (!Main.dedServ) SpawnFinalBurst();
        }
        else if (time > burstTime)
            IceboundNPC.ReleaseGroup(Projectile);

        // 星闪、延迟粒子只属于本地表现；终爆当帧由 SpawnFinalBurst 发射，避免重复。
        if (!Main.dedServ && time != burstTime) ShardbindVisuals.EmitEvents(Projectile.Center, time);
        base.AI();
    }

    private void LoseTarget()
    {
        if (targetLost) return;
        targetLost = true;
        Projectile.ai[0] = 0;
        IceboundNPC.ReleaseGroup(Projectile);
        // 保留控制器当前位置和所有视觉单元，剩余AOE继续完成，不重新找目标。
        if (Main.netMode == NetmodeID.Server)
            NetMessage.SendData(MessageID.SyncProjectile, number: Projectile.whoAmI);
    }

    private void ApplyDamagePulse(int damage, float radius)
    {
        if (!HasAuthority() || damage <= 0) return;
        int previousDamage = Projectile.damage;
        ElementalGlobalProjectile element = Projectile.GetGlobalProjectile<ElementalGlobalProjectile>();
        var configured = element.InfusionElement;
        // 前置冰刺只造成伤害，终爆才开放积蓄。
        if (!exploded) element.InfusionElement = ElementType.Ice;
        damageRadius = radius;
        damagedEntities.Clear();
        Array.Clear(Projectile.localNPCImmunity);
        Projectile.damage = damage;
        damagePulse = Projectile.friendly = true;
        Projectile.Damage();

        damagePulse = Projectile.friendly = false;
        Projectile.damage = previousDamage;
        element.InfusionElement = configured;
    }

    private void ShakeShardImpact(IceCone3DUnit cone)
    {
        Vector2 center = Projectile.Center;
        float intensity = GetCameraImpactIntensity(center);
        if (intensity <= 0f) return;

        Vector2 direction = cone.GetImpactDirection();
        if (direction == Vector2.Zero) return;
        GamePlayStatic.ShakeScreen(center, direction, 2.0f * intensity, 10f, 6);
    }

    private void SpawnFinalBurst()
    {
        Vector2 center = Projectile.Center;
        ShardbindVisuals.EmitBurstEvents(center, 0);

        float intensity = GetCameraImpactIntensity(center);
        if (intensity <= 0f) return;

        GamePlayStatic.ShakeScreen(center, ShardbindVisualSettings.BurstShakeDirection,
            ShardbindVisualSettings.BurstShakeStrength * intensity, ShardbindVisualSettings.BurstShakeFrequency,
            ShardbindVisualSettings.BurstShakeTicks);
        DrawHelper.CreateRadialBlur(center, ShardbindVisualSettings.BurstBlurStrength * intensity,
            ShardbindVisualSettings.BurstBlurTicks, iteration: ShardbindVisualSettings.BurstBlurSamples);
    }

    private static float GetCameraImpactIntensity(Vector2 center)
    {
        const float maxDistance = 1500f;
        float distance = Vector2.Distance(Main.LocalPlayer.Center, center);
        if (distance > maxDistance) return 0f;

        return MathHelper.SmoothStep(1f, .7f, distance / maxDistance);
    }

    public override bool ShouldUpdatePosition()
    {
        return false;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        // 控制器从 BehindProjectiles 提前提交，光片与粒子共用本帧的前景 HDR。
        if (time <= IceCycleBalance.LockChargeStartFrame || time >= IceCycleBalance.LockBurstFrame) return false;
        LayerDrawRequestSystem.RequestBloom("Elaina:Shardbind:Glow", LayerDrawRequestSystem.DrawTargetLayer.OverPlayers,
            LayerDrawRequestSystem.DrawTiming.Before, DrawVisual);
        return false;
    }

    public override bool? CanDamage() => damagePulse && HasAuthority();

    internal static bool InsideImpact(NPC target, Vector2 center) =>
        Vector2.DistanceSquared(target.Hitbox.ClosestPointInRect(center), center)
        <= IceCycleBalance.LockImpactRadius * IceCycleBalance.LockImpactRadius;

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
        Vector2.DistanceSquared(targetHitbox.ClosestPointInRect(Projectile.Center), Projectile.Center)
        <= damageRadius * damageRadius;

    // 共享血池只吃一次；独立血条的 Boss 部件和不同敌人正常受到范围伤害。
    public override bool? CanHitNPC(NPC target) =>
        damagedEntities.Contains(target.realLife >= 0 ? target.realLife : target.whoAmI) ? false : null;

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        if (WaterExposureNPC.IsWet(target))
            modifiers.SourceDamage *= SkillHitBalance.ShardbindWetDamageMultiplier;
        base.ModifyHitNPC(target, ref modifiers);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        damagedEntities.Add(target.realLife >= 0 ? target.realLife : target.whoAmI);
        base.OnHitNPC(target, hit, damageDone);
    }

    private void DrawVisual() => ShardbindVisuals.DrawCharge(Projectile.Center, time);

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
        List<int> behindProjectiles, List<int> overPlayers,
        List<int> overWiresUI)
    {
        if(time<burstTime) VisualUnit.DrawAll(units);
        behindProjectiles.Add(index);
    }

    public override void OnKill(int timeLeft)
    {
        IceboundNPC.ReleaseGroup(Projectile);
        VisualUnit.KillAll(units);
        base.OnKill(timeLeft);
    }

    private void InitializeUnits()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        totalDamage = Projectile.damage;
        Vector2 lockCenter = Projectile.Center;
        List<IceConeSpawnData> spawnData = BuildIceCone3DSpawnData(lockCenter, SphereRadius);
        foreach (IceConeSpawnData spawnDataEntry in spawnData)
        {
            Vector2 shootVelocity = (lockCenter - spawnDataEntry.SpawnWorldPosition)
                .SafeNormalize(Vector2.UnitX) * IceConeSpeed;
            Vector3 spawnPosition3D = new(spawnDataEntry.SpawnWorldPosition, spawnDataEntry.StartDepth);
            Vector3 targetPosition3D = new(lockCenter, 0f);
            Vector3 velocity3D = GetInitialVelocity3D(spawnPosition3D, targetPosition3D, shootVelocity.Length());
            VisualUnit.Spawn(units, IceCone3DUnit.CreatePrepared(spawnPosition3D, targetPosition3D, velocity3D, spawnData.Count, units.Count), Projectile);
        }
    }

    private static Vector3 GetInitialVelocity3D(Vector3 spawnPosition3D, Vector3 targetPosition3D, float speed)
    {
        Vector3 toTarget = targetPosition3D - spawnPosition3D;
        Vector3 direction3D = toTarget.LengthSquared() > 0.0001f
            ? Vector3.Normalize(toTarget)
            : Vector3.UnitZ;

        return direction3D * speed;
    }

    private static List<IceConeSpawnData> BuildIceCone3DSpawnData(Vector2 lockCenter, float sphereRadius)
    {
        List<IceConeSpawnData> result = new();
        List<IceConeRotationConfig> rotations = BuildIceConeFixedRotations();
        int triggerFrame = 56;

        int index = 0;
        foreach (IceConeRotationConfig rotation in rotations)
        {
            Vector3 sphereDirection = GetIceConeSphereDirection(rotation);
            Vector3 spawnOffset3D = sphereDirection * sphereRadius;
            Vector2 spawnWorldPosition = lockCenter + new Vector2(spawnOffset3D.X, spawnOffset3D.Y);
            switch (index)
            {
                case 0:
                    result.Add(new IceConeSpawnData(1, spawnWorldPosition, spawnOffset3D.Z));
                    break;
                case 1:
                    result.Add(new IceConeSpawnData(15, spawnWorldPosition, spawnOffset3D.Z));
                    break;
                case 2:
                    result.Add(new IceConeSpawnData(27, spawnWorldPosition, spawnOffset3D.Z));
                    break;
                case 3:
                    result.Add(new IceConeSpawnData(37, spawnWorldPosition, spawnOffset3D.Z));
                    break;
                case 4:
                    result.Add(new IceConeSpawnData(44, spawnWorldPosition, spawnOffset3D.Z));
                    break;
                case 5:
                    result.Add(new IceConeSpawnData(49, spawnWorldPosition, spawnOffset3D.Z));
                    break;
                case 6:
                    result.Add(new IceConeSpawnData(52, spawnWorldPosition, spawnOffset3D.Z));
                    break;
                case 7:
                    result.Add(new IceConeSpawnData(54, spawnWorldPosition, spawnOffset3D.Z));
                    break;
                default:
                    result.Add(new IceConeSpawnData(triggerFrame, spawnWorldPosition, spawnOffset3D.Z));
                    triggerFrame += 2;
                    break;
            }

            index++;
        }

        return result;
    }

    private static List<IceConeRotationConfig> BuildIceConeFixedRotations()
    {
        return new List<IceConeRotationConfig>
        {
            new(-2.8f, 0.2f),
            new(-0.0f, 0.2f),
            new(2.5f, 0.3f),
            new(0.6f, -0.3f),
            new(1.9f, 0.6f),
            new(2.5f, 0.0f),
            new(3.1f, -0.8f),
            new(-0.5f, 0.3f),
            new(-0.3f, -0.6f),
            new(0.2f, 0.6f),
            new(0.3f, -0.6f),
            new(-0.3f, 0.6f),
            new(-0.8f, 0.6f),
            new(-2.6f, -0.4f),
            new(-1f, -0.2f),

        };
    }

    private static Vector3 GetIceConeSphereDirection(IceConeRotationConfig rotation)
    {
        Vector3 baseDirection = Vector3.UnitZ;
        Matrix rotationMatrix = Matrix.CreateFromYawPitchRoll(rotation.Yaw, rotation.Pitch, rotation.Roll);
        Vector3 sphereDirection = Vector3.TransformNormal(baseDirection, rotationMatrix);
        return sphereDirection.LengthSquared() > 0.0001f
            ? Vector3.Normalize(sphereDirection)
            : Vector3.UnitZ;
    }

    private readonly struct IceConeRotationConfig
    {
        public IceConeRotationConfig(float yaw, float pitch, float roll = 0f)
        {
            Yaw = yaw;
            Pitch = pitch;
            Roll = roll;
        }

        public float Yaw { get; }
        public float Pitch { get; }
        public float Roll { get; }
    }

    private readonly record struct IceConeSpawnData(int TriggerFrame, Vector2 SpawnWorldPosition, float StartDepth);

}
