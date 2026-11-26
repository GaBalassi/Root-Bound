using System;
using UnityEngine;

namespace Rootbound.Player
{
    public enum PlayerVisualState
    {
        Idle,
        Run,
        Jump,
        Fall,
        Dash,
        Attack1,
        Attack2,
        Attack3,
        Charge,
        Root,
        Hurt
    }

    [Serializable]
    public sealed class PlayerMovementConfig
    {
        public float moveSpeed = 8.4f;
        public float groundAcceleration = 72f;
        public float groundDeceleration = 80f;
        public float airAcceleration = 48f;
        public float airDeceleration = 32f;

        public float jumpVelocity = 13.6f;
        public float doubleJumpVelocity = 12.2f;
        public float jumpCutMultiplier = 0.48f;
        public float gravityScale = 3.2f;
        public float fallGravityMultiplier = 1.65f;
        public float maxFallSpeed = 19f;

        public float coyoteTime = 0.12f;
        public float jumpBufferTime = 0.13f;
        public int maxAirJumps = 1;

        public float wallSlideSpeed = 3.2f;
        public float wallJumpHorizontal = 9.5f;
        public float wallJumpVertical = 12.8f;

        public float dashSpeed = 20.5f;
        public float dashDuration = 0.14f;
        public float dashCooldown = 0.55f;

        public float rootPullSpeed = 23f;
    }

    /// <summary>
    /// Modificatori cumulativi applicati da reliquie e potenziamenti del villaggio.
    /// </summary>
    [Serializable]
    public struct PlayerStatsModifiers
    {
        public float moveSpeedMultiplier;
        public float dashDistanceMultiplier;
        public float dashCooldownMultiplier;
        public float rootCooldownMultiplier;
        public float damageMultiplier;
        public float airDamageMultiplier;
        public float lowHpDamageMultiplier;
        public int bonusMaxHp;
        public int incomingDamageReduction;
        public float currencyMultiplier;
        public bool poisonOnHit;
        public bool healingSapOnCombo;

        public static PlayerStatsModifiers Default => new PlayerStatsModifiers
        {
            moveSpeedMultiplier = 1f,
            dashDistanceMultiplier = 1f,
            dashCooldownMultiplier = 1f,
            rootCooldownMultiplier = 1f,
            damageMultiplier = 1f,
            airDamageMultiplier = 1f,
            lowHpDamageMultiplier = 1f,
            bonusMaxHp = 0,
            incomingDamageReduction = 0,
            currencyMultiplier = 1f,
            poisonOnHit = false,
            healingSapOnCombo = false
        };
    }

    /// <summary>
    /// Logica pura di salto/coyote/buffer/dash per test unitari deterministici.
    /// </summary>
    public static class MovementMath
    {
        public static float AdvanceTimer(float value, float dt)
        {
            return value > 0f ? Mathf.Max(0f, value - dt) : 0f;
        }

        public static bool CanGroundJump(bool isGrounded, float coyoteTimer, float jumpBufferTimer)
        {
            return jumpBufferTimer > 0f && (isGrounded || coyoteTimer > 0f);
        }

        public static bool CanAirJump(bool isGrounded, float coyoteTimer, int airJumpsRemaining)
        {
            return !isGrounded && coyoteTimer <= 0f && airJumpsRemaining > 0;
        }
    }
}
