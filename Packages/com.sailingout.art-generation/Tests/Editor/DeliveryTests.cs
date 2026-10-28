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
                var settings = new SerializedObject(profile);
                settings.FindProperty("travelDownward").boolValue = false;
                settings.ApplyModifiedPropertiesWithoutUndo();
                profile = (GenerationProfile)build.Invoke(null, args);
                Assert.IsTrue(profile.TravelDownward, "Rebuilding must restore the demo direction.");
            }
            finally { AssetDatabase.DeleteAsset(temporary); }
        }

        [Test]
        public void AllArtAndColliderRolesArePresentAndPixelImportsAreCorrect()
        {
            string[] prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { Root + "Generated/Prefabs" });
            Assert.AreEqual(15,prefabs.Length);
            foreach (string guid in prefabs)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                SpawnableIdentity identity = prefab.GetComponent<SpawnableIdentity>();
                Assert.IsNotNull(identity?.Definition, prefab.name);
                Collider2D collider = prefab.GetComponent<Collider2D>();
                if (identity.Definition.Category == SpawnCategory.Reef)
                    Assert.IsTrue(collider != null && !collider.isTrigger && identity.Definition.BlocksPath);
                else if (identity.Definition.Category == SpawnCategory.Supply)
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
            Assert.AreEqual(9, AssetDatabase.FindAssets("t:AnimationClip", new[] { Root + "Generated/Animations" }).Length);
            Assert.AreEqual(15, Directory.GetFiles(Root + "Art/SpriteSheets", "*.png").Length);
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
            Assert.That(opening.CountRange(SpawnCategory.Buoy,out int maxBuoys),Is.EqualTo(2));
            Assert.AreEqual(4,maxBuoys);
            Assert.That(opening.CountRange(SpawnCategory.Seabird,out int maxBirds),Is.EqualTo(1));
            Assert.AreEqual(2,maxBirds);
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
            Assert.That(seenCategories.Count, Is.EqualTo(5), "The run must exercise every delivered category.");
            Object.Destroy(testProfile);
            yield return new ExitPlayMode();
        }

        private static string[] Snapshot(SpawnManager manager, float originY, float distance) => manager.GetComponentsInChildren<SpawnableIdentity>()
            .Where(x => (x.transform.position.y - originY) * manager.TravelDirectionSign >= distance)
            .Select(x => $"{x.name}|{x.transform.position.x:R}|{x.transform.position.y:R}")
            .OrderBy(x=>x,System.StringComparer.Ordinal).ToArray();
    }
}
