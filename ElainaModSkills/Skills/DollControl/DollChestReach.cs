using Microsoft.Xna.Framework.Input;
using Terraria.Audio;
using Terraria.ID;

namespace 伊蕾娜.ElainaModSkills.Skills.DollControl;

public sealed partial class DollCommandPlayer
{
    private DollPetProjectile remoteChestHelper;
    private Point? remoteChestOrigin;
    private int remoteChestIndex = -1, remoteChestWait;
    private Chest remoteChestInstance;
    private bool remoteChestClick;
    private bool remoteChestTravelling;
    private Point remoteChestClickedTile;
    private ushort remoteChestTileType;
    private short remoteChestFrameX, remoteChestFrameY;
    private Chest remoteChestExpectedInstance;
    private int remoteChestPreviousChest;
    private On_Player.orig_TileInteractionsUse remoteChestAction;

    internal bool RemoteChestActionFinished => !remoteChestTravelling;

    internal bool IsRemoteChestHelper(DollPetProjectile worker) => worker == remoteChestHelper && RemoteHelperAlive();
    private bool RemoteHelperAlive() => remoteChestHelper?.Projectile is { active: true } projectile
        && projectile.owner == Player.whoAmI && projectile.ModProjectile == remoteChestHelper
        && (!remoteChestHelper.HasWork || remoteChestHelper.IsOpeningChest);

    internal bool TrySelectRemoteChest(DollPetProjectile worker, out Point target)
    {
        target = remoteChestOrigin ?? new Point(-1, -1);
        return IsRemoteChestHelper(worker);
    }

    internal DollPetProjectile FindIdleChestHelper()
    {
        if (RemoteHelperAlive()) return remoteChestHelper;
        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            Projectile projectile = Main.projectile[i];
            if (projectile.active && projectile.owner == Player.whoAmI && projectile.ModProjectile is DollPetProjectile doll && !doll.HasWork && !doll.IsDismissing)
                return doll;
        }
        return null;
    }

    internal void BeginRemoteChest(Point origin, Point clickedTile, On_Player.orig_TileInteractionsUse action)
    {
        // Holding/repeatedly pressing right-click on the same pending target cannot restart its animation.
        if (remoteChestTravelling && remoteChestOrigin == origin && RemoteHelperAlive()) return;
        var helper = FindIdleChestHelper();
        ReleaseRemoteChest(false);
        if (helper == null) return;
        remoteChestHelper = helper;
        remoteChestOrigin = origin;
        remoteChestClickedTile = clickedTile;
        remoteChestAction = action;
        remoteChestTravelling = true;
        remoteChestPreviousChest = Player.chest;
        Tile tile = Main.tile[origin.X, origin.Y];
        remoteChestTileType = tile.TileType;
        remoteChestFrameX = tile.TileFrameX; remoteChestFrameY = tile.TileFrameY;
        int index = Chest.FindChest(origin.X, origin.Y);
        remoteChestExpectedInstance = index >= 0 ? Main.chest[index] : null;
    }

    private bool PendingChestValid()
    {
        if (remoteChestOrigin is not { } origin || !DollChestReachSystem.TryChestOrigin(origin.X, origin.Y, out var current)
            || current != origin) return false;
        Tile tile = Main.tile[origin.X, origin.Y];
        if (tile.TileType != remoteChestTileType || tile.TileFrameX != remoteChestFrameX || tile.TileFrameY != remoteChestFrameY) return false;
        int index = Chest.FindChest(origin.X, origin.Y);
        return remoteChestExpectedInstance == null || index >= 0 && ReferenceEquals(Main.chest[index], remoteChestExpectedInstance);
    }

    internal void CompleteRemoteChestGesture(DollPetProjectile worker)
    {
        if (Player.whoAmI != Main.myPlayer || !remoteChestTravelling || !IsRemoteChestHelper(worker)) return;
        if (!PendingChestValid() || Player.dead || Main.mapFullscreen || Main.inFancyUI || Main.drawingPlayerChat
            || Player.sign >= 0 || Player.talkNPC >= 0
            || Player.chest >= 0 && Player.chest != remoteChestPreviousChest
            || Vector2.DistanceSquared(worker.Projectile.Center, remoteChestOrigin.Value.ToWorldCoordinates(16, 16)) > 80 * 80)
        { ReleaseRemoteChest(true); return; }
        var action = remoteChestAction;
        remoteChestAction = null;
        remoteChestTravelling = false;
        remoteChestClick = true;
        // Replay one genuine tile interaction at the originally clicked tile. Keep the player's real position,
        // inventory and mod checks; restore all temporary input state even if a mod callback fails.
        bool attempted = Player.tileInteractAttempted, released = Player.releaseUseTile, use = Player.controlUseTile;
        bool happened = Player.tileInteractionHappened;
        int targetX = Player.tileTargetX, targetY = Player.tileTargetY;
        try
        {
            Player.tileInteractAttempted = Player.releaseUseTile = Player.controlUseTile = true;
            Player.tileTargetX = remoteChestClickedTile.X; Player.tileTargetY = remoteChestClickedTile.Y;
            action?.Invoke(Player, remoteChestClickedTile.X, remoteChestClickedTile.Y);
        }
        finally
        {
            Player.tileInteractAttempted = attempted; Player.releaseUseTile = released; Player.controlUseTile = use;
            Player.tileInteractionHappened = happened;
            Player.tileTargetX = targetX; Player.tileTargetY = targetY;
            AfterRemoteChestClick();
        }
    }

    internal void AwaitRemoteChest(Point origin)
    {
        if (remoteChestClick && remoteChestOrigin == origin) remoteChestWait = 300;
    }

    internal void AfterRemoteChestClick()
    {
        remoteChestClick = false;
        if (!ObserveOpenedChest() && remoteChestWait <= 0) ReleaseRemoteChest(false);
    }

    private bool ObserveOpenedChest()
    {
        if (remoteChestOrigin is not { } origin || Player.chest < 0 || Player.chest >= Main.maxChests
            || Player.chestX != origin.X || Player.chestY != origin.Y || Main.chest[Player.chest] is not { } chest) return false;
        if (remoteChestIndex >= 0) return remoteChestIndex == Player.chest && ReferenceEquals(chest, remoteChestInstance);
        remoteChestIndex = Player.chest;
        remoteChestInstance = chest;
        remoteChestWait = 0;
        return true;
    }

    internal bool MaintainsRemoteChest(int x, int y) => Player.whoAmI == Main.myPlayer && Main.netMode != NetmodeID.Server
        && !remoteChestTravelling && Player.active && !Player.dead && RemoteHelperAlive() && remoteChestOrigin == new Point(x, y)
        && Main.playerInventory && ObserveOpenedChest() && DollChestReachSystem.TryChestOrigin(x, y, out var origin)
        && origin == remoteChestOrigin && DollChestReachSystem.TileOnScreen(x, y, 64);

    public override void PostUpdate()
    {
        if (Player.whoAmI != Main.myPlayer || remoteChestOrigin is not { } origin) return;
        if (!Player.active || Player.dead || !RemoteHelperAlive()
            || !DollChestReachSystem.TryChestOrigin(origin.X, origin.Y, out var current) || current != origin
            || !DollChestReachSystem.TileOnScreen(origin.X, origin.Y, 64))
        { ReleaseRemoteChest(true); return; }
        if (remoteChestTravelling)
        {
            if (!PendingChestValid() || Main.keyState.IsKeyDown(Keys.Escape) || Main.mapFullscreen || Main.inFancyUI
                || Main.drawingPlayerChat || Player.sign >= 0 || Player.talkNPC >= 0
                || Player.chest >= 0 && Player.chest != remoteChestPreviousChest)
                ReleaseRemoteChest(true);
            return;
        }
        if (ObserveOpenedChest())
        {
            if (!Main.playerInventory) ReleaseRemoteChest(true);
        }
        else if (remoteChestIndex >= 0 || remoteChestWait <= 0 || --remoteChestWait <= 0)
            ReleaseRemoteChest(true);
    }

    public override void UpdateDead() => ReleaseRemoteChest(true);

    internal void ReleaseRemoteChest(bool close)
    {
        int index = remoteChestIndex;
        Point? origin = remoteChestOrigin;
        remoteChestHelper = null;
        remoteChestOrigin = null;
        remoteChestInstance = null;
        remoteChestIndex = -1;
        remoteChestWait = 0;
        remoteChestClick = false;
        remoteChestTravelling = false;
        remoteChestAction = null;
        remoteChestExpectedInstance = null;
        if (close && index >= 0 && Player.chest == index && origin is { } p
            && !Player.IsInInteractionRangeToMultiTileHitbox(p.X, p.Y))
        {
            Player.chest = -1;
            if (!Main.gameMenu && Player.whoAmI == Main.myPlayer)
            {
                Main.recBigList = false;
                SoundEngine.PlaySound(SoundID.MenuClose);
                Recipe.FindRecipes();
            }
        }
    }
}
