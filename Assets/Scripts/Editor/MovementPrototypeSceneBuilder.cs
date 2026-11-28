using System.IO;
using Rootbound.Core;
using Rootbound.Player;
using Rootbound.Visuals;
using Rootbound.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Rootbound.Editor
{
    public static class MovementPrototypeSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/MovementPrototype.unity";

        [MenuItem("Rootbound/Build Movement Prototype")]
        public static void Build()
        {
            var art = AssetDatabase.LoadAssetAtPath<RootboundArtLibrary>(RootboundArtLibraryBuilder.AssetPath);
            if (art == null || art.playerIdle.Length == 0 || art.playerIdle[0] == null)
            {
                throw new System.InvalidOperationException("Libreria grafica del personaggio non disponibile.");
            }
            Directory.CreateDirectory("Assets/Scenes");
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(10f, 5.625f, -10f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.625f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 100f;
            camera.backgroundColor = new Color(0.07f, 0.18f, 0.16f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<UniversalAdditionalCameraData>();
            var follow = cameraObject.AddComponent<CameraFollow2D>();
            var globalLight = new GameObject("Global Light 2D").AddComponent<Light2D>();
            globalLight.lightType = Light2D.LightType.Global;
            globalLight.intensity = 1f;

            var terrain = new GameObject("TrainingGround").transform;
            CreateBlock("Ground", new Rect(0f, 0f, 44f, 2f), art.dirtTiles[0], terrain);
            CreateBlock("LeftBoundary", new Rect(-1f, 0f, 1f, 16f), art.stoneWallTile, terrain);
            CreateBlock("RightBoundary", new Rect(44f, 0f, 1f, 16f), art.stoneWallTile, terrain);
            CreateBlock("WallJump", new Rect(34f, 2f, 1.5f, 6f), art.stoneWallTile, terrain);
            CreateBlock("RaisedGround", new Rect(38f, 2f, 6f, 2f), art.dirtTiles[1], terrain);
            CreatePlatform("Platform_Low", new Rect(7f, 4.2f, 5f, 0.4f), art.woodPlatformTile, terrain);
            CreatePlatform("Platform_High", new Rect(15f, 6.4f, 5f, 0.4f), art.woodPlatformTile, terrain);
            CreatePlatform("Platform_Return", new Rect(24f, 4.6f, 6f, 0.4f), art.woodPlatformTile, terrain);
            var rootBridge = CreatePlatform("RootBridge", new Rect(29f, 8f, 5f, 0.4f), art.bridgeTile, terrain);
            var anchors = new RootAnchor[3];
            var anchorPositions = new[] { new Vector2(12.5f, 7.4f), new Vector2(21f, 9.2f), new Vector2(31f, 8.8f) };
            for (int i = 0; i < anchors.Length; i++)
            {
                var anchorObject = new GameObject("RootAnchor_" + (i + 1));
                anchorObject.transform.SetParent(terrain, false);
                anchorObject.transform.position = anchorPositions[i];
                anchors[i] = anchorObject.AddComponent<RootAnchor>();
                anchors[i].Initialize(art);
            }
            var switchObject = new GameObject("TrainingRootSwitch");
            switchObject.transform.SetParent(terrain, false);
            switchObject.transform.position = new Vector3(28f, 2f, 0f);
            var naturalSwitch = switchObject.AddComponent<NaturalSwitch>();
            naturalSwitch.Initialize(art, rootBridge);
            for (int x = 0; x < 44; x++)
            {
                CreateSprite("Grass", art.grassTopTiles[x % art.grassTopTiles.Length],
                    new Vector3(x + 0.5f, 1.9f, 0f), RootboundLayers.SortTiles, terrain).sortingOrder = 1;
            }

            var playerObject = new GameObject("Player");
            playerObject.tag = "Player";
            playerObject.layer = RootboundLayers.Player;
            playerObject.transform.position = new Vector3(3f, 2.1f, 0f);
            var collider = playerObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.85f, 1.25f);
            collider.offset = new Vector2(0f, 0.625f);
            var body = playerObject.AddComponent<Rigidbody2D>();
            body.freezeRotation = true;
            body.gravityScale = 3.2f;
            var visual = CreateSprite("Visual", art.playerIdle[0], playerObject.transform.position,
                RootboundLayers.SortPlayer, playerObject.transform);
            visual.gameObject.AddComponent<SpriteSheetAnimator>();
            playerObject.AddComponent<PlayerInputReader>();
            var player = playerObject.AddComponent<PlayerController2D>();
            playerObject.AddComponent<RootPowerController>();
            var controller = new GameObject("MovementPrototype").AddComponent<MovementPrototypeController>();
            controller.Configure(art, player, follow, anchors, naturalSwitch, rootBridge);
            CreateInstructions(camera);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
        }

        private static SpriteRenderer CreateSprite(string name, Sprite sprite, Vector3 position, string sortingLayer, Transform parent)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.position = position;
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerName = sortingLayer;
            return renderer;
        }

        private static GameObject CreateBlock(string name, Rect bounds, Sprite sprite, Transform parent)
        {
            var renderer = CreateSprite(name, sprite, bounds.center, RootboundLayers.SortTiles, parent);
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = bounds.size;
            renderer.gameObject.layer = RootboundLayers.Ground;
            renderer.gameObject.AddComponent<BoxCollider2D>().size = bounds.size;
            return renderer.gameObject;
        }

        private static GameObject CreatePlatform(string name, Rect bounds, Sprite sprite, Transform parent)
        {
            var obj = CreateBlock(name, bounds, sprite, parent);
            Object.DestroyImmediate(obj.GetComponent<SpriteRenderer>());
            float tileWidth = sprite.bounds.size.x;
            for (float x = bounds.xMin; x < bounds.xMax; x += tileWidth)
            {
                CreateSprite("Plank", sprite,
                    new Vector3(x + tileWidth * 0.5f, bounds.yMax - sprite.bounds.max.y, 0f),
                    RootboundLayers.SortTiles, obj.transform);
            }
            obj.layer = RootboundLayers.OneWayPlatform;
            obj.GetComponent<BoxCollider2D>().usedByEffector = true;
            var effector = obj.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.useColliderMask = false;
            effector.surfaceArc = 180f;
            return obj;
        }

        private static void CreateInstructions(Camera camera)
        {
            var obj = new GameObject("Instructions", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var canvas = obj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            canvas.sortingLayerName = RootboundLayers.SortForeground;
            canvas.sortingOrder = 100;
            var scaler = obj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(obj.transform, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(20f, -20f);
            rect.sizeDelta = new Vector2(620f, 128f);
            panel.GetComponent<Image>().color = new Color(0.02f, 0.07f, 0.06f, 0.88f);
            var label = new GameObject("Controls", typeof(RectTransform), typeof(Text));
            label.transform.SetParent(panel.transform, false);
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(16f, 10f);
            labelRect.offsetMax = new Vector2(-16f, -10f);
            var text = label.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 20;
            text.color = new Color(0.85f, 1f, 0.87f);
            text.text = "ROOTBOUND — Allenamento nella foresta\nA / D o frecce: movimento   ·   Spazio: salto / doppio salto\nShift: dash   ·   Salta contro i muri   ·   R: torna all'inizio\nQ / F: radici — aggancia i boccioli o attiva il totem";
            text.raycastTarget = false;
        }
    }
}
