using UnityEngine;
using UnityEngine.InputSystem;

namespace Rootbound.Core
{
    /// <summary>
    /// Astrazione degli input di gioco per supportare tastiera/gamepad e test automatici.
    /// </summary>
    public interface IPlayerInput
    {
        float MoveX { get; }
        float MoveY { get; }
        bool JumpPressed { get; }
        bool JumpHeld { get; }
        bool DashPressed { get; }
        bool AttackPressed { get; }
        bool AttackHeld { get; }
        bool AttackReleased { get; }
        bool RootPressed { get; }
        bool InteractPressed { get; }
        bool RestartPressed { get; }
        bool VillagePressed { get; }
        bool PausePressed { get; }
    }

    /// <summary>
    /// Lettore input basato esclusivamente su Unity Input System (UnityEngine.InputSystem).
    /// </summary>
    public sealed class PlayerInputReader : MonoBehaviour, IPlayerInput
    {
        public float MoveX { get; private set; }
        public float MoveY { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool DashPressed { get; private set; }
        public bool AttackPressed { get; private set; }
        public bool AttackHeld { get; private set; }
        public bool AttackReleased { get; private set; }
        public bool RootPressed { get; private set; }
        public bool InteractPressed { get; private set; }
        public bool RestartPressed { get; private set; }
        public bool VillagePressed { get; private set; }
        public bool PausePressed { get; private set; }

        private bool _wasAttackHeld;

        private void Update()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            var gp = Gamepad.current;

            float mx = 0f;
            float my = 0f;

            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) mx -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) mx += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) my -= 1f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) my += 1f;
            }

            if (gp != null)
            {
                Vector2 stick = gp.leftStick.ReadValue();
                if (Mathf.Abs(stick.x) > 0.2f) mx = Mathf.Clamp(mx + stick.x, -1f, 1f);
                if (Mathf.Abs(stick.y) > 0.2f) my = Mathf.Clamp(my + stick.y, -1f, 1f);
            }

            MoveX = Mathf.Clamp(mx, -1f, 1f);
            MoveY = Mathf.Clamp(my, -1f, 1f);

            JumpPressed =
                (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)) ||
                (gp != null && gp.buttonSouth.wasPressedThisFrame);

            JumpHeld =
                (kb != null && (kb.spaceKey.isPressed || kb.wKey.isPressed || kb.upArrowKey.isPressed)) ||
                (gp != null && gp.buttonSouth.isPressed);

            DashPressed =
                (kb != null && (kb.leftShiftKey.wasPressedThisFrame || kb.kKey.wasPressedThisFrame || kb.lKey.wasPressedThisFrame)) ||
                (mouse != null && mouse.rightButton.wasPressedThisFrame) ||
                (gp != null && gp.buttonEast.wasPressedThisFrame);

            bool atkPressed =
                (kb != null && (kb.jKey.wasPressedThisFrame || kb.zKey.wasPressedThisFrame)) ||
                (mouse != null && mouse.leftButton.wasPressedThisFrame) ||
                (gp != null && gp.buttonWest.wasPressedThisFrame);

            bool atkHeld =
                (kb != null && (kb.jKey.isPressed || kb.zKey.isPressed)) ||
                (mouse != null && mouse.leftButton.isPressed) ||
                (gp != null && gp.buttonWest.isPressed);

            bool atkReleased =
                (kb != null && (kb.jKey.wasReleasedThisFrame || kb.zKey.wasReleasedThisFrame)) ||
                (mouse != null && mouse.leftButton.wasReleasedThisFrame) ||
                (gp != null && gp.buttonWest.wasReleasedThisFrame) ||
                (_wasAttackHeld && !atkHeld);

            AttackPressed = atkPressed;
            AttackHeld = atkHeld;
            AttackReleased = atkReleased;
            _wasAttackHeld = atkHeld;

            RootPressed =
                (kb != null && (kb.qKey.wasPressedThisFrame || kb.fKey.wasPressedThisFrame)) ||
                (mouse != null && mouse.middleButton.wasPressedThisFrame) ||
                (gp != null && (gp.rightShoulder.wasPressedThisFrame || gp.buttonNorth.wasPressedThisFrame));

            InteractPressed =
                (kb != null && kb.eKey.wasPressedThisFrame) ||
                (gp != null && gp.dpad.up.wasPressedThisFrame);

            RestartPressed = kb != null && kb.rKey.wasPressedThisFrame;
            VillagePressed = kb != null && kb.vKey.wasPressedThisFrame;
            PausePressed =
                (kb != null && kb.escapeKey.wasPressedThisFrame) ||
                (gp != null && gp.startButton.wasPressedThisFrame);
        }
    }
}
