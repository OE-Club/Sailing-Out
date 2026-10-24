using UnityEngine;

namespace SailingOut
{
    [RequireComponent(typeof(Rigidbody2D))]
    [DisallowMultipleComponent]
    public sealed class DemoVoyager : MonoBehaviour
    {
        private const int MaxBoostsPerVoyage = 3;

        [SerializeField, Min(0f)] private float forwardSpeed = 5f;
        [SerializeField, Min(0f)] private float horizontalSpeed = 6f;
        [SerializeField, Min(0f)] private float horizontalLimit = 7f;
        [SerializeField, Min(1f)] private float boostDuration = 3f;
        [SerializeField, Min(1f)] private float boostMultiplier = 2f;

        private Rigidbody2D body;
        private float horizontalInput;
        private SpawnManager manager;
        private bool guided = true;
        private bool leftHeld;
        private bool rightHeld;
        private bool boostHeld;
        private int boostsRemaining = MaxBoostsPerVoyage;
        private float boostTimeRemaining;

        public bool Guided => guided;
        public int BoostsRemaining => boostsRemaining;
        public bool IsBoosting => boostTimeRemaining > 0f;

        public void ResetVoyage()
        {
            body.position = Vector2.zero;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            boostsRemaining = MaxBoostsPerVoyage;
            boostTimeRemaining = 0f;
        }

        public bool TryBoost()
        {
            if (IsBoosting || boostsRemaining <= 0)
                return false;

            boostsRemaining--;
            boostTimeRemaining = boostDuration;
            return true;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            manager = FindAnyObjectByType<SpawnManager>();
        }

        private void OnGUI()
        {
            Event input = Event.current;
            if (input.type != EventType.KeyDown && input.type != EventType.KeyUp)
                return;
            bool pressed = input.type == EventType.KeyDown;
            if (input.keyCode == KeyCode.A || input.keyCode == KeyCode.LeftArrow)
                leftHeld = pressed;
            if (input.keyCode == KeyCode.D || input.keyCode == KeyCode.RightArrow)
                rightHeld = pressed;
            if (input.keyCode == KeyCode.W)
            {
                if (pressed && !boostHeld)
                    TryBoost();
                boostHeld = pressed;
            }
            if (input.keyCode == KeyCode.Space && pressed)
                guided = !guided;
            horizontalInput = (rightHeld ? 1f : 0f) - (leftHeld ? 1f : 0f);
            if (horizontalInput != 0f)
                guided = false;
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
            {
                leftHeld = rightHeld = false;
                boostHeld = false;
                horizontalInput = 0f;
            }
        }

        private void Update()
        {
            if (IsBoosting)
                boostTimeRemaining = Mathf.Max(0f, boostTimeRemaining - Time.deltaTime);
        }

        private void FixedUpdate()
        {
            Vector2 position = body.position;
            position.x = Mathf.Clamp(position.x + horizontalInput * horizontalSpeed * Time.fixedDeltaTime,
                -horizontalLimit, horizontalLimit);
            float speed = forwardSpeed * (IsBoosting ? boostMultiplier : 1f);
            position.y += speed * Time.fixedDeltaTime;
            if (guided && manager != null)
                position.x = Mathf.MoveTowards(position.x, manager.GetSafeLaneCenter(position.y),
                    horizontalSpeed * Time.fixedDeltaTime);
            body.MovePosition(position);
        }
    }
}
