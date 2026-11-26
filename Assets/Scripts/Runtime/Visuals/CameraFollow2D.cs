using Rootbound.Player;
using UnityEngine;

namespace Rootbound.Visuals
{
    /// <summary>
    /// Camera 2D fluida con lookahead direzionale, limiti stanza e screen shake.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFollow2D : MonoBehaviour
    {
        public static CameraFollow2D Instance { get; private set; }

        [SerializeField] private Transform target;
        [SerializeField] private float smoothTime = 0.14f;
        [SerializeField] private float lookaheadX = 1.6f;
        [SerializeField] private Vector2 baseOffset = new Vector2(0f, 1.3f);

        private Camera _cam;
        private Vector3 _velocity;
        private bool _hasBounds;
        private Bounds _roomBounds;
        private float _shakeTimer;
        private float _shakeDuration;
        private float _shakeIntensity;
        private PlayerController2D _playerTarget;

        private void Awake()
        {
            Instance = this;
            _cam = GetComponent<Camera>();
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            _playerTarget = newTarget != null ? newTarget.GetComponent<PlayerController2D>() : null;
        }

        public void SetBounds(Bounds bounds)
        {
            _roomBounds = bounds;
            _hasBounds = true;
            SnapToTarget();
        }

        public void ClearBounds()
        {
            _hasBounds = false;
        }

        public void SnapToTarget()
        {
            if (target == null) return;
            Vector3 desired = ComputeDesiredPosition(0f);
            desired.z = transform.position.z;
            transform.position = desired;
            _velocity = Vector3.zero;
        }

        public void Shake(float intensity, float duration)
        {
            _shakeIntensity = Mathf.Max(_shakeIntensity, intensity);
            _shakeDuration = Mathf.Max(_shakeDuration, duration);
            _shakeTimer = Mathf.Max(_shakeTimer, duration);
        }

        private void LateUpdate()
        {
            if (target == null) return;

            float facing = _playerTarget != null ? _playerTarget.Facing : 1f;
            Vector3 desired = ComputeDesiredPosition(facing * lookaheadX);
            desired.z = transform.position.z;

            Vector3 smoothed = Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothTime);
            smoothed.z = transform.position.z;

            if (_shakeTimer > 0f)
            {
                _shakeTimer -= Time.unscaledDeltaTime;
                float norm = _shakeDuration > 0f ? Mathf.Clamp01(_shakeTimer / _shakeDuration) : 0f;
                float amp = _shakeIntensity * norm;
                smoothed.x += Random.Range(-amp, amp);
                smoothed.y += Random.Range(-amp, amp);
                if (_shakeTimer <= 0f)
                {
                    _shakeIntensity = 0f;
                    _shakeDuration = 0f;
                }
            }

            transform.position = smoothed;
        }

        private Vector3 ComputeDesiredPosition(float extraLookX)
        {
            Vector3 desired = target.position + (Vector3)baseOffset + new Vector3(extraLookX, 0f, 0f);
            if (_hasBounds && _cam != null)
            {
                float halfH = _cam.orthographicSize;
                float halfW = halfH * _cam.aspect;
                float minX = _roomBounds.min.x + halfW;
                float maxX = _roomBounds.max.x - halfW;
                float minY = _roomBounds.min.y + halfH;
                float maxY = _roomBounds.max.y - halfH;

                desired.x = minX <= maxX ? Mathf.Clamp(desired.x, minX, maxX) : _roomBounds.center.x;
                desired.y = minY <= maxY ? Mathf.Clamp(desired.y, minY, maxY) : _roomBounds.center.y;
            }
            return desired;
        }
    }
}
