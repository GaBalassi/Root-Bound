using Rootbound.Core;
using Rootbound.Player;
using Rootbound.Visuals;
using UnityEngine;

namespace Rootbound.World
{
    public sealed class MovementPrototypeController : MonoBehaviour
    {
        [SerializeField] private RootboundArtLibrary artLibrary;
        [SerializeField] private PlayerController2D player;
        [SerializeField] private CameraFollow2D followCamera;
        [SerializeField] private Vector2 spawnPoint = new Vector2(3f, 2.1f);
        private IPlayerInput _input;

        public void Configure(RootboundArtLibrary art, PlayerController2D controller, CameraFollow2D camera)
        {
            artLibrary = art;
            player = controller;
            followCamera = camera;
        }

        private void Start()
        {
            RootboundLayers.ConfigureCollisionMatrix();
            VfxFactory.SetArtLibrary(artLibrary);
            player.Initialize(artLibrary);
            _input = player.GetComponent<IPlayerInput>();
            followCamera.SetTarget(player.transform);
            followCamera.SetBounds(new Bounds(new Vector3(22f, 8f, 0f), new Vector3(44f, 16f, 1f)));
        }

        private void Update()
        {
            if ((_input != null && _input.RestartPressed) || player.Body.position.y < -6f)
            {
                ResetPlayer();
            }
        }

        public void ResetPlayer()
        {
            player.CancelRootPull(false);
            player.Body.position = spawnPoint;
            player.Body.linearVelocity = Vector2.zero;
            player.transform.position = spawnPoint;
            followCamera.SnapToTarget();
        }
    }
}
