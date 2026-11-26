using System;
using Rootbound.Audio;
using Rootbound.Core;
using Rootbound.Visuals;
using UnityEngine;

namespace Rootbound.Player
{
    /// <summary>
    /// Controller 2D reattivo del Guardiano della Foresta: corsa fluida, salto variabile,
    /// doppio salto, wall jump, dash con i-frame e slancio con Potere delle Radici.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class PlayerController2D : MonoBehaviour
    {
        [SerializeField] private PlayerMovementConfig config = new PlayerMovementConfig();
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private SpriteSheetAnimator animator;

        private Rigidbody2D _rb;
        private BoxCollider2D _col;
        private IPlayerInput _input;
        private RootboundArtLibrary _artLibrary;
        private PlayerStatsModifiers _mods = PlayerStatsModifiers.Default;

        private float _coyoteTimer;
        private float _jumpBufferTimer;
        private int _airJumpsRemaining;
        private bool _wasGrounded;

        private float _dashTimer;
        private float _dashCooldownTimer;
        private float _dashDirection = 1f;
        private float _afterimageTimer;

        private bool _isRootPulling;
        private Vector2 _rootPullTarget;
        private float _rootPullTimer;

        private bool _isWallSliding;
        private int _wallDirection;
        private float _wallJumpLockTimer;

        private bool _controlLocked;
        private float _hurtVisualTimer;
        private float _combatAnimTimer;
        private PlayerVisualState _combatAnimState;

        private Vector3 _visualBaseScale = Vector3.one;
        private Vector3 _visualCurrentScale = Vector3.one;
        private Transform _visualTransform;

        public int Facing { get; private set; } = 1;
        public bool IsGrounded { get; private set; }
        public bool IsDashing => _dashTimer > 0f;
        public bool IsRootPulling => _isRootPulling;
        public bool IsWallSliding => _isWallSliding;
        public bool IsControlLocked => _controlLocked;
        public float DashCooldownNormalized =>
            config.dashCooldown <= 0f ? 0f : Mathf.Clamp01(_dashCooldownTimer / (config.dashCooldown * _mods.dashCooldownMultiplier));
        public PlayerMovementConfig Config => config;
        public PlayerStatsModifiers Modifiers => _mods;
        public Rigidbody2D Body => _rb;

        public event Action OnLanded;
        public event Action OnDashed;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<BoxCollider2D>();
            _rb.freezeRotation = true;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<SpriteSheetAnimator>();
            }

            _visualTransform = spriteRenderer != null ? spriteRenderer.transform : transform;
            _visualBaseScale = _visualTransform.localScale;
            _visualCurrentScale = _visualBaseScale;

            _input = GetComponent<IPlayerInput>();
            _airJumpsRemaining = config.maxAirJumps;
        }

        public void Initialize(RootboundArtLibrary artLibrary)
        {
            _artLibrary = artLibrary;
            if (animator == null && spriteRenderer != null)
            {
                animator = spriteRenderer.gameObject.GetComponent<SpriteSheetAnimator>();
                if (animator == null)
                {
                    animator = spriteRenderer.gameObject.AddComponent<SpriteSheetAnimator>();
                }
            }

            if (animator != null && _artLibrary != null)
            {
                animator.PlayLoop(_artLibrary.playerIdle, 8f);
            }
        }

        public void SetInputSource(IPlayerInput customInput)
        {
            _input = customInput;
        }

        public void SetModifiers(PlayerStatsModifiers modifiers)
        {
            _mods = modifiers;
        }

        public void SetControlLocked(bool locked)
        {
            _controlLocked = locked;
            if (locked && _rb != null)
            {
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
            }
        }

        public void TriggerCombatAnimation(PlayerVisualState state, float duration)
        {
            _combatAnimState = state;
            _combatAnimTimer = duration;
        }

        public void TriggerHurtVisual(float duration = 0.25f)
        {
            _hurtVisualTimer = duration;
        }

        /// <summary>
        /// Aggancia il giocatore verso un'ancora delle radici e ricarica doppio salto e dash.
        /// </summary>
        public void ApplyRootPull(Vector2 targetPosition, float speedOverride = -1f)
        {
            _isRootPulling = true;
            _dashTimer = 0f;
            _rootPullTarget = targetPosition;
            _rootPullTimer = 0.45f;

            Vector2 dir = (targetPosition - _rb.position);
            float dist = dir.magnitude;
            float spd = speedOverride > 0f ? speedOverride : config.rootPullSpeed;
            if (dist > 0.05f)
            {
                dir /= dist;
                Facing = dir.x >= 0f ? 1 : -1;
                _rb.gravityScale = 0f;
                _rb.linearVelocity = dir * spd;
                _rootPullTimer = Mathf.Clamp(dist / spd + 0.04f, 0.1f, 0.45f);
            }

            _airJumpsRemaining = config.maxAirJumps;
            _dashCooldownTimer = 0f;
            _visualCurrentScale = new Vector3(_visualBaseScale.x * 1.18f, _visualBaseScale.y * 0.85f, 1f);
        }

        public void CancelRootPull(bool upwardBoost = true)
        {
            if (!_isRootPulling) return;
            _isRootPulling = false;
            _rb.gravityScale = config.gravityScale;
            if (upwardBoost && _rb.linearVelocity.y < 6.5f)
            {
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x * 0.72f, 7.2f);
            }
        }

        public void ResetAirMobility()
        {
            _airJumpsRemaining = config.maxAirJumps;
            _dashCooldownTimer = 0f;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _coyoteTimer = MovementMath.AdvanceTimer(_coyoteTimer, dt);
            _jumpBufferTimer = MovementMath.AdvanceTimer(_jumpBufferTimer, dt);
            _dashCooldownTimer = MovementMath.AdvanceTimer(_dashCooldownTimer, dt);
            _wallJumpLockTimer = MovementMath.AdvanceTimer(_wallJumpLockTimer, dt);
            _hurtVisualTimer = MovementMath.AdvanceTimer(_hurtVisualTimer, dt);
            _combatAnimTimer = MovementMath.AdvanceTimer(_combatAnimTimer, dt);

            UpdateGroundAndWallChecks();

            if (_input == null || _controlLocked)
            {
                UpdateVisuals(dt);
                return;
            }

            float moveX = _input.MoveX;
            if (Mathf.Abs(moveX) > 0.08f && _dashTimer <= 0f && _wallJumpLockTimer <= 0f)
            {
                Facing = moveX > 0f ? 1 : -1;
            }

            if (_input.JumpPressed)
            {
                _jumpBufferTimer = config.jumpBufferTime;
            }

            // Dash
            if (_input.DashPressed && _dashCooldownTimer <= 0f && _dashTimer <= 0f)
            {
                StartDash(moveX);
            }

            // Salto a terra / coyote / wall jump / doppio salto
            if (_jumpBufferTimer > 0f)
            {
                if (_isRootPulling)
                {
                    CancelRootPull(false);
                    ExecuteJump(config.jumpVelocity);
                }
                else if (MovementMath.CanGroundJump(IsGrounded, _coyoteTimer, _jumpBufferTimer))
                {
                    ExecuteJump(config.jumpVelocity);
                }
                else if (_isWallSliding)
                {
                    ExecuteWallJump();
                }
                else if (MovementMath.CanAirJump(IsGrounded, _coyoteTimer, _airJumpsRemaining))
                {
                    _airJumpsRemaining--;
                    ExecuteJump(config.doubleJumpVelocity, isDoubleJump: true);
                }
            }

            // Altezza variabile al rilascio del salto
            if (!_input.JumpHeld && _rb.linearVelocity.y > 0f && !_isRootPulling && _dashTimer <= 0f)
            {
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _rb.linearVelocity.y * config.jumpCutMultiplier);
            }

            UpdateVisuals(dt);
        }

        private void FixedUpdate()
        {
            float fdt = Time.fixedDeltaTime;

            if (_isRootPulling)
            {
                _rootPullTimer -= fdt;
                float dist = Vector2.Distance(_rb.position, _rootPullTarget);
                if (dist <= 0.65f || _rootPullTimer <= 0f)
                {
                    CancelRootPull(true);
                }
                else
                {
                    Vector2 dir = (_rootPullTarget - _rb.position).normalized;
                    _rb.linearVelocity = dir * config.rootPullSpeed;
                }
                return;
            }

            if (_dashTimer > 0f)
            {
                _dashTimer -= fdt;
                _afterimageTimer -= fdt;
                _rb.gravityScale = 0f;
                float dashSpd = config.dashSpeed * _mods.dashDistanceMultiplier;
                _rb.linearVelocity = new Vector2(_dashDirection * dashSpd, 0f);

                if (_afterimageTimer <= 0f && spriteRenderer != null)
                {
                    _afterimageTimer = 0.032f;
                    VfxFactory.SpawnAfterimage(spriteRenderer, new Color(0.35f, 1f, 0.72f, 0.55f));
                }

                if (_dashTimer <= 0f)
                {
                    _rb.gravityScale = config.gravityScale;
                    _rb.linearVelocity = new Vector2(_rb.linearVelocity.x * 0.52f, 0f);
                }
                return;
            }

            float targetMoveX = (_input != null && !_controlLocked) ? _input.MoveX : 0f;
            float maxSpeed = config.moveSpeed * _mods.moveSpeedMultiplier;
            float desiredVx = targetMoveX * maxSpeed;

            float accel = IsGrounded
                ? (Mathf.Abs(desiredVx) > 0.05f ? config.groundAcceleration : config.groundDeceleration)
                : (Mathf.Abs(desiredVx) > 0.05f ? config.airAcceleration : config.airDeceleration);

            if (_wallJumpLockTimer > 0f)
            {
                accel *= 0.25f;
            }

            float newVx = Mathf.MoveTowards(_rb.linearVelocity.x, desiredVx, accel * fdt);
            float newVy = _rb.linearVelocity.y;

            if (_isWallSliding && newVy < -config.wallSlideSpeed)
            {
                newVy = -config.wallSlideSpeed;
            }
            else if (newVy < 0f)
            {
                _rb.gravityScale = config.gravityScale * config.fallGravityMultiplier;
                if (newVy < -config.maxFallSpeed)
                {
                    newVy = -config.maxFallSpeed;
                }
            }
            else
            {
                _rb.gravityScale = config.gravityScale;
            }

            _rb.linearVelocity = new Vector2(newVx, newVy);
        }

        private void UpdateGroundAndWallChecks()
        {
            Bounds b = _col.bounds;
            Vector2 feetOrigin = new Vector2(b.center.x, b.min.y + 0.04f);
            Vector2 feetSize = new Vector2(Mathf.Max(0.2f, b.size.x - 0.14f), 0.14f);
            RaycastHit2D groundHit = Physics2D.BoxCast(feetOrigin, feetSize, 0f, Vector2.down, 0.08f, RootboundLayers.SolidSurfaceMask);

            bool groundedNow = groundHit.collider != null && _rb.linearVelocity.y <= 0.35f;

            if (groundedNow && !_wasGrounded)
            {
                _visualCurrentScale = new Vector3(_visualBaseScale.x * 1.22f, _visualBaseScale.y * 0.78f, 1f);
                VfxFactory.SpawnDustBurst(new Vector2(b.center.x, b.min.y), new Color(0.65f, 0.85f, 0.62f, 0.75f), 5);
                OnLanded?.Invoke();
            }

            IsGrounded = groundedNow;
            _wasGrounded = groundedNow;

            if (IsGrounded)
            {
                _coyoteTimer = config.coyoteTime;
                _airJumpsRemaining = config.maxAirJumps;
            }

            // Controllo muro per Wall Slide / Wall Jump
            _isWallSliding = false;
            _wallDirection = 0;
            if (!IsGrounded && _rb.linearVelocity.y < 0.2f && _input != null && Mathf.Abs(_input.MoveX) > 0.15f)
            {
                int dir = _input.MoveX > 0f ? 1 : -1;
                Vector2 sideOrigin = new Vector2(b.center.x + dir * (b.extents.x * 0.7f), b.center.y);
                Vector2 sideSize = new Vector2(0.18f, b.size.y * 0.65f);
                RaycastHit2D wallHit = Physics2D.BoxCast(sideOrigin, sideSize, 0f, Vector2.right * dir, 0.1f, RootboundLayers.GroundMask);
                if (wallHit.collider != null)
                {
                    _isWallSliding = true;
                    _wallDirection = dir;
                }
            }
        }

        private void ExecuteJump(float jumpVel, bool isDoubleJump = false)
        {
            _jumpBufferTimer = 0f;
            _coyoteTimer = 0f;
            _rb.gravityScale = config.gravityScale;
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpVel);
            _visualCurrentScale = new Vector3(_visualBaseScale.x * 0.78f, _visualBaseScale.y * 1.24f, 1f);

            Bounds b = _col.bounds;
            Color dustCol = isDoubleJump ? new Color(0.42f, 1f, 0.68f, 0.85f) : new Color(0.75f, 0.88f, 0.7f, 0.8f);
            VfxFactory.SpawnDustBurst(new Vector2(b.center.x, b.min.y), dustCol, isDoubleJump ? 7 : 5);
            RetroAudioManager.Play(isDoubleJump ? SoundEffectId.DoubleJump : SoundEffectId.Jump);
        }

        private void ExecuteWallJump()
        {
            _jumpBufferTimer = 0f;
            int pushDir = -_wallDirection;
            Facing = pushDir;
            _wallJumpLockTimer = 0.14f;
            _rb.linearVelocity = new Vector2(pushDir * config.wallJumpHorizontal, config.wallJumpVertical);
            _visualCurrentScale = new Vector3(_visualBaseScale.x * 0.8f, _visualBaseScale.y * 1.2f, 1f);
            RetroAudioManager.Play(SoundEffectId.Jump);
        }

        private void StartDash(float moveX)
        {
            if (_isRootPulling)
            {
                CancelRootPull(false);
            }

            _dashDirection = Mathf.Abs(moveX) > 0.1f ? (moveX > 0f ? 1f : -1f) : Facing;
            Facing = _dashDirection >= 0f ? 1 : -1;
            _dashTimer = config.dashDuration;
            _dashCooldownTimer = config.dashCooldown * _mods.dashCooldownMultiplier;
            _afterimageTimer = 0f;
            _visualCurrentScale = new Vector3(_visualBaseScale.x * 1.28f, _visualBaseScale.y * 0.76f, 1f);

            Bounds b = _col.bounds;
            VfxFactory.SpawnDustBurst(new Vector2(b.center.x, b.min.y), new Color(0.45f, 1f, 0.78f, 0.85f), 6);
            RetroAudioManager.Play(SoundEffectId.Dash);
            OnDashed?.Invoke();
        }

        private void UpdateVisuals(float dt)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = Facing < 0;
            }

            _visualCurrentScale = Vector3.Lerp(_visualCurrentScale, _visualBaseScale, Mathf.Clamp01(dt * 14f));
            if (_visualTransform != null)
            {
                _visualTransform.localScale = _visualCurrentScale;
            }

            if (animator == null || _artLibrary == null)
            {
                return;
            }

            PlayerVisualState state = ResolveVisualState();
            switch (state)
            {
                case PlayerVisualState.Hurt:
                    animator.PlaySingle(_artLibrary.playerHurt);
                    break;
                case PlayerVisualState.Root:
                    animator.PlayLoop(_artLibrary.playerRoot, 12f);
                    break;
                case PlayerVisualState.Charge:
                    animator.PlayLoop(_artLibrary.playerCharge, 10f);
                    break;
                case PlayerVisualState.Attack1:
                    animator.PlayLoop(_artLibrary.playerAttack1, 14f);
                    break;
                case PlayerVisualState.Attack2:
                    animator.PlayLoop(_artLibrary.playerAttack2, 14f);
                    break;
                case PlayerVisualState.Attack3:
                    animator.PlayLoop(_artLibrary.playerAttack3, 12f);
                    break;
                case PlayerVisualState.Dash:
                    animator.PlayLoop(_artLibrary.playerDash, 14f);
                    break;
                case PlayerVisualState.Jump:
                    animator.PlayLoop(_artLibrary.playerJump, 10f);
                    break;
                case PlayerVisualState.Fall:
                    animator.PlayLoop(_artLibrary.playerFall, 10f);
                    break;
                case PlayerVisualState.Run:
                    animator.PlayLoop(_artLibrary.playerRun, 12f);
                    break;
                default:
                    animator.PlayLoop(_artLibrary.playerIdle, 7f);
                    break;
            }
        }

        private PlayerVisualState ResolveVisualState()
        {
            if (_hurtVisualTimer > 0f) return PlayerVisualState.Hurt;
            if (_isRootPulling) return PlayerVisualState.Root;
            if (_combatAnimTimer > 0f) return _combatAnimState;
            if (IsDashing) return PlayerVisualState.Dash;
            if (!IsGrounded)
            {
                return _rb.linearVelocity.y >= 0f ? PlayerVisualState.Jump : PlayerVisualState.Fall;
            }

            return Mathf.Abs(_rb.linearVelocity.x) > 0.45f ? PlayerVisualState.Run : PlayerVisualState.Idle;
        }
    }
}
