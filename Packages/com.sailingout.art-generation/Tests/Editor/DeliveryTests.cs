using System.Collections;
using System.IO;
using System.Linq;
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
                else if (identity.Definition.Category == SpawnCategory.Buoy)
                    Assert.IsTrue(collider != null && collider.isTrigger &&
                        !identity.Definition.BlocksPath && !identity.Definition.Collectible);
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

            // Validate actual delivered definitions, not just synthetic test prefabs.
            for (int seed = 23; seed < 43; seed++)
            {
                manager.Initialize(seed);
                for (float y = 0f; y <= 3200f; y += 27f)
                {
                    manager.RecycleBehind(y);
                    manager.EnsureAhead(y,0f);
                    var objects = Object.FindObjectsByType<SpawnableIdentity>();
                    for (int i=0;i<objects.Length;i++)
                    {
                        var a=objects[i];
                        float radius=a.Definition.ClearanceRadius;
                        for (int j=i+1;j<objects.Length;j++)
                        {
                            var b=objects[j];
                            float required=radius+b.Definition.ClearanceRadius + Mathf.Max(
                                manager.Profile.MinimumObjectSeparation,Mathf.Max(a.Definition.MinimumDistance,b.Definition.MinimumDistance));
                            Assert.That(Vector2.Distance(a.transform.position,b.transform.position),Is.GreaterThanOrEqualTo(required-0.001f));
                        }
                        if (!a.Definition.BlocksPath) continue;
                        for (float offset=-radius-0.675f;offset<=radius+0.675f;offset+=0.125f)
                            Assert.That(Mathf.Abs(a.transform.position.x-manager.GetSafeLaneCenter(a.transform.position.y+offset)),
                                Is.GreaterThanOrEqualTo(manager.SafeLaneWidth*0.5f+radius-0.001f));
                    }
                }
                string[] first=Snapshot(manager);
                manager.Initialize(seed);
                manager.RecycleBehind(3186f);
                manager.EnsureAhead(3186f,0f);
                CollectionAssert.AreEqual(first,Snapshot(manager), "Actual types, variants and positions must repeat after teleport/replay.");
            }
            yield return new ExitPlayMode();
        }

        private static string[] Snapshot(SpawnManager manager) => manager.GetComponentsInChildren<SpawnableIdentity>()
            .Where(x => x.transform.position.y >= 3186f)
            .Select(x => $"{x.name}|{x.transform.position.x:R}|{x.transform.position.y:R}")
            .OrderBy(x=>x,System.StringComparer.Ordinal).ToArray();
    }
}
