using System.Collections.Generic;
using Rootbound.Audio;
using Rootbound.Core;
using Rootbound.Visuals;
using UnityEngine;

namespace Rootbound.Player
{
    public enum RootTargetKind
    {
        Anchor = 0,
        Enemy = 1,
        NaturalSwitch = 2
    }

    public interface IRootTarget
    {
        Vector2 RootPoint { get; }
        RootTargetKind Kind { get; }
        bool IsRootTargetAvailable { get; }
    }

    public interface IRootBindable
    {
        void ApplyRootBind(float duration);
    }

    /// <summary>
    /// Selezione deterministica del miglior bersaglio per il Potere delle Radici.
    /// </summary>
    public static class RootPowerTargeting
    {
        public readonly struct Candidate
        {
            public readonly int Index;
            public readonly Vector2 Position;
            public readonly RootTargetKind Kind;

            public Candidate(int index, Vector2 position, RootTargetKind kind)
            {
                Index = index;
                Position = position;
                Kind = kind;
            }
        }

        public static int SelectBestIndex(Vector2 origin, int facing, float maxRange, IReadOnlyList<Candidate> candidates)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return -1;
            }

            int bestIndex = -1;
            float bestScore = float.MaxValue;
            float rangeSq = maxRange * maxRange;

            for (int i = 0; i < candidates.Count; i++)
            {
                Vector2 delta = candidates[i].Position - origin;
                float distSq = delta.sqrMagnitude;
                if (distSq > rangeSq || distSq < 0.16f)
                {
                    continue;
                }

                float dist = Mathf.Sqrt(distSq);
                float facingDot = delta.normalized.x * facing;
                // Premia i bersagli davanti allo sguardo e vicini; priorita' leggera ad ancora/interruttore in salto.
                float directionPenalty = facingDot < -0.25f ? 3.5f : (1f - facingDot) * 1.1f;
                float typeBonus = candidates[i].Kind == RootTargetKind.Enemy ? 0.35f : 0f;
                float score = dist + directionPenalty + typeBonus;

                if (score < bestScore)
                {
                    bestScore = score;
                    bestIndex = candidates[i].Index;
                }
            }

            return bestIndex;
        }
    }

    /// <summary>
    /// Gestisce la meccanica centrale "Potere delle Radici": aggancio a punti verdi,
    /// blocco temporaneo dei nemici e attivazione di interruttori naturali.
    /// </summary>
    public sealed class RootPowerController : MonoBehaviour
    {
        [SerializeField] private float rootRange = 8.2f;
        [SerializeField] private float baseCooldown = 0.62f;
        [SerializeField] private float enemyBindDuration = 1.7f;

        private PlayerController2D _player;
        private IPlayerInput _input;
        private RootboundArtLibrary _artLibrary;

        private float _cooldownTimer;
        private SpriteRenderer _reticleRenderer;
        private LineRenderer _vineLine;
        private float _vineVisibleTimer;
        private Vector2 _vineEndWorld;

        private readonly List<IRootTarget> _discoveredTargets = new List<IRootTarget>(16);
        private readonly List<RootPowerTargeting.Candidate> _candidates = new List<RootPowerTargeting.Candidate>(16);
        private readonly Collider2D[] _overlapBuffer = new Collider2D[24];

        public IRootTarget CurrentBestTarget { get; private set; }
        public float CooldownNormalized =>
            baseCooldown <= 0f ? 0f : Mathf.Clamp01(_cooldownTimer / (baseCooldown * (_player != null ? _player.Modifiers.rootCooldownMultiplier : 1f)));

        public void Initialize(PlayerController2D player, RootboundArtLibrary artLibrary)
        {
            _player = player;
            _artLibrary = artLibrary;
            _input = GetComponent<IPlayerInput>();

            CreateReticle();
            CreateVineRenderer();
        }

        public void SetInputSource(IPlayerInput customInput)
        {
            _input = customInput;
        }

        private void CreateReticle()
        {
            if (_reticleRenderer != null) return;
            var go = new GameObject("RootReticle");
            _reticleRenderer = go.AddComponent<SpriteRenderer>();
            _reticleRenderer.sprite = _artLibrary != null ? _artLibrary.rootReticle : null;
            _reticleRenderer.sortingLayerName = RootboundLayers.SortVfx;
            _reticleRenderer.sortingOrder = 15;
            _reticleRenderer.color = new Color(0.45f, 1f, 0.72f, 0.95f);
            go.SetActive(false);
        }

        private void CreateVineRenderer()
        {
            if (_vineLine != null) return;
            var go = new GameObject("RootVineEffect");
            go.transform.SetParent(transform, false);
            _vineLine = go.AddComponent<LineRenderer>();
            _vineLine.useWorldSpace = true;
            _vineLine.positionCount = 2;
            _vineLine.startWidth = 0.24f;
            _vineLine.endWidth = 0.16f;
            _vineLine.sortingLayerName = RootboundLayers.SortVfx;
            _vineLine.sortingOrder = 12;
            _vineLine.material = new Material(Shader.Find("Sprites/Default"));
            _vineLine.startColor = new Color(0.22f, 0.85f, 0.48f, 1f);
            _vineLine.endColor = new Color(0.55f, 1f, 0.74f, 1f);
            _vineLine.enabled = false;
        }

        private void Update()
        {
            if (_player == null) return;

            _cooldownTimer = MovementMath.AdvanceTimer(_cooldownTimer, Time.deltaTime);

            ScanBestTarget();
            UpdateReticleVisual();
            UpdateVineVisual();

            if (_input != null && !_player.IsControlLocked && _input.RootPressed && _cooldownTimer <= 0f)
            {
                TryCastRoot();
            }
        }

        public bool TryCastRoot()
        {
            if (_player == null || _cooldownTimer > 0f) return false;
            ScanBestTarget();
            if (CurrentBestTarget == null)
            {
                return false;
            }

            _cooldownTimer = baseCooldown * _player.Modifiers.rootCooldownMultiplier;
            _vineEndWorld = CurrentBestTarget.RootPoint;
            _vineVisibleTimer = 0.24f;
            _player.TriggerCombatAnimation(PlayerVisualState.Root, 0.22f);

            RetroAudioManager.Play(SoundEffectId.RootWhip);

            switch (CurrentBestTarget.Kind)
            {
                case RootTargetKind.Anchor:
                    _player.ApplyRootPull(CurrentBestTarget.RootPoint);
                    VfxFactory.SpawnLeafBurst(CurrentBestTarget.RootPoint, 6);
                    RetroAudioManager.Play(SoundEffectId.RootLatch);
                    break;

                case RootTargetKind.Enemy:
                    if (CurrentBestTarget is MonoBehaviour enemyMb && enemyMb.TryGetComponent<IRootBindable>(out var bindable))
                    {
                        bindable.ApplyRootBind(enemyBindDuration);
                    }
                    VfxFactory.SpawnDustBurst(CurrentBestTarget.RootPoint, new Color(0.35f, 1f, 0.65f, 0.9f), 8);
                    RetroAudioManager.Play(SoundEffectId.RootLatch);
                    break;

                case RootTargetKind.NaturalSwitch:
                    if (CurrentBestTarget is MonoBehaviour swMb)
                    {
                        swMb.SendMessage("ActivateFromRoot", SendMessageOptions.DontRequireReceiver);
                    }
                    VfxFactory.SpawnLeafBurst(CurrentBestTarget.RootPoint, 8);
                    RetroAudioManager.Play(SoundEffectId.RootLatch);
                    break;
            }

            return true;
        }

        private void ScanBestTarget()
        {
            _discoveredTargets.Clear();
            _candidates.Clear();

            Vector2 origin = (Vector2)transform.position + Vector2.up * 0.6f;
            int count = Physics2D.OverlapCircleNonAlloc(origin, rootRange, _overlapBuffer, RootboundLayers.RootTargetMask);
            for (int i = 0; i < count; i++)
            {
                Collider2D col = _overlapBuffer[i];
                if (col == null) continue;
                var target = col.GetComponentInParent<IRootTarget>();
                if (target == null || !target.IsRootTargetAvailable) continue;
                if (_discoveredTargets.Contains(target)) continue;

                int idx = _discoveredTargets.Count;
                _discoveredTargets.Add(target);
                _candidates.Add(new RootPowerTargeting.Candidate(idx, target.RootPoint, target.Kind));
            }

            int bestIdx = RootPowerTargeting.SelectBestIndex(origin, _player.Facing, rootRange, _candidates);
            CurrentBestTarget = bestIdx >= 0 ? _discoveredTargets[bestIdx] : null;
        }

        private void UpdateReticleVisual()
        {
            if (_reticleRenderer == null) return;
            if (CurrentBestTarget == null || _player.IsControlLocked)
            {
                if (_reticleRenderer.gameObject.activeSelf)
                {
                    _reticleRenderer.gameObject.SetActive(false);
                }
                return;
            }

            if (!_reticleRenderer.gameObject.activeSelf)
            {
                _reticleRenderer.gameObject.SetActive(true);
            }

            _reticleRenderer.transform.position = (Vector3)CurrentBestTarget.RootPoint;
            float pulse = 1f + Mathf.Sin(Time.time * 9f) * 0.14f;
            _reticleRenderer.transform.localScale = new Vector3(pulse, pulse, 1f);
            _reticleRenderer.transform.Rotate(0f, 0f, 65f * Time.deltaTime);
        }

        private void UpdateVineVisual()
        {
            if (_vineLine == null) return;
            if (_vineVisibleTimer > 0f)
            {
                _vineVisibleTimer -= Time.deltaTime;
                _vineLine.enabled = true;
                Vector3 start = transform.position + Vector3.up * 0.65f;
                Vector3 end = _player.IsRootPulling ? _vineEndWorld : (Vector3)_vineEndWorld;
                _vineLine.SetPosition(0, start);
                _vineLine.SetPosition(1, end);
            }
            else if (_vineLine.enabled)
            {
                _vineLine.enabled = false;
            }
        }

        private void OnDestroy()
        {
            if (_reticleRenderer != null)
            {
                Destroy(_reticleRenderer.gameObject);
            }
        }
    }
}
