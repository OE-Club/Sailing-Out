using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SailingOut.Tests
{
    public sealed class SpawnManagerTests
    {
        private readonly List<Object> createdObjects = new List<Object>();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                if (createdObjects[i] != null)
                    Object.Destroy(createdObjects[i]);
            }
            yield return null;
            createdObjects.Clear();
        }

        [UnityTest]
        public IEnumerator SameSeedAndStartPositionCreateSameSpawnLayout()
        {
            SpawnManager first = CreateManager(9876, out GameObject firstRoot);
            SpawnManager second = CreateManager(9876, out GameObject secondRoot);
            SpawnManager third = CreateManager(9877, out GameObject thirdRoot);

            first.Initialize(9876);
            second.Initialize(9876);
            third.Initialize(9877);
            yield return null;

            List<Vector2> firstPositions = ActivePositions(firstRoot.transform);
            List<Vector2> secondPositions = ActivePositions(secondRoot.transform);
            Assert.AreEqual(firstPositions.Count, secondPositions.Count);
            for (int i = 0; i < firstPositions.Count; i++)
            {
                Assert.That(Vector2.Distance(firstPositions[i], secondPositions[i]), Is.LessThan(0.0001f));
                for (int j = i + 1; j < firstPositions.Count; j++)
                    Assert.That(Vector2.Distance(firstPositions[i], firstPositions[j]), Is.GreaterThanOrEqualTo(1.1f - 0.0001f));
            }
            List<Vector2> thirdPositions = ActivePositions(thirdRoot.transform);
            bool changed = firstPositions.Count != thirdPositions.Count;
            for (int i = 0; !changed && i < firstPositions.Count; i++)
                changed = Vector2.Distance(firstPositions[i], thirdPositions[i]) > 0.0001f;
            Assert.IsTrue(changed, "A different run seed should change the generated layout.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator DownwardTravelGeneratesAheadAndRecyclesBehind()
        {
            SpawnManager manager = CreateManager(4102, out GameObject root);
            GenerationProfile profile = GetProfile(manager);
            Set(profile, "travelDownward", true);

            manager.Initialize(4102);
            float playerY = -40f;
            manager.RecycleBehind(playerY);
            manager.EnsureAhead(playerY, 0f);

            Assert.Less(manager.GeneratedThroughY, playerY - 25f,
                "The generated boundary should stay ahead in the downward travel direction.");
            Assert.That(ActivePositions(root.transform).Exists(position => position.y < playerY), Is.True,
                "The manager should place active objects in front of the descending player.");

            manager.RecycleBehind(playerY);
            foreach (Vector2 position in ActivePositions(root.transform))
                Assert.That(position.y, Is.LessThanOrEqualTo(playerY + 15f + 0.0001f),
                    "Objects farther behind than the recycle distance should be returned to the pool.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator ActiveObjectsStayWithinConfiguredLimitAcrossLongJumps()
        {
            SpawnManager manager = CreateManager(64, out _);
            GenerationProfile profile = GetProfile(manager);
            Set(profile, "maxActiveObjects", 3);

            manager.Initialize(64);
            Assert.AreEqual(3, manager.ActiveObjectCount);

            manager.RecycleBehind(1000f);
            manager.EnsureAhead(1000f, 0f);
            Assert.AreEqual(3, manager.ActiveObjectCount);

            yield return null;
        }

        [UnityTest]
        public IEnumerator PooledObjectCountStaysWithinConfiguredLimit()
        {
            SpawnManager manager = CreateManager(721, out GameObject root);
            GenerationProfile profile = GetProfile(manager);
            Set(profile, "maxPooledObjects", 2);

            manager.Initialize(721);
            Assert.That(manager.ActiveObjectCount, Is.GreaterThan(2));
            manager.RecycleBehind(1000f);
            Assert.AreEqual(2, manager.PooledObjectCount);
            Assert.AreEqual(2, manager.ManagedObjectCount);

            for (int i = 0; i < 5; i++)
            {
                manager.EnsureAhead(1000f + i * 100f, 0f);
                Assert.That(manager.ActiveObjectCount, Is.GreaterThan(0));
                Assert.That(manager.ManagedObjectCount, Is.LessThanOrEqualTo(8));
                manager.RecycleBehind(2000f + i * 100f);
                Assert.AreEqual(2, manager.PooledObjectCount);
                Assert.AreEqual(2, manager.ManagedObjectCount);
            }

            yield return null;
            Assert.AreEqual(2, root.transform.childCount,
                "Surplus instances should be destroyed instead of accumulating in the pool.");
        }

        [UnityTest]
        public IEnumerator RecycleAndClearReturnObjectsWithoutChangingLayoutForSeed()
        {
            SpawnManager manager = CreateManager(120, out GameObject root);
            manager.Initialize(120);
            yield return null;
            int initialCount = manager.ActiveObjectCount;
            Assert.That(initialCount, Is.GreaterThan(0));
            int allocatedCount = root.transform.childCount;

            manager.RecycleBehind(1000f);
            Assert.AreEqual(0, manager.ActiveObjectCount);
            manager.EnsureAhead(1000f, 0f);
            Assert.That(manager.ActiveObjectCount, Is.GreaterThan(0));
            Assert.AreEqual(allocatedCount, root.transform.childCount,
                "Recycling should reuse the existing instances instead of allocating new ones.");

            manager.ClearAndReturnToPool();
            Assert.AreEqual(0, manager.ActiveObjectCount);
            yield return null;
            Assert.AreEqual(0, manager.ActiveObjectCount, "Clear must stop automatic maintenance until Initialize.");
        }

        [UnityTest]
        public IEnumerator TenMinutesAtSixtyStepsPerSecondKeepPoolBoundedAndRouteOpen()
        {
            SpawnManager manager = CreateManager(314159, out GameObject root);
            manager.Initialize(314159);
            int peak = 0;
            int allocationsAfterWarmup = 0;
            float previousEnd = float.NegativeInfinity;
            for (int tick = 0; tick <= 600 * 60; tick++)
            {
                float y = tick * (5f / 60f);
                manager.RecycleBehind(y);
                manager.EnsureAhead(y, 0f);
                peak = Mathf.Max(peak, manager.ManagedObjectCount);
                if (tick == 120 * 60) allocationsAfterWarmup = root.transform.childCount;
                if (manager.GeneratedThroughY == previousEnd) continue;
                previousEnd = manager.GeneratedThroughY;
                List<Vector2> positions = ActivePositions(root.transform);
                for (int i = 0; i < positions.Count; i++)
                {
                    for (int j = i+1; j < positions.Count; j++)
                        Assert.That(Vector2.Distance(positions[i], positions[j]), Is.GreaterThanOrEqualTo(1.1f-0.0001f));
                    for (float dy = -0.925f; dy <= 0.925f; dy += 0.125f)
                        Assert.That(Mathf.Abs(positions[i].x - manager.GetSafeLaneCenter(positions[i].y + dy)),
                            Is.GreaterThanOrEqualTo(manager.SafeLaneWidth * 0.5f + 0.425f - 0.0001f));
                }
                Assert.That(manager.GeneratedThroughY, Is.GreaterThanOrEqualTo(y + 25f));
            }
            Assert.That(peak, Is.LessThanOrEqualTo(8));
            Assert.AreEqual(allocationsAfterWarmup, root.transform.childCount, "Allocations must plateau after warmup.");
            Debug.Log($"Simulated 600 seconds at 60 steps/s: peak managed {peak}; final allocated {root.transform.childCount}.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator RecyclingCadenceAndPlayerSteeringDoNotChangeSegmentLayouts()
        {
            SpawnManager slow = CreateManager(954, out GameObject slowRoot);
            SpawnManager fast = CreateManager(954, out GameObject fastRoot);
            slow.Initialize(954); fast.Initialize(954);
            for (float y = 0; y <= 160; y += 0.5f)
            {
                slow.RecycleBehind(y); slow.EnsureAhead(y, Mathf.Sin(y) * 3f);
            }
            fast.RecycleBehind(160f); fast.EnsureAhead(160f, 7f);
            // Both have fully generated segment 8 even though one teleported.
            List<Vector2> a = ActivePositions(slowRoot.transform).FindAll(p => p.y >= 160f && p.y < 180f);
            List<Vector2> b = ActivePositions(fastRoot.transform).FindAll(p => p.y >= 160f && p.y < 180f);
            Assert.That(a.Count, Is.GreaterThan(0));
            CollectionAssert.AreEquivalent(a,b);
            yield return null;
        }

        private SpawnManager CreateManager(int seed, out GameObject root)
        {
            GenerationProfile profile = ScriptableObject.CreateInstance<GenerationProfile>();
            createdObjects.Add(profile);
            Set(profile, "segmentLengthScreens", 2f);
            Set(profile, "generateAheadScreens", 2.5f);
            Set(profile, "recycleBehindScreens", 1.5f);
            Set(profile, "horizontalEdgeBuffer", 0.5f);
            Set(profile, "safeLaneMinimumWidth", 1.5f);
            Set(profile, "safeLaneSideBuffer", 0.25f);
            Set(profile, "maximumLaneOffset", 2f);
            Set(profile, "minimumObjectSeparation", 0.2f);
            Set(profile, "placementAttempts", 40);
            Set(profile, "maxActiveObjects", 256);
            Set(profile, "maxPooledObjects", 256);
            Set(profile, "seed", seed);

            GameObject prefab = new GameObject("ReefPrefab");
            createdObjects.Add(prefab);
            List<SpawnableDefinition> definitions = new List<SpawnableDefinition>();
            SpawnableDefinition definition = ScriptableObject.CreateInstance<SpawnableDefinition>();
            createdObjects.Add(definition);
            Set(definition, "category", SpawnCategory.Reef);
            Set(definition, "prefab", prefab);
            Set(definition, "variants", new List<GameObject> { prefab });
            Set(definition, "weight", 1f);
            Set(definition, "minimumDistance", 0.25f);
            Set(definition, "clearanceRadius", 0.3f);
            Set(definition, "blocksPath", true);
            definitions.Add(definition);
            Set(profile, "definitions", definitions);

            GenerationStage stage = new GenerationStage();
            Set(stage, "startDistance", 0f);
            Set(stage, "obstacleDensityMultiplier", 1f);
            Set(stage, "counts", new List<CategorySpawnRange>
            {
                MakeRange(SpawnCategory.Reef, 2, 2)
            });
            Set(profile, "stages", new List<GenerationStage> { stage });

            GameObject cameraRoot = new GameObject("TestCamera", typeof(Camera));
            createdObjects.Add(cameraRoot);
            Camera camera = cameraRoot.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.aspect = 16f / 9f;

            GameObject player = new GameObject("Player", typeof(BoxCollider2D));
            createdObjects.Add(player);
            Collider2D collider = player.GetComponent<Collider2D>();
            collider.transform.position = Vector3.zero;

            root = new GameObject("TestSpawnManager");
            createdObjects.Add(root);
            SpawnManager manager = root.AddComponent<SpawnManager>();
            Set(manager, "profile", profile);
            Set(manager, "worldCamera", camera);
            Set(manager, "playerCollider", collider);
            Set(manager, "initializeOnStart", false);

            return manager;
        }

        private static CategorySpawnRange MakeRange(SpawnCategory category, int minimum, int maximum)
        {
            CategorySpawnRange range = new CategorySpawnRange();
            Set(range, "category", category);
            Set(range, "minimum", minimum);
            Set(range, "maximum", maximum);
            return range;
        }

        private static List<Vector2> ActivePositions(Transform root)
        {
            List<Vector2> positions = new List<Vector2>();
            foreach (Transform child in root)
            {
                if (child.gameObject.activeSelf)
                    positions.Add(child.position);
            }
            return positions;
        }

        private static void Set(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected serialized field {fieldName}");
            field.SetValue(target, value);
        }

        private static GenerationProfile GetProfile(SpawnManager manager)
        {
            FieldInfo field = typeof(SpawnManager).GetField("profile",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "Expected SpawnManager profile field");
            return (GenerationProfile)field.GetValue(manager);
        }
    }
}
