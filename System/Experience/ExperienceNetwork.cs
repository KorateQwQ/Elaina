using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace 伊蕾娜.System.Experience;

internal static class ExperienceNetwork
{
    internal static void Award(Player target, int amount)
    {
        if (amount <= 0 || Main.netMode == NetmodeID.MultiplayerClient) return;
        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            Receive(target, amount);
            return;
        }

        ModPacket packet = ModContent.GetInstance<ExperienceDropNPC>().Mod.GetPacket();
        packet.Write((byte)global::伊蕾娜.伊蕾娜.MessageType.Experience);
        packet.Write(target.whoAmI);
        packet.Write(amount);
        packet.Send(target.whoAmI);
    }

    internal static void HandlePacket(BinaryReader reader, int playerIndex)
    {
        int amount = reader.ReadInt32();
        // 只有服务器能确认吸收。客户端不能发送“加经验”请求给服务器或其他玩家。
        if (Main.netMode != NetmodeID.MultiplayerClient || playerIndex != Main.myPlayer || amount <= 0) return;
        Receive(Main.LocalPlayer, amount);
    }

    private static void Receive(Player player, int amount)
    {
        if (!player.active || !player.GetModPlayer<ElainaModplayer>().Elaina) return;
        // 与现有 Boss 进度一致：服务器确认奖励，角色拥有端修改并保存等级/经验。
        // 不因网络延迟期间玩家死亡而丢弃已经确认的奖励。
        player.GetModPlayer<ElainaStatePlayer>().AddExperience(amount);
        SoundEngine.PlaySound(SoundID.Grab with { Volume = .35f, Pitch = .45f, MaxInstances = 3 }, player.Center);
        for (int i = 0; i < 6; i++)
        {
            Vector2 velocity = new Vector2(1.8f, 0).RotatedBy(MathHelper.TwoPi * i / 6);
            Dust dust = Dust.NewDustPerfect(player.Center, DustID.Enchanted_Pink, velocity, 100, new Color(202, 163, 255), .8f);
            dust.noGravity = true;
        }
    }
}
