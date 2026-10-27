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

        private float ReadSlowdownRemaining()
        {
            FieldInfo field = typeof(DemoVoyager).GetField("buoySlowdownRemaining",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field);
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
