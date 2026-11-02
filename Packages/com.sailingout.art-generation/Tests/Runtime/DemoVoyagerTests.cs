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
        private GameObject pickupObject;
        private GenerationProfile profile;
        private SpawnableDefinition pickupDefinition;
        private GameObject cameraObject;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (boatObject != null)
                Object.Destroy(boatObject);
            if (managerObject != null)
                Object.Destroy(managerObject);
            if (pickupObject != null)
                Object.Destroy(pickupObject);
            if (profile != null)
                Object.Destroy(profile);
            if (pickupDefinition != null)
                Object.Destroy(pickupDefinition);
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

        [UnityTest]
        public IEnumerator DefaultSailingAndSteeringSpeedsAreIncreased()
        {
            boatObject = new GameObject("TestBoat", typeof(Rigidbody2D));
            boatObject.AddComponent<DemoVoyager>();
            yield return null;

            Assert.That(GetFloat(boatObject.GetComponent<DemoVoyager>(), "forwardSpeed"),
                Is.EqualTo(6f).Within(0.001f));
            Assert.That(GetFloat(boatObject.GetComponent<DemoVoyager>(), "horizontalSpeed"),
                Is.EqualTo(7.2f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator LightningPickupRestoresOneChargeUpToThreeAndResetsWithVoyage()
        {
            boatObject = new GameObject("TestBoat", typeof(Rigidbody2D), typeof(BoxCollider2D));
            DemoVoyager boat = boatObject.AddComponent<DemoVoyager>();
            pickupDefinition = ScriptableObject.CreateInstance<SpawnableDefinition>();
            Set(pickupDefinition, "category", SpawnCategory.Lightning);
            pickupObject = new GameObject("Lightning", typeof(CircleCollider2D), typeof(SpawnableIdentity));
            pickupObject.transform.position = new Vector3(100f, 0f, 0f);
            Set(pickupObject.GetComponent<SpawnableIdentity>(), "definition", pickupDefinition);
            Collider2D pickupCollider = pickupObject.GetComponent<Collider2D>();
            pickupCollider.isTrigger = true;
            yield return null;

            Assert.AreEqual(3, boat.BoostsRemaining);
            Assert.IsTrue(boat.TryBoost());
            Assert.AreEqual(2, boat.BoostsRemaining);
            typeof(DemoVoyager).GetMethod("OnTriggerEnter2D",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(boat, new object[] { pickupCollider });
            Assert.AreEqual(3, boat.BoostsRemaining);
            Assert.IsFalse(pickupObject.activeSelf);

            pickupObject.SetActive(true);
            typeof(DemoVoyager).GetMethod("OnTriggerEnter2D",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(boat, new object[] { pickupCollider });
            Assert.AreEqual(3, boat.BoostsRemaining);
            Assert.IsFalse(pickupObject.activeSelf);

            Set(boat, "boostTimeRemaining", 0f);
            Assert.IsTrue(boat.TryBoost());
            Assert.AreEqual(2, boat.BoostsRemaining);
            boat.ResetVoyage();
            Assert.AreEqual(3, boat.BoostsRemaining);
        }

        private static float GetFloat(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected serialized field {fieldName}");
            return (float)field.GetValue(target);
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
