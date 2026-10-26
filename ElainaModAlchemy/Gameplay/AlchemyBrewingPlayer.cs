using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using 伊蕾娜.ElainaModAlchemy.UI;
using 伊蕾娜.ElainaModSkills.Skills.Alchemy;
using static 伊蕾娜.Localization.UIText;

namespace 伊蕾娜.ElainaModAlchemy.Gameplay;

/// <summary>Owns the cauldron interaction and pending job; the notebook only observes it.</summary>
public sealed class AlchemyBrewingPlayer : ModPlayer
{
    public AlchemyBrewSession Session { get; } = new();
    public bool Active => Session.Active;
    public bool IsReading => Session.Phase == AlchemyBrewPhase.Reading;
    public bool IsBrewing => Session.Brewing;
    public int AnimationFrame => Session.Frame;
    public Point Cauldron { get; private set; }
    public AlchemyNotebookResult LastResult { get; private set; }
    private string _recipe;
    private int _batches, _sequence;
    private Vector2 Anchor => new(Cauldron.X * 16 - 8, Cauldron.Y * 16 - 10);

    public static bool TryGetCauldron(int i, int j, out Point origin)
    {
        origin = default;
        if (!WorldGen.InWorld(i, j, 2)) return false;
        Tile tile = Main.tile[i, j];
        // Cooking pot style 0 is not a cauldron. Style 1 occupies a 2 x 2 frame.
        if (!tile.HasTile || tile.IsActuated || tile.TileType != TileID.CookingPots || tile.TileFrameX / 36 != 1) return false;
        origin = new Point(i - tile.TileFrameX % 36 / 18, j - tile.TileFrameY % 36 / 18);
        for (int x = 0; x < 2; x++)
        for (int y = 0; y < 2; y++)
        {
            Tile part = Main.tile[origin.X + x, origin.Y + y];
            if (!part.HasTile || part.IsActuated || part.TileType != TileID.CookingPots ||
                part.TileFrameX != 36 + 18 * x || part.TileFrameY != 18 * y) return false;
        }
        return true;
    }

    private bool CanUse => Player.active && !Player.dead && !Player.ghost && Player.gravDir == 1 &&
        Player.GetModPlayer<ElainaModplayer>().Elaina;
    private bool StationValid => TryGetCauldron(Cauldron.X, Cauldron.Y, out var origin) && origin == Cauldron;

    public bool Enter(int i, int j)
    {
        if (Main.dedServ || Player.whoAmI != Main.myPlayer || !CanUse || !AlchemySkill.IsLearned(Player) ||
            !Player.IsInTileInteractionRange(i, j, TileReachCheckSettings.Simple) ||
            !TryGetCauldron(i, j, out var origin) || IsBrewing && origin != Cauldron) return false;
        var position = new Vector2(origin.X * 16 - 8, origin.Y * 16 - 10);
        if (Collision.SolidCollision(position, Player.width, Player.height)) return false;
        if (!AlchemyNotebookPanel.OpenPanel()) return false;
        Cauldron = origin;
        Player.mount.Dismount(Player);
        Player.RemoveAllGrapplingHooks();
        Player.CloseSign();
        Player.SetTalkNPC(-1);
        Player.chest = -1;
        Player.position = position;
        Player.velocity = Vector2.Zero;
        Player.direction = 1;
        Player.fallStart = (int)(Player.position.Y / 16);
        Player.tileInteractionHappened = true;
        Main.npcChatCornerItem = 0;
        Main.mouseRightRelease = false;
        Session.Open();
        SendState();
        return true;
    }

    public void CloseNotebook()
    {
        bool active = Active;
        Session.Close();
        if (active && !Active) SendState();
    }

    public bool Request(AlchemyNotebookAction action, string id, int batches)
    {
        if (Main.dedServ || Player != Main.LocalPlayer || IsBrewing) return false;
        LastResult = AlchemyCraftingService.Execute(Player, action, id, batches, ++_sequence);
        return true;
    }

    internal AlchemyNotebookResult StartBrew(string id, int batches, int sequence)
    {
        AlchemyNotebookResult Fail(string key) => new(sequence, AlchemyNotebookAction.Craft, id, false, LocalizeUI(key));
        if (!CanUse || !AlchemySkill.IsLearned(Player) || !Active || !StationValid) return Fail("AlchemyNeedCauldron");
        if (!AlchemyVanillaRecipes.CanCraft(Player, id) || AlchemyVanillaRecipes.MaxBatches(Player, id) < batches)
            return Fail("CannotCraft");
        if (!Session.Start(Player.GetModPlayer<AlchemyProgressPlayer>().Level)) return Fail("CannotAlchemy");
        _recipe = id;
        _batches = batches;
        SendState();
        return new(sequence, AlchemyNotebookAction.Craft, id, true, "", Pending: true);
    }

    public override void SetControls()
    {
        if (!Active) return;
        Player.controlLeft = Player.controlRight = Player.controlJump = Player.controlUp = Player.controlDown = false;
        Player.controlHook = Player.controlMount = Player.controlThrow = Player.controlUseItem = false;
    }

    public override void HideDrawLayers(PlayerDrawSet drawInfo)
    {
        if (!Active || drawInfo.headOnlyRender) return;
        PlayerDrawLayers.PortableStool.Hide();
        PlayerDrawLayers.Skin.Hide();
        PlayerDrawLayers.Leggings.Hide();
        PlayerDrawLayers.Shoes.Hide();
        PlayerDrawLayers.Robe.Hide();
        PlayerDrawLayers.SkinLongCoat.Hide();
        PlayerDrawLayers.ArmorLongCoat.Hide();
        PlayerDrawLayers.Head.Hide();
        PlayerDrawLayers.HeadBack.Hide();
        PlayerDrawLayers.HairBack.Hide();
        PlayerDrawLayers.Torso.Hide();
        PlayerDrawLayers.ArmOverItem.Hide();
        PlayerDrawLayers.HeldItem.Hide();
    }

    public override void PreUpdateMovement()
    {
        if (!Active) return;
        if (!CanUse || !StationValid || Vector2.DistanceSquared(Player.position, Anchor) > 64 * 64)
        { Cancel(); return; }
        Player.velocity = Vector2.Zero;
        Player.gravity = 0;
        Player.position = Anchor;
        Player.direction = 1;
        Player.fallStart = (int)(Anchor.Y / 16);
    }

    public override void PostUpdate()
    {
        if (!Active) return;
        if (!CanUse || !StationValid || Vector2.DistanceSquared(Player.position, Anchor) > 64 * 64) { Cancel(); return; }
        bool finished = Session.Tick();
        if (!finished || Main.dedServ || Player.whoAmI != Main.myPlayer) return;
        // No stock is reserved. Recollect, validate, consume and deliver together on this tick.
        string id = _recipe;
        int batches = _batches;
        _recipe = null;
        _batches = 0;
        LastResult = AlchemyCraftingService.CompleteCraft(Player, id, batches, _sequence);
        if (Session.Brewing) Session.Complete();
        SendState();
        if (!Session.NotebookOpen && LastResult != null) Main.NewText(LastResult.Message);
    }

    public void Cancel(bool synchronize = true)
    {
        bool active = Active;
        if (_recipe != null)
            LastResult = new(_sequence, AlchemyNotebookAction.Craft, _recipe, false, LocalizeUI("AlchemyInterrupted"));
        _recipe = null;
        _batches = 0;
        Session.Cancel();
        if (active && synchronize) SendState();
    }

    public override void OnHurt(Player.HurtInfo info) { if (info.Damage > 0) Cancel(); }
    public override void UpdateDead() => Cancel();
    public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource) => Cancel();
    public override void OnEnterWorld() { Cancel(false); Session.Close(); LastResult = null; }
    public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) => SendState(toWho, fromWho);

    private void SendState(int toWho = -1, int ignore = -1)
    {
        if (Main.netMode == NetmodeID.SinglePlayer || Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI != Main.myPlayer) return;
        var packet = Mod.GetPacket();
        packet.Write((byte)global::伊蕾娜.伊蕾娜.MessageType.炼金状态);
        packet.Write(Player.whoAmI);
        packet.Write((byte)Session.Phase);
        packet.Write(Cauldron.X);
        packet.Write(Cauldron.Y);
        packet.Write(Session.Ticks);
        packet.Write(Session.DurationTicks);
        packet.Send(toWho, ignore);
    }

    internal static void Receive(BinaryReader reader, int sender, int playerIndex)
    {
        var phase = (AlchemyBrewPhase)reader.ReadByte();
        var origin = new Point(reader.ReadInt32(), reader.ReadInt32());
        int ticks = reader.ReadInt32();
        int durationTicks = reader.ReadInt32();
        if (playerIndex < 0 || playerIndex >= Main.maxPlayers || phase > AlchemyBrewPhase.Stirring) return;
        if (Main.netMode == NetmodeID.Server && sender != playerIndex) return;
        var player = Main.player[playerIndex];
        var brewing = player.GetModPlayer<AlchemyBrewingPlayer>();
        if (phase != AlchemyBrewPhase.Idle && (!player.active || player.dead ||
            !TryGetCauldron(origin.X, origin.Y, out var found) || found != origin ||
            !player.IsInTileInteractionRange(origin.X, origin.Y, TileReachCheckSettings.Simple))) return;
        if (phase == AlchemyBrewPhase.Idle) brewing.Cancel(false);
        brewing.Cauldron = origin;
        brewing.Session.ApplyVisualState(phase, ticks, durationTicks);
        if (Main.netMode == NetmodeID.Server) brewing.SendState(-1, sender);
    }
}
