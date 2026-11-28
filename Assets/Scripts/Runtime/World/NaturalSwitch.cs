using System;
using Rootbound.Core;
using Rootbound.Player;
using UnityEngine;

namespace Rootbound.World
{
    /// <summary>
    /// Totem naturale attivabile con il Potere delle Radici: sblocca ponti, piattaforme o premi segreti.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class NaturalSwitch : MonoBehaviour, IRootTarget
    {
        private SpriteRenderer _renderer;
        private RootboundArtLibrary _art;
        private GameObject[] _revealTargets;

        public bool IsActivated { get; private set; }
        public Vector2 RootPoint => (Vector2)transform.position + Vector2.up * 0.7f;
        public RootTargetKind Kind => RootTargetKind.NaturalSwitch;
        public bool IsRootTargetAvailable => !IsActivated && gameObject.activeInHierarchy;

        public event Action OnActivated;

        public void Initialize(RootboundArtLibrary art, params GameObject[] revealTargets)
        {
            _art = art;
            _revealTargets = revealTargets;
            gameObject.layer = RootboundLayers.Interactable;

            var col = GetComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.1f, 1.5f);
            col.offset = new Vector2(0f, 0.75f);

            _renderer = GetComponent<SpriteRenderer>();
            if (_renderer == null) _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sortingLayerName = RootboundLayers.SortProps;
            _renderer.sortingOrder = 4;
            if (_art != null)
            {
                _renderer.sprite = _art.naturalSwitchOff;
            }

            if (_revealTargets != null)
            {
                foreach (var target in _revealTargets)
                {
                    if (target != null) target.SetActive(false);
                }
            }
        }

        public void ActivateFromRoot()
        {
            if (IsActivated) return;
            IsActivated = true;
            if (_renderer != null && _art != null)
            {
                _renderer.sprite = _art.naturalSwitchOn;
            }

            if (_revealTargets != null)
            {
                foreach (var target in _revealTargets)
                {
                    if (target != null) target.SetActive(true);
                }
            }

            OnActivated?.Invoke();
        }
    }
}
