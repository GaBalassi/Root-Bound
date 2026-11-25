using UnityEngine;

namespace Rootbound.Core
{
    public enum WeaponType
    {
        LamaDiRovo = 0,
        MartelloDiCorteccia = 1,
        FrustaDiLiana = 2
    }

    public enum RelicType
    {
        SpinaVelenosa = 0,
        RadiceFuriosa = 1,
        FogliaDelVento = 2,
        LinfaCurativa = 3,
        CuoreDiQuercia = 4
    }

    public enum RoomType
    {
        Entrance = 0,
        Combat = 1,
        Parkour = 2,
        EliteCombat = 3,
        Merchant = 4,
        SecretRoots = 5,
        PreBoss = 6,
        Boss = 7
    }

    /// <summary>
    /// Costanti centralizzate di Layer, Sorting Layer e LayerMask allineate a TagManager.asset.
    /// </summary>
    public static class RootboundLayers
    {
        public const int Ground = 6;
        public const int OneWayPlatform = 7;
        public const int Player = 8;
        public const int Enemy = 9;
        public const int Hazard = 10;
        public const int RootAnchor = 11;
        public const int Interactable = 12;
        public const int Projectile = 13;

        public const string SortBackground = "Background";
        public const string SortMidground = "Midground";
        public const string SortTiles = "Tiles";
        public const string SortProps = "Props";
        public const string SortEntities = "Entities";
        public const string SortPlayer = "Player";
        public const string SortVfx = "VFX";
        public const string SortForeground = "Foreground";

        public static int GroundMask => 1 << Ground;
        public static int OneWayMask => 1 << OneWayPlatform;
        public static int SolidSurfaceMask => (1 << Ground) | (1 << OneWayPlatform);
        public static int EnemyMask => 1 << Enemy;
        public static int HazardMask => 1 << Hazard;
        public static int RootAnchorMask => 1 << RootAnchor;
        public static int InteractableMask => 1 << Interactable;
        public static int ProjectileMask => 1 << Projectile;
        public static int RootTargetMask => (1 << RootAnchor) | (1 << Enemy) | (1 << Interactable);

        public static void ConfigureCollisionMatrix()
        {
            Physics2D.IgnoreLayerCollision(Enemy, Enemy, true);
            Physics2D.IgnoreLayerCollision(Player, Interactable, true);
            Physics2D.IgnoreLayerCollision(Player, RootAnchor, true);
            Physics2D.IgnoreLayerCollision(Enemy, Interactable, true);
            Physics2D.IgnoreLayerCollision(Enemy, RootAnchor, true);
            Physics2D.IgnoreLayerCollision(Projectile, Enemy, true);
            Physics2D.IgnoreLayerCollision(Projectile, Interactable, true);
            Physics2D.IgnoreLayerCollision(Projectile, RootAnchor, true);
        }
    }
}
