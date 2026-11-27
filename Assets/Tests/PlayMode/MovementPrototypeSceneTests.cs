using System.Collections;
using System.IO;
using NUnit.Framework;
using Rootbound.Core;
using Rootbound.Player;
using Rootbound.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Rootbound.Tests
{
    public sealed class MovementPrototypeSceneTests
    {
        private Keyboard _keyboard;
        private InputSettings.BackgroundBehavior _backgroundBehavior;
#if UNITY_EDITOR
        private InputSettings.EditorInputBehaviorInPlayMode _editorBehavior;
#endif

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _backgroundBehavior = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            _editorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            _keyboard = InputSystem.AddDevice<Keyboard>();
            SceneManager.LoadScene("MovementPrototype");
            yield return null;
            yield return new WaitForSeconds(0.4f);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            InputSystem.RemoveDevice(_keyboard);
            InputSystem.settings.backgroundBehavior = _backgroundBehavior;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = _editorBehavior;
#endif
            var scene = SceneManager.CreateScene("EmptyMovementTestScene");
            SceneManager.SetActiveScene(scene);
            yield return SceneManager.UnloadSceneAsync("MovementPrototype");
            var audio = GameObject.Find("RootboundAudioManager");
            if (audio != null) Object.Destroy(audio);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SceneLoadsWithVisiblePlayerAndKeyboardMovementAndRestart()
        {
            var player = Object.FindAnyObjectByType<PlayerController2D>();
            Assert.IsNotNull(player);
            Assert.IsNotNull(player.GetComponent<PlayerInputReader>());
            Assert.IsNotNull(player.GetComponentInChildren<SpriteRenderer>().sprite);
            Assert.IsTrue(player.IsGrounded);
            Assert.IsNotNull(GraphicsSettings.currentRenderPipeline);
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
                    Assert.IsNotNull(component, "La scena contiene uno script mancante.");

            float startX = player.Body.position.x;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.D));
            yield return new WaitForSeconds(0.3f);
            Assert.Greater(player.Body.position.x - startX, 1f,
                $"Input orizzontale: {player.GetComponent<PlayerInputReader>().MoveX}; tastiera attiva: {_keyboard.enabled}");
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            player.Body.position = new Vector2(22f, 8f);
            yield return new WaitForSeconds(0.4f);
            Assert.Greater(Camera.main.transform.position.x, 15f);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.R));
            yield return null;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return new WaitForSeconds(0.25f);
            Assert.AreEqual(3f, player.Body.position.x, 0.1f);
            Assert.IsTrue(player.IsGrounded);

            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
            {
                var camera = Camera.main;
                var target = new RenderTexture(1280, 720, 24);
                var previous = camera.targetTexture;
                camera.targetTexture = target;
                yield return null;
                yield return null;
                var active = RenderTexture.active;
                RenderTexture.active = target;
                var image = new Texture2D(1280, 720, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                image.Apply();
                Directory.CreateDirectory("Logs");
                File.WriteAllBytes("Logs/movement-prototype.png", image.EncodeToPNG());
                RenderTexture.active = active;
                camera.targetTexture = previous;
                Object.Destroy(image);
                Object.Destroy(target);
            }
        }

        [UnityTest]
        public IEnumerator PlayerJumpsThroughAndLandsOnTheOneWayPlatform()
        {
            var player = Object.FindAnyObjectByType<PlayerController2D>();
            player.Body.position = new Vector2(9f, 2.05f);
            player.Body.linearVelocity = Vector2.zero;
            yield return new WaitForSeconds(0.1f);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Space));
            yield return new WaitForSeconds(0.45f);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return new WaitForSeconds(0.7f);
            Assert.IsTrue(player.IsGrounded);
            Assert.AreEqual(4.6f, player.Body.position.y, 0.15f);
        }
    }
}
