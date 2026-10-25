using System;
using KL.SkillSystem;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using 伊蕾娜.Buffs;
using 伊蕾娜.Config;
using 伊蕾娜.ElainaAttribute;
using 伊蕾娜.ElainaModAlchemy.Buffs;
using 伊蕾娜.ElainaModSkills;
using 伊蕾娜.ElainaModSkills.Skills.Flight;

namespace 伊蕾娜.Mounts;

public class 扫帚 : ModMount
{
    public override void SetStaticDefaults()
    {
        MountData.buff = ModContent.BuffType<扫帚buff>();
        MountData.playerYOffsets = [0];
        MountData.xOffset = 1;
        MountData.yOffset = -4;
        MountData.totalFrames = 1;
        MountData.spawnDust = 226;
        MountData.spawnDustNoGravity = true;
        MountData.heightBoost = 0;
        MountData.flightTimeMax = 320;
        MountData.fatigueMax = 500;
        MountData.fallDamage = 0f;
        MountData.usesHover = false;
        MountData.runSpeed = 8f;
        MountData.acceleration = .4f;
        MountData.blockExtraJumps = true;
        if (!Main.dedServ)
        {
            MountData.textureWidth = MountData.backTexture.Width();
            MountData.textureHeight = MountData.backTexture.Height();
        }
    }

    public override void UpdateEffects(Player player)
    {
        var flight = player.GetModPlayer<扫帚ModPlayer>();
        if (!BroomAccessSystem.CanRide(player))
        {
            flight.CancelFlight();
            player.mount.Dismount(player);
            return;
        }
        flight.PoweredFlight = !player.dead && player.GetModPlayer<ElainaModplayer>().Elaina;
        if (!flight.PoweredFlight) return;
        // 普通飞行完全免费，也不以剩余魔力作为悬停条件。
        player.controlJump = false;
        player.gravity = 0;
        player.fallStart = (int)(player.position.Y / 16f);
        player.mount._flyTime = 320;
        player.mount._fatigue = 0;
        flight.FlightSpeed = flight.CruiseSpeed;
        player.maxFallSpeed = flight.CruiseSpeed * FlightSkill.Balance.DashMultiplier;
    }

    public override void Dismount(Player player, ref bool skipDust)
    {
        player.GetModPlayer<扫帚ModPlayer>().CancelFlight();
        player.fullRotation = 0;
    }

    // 保留类型名以兼容已有坐骑、平台碰撞钩子；旧熟练度和输入逻辑已全部移除。
    public class 扫帚ModPlayer : ModPlayer
    {
        public bool PoweredFlight;
        public float FlightSpeed;
        public BroomFlightState FlightState { get; private set; } = new();
        private Vector2 dashDirection;
        private Vector2 dashVisualOrigin;
        private bool dashVisualPending;
        public int LearnedFlightLevel => Player.GetModPlayer<ElainaSkillModPlayer>()
            .TryGetUnlockedModSkill<FlightSkill>(out var skill) && skill.BasicStatus == Skill.SKillBasicStatus.UnLock
                ? Math.Clamp(skill.Level, 0,
                SkillBalanceSystem.TryGetSkill(nameof(FlightSkill), out var entry) && entry.MaxLevel > 0 ? entry.MaxLevel : 9) : 0;
        private float TravelMultiplier => Player.GetModPlayer<AlchemyBuffPlayer>().TravelSpeedMultiplier;
        public float CruiseSpeed => FlightSkill.Balance.Speed(LearnedFlightLevel) * TravelMultiplier;
        public float BoostSpeed => CruiseSpeed * FlightSkill.Balance.BoostMultiplier;
        public float HandlingPercent => FlightSkill.Balance.HandlingPercent;
        private bool Riding => Player.mount.Active && Player.mount.Type == ModContent.MountType<扫帚>();

        public override void ResetEffects() => PoweredFlight = false;
        public override void Initialize() => FlightState = new();
        public override void OnEnterWorld() => FlightState = new();
        public override void UpdateDead() => CancelFlight();
        public void CancelFlight()
        {
            PoweredFlight = false;
            FlightState.Cancel();
            dashVisualPending = false;
        }

        public override void PostUpdate()
        {
            if (!dashVisualPending) return;
            dashVisualPending = false;
            if (Main.dedServ || Player.whoAmI != Main.myPlayer || !Riding || !PoweredFlight) return;
            Projectile.NewProjectile(Player.GetSource_Misc("BroomDash"), dashVisualOrigin, Vector2.Zero,
                ModContent.ProjectileType<BroomDashWind>(), 0, 0, Player.whoAmI,
                dashDirection.ToRotation(), CruiseSpeed * FlightSkill.Balance.DashMultiplier,
                FlightBalance.Ticks(FlightSkill.Balance.DashSeconds));
        }

        public override void PostUpdateRunSpeeds()
        {
            if (!PoweredFlight || !Riding) return;
            // 速度属于骑乘者，绝不写回所有玩家共享的 MountData。
            Player.maxRunSpeed = Player.accRunSpeed = FlightSpeed;
            // 原版水平移动在 PreUpdateMovement 之前执行。让统一的二维控制器处理加速/刹车。
            Player.runAcceleration = Player.runSlowdown = 0f;
        }

        public override void PreUpdateMovement()
        {
            if (!Riding || !PoweredFlight)
            {
                FlightState.Cancel();
                return;
            }
            // 远端仅使用原版 PlayerControls 同步的速度，不读取本机按键或再次扣蓝。
            if (Player.whoAmI != Main.myPlayer || Main.netMode == NetmodeID.Server)
            {
                UpdateRotation();
                return;
            }
            var balance = FlightSkill.Balance;
            bool canControl = !Player.CCed && !Player.tongued && !Player.shimmering;
            bool inputEnabled = canControl && !BroomInputSystem.InputBlocked;
            var key = KeyBind.BroomAcceleration;
            bool wasDashing = FlightState.Dashing;
            // 方向控制属于基础扫帚；加速和冲刺必须实际习得飞行魔法。
            // 失去技能时同样走取消路径，丢弃正在加速/冲刺或等待释放的手势。
            FlightState.Tick(Main.GameUpdateCount, inputEnabled && LearnedFlightLevel > 0, key?.Current == true,
                key?.JustPressed == true, key?.JustReleased == true, balance,
                amount => Player.GetModPlayer<ElainaAttributeModPlayer>()
                    .ConsumeMagicPoint(amount, allowLifeConversion: false));
            if (!canControl) return;
            Vector2 direction = inputEnabled ? new Vector2(
                (Player.controlRight ? 1 : 0) - (Player.controlLeft ? 1 : 0),
                (Player.controlDown ? 1 : 0) - (Player.controlUp ? 1 : 0)) : Vector2.Zero;
            if (direction.LengthSquared() > 0) direction.Normalize();
            if (FlightState.DashStarted)
            {
                dashDirection = direction.LengthSquared() > 0 ? direction
                    : Player.velocity.SafeNormalize(new Vector2(Player.direction, 0));
                dashVisualOrigin = Player.MountedCenter;
                dashVisualPending = true;
            }
            FlightSpeed = CruiseSpeed * (FlightState.Boosting ? balance.BoostMultiplier : 1f);
            if (FlightState.Dashing)
                Player.velocity = dashDirection * CruiseSpeed * balance.DashMultiplier;
            else
            {
                // 冲刺自然结束时短暂保留余速并逐渐收回；取消技能/失焦仍立即回到普通上限。
                float speedLimit = MathHelper.Lerp(CruiseSpeed * balance.DashMultiplier,
                    FlightSpeed, FlightState.RecoveryProgress);
                if (Player.velocity.LengthSquared() > speedLimit * speedLimit)
                    Player.velocity = Player.velocity.SafeNormalize(Vector2.Zero) * speedLimit;
                Vector2 target = direction * FlightSpeed;
                // 操控性为固定百分比：速度成长和旅行加成不改变达到目标速度的响应时间。
                // 起步、转向、松键刹车统一收敛；足够接近时归位，避免悬停残余漂移。
                Player.velocity = Vector2.Lerp(Player.velocity, target, balance.HandlingPercent / 100f);
                float settleSpeed = FlightSpeed * .01f;
                if (Vector2.DistanceSquared(Player.velocity, target) <= settleSpeed * settleSpeed)
                    Player.velocity = target;
            }
            Player.maxFallSpeed = Math.Max(FlightSpeed, Player.velocity.Length());
            UpdateRotation();
            if (Main.netMode == NetmodeID.MultiplayerClient && (FlightState.DashStarted || wasDashing != FlightState.Dashing))
                NetMessage.SendData(MessageID.PlayerControls, -1, -1, null, Player.whoAmI);
        }

        private void UpdateRotation()
        {
            Player.fullRotation = MathHelper.Clamp(Player.velocity.Y / 5f, -1f, 1f) * .15f * Player.direction;
            Player.fullRotationOrigin = Player.Size * .5f;
        }
    }
}
