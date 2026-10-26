using System.Collections.Generic;
using UnityEngine;

namespace SailingOut
{
    [DisallowMultipleComponent]
    public sealed class SpawnManager : MonoBehaviour
    {
        [SerializeField] private GenerationProfile profile;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Collider2D playerCollider;
        [SerializeField] private bool initializeOnStart = true;
        [SerializeField] private bool maintainAutomatically = true;

        private readonly Dictionary<GameObject, Stack<GameObject>> pools = new Dictionary<GameObject, Stack<GameObject>>();
        private readonly List<SpawnedEntry> active = new List<SpawnedEntry>();
        private int currentSeed;
        private int pooledObjectCount;
        private bool initialized;
        private float runOriginY;
        private float runOriginX;
        private float runHalfWidth;
        private float segmentLength;
        private float safeLaneWidth;
        private float maxLaneOffset;
        private float boatHalfLength;
        private float boundarySeparation;
        private long highestGeneratedSegment = long.MinValue;

        private static readonly SpawnCategory[] Categories =
        {
            SpawnCategory.Reef, SpawnCategory.Supply, SpawnCategory.Buoy,
            SpawnCategory.Seabird, SpawnCategory.Decoration
        };

        public int CurrentSeed => currentSeed;
        public int ActiveObjectCount => active.Count;
        public int PooledObjectCount => pooledObjectCount;
        public int ManagedObjectCount => active.Count + pooledObjectCount;
        public GenerationProfile Profile => profile;
        public float SafeLaneWidth => safeLaneWidth;
        public float GeneratedThroughY => initialized && highestGeneratedSegment != long.MinValue
            ? (highestGeneratedSegment + 1) * segmentLength : float.NegativeInfinity;

        /// <summary>Assign once before Initialize. Camera size, boat bounds and world width are frozen per run.</summary>
        public void Configure(GenerationProfile configuration, Camera camera, Collider2D boat, bool automatic = true)
        {
            ClearAndReturnToPool();
            initialized = false;
            profile = configuration;
            worldCamera = camera;
            playerCollider = boat;
            initializeOnStart = automatic;
            maintainAutomatically = automatic;
        }

        public float GetSafeLaneCenter(float worldY) => GenerationMath.LaneCenterAt(currentSeed,
            worldY, segmentLength, runOriginX, maxLaneOffset);

        private void Start()
        {
            if (initializeOnStart && profile != null)
                Initialize(profile.RandomizeSeedOnStart ? Random.Range(0, int.MaxValue) : profile.Seed);
        }

        private void Update()
        {
            if (!initialized || !maintainAutomatically || playerCollider == null)
                return;

            RecycleBehind(playerCollider.bounds.center.y);
            EnsureAhead(playerCollider.bounds.center.y, playerCollider.bounds.center.x);
        }

        public void Initialize(int seed)
        {
            if (profile == null)
            {
                Debug.LogError("SpawnManager requires a GenerationProfile.", this);
                return;
            }

            if (worldCamera == null)
                worldCamera = Camera.main;

            if (worldCamera == null || !worldCamera.orthographic)
            {
                Debug.LogError("SpawnManager requires an orthographic world camera.", this);
                return;
            }

            if (initialized)
                ClearAndReturnToPool();

            currentSeed = seed;
            runOriginY = playerCollider != null ? playerCollider.bounds.center.y : 0f;
            runOriginX = playerCollider != null ? playerCollider.bounds.center.x : worldCamera.transform.position.x;
            runHalfWidth = worldCamera.orthographicSize * worldCamera.aspect;
            segmentLength = profile.GetSegmentLength(worldCamera);
            float playerWidth = playerCollider != null ? playerCollider.bounds.size.x : 1f;
            boatHalfLength = playerCollider != null ? playerCollider.bounds.extents.y : 1f;
            safeLaneWidth = Mathf.Max(profile.SafeLaneMinimumWidth, playerWidth + profile.SafeLaneSideBuffer * 2f);
            maxLaneOffset = Mathf.Min(profile.MaximumLaneOffset,
                Mathf.Max(0f, runHalfWidth - safeLaneWidth * 0.5f - profile.HorizontalEdgeBuffer));
            boundarySeparation = profile.MinimumObjectSeparation;
            foreach (SpawnableDefinition definition in profile.Definitions)
                if (definition != null)
                    boundarySeparation = Mathf.Max(boundarySeparation, definition.MinimumDistance);
            initialized = true;
            EnsureAhead(playerCollider != null ? playerCollider.bounds.center.y : 0f,
                playerCollider != null ? playerCollider.bounds.center.x : 0f);
        }

        public void EnsureAhead(float playerY, float playerX)
        {
            if (!initialized || profile == null || worldCamera == null)
                return;

            float aheadY = Mathf.Max(playerY + profile.GetAheadDistance(worldCamera),
                worldCamera.transform.position.y + worldCamera.orthographicSize * 2f);
            long firstSegment = Mathf.FloorToInt((playerY - profile.GetRecycleDistance(worldCamera)) / segmentLength);
            long lastSegment = Mathf.FloorToInt(aheadY / segmentLength);

            if (highestGeneratedSegment == long.MinValue)
                highestGeneratedSegment = firstSegment - 1;
            else if (firstSegment > highestGeneratedSegment + 1)
                // A teleport, scene restore, or a very long frame must not make
                // the manager backfill every skipped segment in one burst.
                highestGeneratedSegment = firstSegment - 1;

            long start = System.Math.Max(firstSegment, highestGeneratedSegment + 1);
            for (long segment = start; segment <= lastSegment; segment++)
            {
                GenerateSegment(segment);
                highestGeneratedSegment = segment;
            }
        }

        public void RecycleBehind(float playerY)
        {
            if (!initialized || profile == null || worldCamera == null)
                return;

            float recycleY = playerY - profile.GetRecycleDistance(worldCamera);
            for (int i = active.Count - 1; i >= 0; i--)
            {
                if (active[i].Position.y >= recycleY)
                    continue;

                ReturnToPool(active[i]);
                active.RemoveAt(i);
            }
        }

        public void ClearAndReturnToPool()
        {
            for (int i = active.Count - 1; i >= 0; i--)
                ReturnToPool(active[i]);

            active.Clear();
            highestGeneratedSegment = long.MinValue;
            initialized = false;
        }

        private void GenerateSegment(long segment)
        {
            float yStart = segment * segmentLength;
            float yEnd = yStart + segmentLength;
            float visibleCenterX = runOriginX;
            float viewHalfWidth = runHalfWidth;
            float distance = Mathf.Max(0f, yStart - runOriginY);
            GenerationStage stage = profile.GetStage(distance);
            SeededRandom random = new SeededRandom(SeededRandom.SegmentSeed(currentSeed, segment));
            for (int c = 0; c < Categories.Length; c++)
            {
                if (active.Count >= profile.MaxActiveObjects)
                    break;

                SpawnCategory category = Categories[c];
                int count = profile.GetCount(stage, category, ref random, distance);
                for (int i = 0; i < count; i++)
                {
                    if (active.Count >= profile.MaxActiveObjects)
                        break;

                    SpawnableDefinition definition = profile.ChooseDefinition(category, ref random);
                    if (definition == null)
                        continue;

                    GameObject prefab = definition.ChoosePrefab(ref random);
                    if (prefab == null)
                        continue;

                    if (TryPlace(category, definition, yStart, yEnd, visibleCenterX,
                        runOriginX,
                        viewHalfWidth, safeLaneWidth, maxLaneOffset, segmentLength, ref random,
                        out Vector2 position))
                    {
                        GameObject instance = GetFromPool(prefab);
                        instance.transform.SetPositionAndRotation(position, Quaternion.identity);
                        instance.transform.localScale = prefab.transform.localScale;
                        instance.SetActive(true);
                        NotifySpawned(instance);
                        active.Add(new SpawnedEntry(prefab, instance, position, definition.ClearanceRadius,
                            definition.MinimumDistance));
                    }
                }
            }
        }

        private bool TryPlace(
            SpawnCategory category,
            SpawnableDefinition definition,
            float yStart,
            float yEnd,
            float visibleCenterX,
            float laneOriginX,
            float viewHalfWidth,
            float safeLaneWidth,
            float maxLaneOffset,
            float segmentLength,
            ref SeededRandom random,
            out Vector2 position)
        {
            float radius = definition.ClearanceRadius;
            float minX = visibleCenterX - viewHalfWidth + profile.HorizontalEdgeBuffer + radius;
            float maxX = visibleCenterX + viewHalfWidth - profile.HorizontalEdgeBuffer - radius;
            // Reserving half the largest spacing at each boundary makes adjacent
            // segments independent of the recycling cadence and teleport history.
            float minY = yStart + radius + boundarySeparation * 0.5f;
            float maxY = yEnd - radius - boundarySeparation * 0.5f;

            if (maxX <= minX || maxY <= minY)
            {
                position = default;
                return false;
            }

            for (int attempt = 0; attempt < profile.PlacementAttempts; attempt++)
            {
                float y = random.Range(minY, maxY);
                float laneX = GenerationMath.LaneCenterAt(currentSeed, y, segmentLength, laneOriginX, maxLaneOffset);
                float x;

                if (definition.BlocksPath)
                {
                    Vector2 safeBounds = GenerationMath.LaneBoundsForCircle(currentSeed, y,
                        segmentLength, laneOriginX, maxLaneOffset, safeLaneWidth, radius + boatHalfLength);
                    float leftWidth = Mathf.Max(0f, safeBounds.x - minX);
                    float rightStart = Mathf.Min(maxX, safeBounds.y);
                    float rightWidth = Mathf.Max(0f, maxX - rightStart);
                    if (leftWidth + rightWidth <= 0.01f)
                        continue;

                    bool chooseRight = rightWidth > 0f && (leftWidth <= 0f || random.Range(0f, leftWidth + rightWidth) >= leftWidth);
                    x = chooseRight ? random.Range(rightStart, maxX) : random.Range(minX, safeBounds.x);
                    if (!GenerationMath.IsOutsideSafeLane(currentSeed, x, y, segmentLength, laneOriginX,
                        maxLaneOffset, safeLaneWidth, radius + boatHalfLength))
                        continue;
                }
                else if (category == SpawnCategory.Supply)
                {
                    float nearLaneRange = Mathf.Max(0f, safeLaneWidth * 0.5f - radius);
                    x = Mathf.Clamp(laneX + random.Range(-nearLaneRange, nearLaneRange), minX, maxX);
                }
                else
                {
                    x = random.Range(minX, maxX);
                }

                Vector2 candidate = new Vector2(x, y);
                if (!HasClearance(candidate, radius, definition.MinimumDistance))
                    continue;

                position = candidate;
                return true;
            }

            position = default;
            return false;
        }

        private bool HasClearance(Vector2 candidate, float radius, float minimumDistance)
        {
            for (int i = 0; i < active.Count; i++)
            {
                float required = radius + active[i].Radius + Mathf.Max(profile.MinimumObjectSeparation,
                    Mathf.Max(minimumDistance, active[i].MinimumDistance));
                if ((candidate - active[i].Position).sqrMagnitude < required * required)
                    return false;
            }

            return true;
        }

        private GameObject GetFromPool(GameObject prefab)
        {
            if (pools.TryGetValue(prefab, out Stack<GameObject> pool) && pool.Count > 0)
            {
                pooledObjectCount--;
                return pool.Pop();
            }

            GameObject instance = Instantiate(prefab, transform);
            instance.name = prefab.name;
            return instance;
        }

        private void ReturnToPool(SpawnedEntry entry)
        {
            if (entry.Instance == null)
                return;
            NotifyDespawned(entry.Instance);
            entry.Instance.SetActive(false);
            entry.Instance.transform.SetParent(transform, false);

            if (profile == null || pooledObjectCount >= profile.MaxPooledObjects)
            {
                Destroy(entry.Instance);
                return;
            }

            if (!pools.TryGetValue(entry.Prefab, out Stack<GameObject> pool))
            {
                pool = new Stack<GameObject>();
                pools.Add(entry.Prefab, pool);
            }

            pool.Push(entry.Instance);
            pooledObjectCount++;
        }

        private static void NotifySpawned(GameObject instance)
        {
            MonoBehaviour[] behaviours = instance.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IPooledSpawnable pooled)
                    pooled.OnSpawned();
            }
        }

        private static void NotifyDespawned(GameObject instance)
        {
            MonoBehaviour[] behaviours = instance.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IPooledSpawnable pooled)
                    pooled.OnDespawned();
            }
        }

        private struct SpawnedEntry
        {
            public readonly GameObject Prefab;
            public readonly GameObject Instance;
            public readonly Vector2 Position;
            public readonly float Radius;
            public readonly float MinimumDistance;

            public SpawnedEntry(GameObject prefab, GameObject instance, Vector2 position, float radius,
                float minimumDistance)
            {
                Prefab = prefab;
                Instance = instance;
                Position = position;
                Radius = radius;
                MinimumDistance = minimumDistance;
            }
        }

    }
}
