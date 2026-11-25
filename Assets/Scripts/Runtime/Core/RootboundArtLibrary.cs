using UnityEngine;

namespace Rootbound.Core
{
    /// <summary>
    /// Libreria serializzata di tutti gli sprite di Rootbound.
    /// Evita dipendenze runtime da AssetDatabase o Resources.
    /// </summary>
    [CreateAssetMenu(fileName = "RootboundArtLibrary", menuName = "Rootbound/Art Library")]
    public sealed class RootboundArtLibrary : ScriptableObject
    {
        [Header("Player")]
        public Sprite[] playerIdle;
        public Sprite[] playerRun;
        public Sprite[] playerJump;
        public Sprite[] playerFall;
        public Sprite[] playerDash;
        public Sprite[] playerAttack1;
        public Sprite[] playerAttack2;
        public Sprite[] playerAttack3;
        public Sprite[] playerCharge;
        public Sprite[] playerRoot;
        public Sprite playerHurt;

        [Header("Enemies")]
        public Sprite[] slimeFrames;
        public Sprite slimeTelegraph;
        public Sprite[] wolfFrames;
        public Sprite wolfTelegraph;
        public Sprite[] plantFrames;
        public Sprite plantSpit;

        [Header("Boss - Guardiano delle Radici")]
        public Sprite[] bossIdle;
        public Sprite bossSlam;
        public Sprite bossSummon;
        public Sprite bossStun;
        public Sprite[] bossSpikeFrames;

        [Header("Tiles")]
        public Sprite[] grassTopTiles;
        public Sprite[] dirtTiles;
        public Sprite stoneWallTile;
        public Sprite woodPlatformTile;
        public Sprite villageGroundTile;
        public Sprite thornsHazardTile;
        public Sprite bridgeTile;

        [Header("Props")]
        public Sprite[] rootAnchorFrames;
        public Sprite naturalSwitchOff;
        public Sprite naturalSwitchOn;
        public Sprite chestClosed;
        public Sprite chestOpen;
        public Sprite[] portalFrames;
        public Sprite doorEasy;
        public Sprite doorHard;
        public Sprite doorMerchant;
        public Sprite doorBoss;
        public Sprite altarSeeds;
        public Sprite weaponRack;
        public Sprite[] elderNpcFrames;
        public Sprite[] merchantNpcFrames;
        public Sprite treeBgProp;
        public Sprite mushroomGlow;
        public Sprite signpost;

        [Header("Backgrounds")]
        public Sprite skyForest;
        public Sprite skyVillage;
        public Sprite parallaxFarTrees;
        public Sprite parallaxMidTrunks;
        public Sprite parallaxNearCanopy;

        [Header("VFX & UI")]
        public Sprite[] slashArcs;
        public Sprite hammerSmash;
        public Sprite whipArc;
        public Sprite rootVineSegment;
        public Sprite rootVineTip;
        public Sprite rootReticle;
        public Sprite projectileSpore;
        public Sprite particleCircle;
        public Sprite particleLeaf;
        public Sprite warnMarker;
        public Sprite uiPanel;

        [Header("Icons")]
        public Sprite heartFull;
        public Sprite heartEmpty;
        public Sprite seedIcon;
        public Sprite coinIcon;
        public Sprite relicPoison;
        public Sprite relicFury;
        public Sprite relicWind;
        public Sprite relicSap;
        public Sprite relicOak;
        public Sprite weaponBlade;
        public Sprite weaponHammer;
        public Sprite weaponWhip;

        public Sprite GetRelicIcon(RelicType type)
        {
            return type switch
            {
                RelicType.SpinaVelenosa => relicPoison,
                RelicType.RadiceFuriosa => relicFury,
                RelicType.FogliaDelVento => relicWind,
                RelicType.LinfaCurativa => relicSap,
                RelicType.CuoreDiQuercia => relicOak,
                _ => seedIcon
            };
        }

        public Sprite GetWeaponIcon(WeaponType type)
        {
            return type switch
            {
                WeaponType.LamaDiRovo => weaponBlade,
                WeaponType.MartelloDiCorteccia => weaponHammer,
                WeaponType.FrustaDiLiana => weaponWhip,
                _ => weaponBlade
            };
        }
    }
}
