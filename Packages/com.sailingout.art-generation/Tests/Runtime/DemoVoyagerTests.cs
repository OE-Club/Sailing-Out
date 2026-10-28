using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SailingOut.Tests
{
    public sealed class DemoVoyagerTests
    {
        private GameObject boatObject;
        private GameObject managerObject;
        private GenerationProfile profile;
        private GameObject cameraObject;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (boatObject != null)
                Object.Destroy(boatObject);
            if (managerObject != null)
                Object.Destroy(managerObject);
            if (profile != null)
                Object.Destroy(profile);
            if (cameraObject != null)
                Object.Destroy(cameraObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DownwardProfileMovesVoyagerDownAndTurnsItsBow()
        {
            profile = ScriptableObject.CreateInstance<GenerationProfile>();
            Set(profile, "travelDownward", true);

            managerObject = new GameObject("TestSpawnManager");
            SpawnManager manager = managerObject.AddComponent<SpawnManager>();
            Set(manager, "profile", profile);
            Set(manager, "initializeOnStart", false);

            boatObject = new GameObject("TestBoat", typeof(Rigidbody2D), typeof(SpriteRenderer));
            DemoVoyager boat = boatObject.AddComponent<DemoVoyager>();
            yield return new WaitForSeconds(0.05f);

            Assert.Less(boat.transform.position.y, 0f);
            Assert.Greater(boat.DistanceTravelled, 0f);
            Assert.IsTrue(boatObject.GetComponent<SpriteRenderer>().flipY);

            boat.ResetVoyage();
            Assert.That(boatObject.GetComponent<Rigidbody2D>().position.y, Is.EqualTo(0f).Within(0.001f));
            Assert.That(boat.DistanceTravelled, Is.EqualTo(0f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator CameraLooksAheadForBothTravelDirections()
        {
            profile = ScriptableObject.CreateInstance<GenerationProfile>();
            managerObject = new GameObject("TestSpawnManager");
            SpawnManager manager = managerObject.AddComponent<SpawnManager>();
            Set(manager, "profile", profile);
            Set(manager, "initializeOnStart", false);
            boatObject = new GameObject("CameraTarget");
            boatObject.transform.position = new Vector3(0f, 10f, 0f);
            cameraObject = new GameObject("TestCamera");
            DemoCameraFollow follow = cameraObject.AddComponent<DemoCameraFollow>();
            follow.Target = boatObject.transform;
            yield return null;

            foreach (bool downward in new[] { false, true })
            {
                Set(profile, "travelDownward", downward);
                follow.SnapToTarget();
                Assert.That(cameraObject.transform.position.y,
                    Is.EqualTo(downward ? 7f : 13f).Within(0.001f));
            }
        }

        private static void Set(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected serialized field {fieldName}");
            field.SetValue(target, value);
        }
    }
}
