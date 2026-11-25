using System;
using System.IO;
using System.Reflection;
using Rootbound.Core;
using UnityEditor;
using UnityEngine;

namespace Rootbound.Editor
{
    /// <summary>
    /// Popola e valida Assets/Data/RootboundArtLibrary.asset a partire dagli sprite in Assets/Art/.
    /// </summary>
    public static class RootboundArtLibraryBuilder
    {
        public const string AssetPath = "Assets/Data/RootboundArtLibrary.asset";

        [MenuItem("Rootbound/Build Art Library")]
        public static void BuildLibraryMenu()
        {
            BuildOrLoadLibrary();
        }

        public static void BuildLibraryBatch()
        {
            try
            {
                BuildOrLoadLibrary();
                Debug.Log("[Rootbound] RootboundArtLibrary costruita e validata con successo.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Rootbound] Errore nella costruzione ArtLibrary: {ex}");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
            }
        }

        public static RootboundArtLibrary BuildOrLoadLibrary()
        {
            Directory.CreateDirectory("Assets/Data");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var lib = AssetDatabase.LoadAssetAtPath<RootboundArtLibrary>(AssetPath);
            if (lib == null)
            {
                lib = ScriptableObject.CreateInstance<RootboundArtLibrary>();
                AssetDatabase.CreateAsset(lib, AssetPath);
            }

            // Player
            lib.playerIdle = LoadFrames("Player/idle", 4);
            lib.playerRun = LoadFrames("Player/run", 6);
            lib.playerJump = LoadFrames("Player/jump", 2);
            lib.playerFall = LoadFrames("Player/fall", 2);
            lib.playerDash = LoadFrames("Player/dash", 2);
            lib.playerAttack1 = LoadFrames("Player/attack1", 2);
            lib.playerAttack2 = LoadFrames("Player/attack2", 2);
            lib.playerAttack3 = LoadFrames("Player/attack3", 2);
            lib.playerCharge = LoadFrames("Player/charge", 2);
            lib.playerRoot = LoadFrames("Player/root", 2);
            lib.playerHurt = LoadSprite("Player/hurt_0.png");

            // Enemies
            lib.slimeFrames = LoadFrames("Enemies/slime", 4);
            lib.slimeTelegraph = LoadSprite("Enemies/slime_telegraph.png");
            lib.wolfFrames = LoadFrames("Enemies/wolf", 4);
            lib.wolfTelegraph = LoadSprite("Enemies/wolf_telegraph.png");
            lib.plantFrames = LoadFrames("Enemies/plant", 4);
            lib.plantSpit = LoadSprite("Enemies/plant_spit.png");

            // Boss
            lib.bossIdle = LoadFrames("Boss/root_guardian_idle", 4);
            lib.bossSlam = LoadSprite("Boss/root_guardian_slam.png");
            lib.bossSummon = LoadSprite("Boss/root_guardian_summon.png");
            lib.bossStun = LoadSprite("Boss/root_guardian_stun.png");
            lib.bossSpikeFrames = LoadFrames("Boss/root_spike", 3);

            // Tiles
            lib.grassTopTiles = LoadFrames("Tiles/grass_top", 3);
            lib.dirtTiles = LoadFrames("Tiles/dirt", 3);
            lib.stoneWallTile = LoadSprite("Tiles/stone_wall.png");
            lib.woodPlatformTile = LoadSprite("Tiles/wood_platform.png");
            lib.villageGroundTile = LoadSprite("Tiles/village_ground.png");
            lib.thornsHazardTile = LoadSprite("Tiles/thorns_hazard.png");
            lib.bridgeTile = LoadSprite("Tiles/bridge_tile.png");

            // Props
            lib.rootAnchorFrames = LoadFrames("Props/root_anchor", 2);
            lib.naturalSwitchOff = LoadSprite("Props/natural_switch_off.png");
            lib.naturalSwitchOn = LoadSprite("Props/natural_switch_on.png");
            lib.chestClosed = LoadSprite("Props/chest_closed.png");
            lib.chestOpen = LoadSprite("Props/chest_open.png");
            lib.portalFrames = LoadFrames("Props/portal", 3);
            lib.doorEasy = LoadSprite("Props/door_easy.png");
            lib.doorHard = LoadSprite("Props/door_hard.png");
            lib.doorMerchant = LoadSprite("Props/door_merchant.png");
            lib.doorBoss = LoadSprite("Props/door_boss.png");
            lib.altarSeeds = LoadSprite("Props/altar_seeds.png");
            lib.weaponRack = LoadSprite("Props/weapon_rack.png");
            lib.elderNpcFrames = LoadFrames("Props/elder_npc", 2);
            lib.merchantNpcFrames = LoadFrames("Props/merchant_npc", 2);
            lib.treeBgProp = LoadSprite("Props/tree_bg_prop.png");
            lib.mushroomGlow = LoadSprite("Props/mushroom_glow.png");
            lib.signpost = LoadSprite("Props/signpost.png");

            // Backgrounds
            lib.skyForest = LoadSprite("Backgrounds/sky_forest.png");
            lib.skyVillage = LoadSprite("Backgrounds/sky_village.png");
            lib.parallaxFarTrees = LoadSprite("Backgrounds/parallax_far_trees.png");
            lib.parallaxMidTrunks = LoadSprite("Backgrounds/parallax_mid_trunks.png");
            lib.parallaxNearCanopy = LoadSprite("Backgrounds/parallax_near_canopy.png");

            // VFX & UI
            lib.slashArcs = LoadFrames("VFX/slash_arc", 3);
            lib.hammerSmash = LoadSprite("VFX/hammer_smash.png");
            lib.whipArc = LoadSprite("VFX/whip_arc.png");
            lib.rootVineSegment = LoadSprite("VFX/root_vine_segment.png");
            lib.rootVineTip = LoadSprite("VFX/root_vine_tip.png");
            lib.rootReticle = LoadSprite("VFX/root_reticle.png");
            lib.projectileSpore = LoadSprite("VFX/projectile_spore.png");
            lib.particleCircle = LoadSprite("VFX/particle_circle.png");
            lib.particleLeaf = LoadSprite("VFX/particle_leaf.png");
            lib.warnMarker = LoadSprite("VFX/warn_marker.png");
            lib.uiPanel = LoadSprite("VFX/ui_panel.png");

            // Icons
            lib.heartFull = LoadSprite("Icons/heart_full.png");
            lib.heartEmpty = LoadSprite("Icons/heart_empty.png");
            lib.seedIcon = LoadSprite("Icons/seed.png");
            lib.coinIcon = LoadSprite("Icons/coin.png");
            lib.relicPoison = LoadSprite("Icons/relic_poison.png");
            lib.relicFury = LoadSprite("Icons/relic_fury.png");
            lib.relicWind = LoadSprite("Icons/relic_wind.png");
            lib.relicSap = LoadSprite("Icons/relic_sap.png");
            lib.relicOak = LoadSprite("Icons/relic_oak.png");
            lib.weaponBlade = LoadSprite("Icons/weapon_blade.png");
            lib.weaponHammer = LoadSprite("Icons/weapon_hammer.png");
            lib.weaponWhip = LoadSprite("Icons/weapon_whip.png");

            ValidateAllFields(lib);

            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();
            return lib;
        }

        private static Sprite LoadSprite(string relPath)
        {
            string full = $"Assets/Art/{relPath}";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(full);
            if (sprite == null)
            {
                throw new InvalidOperationException($"Sprite mancante in {full}");
            }
            return sprite;
        }

        private static Sprite[] LoadFrames(string prefix, int count)
        {
            var arr = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                arr[i] = LoadSprite($"{prefix}_{i}.png");
            }
            return arr;
        }

        private static void ValidateAllFields(RootboundArtLibrary lib)
        {
            foreach (FieldInfo f in typeof(RootboundArtLibrary).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                object val = f.GetValue(lib);
                if (val == null)
                {
                    throw new InvalidOperationException($"Campo nullo in RootboundArtLibrary: {f.Name}");
                }
                if (val is Sprite[] arr)
                {
                    if (arr.Length == 0)
                    {
                        throw new InvalidOperationException($"Array vuoto in RootboundArtLibrary: {f.Name}");
                    }
                    for (int i = 0; i < arr.Length; i++)
                    {
                        if (arr[i] == null)
                        {
                            throw new InvalidOperationException($"Elemento nullo {f.Name}[{i}]");
                        }
                    }
                }
            }
        }
    }
}
