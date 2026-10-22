using UnityEngine;
using UnityEngine.InputSystem;

namespace SailingOut.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class SailboatMovement : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float moveSpeed = 5f;

        private Rigidbody2D body;
        private Vector2 moveInput;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
        }

        private void Update()
        {
            moveInput = ReadMovementInput();
        }

        private void FixedUpdate()
        {
            body.linearVelocity = moveInput * moveSpeed;
        }

        private static Vector2 ReadMovementInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return Vector2.zero;
            }

            Vector2 input = Vector2.zero;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                input.x -= 1f;
            }

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                input.x += 1f;
            }

            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            {
                input.y -= 1f;
            }

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            {
                input.y += 1f;
            }

            return Vector2.ClampMagnitude(input, 1f);
        }
    }
}
