using Rootbound.Core;
using Rootbound.Player;
using Rootbound.Visuals;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Rootbound.World
{
    /// <summary>
    /// Bocciolo luminoso agganciabile dal Potere delle Radici per slanci aerei.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class RootAnchor : MonoBehaviour, IRootTarget
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        private SpriteSheetAnimator _animator;

        public Vector2 RootPoint => transform.position;
        public RootTargetKind Kind => RootTargetKind.Anchor;
        public bool IsRootTargetAvailable => gameObject.activeInHierarchy;

        public void Initialize(RootboundArtLibrary art)
        {
            gameObject.layer = RootboundLayers.RootAnchor;
            var col = GetComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.75f;

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer == null)
                {
                    spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                }
            }

            spriteRenderer.sortingLayerName = RootboundLayers.SortProps;
            spriteRenderer.sortingOrder = 5;

            if (art != null && art.rootAnchorFrames != null && art.rootAnchorFrames.Length > 0)
            {
                _animator = gameObject.GetComponent<SpriteSheetAnimator>();
                if (_animator == null) _animator = gameObject.AddComponent<SpriteSheetAnimator>();
                _animator.PlayLoop(art.rootAnchorFrames, 4f);
            }

            if (GetComponentInChildren<Light2D>() == null)
            {
                var lightGo = new GameObject("AnchorGlow");
                lightGo.transform.SetParent(transform, false);
                var light = lightGo.AddComponent<Light2D>();
                light.lightType = Light2D.LightType.Point;
                light.color = new Color(0.35f, 1f, 0.68f, 1f);
                light.intensity = 1.15f;
                light.pointLightOuterRadius = 3.2f;
                light.pointLightInnerRadius = 0.4f;
            }
        }
    }
}
