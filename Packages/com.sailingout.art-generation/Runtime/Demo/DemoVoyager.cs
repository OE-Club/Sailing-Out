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
        [SerializeField, Min(0f)] private float maxLeanAngle = 12f;
        [SerializeField, Min(0f)] private float leanSpeed = 90f;
        [SerializeField, Min(0f)] private float buoySlowdownDuration = 0.5f;
        [SerializeField, Range(0f, 1f)] private float buoySlowdownMultiplier = 0.5f;
        [SerializeField, Min(0f)] private float buoyPushSpeed = 8f;
        [SerializeField, Min(0f)] private float buoyPushDuration = 0.25f;

        private Rigidbody2D body;
        private SpriteRenderer boatSprite;
        private Vector2 voyageStart;
        private float voyageStartRotation;
        private RigidbodyConstraints2D originalConstraints;
        private float horizontalInput;
        private float buoySlowdownRemaining;
        private float buoyPushRemaining;
        private int buoyPushDirection;
        private float health;
        private SpawnManager manager;
        private bool guided = true;
        private bool leftHeld;
        private bool rightHeld;

        public bool Guided => guided;
        public const float MaximumHealth = 3f;
        public const float CollisionDamage = 0.5f;
        public float Health => health;
        public bool IsDead => health <= 0f;
        public int BuoyPushDirection => buoyPushDirection;
        public float DistanceTravelled => Mathf.Max(0f,
            (body.position.y - voyageStart.y) * (manager != null ? manager.TravelDirectionSign : 1f));

        public void ResetVoyage()
        {
            body.position = voyageStart;
            body.rotation = voyageStartRotation;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            buoySlowdownRemaining = 0f;
            buoyPushRemaining = 0f;
            buoyPushDirection = 0;
            horizontalInput = 0f;
            leftHeld = false;
            rightHeld = false;
            health = MaximumHealth;
            Physics2D.SyncTransforms();
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            voyageStart = body.position;
            boatSprite = GetComponent<SpriteRenderer>();
            voyageStartRotation = body.rotation;
            originalConstraints = body.constraints;
            health = MaximumHealth;
            body.constraints &= ~RigidbodyConstraints2D.FreezeRotation;
            manager = FindAnyObjectByType<SpawnManager>();
            if (boatSprite != null && manager != null)
                boatSprite.flipY = manager.TravelDirectionSign < 0f;
        }

        private void OnGUI()
        {
            Event input = Event.current;
            if (input.type != EventType.KeyDown && input.type != EventType.KeyUp)
                return;
            if (IsDead)
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

        private void OnDestroy()
        {
            if (body != null)
                body.constraints = originalConstraints;
        }

        private void FixedUpdate()
        {
            if (IsDead)
            {
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
                return;
            }

            Vector2 position = body.position;
            position.x = Mathf.Clamp(position.x + horizontalInput * horizontalSpeed * Time.fixedDeltaTime,
                -horizontalLimit, horizontalLimit);
            float direction = manager != null ? manager.TravelDirectionSign : 1f;
            float speedMultiplier = buoySlowdownRemaining > 0f
                ? Mathf.Clamp01(buoySlowdownMultiplier)
                : 1f;
            position.y += direction * forwardSpeed * speedMultiplier * Time.fixedDeltaTime;
            if (guided && manager != null)
                position.x = Mathf.MoveTowards(position.x, manager.GetSafeLaneCenter(position.y),
                    horizontalSpeed * Time.fixedDeltaTime);
            body.MovePosition(position);

            if (buoySlowdownRemaining > 0f)
                buoySlowdownRemaining = Mathf.Max(0f, buoySlowdownRemaining - Time.fixedDeltaTime);
            if (buoyPushRemaining > 0f)
            {
                position.x = Mathf.Clamp(position.x + buoyPushDirection * buoyPushSpeed * Time.fixedDeltaTime,
                    -horizontalLimit, horizontalLimit);
                body.MovePosition(position);
                buoyPushRemaining = Mathf.Max(0f, buoyPushRemaining - Time.fixedDeltaTime);
            }

            float bowDirection = boatSprite != null && boatSprite.flipY ? -1f : 1f;
            float targetRotation = voyageStartRotation - horizontalInput * bowDirection * maxLeanAngle;
            float rotation = Mathf.MoveTowardsAngle(body.rotation, targetRotation,
                leanSpeed * Time.fixedDeltaTime);
            body.MoveRotation(rotation);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            SpawnableIdentity identity = other.GetComponentInParent<SpawnableIdentity>();
            if (identity == null || identity.Definition == null ||
                identity.Definition.Category != SpawnCategory.Buoy)
                return;

            buoySlowdownRemaining = Mathf.Max(0f, buoySlowdownDuration);
            buoyPushRemaining = Mathf.Max(0f, buoyPushDuration);
            buoyPushDirection = Random.value < 0.5f ? -1 : 1;
            TakeCollisionDamage();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            SpawnableIdentity identity = collision.collider.GetComponentInParent<SpawnableIdentity>();
            if (identity == null || identity.Definition == null ||
                identity.Definition.Category != SpawnCategory.Reef)
                return;

            TakeCollisionDamage();
        }

        private void TakeCollisionDamage()
        {
            health = Mathf.Max(0f, health - CollisionDamage);
            if (IsDead)
            {
                horizontalInput = 0f;
                leftHeld = false;
                rightHeld = false;
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
            }
        }
    }
}
