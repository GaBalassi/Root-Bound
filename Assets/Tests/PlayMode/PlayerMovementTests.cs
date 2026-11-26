using System.Collections;
using NUnit.Framework;
using Rootbound.Core;
using Rootbound.Player;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Rootbound.Tests
{
    public sealed class PlayerMovementTests
    {
        private sealed class TestInput : IPlayerInput
        {
            public float MoveX { get; set; }
            public float MoveY => 0f;
            public bool JumpPressed { get; set; }
            public bool JumpHeld { get; set; }
            public bool DashPressed { get; set; }
            public bool AttackPressed => false;
            public bool AttackHeld => false;
            public bool AttackReleased => false;
            public bool RootPressed => false;
            public bool InteractPressed => false;
            public bool RestartPressed => false;
            public bool VillagePressed => false;
            public bool PausePressed => false;
        }

        private GameObject _floor;
        private GameObject _player;
        private PlayerController2D _controller;
        private TestInput _input;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _floor = new GameObject("MovementTestFloor");
            _floor.layer = RootboundLayers.Ground;
            _floor.transform.position = new Vector3(0f, -0.5f, 0f);
            _floor.AddComponent<BoxCollider2D>().size = new Vector2(40f, 1f);

            _player = new GameObject("MovementTestPlayer");
            _player.layer = RootboundLayers.Player;
            _player.transform.position = new Vector3(0f, 1.5f, 0f);
            var visual = new GameObject("Visual");
            visual.transform.SetParent(_player.transform, false);
            visual.AddComponent<SpriteRenderer>();
            var collider = _player.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.65f, 1.4f);
            collider.offset = new Vector2(0f, 0.7f);
            _player.AddComponent<Rigidbody2D>();
            _controller = _player.AddComponent<PlayerController2D>();
            _input = new TestInput();
            _controller.SetInputSource(_input);
            yield return new WaitForSeconds(0.8f);
            Assert.IsTrue(_controller.IsGrounded, "Il personaggio deve atterrare sul pavimento.");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(_player);
            Object.Destroy(_floor);
            yield return new WaitForSeconds(0.5f);
        }

        [UnityTest]
        public IEnumerator MovementAcceleratesAndControlLockStopsHorizontalMotion()
        {
            float startX = _controller.Body.position.x;
            _input.MoveX = 1f;
            yield return new WaitForSeconds(0.3f);
            Assert.Greater(_controller.Body.position.x - startX, 1f);
            _controller.SetControlLocked(true);
            float lockedX = _controller.Body.position.x;
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(lockedX, _controller.Body.position.x, 0.05f);
        }

        [UnityTest]
        public IEnumerator JumpCanBeRepeatedOnceInAirAndDashExpires()
        {
            _input.JumpHeld = true;
            _input.JumpPressed = true;
            yield return null;
            _input.JumpPressed = false;
            yield return new WaitForSeconds(0.12f);
            Assert.Greater(_controller.Body.linearVelocity.y, 0f);
            Assert.IsFalse(_controller.IsGrounded);

            _controller.Body.linearVelocity = new Vector2(0f, -1f);
            _input.JumpPressed = true;
            yield return null;
            _input.JumpPressed = false;
            yield return new WaitForFixedUpdate();
            Assert.Greater(_controller.Body.linearVelocity.y, 5f);

            _controller.Body.linearVelocity = new Vector2(0f, -1f);
            _input.JumpPressed = true;
            yield return null;
            _input.JumpPressed = false;
            yield return new WaitForFixedUpdate();
            Assert.Less(_controller.Body.linearVelocity.y, 0f, "Il terzo salto non deve essere disponibile.");

            _input.MoveX = 1f;
            _input.DashPressed = true;
            yield return null;
            _input.DashPressed = false;
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(_controller.IsDashing);
            Assert.Greater(_controller.Body.linearVelocity.x, 15f);
            yield return new WaitForSeconds(0.25f);
            Assert.IsFalse(_controller.IsDashing);
            Assert.Greater(_controller.Body.gravityScale, 0f);
        }
    }
}
