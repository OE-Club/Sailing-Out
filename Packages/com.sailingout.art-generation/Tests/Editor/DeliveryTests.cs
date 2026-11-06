using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace SailingOut.Tests
{
    public sealed class DeliveryTests
    {
        private const string Root = "Assets/SailingOut/";

        [Test]
        public void BuilderWritesDownwardDirectionForNewAndExistingProfiles()
        {
            const string temporary = "Assets/SailingOutDirectionTest";
            AssetDatabase.CreateFolder("Assets", "SailingOutDirectionTest");
            AssetDatabase.CreateFolder(temporary, "Configs");
            try
            {
                MethodInfo build = typeof(SailingOut.Editor.SailingOutAssetBuilder).GetMethod("BuildProfile",
                    BindingFlags.Static | BindingFlags.NonPublic);
                Assert.IsNotNull(build);
                object[] args = { new SpawnableDefinition[0], temporary, 314159 };
                var profile = (GenerationProfile)build.Invoke(null, args);
                Assert.IsTrue(profile.TravelDownward, "A fresh generated demo must sail downward.");
                AssertSparseSupplyStages(profile);
                AssertIrregularObstacleStages(profile);
                var settings = new SerializedObject(profile);
                settings.FindProperty("travelDownward").boolValue = false;
                settings.ApplyModifiedPropertiesWithoutUndo();
                profile = (GenerationProfile)build.Invoke(null, args);
                Assert.IsTrue(profile.TravelDownward, "Rebuilding must restore the demo direction.");
                AssertSparseSupplyStages(profile);
                AssertIrregularObstacleStages(profile);
            }
            finally { AssetDatabase.DeleteAsset(temporary); }
        }

        [Test]
        public void AllArtAndColliderRolesArePresentAndPixelImportsAreCorrect()
        {
            string[] prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { Root + "Generated/Prefabs" });
            Assert.AreEqual(18,prefabs.Length);
            foreach (string guid in prefabs)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                SpawnableIdentity identity = prefab.GetComponent<SpawnableIdentity>();
                Assert.IsNotNull(identity?.Definition, prefab.name);
                Collider2D collider = prefab.GetComponent<Collider2D>();
                if (identity.Definition.Category == SpawnCategory.Reef)
                    Assert.IsTrue(collider != null && !collider.isTrigger && identity.Definition.BlocksPath);
                else if (identity.Definition.Category == SpawnCategory.Buoy)
                    Assert.IsTrue(collider != null && collider.isTrigger &&
                        !identity.Definition.BlocksPath && !identity.Definition.Collectible);
                else if (identity.Definition.Category == SpawnCategory.Supply ||
                    identity.Definition.Category == SpawnCategory.Lightning)
                    Assert.IsTrue(collider != null && collider.isTrigger && identity.Definition.Collectible);
                else Assert.IsNull(collider);
                Sprite sprite = prefab.GetComponent<SpriteRenderer>().sprite;
                Assert.That(identity.Definition.ClearanceRadius,
                    Is.GreaterThanOrEqualTo(sprite.bounds.extents.magnitude));
            }
            foreach (string path in Directory.GetFiles(Root + "Art/Sprites", "*.png"))
            {
                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));
                Assert.AreEqual(16f,importer.spritePixelsPerUnit);
                Assert.AreEqual(FilterMode.Point,importer.filterMode);
                Assert.AreEqual(TextureImporterCompression.Uncompressed,importer.textureCompression);
                Assert.IsFalse(importer.mipmapEnabled);
            }
            Assert.AreEqual(12, AssetDatabase.FindAssets("t:AnimationClip", new[] { Root + "Generated/Animations" }).Length);
            Assert.AreEqual(18, Directory.GetFiles(Root + "Art/SpriteSheets", "*.png").Length);
        }

        [Test]
        public void OpeningStageContainsOnlyBuoysAndSeabirdsApartFromWaterAndPlayer()
        {
            GenerationProfile profile=AssetDatabase.LoadAssetAtPath<GenerationProfile>(
                Root+"Generated/Configs/DefaultGenerationProfile.asset");
            GenerationStage opening=profile.GetStage(0f);
            Assert.AreEqual(0,opening.CountRange(SpawnCategory.Reef,out _));
            Assert.AreEqual(0,opening.CountRange(SpawnCategory.Supply,out _));
            Assert.AreEqual(0,opening.CountRange(SpawnCategory.Decoration,out _));
            Assert.That(opening.CountRange(SpawnCategory.Lightning,out int maxCharges),Is.EqualTo(0));
            Assert.AreEqual(1,maxCharges);
            Assert.That(opening.CountRange(SpawnCategory.Buoy,out int maxBuoys),Is.EqualTo(0));
            Assert.AreEqual(6,maxBuoys);
            Assert.That(opening.CountRange(SpawnCategory.Seabird,out int maxBirds),Is.EqualTo(1));
            Assert.AreEqual(2,maxBirds);
        }

        [Test]
        public void DeliveredProfileLimitsSuppliesWithoutReducingLightning()
        {
            GenerationProfile profile = AssetDatabase.LoadAssetAtPath<GenerationProfile>(
                Root + "Generated/Configs/DefaultGenerationProfile.asset");
            Assert.IsNotNull(profile);
            AssertSparseSupplyStages(profile);
            AssertIrregularObstacleStages(profile);
        }

        private static void AssertIrregularObstacleStages(GenerationProfile profile)
        {
            float[] distances = { 0f, 120f, 300f, 600f };
            int[,] expected =
            {
                { 0, 6, 0, 0 },
                { 0, 6, 0, 3 },
                { 1, 7, 0, 6 },
                { 2, 8, 2, 8 }
            };
            for (int i = 0; i < distances.Length; i++)
            {
                GenerationStage stage = profile.GetStage(distances[i]);
                Assert.AreEqual(expected[i, 0], stage.CountRange(SpawnCategory.Buoy, out int maxBuoys));
                Assert.AreEqual(expected[i, 1], maxBuoys);
                Assert.AreEqual(expected[i, 2], stage.CountRange(SpawnCategory.Reef, out int maxReefs));
                Assert.AreEqual(expected[i, 3], maxReefs);
            }
        }

        private static void AssertSparseSupplyStages(GenerationProfile profile)
        {
            foreach (float distance in new[] { 120f, 300f, 600f })
            {
                GenerationStage stage = profile.GetStage(distance);
                Assert.IsNotNull(stage);
                Assert.AreEqual(0, stage.CountRange(SpawnCategory.Supply, out int maxSupplies));
                Assert.AreEqual(1, maxSupplies);
                Assert.AreEqual(1, stage.CountRange(SpawnCategory.Lightning, out int maxCharges));
                Assert.AreEqual(1, maxCharges);
            }
        }

        [UnityTest]
        public IEnumerator IncludedDemoRunsAndPooledAnimationRestartsAtFirstFrame()
        {
            EditorSceneManager.OpenScene(Root + "Generated/Demo/SailingOutGenerationDemo.unity");
            yield return new EnterPlayMode();
            yield return null;
            var manager = Object.FindAnyObjectByType<SpawnManager>();
            Assert.That(manager.ActiveObjectCount,Is.GreaterThan(0));
            var bird = Object.FindObjectsByType<SpawnableIdentity>()
                .First(x => x.Definition.Category == SpawnCategory.Seabird);
            var animator = bird.GetComponent<Animator>();
            animator.Update(0.25f);
            manager.ClearAndReturnToPool();
            manager.Initialize(314159);
            foreach (var animation in Object.FindObjectsByType<PooledAnimatorReset>())
                Assert.That(animation.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).normalizedTime,
                    Is.LessThan(0.05f));
            Camera camera = Camera.main;
            Assert.AreEqual(384,camera.targetTexture.width);
            Assert.AreEqual(FilterMode.Point,camera.targetTexture.filterMode);
            Assert.That(Object.FindObjectsByType<Camera>().Count(c => c.enabled && c.targetTexture == null),
                Is.GreaterThanOrEqualTo(1), "Standalone output requires a presentation camera targeting the display.");

            // Freeze physics and clone the profile so replay checks never edit a delivered asset.
            var boat = Object.FindAnyObjectByType<DemoVoyager>();
            boat.enabled = false;
            boat.ResetVoyage();
            var testProfile = Object.Instantiate(manager.Profile);
            manager.Configure(testProfile, camera, boat.GetComponent<Collider2D>(), false);
            float originY = boat.transform.position.y;
            var seenCategories = new HashSet<SpawnCategory>();
            bool sawOffCenterSupply = false;
            foreach (bool downward in new[] { false, true })
            {
                var settings = new SerializedObject(testProfile);
                settings.FindProperty("travelDownward").boolValue = downward;
                settings.ApplyModifiedPropertiesWithoutUndo();
                float direction = manager.TravelDirectionSign;
                for (int seed = 23; seed < 43; seed++)
                {
                    manager.Initialize(seed);
                    for (float distance = 0f; distance <= 3200f; distance += 27f)
                    {
                        float worldY = originY + direction * distance;
                        manager.RecycleBehind(worldY);
                        manager.EnsureAhead(worldY, 0f);
                        var objects = manager.GetComponentsInChildren<SpawnableIdentity>();
                        Assert.That(objects.Length, Is.GreaterThan(0),
                            $"Empty generation at seed {seed}, distance {distance}, direction {direction}");
                        for (int i = 0; i < objects.Length; i++)
                        {
                            var a = objects[i];
                            seenCategories.Add(a.Definition.Category);
                            if (a.Definition.Category == SpawnCategory.Supply &&
                                Mathf.Abs(a.transform.position.x - manager.GetSafeLaneCenter(a.transform.position.y)) > 0.3f)
                                sawOffCenterSupply = true;
                            float radius = a.Definition.ClearanceRadius;
                            for (int j = i + 1; j < objects.Length; j++)
                            {
                                var b = objects[j];
                                float required = radius + b.Definition.ClearanceRadius + Mathf.Max(
                                    manager.Profile.MinimumObjectSeparation, Mathf.Max(a.Definition.MinimumDistance, b.Definition.MinimumDistance));
                                Assert.That(Vector2.Distance(a.transform.position, b.transform.position), Is.GreaterThanOrEqualTo(required - 0.001f));
                            }
                            if (!a.Definition.BlocksPath) continue;
                            for (float offset = -radius - 0.675f; offset <= radius + 0.675f; offset += 0.125f)
                                Assert.That(Mathf.Abs(a.transform.position.x - manager.GetSafeLaneCenter(a.transform.position.y + offset)),
                                    Is.GreaterThanOrEqualTo(manager.SafeLaneWidth * 0.5f + radius - 0.001f));
                        }
                    }
                    string[] first = Snapshot(manager, originY, 3186f);
                    Assert.That(first.Length, Is.GreaterThan(0), "Replay must compare populated snapshots.");
                    manager.Initialize(seed);
                    float replayY = originY + direction * 3186f;
                    manager.RecycleBehind(replayY);
                    manager.EnsureAhead(replayY, 0f);
                    string[] replay = Snapshot(manager, originY, 3186f);
                    Assert.That(replay.Length, Is.GreaterThan(0));
                    CollectionAssert.AreEqual(first, replay, "Actual types, variants and positions must repeat after teleport/replay.");
                }
            }
            Assert.That(seenCategories.Count, Is.EqualTo(6), "The run must exercise every delivered category.");
            Assert.IsTrue(sawOffCenterSupply, "Supplies should not all line up with the lane center.");
            Object.Destroy(testProfile);
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator AcceptanceCaptureRestartsADeadVoyageThroughDemoControls()
        {
            EditorSceneManager.OpenScene(Root + "Generated/Demo/SailingOutGenerationDemo.unity");
            yield return new EnterPlayMode();
            yield return null;
            var boat = Object.FindAnyObjectByType<DemoVoyager>();
            var manager = Object.FindAnyObjectByType<SpawnManager>();
            int seed = manager.CurrentSeed;
            string destination = Path.Combine(Path.GetTempPath(), "sailingout-soak-test-" + System.Guid.NewGuid());
            var capture = new GameObject("Test Acceptance Capture").AddComponent<DemoSoakCapture>();
            typeof(DemoSoakCapture).GetField("destination", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(capture, destination);
            yield return null;
            yield return null;
            MethodInfo damage = typeof(DemoVoyager).GetMethod("TakeCollisionDamage",
                BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < 6; i++) damage.Invoke(boat, null);
            Assert.IsTrue(boat.IsDead);
            float deadline = Time.realtimeSinceStartup + 2f;
            while ((boat.IsDead || boat.DistanceTravelled <= 0f) && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.IsFalse(boat.IsDead, "The acceptance runner must recover through normal restart controls.");
            Assert.AreEqual(seed + 1, manager.CurrentSeed);
            Assert.That(boat.DistanceTravelled, Is.GreaterThan(0f));
            Object.Destroy(capture.gameObject);
            if (Directory.Exists(destination)) Directory.Delete(destination, true);
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator AcceptanceCaptureRejectsAStationaryBoat()
        {
            EditorSceneManager.OpenScene(Root + "Generated/Demo/SailingOutGenerationDemo.unity");
            yield return new EnterPlayMode();
            yield return null;
            var boat = Object.FindAnyObjectByType<DemoVoyager>();
            boat.enabled = false;
            boat.GetComponent<Rigidbody2D>().simulated = false;
            string destination = Path.Combine(Path.GetTempPath(), "sailingout-stalled-test-" + System.Guid.NewGuid());
            var capture = new GameObject("Test Stalled Capture").AddComponent<DemoSoakCapture>();
            typeof(DemoSoakCapture).GetField("destination", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(capture, destination);
            yield return new WaitForSecondsRealtime(5.5f);
            string report = File.ReadAllText(Path.Combine(destination, "soak-report.txt"));
            StringAssert.Contains("Result: FAIL", report);
            StringAssert.Contains("No measured voyage progress", report);
            Object.Destroy(capture.gameObject);
            Directory.Delete(destination, true);
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator AcceptanceCaptureStartsTheDeliveredMenuBeforeMeasuringTravel()
        {
            EditorSceneManager.OpenScene(Root + "Generated/Demo/SailingOutGenerationDemo.unity");
            yield return new EnterPlayMode();
            yield return null;
            var boat = Object.FindAnyObjectByType<DemoVoyager>();
            float deadline = Time.realtimeSinceStartup + 2f;
            while (boat.IsRunning && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.IsFalse(boat.IsRunning, "The delivered scene must wait at its start menu.");
            Assert.IsFalse(boat.TryBoost(), "Waiting at the menu must not consume a boost.");
            float heldDistance = boat.DistanceTravelled;
            deadline = Time.realtimeSinceStartup + 0.1f;
            while (Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(boat.DistanceTravelled, Is.EqualTo(heldDistance).Within(0.001f));

            string destination = Path.Combine(Path.GetTempPath(), "sailingout-start-test-" + System.Guid.NewGuid());
            var capture = new GameObject("Test Start Capture").AddComponent<DemoSoakCapture>();
            typeof(DemoSoakCapture).GetField("destination", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(capture, destination);
            deadline = Time.realtimeSinceStartup + 2f;
            while ((!boat.IsRunning || boat.DistanceTravelled <= heldDistance) &&
                Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.IsTrue(boat.IsRunning, "Acceptance must activate the same start action as the button.");
            Assert.That(boat.DistanceTravelled, Is.GreaterThan(heldDistance));
            Assert.AreEqual(3, boat.BoostsRemaining);
            Object.Destroy(capture.gameObject);
            if (Directory.Exists(destination)) Directory.Delete(destination, true);
            yield return new ExitPlayMode();
        }

        private static string[] Snapshot(SpawnManager manager, float originY, float distance) => manager.GetComponentsInChildren<SpawnableIdentity>()
            .Where(x => (x.transform.position.y - originY) * manager.TravelDirectionSign >= distance)
            .Select(x => $"{x.name}|{x.transform.position.x:R}|{x.transform.position.y:R}")
            .OrderBy(x=>x,System.StringComparer.Ordinal).ToArray();
    }
}
