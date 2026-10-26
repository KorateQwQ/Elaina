using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics;
using Terraria.Graphics.Renderers;
using Terraria.ID;
using Terraria.ModLoader;

namespace 伊蕾娜.System;

/// <summary>Choose the visual equipment before vanilla derives hair, glow and composite-arm state.</summary>
public sealed class ElainaAppearanceSystem : ModSystem
{
    private static int _head = -1, _body = -1, _legs = -1;

    public override void Load()
    {
        if (Main.dedServ) return;
        On_LegacyPlayerRenderer.DrawPlayer += DrawPlayer;
        On_LegacyPlayerRenderer.DrawPlayerHead += DrawHead;
    }

    public override void PostSetupContent()
    {
        if (Main.dedServ) return;
        _head = EquipLoader.GetEquipSlot(Mod, "Elaina2", EquipType.Head);
        _body = EquipLoader.GetEquipSlot(Mod, "Elaina2", EquipType.Body);
        _legs = EquipLoader.GetEquipSlot(Mod, "Elaina2", EquipType.Legs);
        ArmorIDs.Head.Sets.DrawHead[_head] = false;
        ArmorIDs.Body.Sets.HidesTopSkin[_body] = true;
        ArmorIDs.Body.Sets.HidesArms[_body] = true;
        ArmorIDs.Legs.Sets.HidesBottomSkin[_legs] = true;
    }

    public override void Unload()
    {
        On_LegacyPlayerRenderer.DrawPlayer -= DrawPlayer;
        On_LegacyPlayerRenderer.DrawPlayerHead -= DrawHead;
        _head = _body = _legs = -1;
    }

    private static AppearanceScope Begin(Player player) => _head < 0 ||
        !player.GetModPlayer<ElainaModplayer>().Elaina ? default : new(player, _head, _body, _legs);

    private static void DrawPlayer(On_LegacyPlayerRenderer.orig_DrawPlayer orig, LegacyPlayerRenderer renderer,
        Camera camera, Player player, Vector2 position, float rotation, Vector2 rotationOrigin, float shadow, float scale)
    {
        using var appearance = Begin(player);
        orig(renderer, camera, player, position, rotation, rotationOrigin, shadow, scale);
    }

    private static void DrawHead(On_LegacyPlayerRenderer.orig_DrawPlayerHead orig, LegacyPlayerRenderer renderer,
        Camera camera, Player player, Vector2 position, float alpha, float scale, Color borderColor)
    {
        using var appearance = Begin(player);
        if (!player.GetModPlayer<ElainaModplayer>().Elaina || !player.sleeping.isSleeping)
        {
            orig(renderer, camera, player, position, alpha, scale, borderColor);
            return;
        }

        // Sleeping hides the world equipment using an empty frame. Portraits still
        // need the normal head frame, which vanilla reads from bodyFrame.
        Rectangle bodyFrame = player.bodyFrame;
        try
        {
            player.bodyFrame = new Rectangle(0, 0, bodyFrame.Width, bodyFrame.Height);
            orig(renderer, camera, player, position, alpha, scale, borderColor);
        }
        finally
        {
            player.bodyFrame = bodyFrame;
        }
    }

    // A draw may be a paused world, a menu clone, a portrait or a nested preview.
    // Never leave its temporary visual equipment on the player's game state.
    internal readonly struct AppearanceScope : IDisposable
    {
        private readonly Player _player;
        private readonly int _head, _body, _legs, _skin;
        private readonly bool _robe;

        internal AppearanceScope(Player player, int head, int body, int legs)
        {
            _player = player;
            _head = player.head; _body = player.body; _legs = player.legs;
            _skin = player.skinVariant; _robe = player.wearsRobe;
            player.head = head; player.body = body; player.legs = legs;
            player.Male = false;
            player.wearsRobe = true;
        }

        public void Dispose()
        {
            if (_player == null) return;
            _player.head = _head; _player.body = _body; _player.legs = _legs;
            _player.skinVariant = _skin; _player.wearsRobe = _robe;
        }
    }
}
