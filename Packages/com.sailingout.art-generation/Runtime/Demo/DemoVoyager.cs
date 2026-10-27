using UnityEngine;

namespace SailingOut
{
    [RequireComponent(typeof(Rigidbody2D))]
    [DisallowMultipleComponent]
    public sealed class DemoVoyager : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float forwardSpeed = 5f;
        [SerializeField, Min(0f)] private float horizontalSpeed = 6f;
        [SerializeField, Min(0f)] private float horizontalLimit = 7f;

        private Rigidbody2D body;
        private SpriteRenderer boatSprite;
        private Vector2 voyageStart;
        private float horizontalInput;
        private SpawnManager manager;
        private bool guided = true;
        private bool leftHeld;
        private bool rightHeld;

        public bool Guided => guided;
        public float DistanceTravelled => Mathf.Max(0f,
            (body.position.y - voyageStart.y) * (manager != null ? manager.TravelDirectionSign : 1f));

        public void ResetVoyage()
        {
            body.position = voyageStart;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            Physics2D.SyncTransforms();
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            voyageStart = body.position;
            manager = FindAnyObjectByType<SpawnManager>();
            boatSprite = GetComponent<SpriteRenderer>();
            if (boatSprite != null && manager != null)
                boatSprite.flipY = manager.TravelDirectionSign < 0f;
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
                horizontalInput = 0f;
            }
        }

        private void FixedUpdate()
        {
            Vector2 position = body.position;
            position.x = Mathf.Clamp(position.x + horizontalInput * horizontalSpeed * Time.fixedDeltaTime,
                -horizontalLimit, horizontalLimit);
            float direction = manager != null ? manager.TravelDirectionSign : 1f;
            position.y += direction * forwardSpeed * Time.fixedDeltaTime;
            if (guided && manager != null)
                position.x = Mathf.MoveTowards(position.x, manager.GetSafeLaneCenter(position.y),
                    horizontalSpeed * Time.fixedDeltaTime);
            body.MovePosition(position);
        }
    }
}
