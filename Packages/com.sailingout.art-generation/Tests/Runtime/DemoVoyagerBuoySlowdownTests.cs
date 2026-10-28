using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SailingOut.Tests
{
    public sealed class DemoVoyagerBuoySlowdownTests
    {
        private GameObject boatObject;
        private GameObject buoyObject;
        private SpawnableDefinition buoyDefinition;
        private DemoVoyager boat;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (boatObject != null)
                Object.Destroy(boatObject);
            if (buoyObject != null)
                Object.Destroy(buoyObject);
            if (buoyDefinition != null)
                Object.Destroy(buoyDefinition);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BuoyContactSlowsForHalfASecondThenRestoresSpeed()
        {
            CreateBoat();
            CreateBuoy(1.5f);

            float timeout = Time.time + 1f;
            while (ReadSlowdownRemaining() <= 0f && Time.time < timeout)
                yield return new WaitForFixedUpdate();

            Assert.Greater(ReadSlowdownRemaining(), 0f, "The buoy trigger should start the slowdown.");
            Assert.That(boat.Health, Is.EqualTo(2.5f).Within(0.001f));
            Assert.That(boat.BuoyPushDirection, Is.EqualTo(-1).Or.EqualTo(1));

            float slowStartY = boatObject.transform.position.y;
            yield return new WaitForSeconds(0.16f);
            float slowedDistance = boatObject.transform.position.y - slowStartY;
            Assert.That(slowedDistance, Is.InRange(0.25f, 0.55f));

            timeout = Time.time + 0.7f;
            while (ReadSlowdownRemaining() > 0f && Time.time < timeout)
                yield return new WaitForFixedUpdate();

            Assert.That(ReadSlowdownRemaining(), Is.EqualTo(0f));
            float recoveryStartY = boatObject.transform.position.y;
            yield return new WaitForSeconds(0.12f);
            float recoveredDistance = boatObject.transform.position.y - recoveryStartY;
            Assert.That(recoveredDistance, Is.InRange(0.45f, 0.75f));
        }

        [UnityTest]
        public IEnumerator ReefContactRemovesHalfAHeart()
        {
            CreateBoat();
            CreateReef(1.5f);

            float timeout = Time.time + 1f;
            while (boat.Health >= DemoVoyager.MaximumHealth && Time.time < timeout)
                yield return new WaitForFixedUpdate();

            Assert.That(boat.Health, Is.EqualTo(2.5f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator DeathStopsVoyageAndRestartRestoresBoat()
        {
            CreateBoat();
            MethodInfo damage = typeof(DemoVoyager).GetMethod("TakeCollisionDamage",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(damage);
            for (int i = 0; i < 6; i++)
                damage.Invoke(boat, null);

            Assert.IsTrue(boat.IsDead);
            Assert.That(boat.Health, Is.EqualTo(0f));
            float deadPosition = boatObject.transform.position.y;
            yield return new WaitForSeconds(0.15f);
            Assert.That(boatObject.transform.position.y, Is.EqualTo(deadPosition).Within(0.02f));

            boat.ResetVoyage();
            Assert.IsFalse(boat.IsDead);
            Assert.That(boat.Health, Is.EqualTo(DemoVoyager.MaximumHealth));
            yield return new WaitForSeconds(0.12f);
            Assert.That(boatObject.transform.position.y, Is.GreaterThan(deadPosition));
        }

        [UnityTest]
        public IEnumerator BoostUsesThreeChargesAndCannotRestartWhileActive()
        {
            CreateBoat();
            yield return null;

            Assert.AreEqual(3, boat.BoostsRemaining);
            Assert.AreEqual(3f, GetFloat("boostDuration"));
            Assert.AreEqual(2f, GetFloat("boostMultiplier"));
            for (int use = 0; use < 3; use++)
            {
                Assert.IsTrue(boat.TryBoost());
                Assert.AreEqual(2 - use, boat.BoostsRemaining);
                Assert.IsTrue(boat.IsBoosting);
                Assert.IsFalse(boat.TryBoost(), "An active boost must not consume another charge.");

                Set(boat, "boostTimeRemaining", 0.01f);
                yield return new WaitForSeconds(0.05f);
                Assert.IsFalse(boat.IsBoosting);
            }

            Assert.AreEqual(0, boat.BoostsRemaining);
            Assert.IsFalse(boat.TryBoost());
            boat.ResetVoyage();
            Assert.AreEqual(3, boat.BoostsRemaining);
            Assert.IsFalse(boat.IsBoosting);
        }

        [UnityTest]
        public IEnumerator SupplyRestoresHalfAHeartAndDisappears()
        {
            CreateBoat();
            ApplyDamage(1);
            CreateSupply(0f);

            float timeout = Time.time + 1f;
            while (buoyObject.activeSelf && Time.time < timeout)
                yield return new WaitForFixedUpdate();

            Assert.IsFalse(buoyObject.activeSelf, "Collected supply should disappear immediately.");
            Assert.That(boat.Health, Is.EqualTo(DemoVoyager.MaximumHealth).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator SupplyAtFullHealthDisappearsWithoutAddingHealth()
        {
            CreateBoat();
            CreateSupply(0f);

            float timeout = Time.time + 1f;
            while (buoyObject.activeSelf && Time.time < timeout)
                yield return new WaitForFixedUpdate();

            Assert.IsFalse(buoyObject.activeSelf, "Supply should still be consumed at full health.");
            Assert.That(boat.Health, Is.EqualTo(DemoVoyager.MaximumHealth).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator BoatRemainsStoppedUntilStarted()
        {
            CreateBoat();
            boat.SetRunning(false);
            float startY = boatObject.transform.position.y;
            Assert.IsFalse(boat.TryBoost());
            Assert.AreEqual(3, boat.BoostsRemaining);

            yield return new WaitForSeconds(0.15f);

            Assert.IsFalse(boat.IsRunning);
            Assert.That(boatObject.transform.position.y, Is.EqualTo(startY).Within(0.01f));
            boat.SetRunning(true);
            yield return new WaitForSeconds(0.12f);
            Assert.That(boatObject.transform.position.y, Is.GreaterThan(startY));
        }

        [UnityTest]
        public IEnumerator ResetVoyageRestoresBoostChargesAndCancelsAnActiveBoost()
        {
            CreateBoat();
            yield return null;
            Assert.IsTrue(boat.TryBoost());
            Assert.IsTrue(boat.IsBoosting);
            Assert.AreEqual(2, boat.BoostsRemaining);

            boat.ResetVoyage();

            Assert.AreEqual(3, boat.BoostsRemaining);
            Assert.IsFalse(boat.IsBoosting);
            Assert.That(boat.DistanceTravelled, Is.EqualTo(0f).Within(0.001f));
            Assert.IsTrue(boat.TryBoost(), "A new boost must be available immediately after restart.");
            Assert.AreEqual(2, boat.BoostsRemaining);
        }

        private void CreateBoat()
        {
            boatObject = new GameObject("TestBoat", typeof(Rigidbody2D), typeof(BoxCollider2D),
                typeof(SpriteRenderer));
            Rigidbody2D body = boatObject.GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            boatObject.GetComponent<BoxCollider2D>().size = new Vector2(0.65f, 1.35f);
            boat = boatObject.AddComponent<DemoVoyager>();
            Set(boat, "forwardSpeed", 5f);
            Set(boat, "buoySlowdownDuration", 0.5f);
            Set(boat, "buoySlowdownMultiplier", 0.5f);
        }

        private void CreateBuoy(float y)
        {
            buoyDefinition = ScriptableObject.CreateInstance<SpawnableDefinition>();
            Set(buoyDefinition, "category", SpawnCategory.Buoy);

            buoyObject = new GameObject("TestBuoy", typeof(CircleCollider2D), typeof(SpawnableIdentity));
            CircleCollider2D collider = buoyObject.GetComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.4f;
            buoyObject.transform.position = new Vector3(0f, y, 0f);
            Set(buoyObject.GetComponent<SpawnableIdentity>(), "definition", buoyDefinition);
        }

        private void CreateReef(float y)
        {
            buoyDefinition = ScriptableObject.CreateInstance<SpawnableDefinition>();
            Set(buoyDefinition, "category", SpawnCategory.Reef);

            buoyObject = new GameObject("TestReef", typeof(CircleCollider2D), typeof(SpawnableIdentity));
            CircleCollider2D collider = buoyObject.GetComponent<CircleCollider2D>();
            collider.radius = 0.4f;
            buoyObject.transform.position = new Vector3(0f, y, 0f);
            Set(buoyObject.GetComponent<SpawnableIdentity>(), "definition", buoyDefinition);
        }

        private void CreateSupply(float y)
        {
            buoyDefinition = ScriptableObject.CreateInstance<SpawnableDefinition>();
            Set(buoyDefinition, "category", SpawnCategory.Supply);

            buoyObject = new GameObject("TestSupply", typeof(CircleCollider2D), typeof(SpawnableIdentity));
            CircleCollider2D collider = buoyObject.GetComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.4f;
            buoyObject.transform.position = new Vector3(0f, y, 0f);
            Set(buoyObject.GetComponent<SpawnableIdentity>(), "definition", buoyDefinition);
        }

        private void ApplyDamage(int count)
        {
            MethodInfo damage = typeof(DemoVoyager).GetMethod("TakeCollisionDamage",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(damage);
            for (int i = 0; i < count; i++)
                damage.Invoke(boat, null);
        }

        private float ReadSlowdownRemaining()
        {
            FieldInfo field = typeof(DemoVoyager).GetField("buoySlowdownRemaining",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field);
            return (float)field.GetValue(boat);
        }

        private float GetFloat(string fieldName)
        {
            FieldInfo field = typeof(DemoVoyager).GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected serialized field {fieldName}");
            return (float)field.GetValue(boat);
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
