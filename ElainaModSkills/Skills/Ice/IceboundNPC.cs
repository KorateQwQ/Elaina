using System.Collections.Generic;
using System.IO;
using KL.Extensions;
using Terraria.ID;
using Terraria.ModLoader.IO;

namespace 伊蕾娜.ElainaModSkills.Skills.Ice;

/// <summary>冰凌锁独立控制。不是Buff，不写入KL冻结状态；多个冰凌锁分别持有控制权。</summary>
public sealed class IceboundNPC : GlobalNPC
{
    public override bool InstancePerEntity => true;
    private static uint nextSpawnSerial;
    public uint SpawnSerial { get; private set; }
    private readonly Dictionary<(int Owner, int Identity), ulong> holds = new();
    private readonly List<(int Owner, int Identity)> expired = new();
    private bool pinned;
    private Vector2 pinnedPosition, savedVelocity;
    private Rectangle pinnedFrame;
    private double pinnedFrameCounter;
    private float pinnedRotation;
    private int pinnedDirection, pinnedSpriteDirection;

    public override void SetDefaults(NPC npc)
    {
        holds.Clear();
        pinned = false;
        SpawnSerial = Main.netMode == NetmodeID.MultiplayerClient ? 0 : ++nextSpawnSerial;
    }

    public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter writer) => writer.Write(SpawnSerial);
    public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader reader) => SpawnSerial = reader.ReadUInt32();

    public bool IsHeld(NPC npc)
    {
        expired.Clear();
        foreach (var pair in holds)
            if (Main.GameUpdateCount >= pair.Value) expired.Add(pair.Key);
        foreach (var key in expired) holds.Remove(key);
        if (holds.Count == 0 && pinned)
        {
            pinned = false;
            npc.velocity = savedVelocity;
        }
        return pinned;
    }

    public void Hold(NPC npc, int owner, int identity, int ticks)
    {
        IsHeld(npc);
        if (!pinned)
        {
            pinnedPosition = npc.position;
            savedVelocity = npc.velocity;
            pinnedFrame = npc.frame;
            pinnedFrameCounter = npc.frameCounter;
            pinnedRotation = npc.rotation;
            pinnedDirection = npc.direction;
            pinnedSpriteDirection = npc.spriteDirection;
            pinned = true;
        }
        holds[(owner, identity)] = Main.GameUpdateCount + (ulong)global::System.Math.Max(1, ticks);
        Pin(npc);
    }

    public void Release(NPC npc, int owner, int identity)
    {
        holds.Remove((owner, identity));
        IsHeld(npc);
    }

    public void Pin(NPC npc)
    {
        if (!IsHeld(npc)) return;
        npc.position = pinnedPosition;
        npc.velocity = Vector2.Zero;
        npc.frame = pinnedFrame;
        npc.frameCounter = pinnedFrameCounter;
        npc.rotation = pinnedRotation;
        npc.direction = pinnedDirection;
        npc.spriteDirection = pinnedSpriteDirection;
    }

    public override bool PreAI(NPC npc)
    {
        if (!IsHeld(npc)) return true;
        Pin(npc);
        return false;
    }

    public static void HoldGroup(NPC selected, Projectile source, int ticks)
    {
        NPC anchor = selected.GetElementAnchorNPC();
        if (anchor is not { active: true }) return;
        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (npc.GetElementAnchorNPC() != anchor) continue;
            npc.GetGlobalNPC<IceboundNPC>().Hold(npc, source.owner, source.identity, ticks);
        }
    }

    public static void ReleaseGroup(Projectile source)
    {
        // 按施法身份释放，目标已死亡/变形/换锚点时也能清除原部位，不影响另一名玩家的冰封。
        foreach (NPC npc in Main.ActiveNPCs)
            npc.GetGlobalNPC<IceboundNPC>().Release(npc, source.owner, source.identity);
    }
}

public sealed class IceboundSystem : ModSystem
{
    public override void Load() => On_NPC.UpdateNPC += UpdateHeldNPC;
    public override void Unload() => On_NPC.UpdateNPC -= UpdateHeldNPC;

    private static void UpdateHeldNPC(On_NPC.orig_UpdateNPC orig, NPC npc, int index)
    {
        // 保留正常Buff计时/伤害等更新，仅跳过AI并在完整物理更新后固定位置。
        // PreAI之后仍有重力、碰撞和移动，不能只return false就声称完全静止。
        if (!npc.TryGetGlobalNPC(out IceboundNPC state))
        {
            orig(npc, index);
            return;
        }
        state.Pin(npc);
        orig(npc, index);
        if (npc.active) state.Pin(npc);
    }

    public override void PostUpdateNPCs()
    {
        // 防止晚更新的关联部位或其他NPC的AI重新挪动已更新的部位。
        foreach (NPC npc in Main.ActiveNPCs) npc.GetGlobalNPC<IceboundNPC>().Pin(npc);
    }
}
