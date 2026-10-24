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
        private DemoVoyager boat;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (boatObject != null)
                Object.Destroy(boatObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BoostUsesThreeChargesAndCannotBeRestartedUntilItEnds()
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
                Assert.AreEqual(2 - use, boat.BoostsRemaining);

                SetBoostTimeRemaining(0.01f);
                yield return new WaitForSeconds(0.05f);
                Assert.IsFalse(boat.IsBoosting);
            }

            Assert.AreEqual(0, boat.BoostsRemaining);
            Assert.IsFalse(boat.TryBoost());
        }

        [UnityTest]
        public IEnumerator ResetVoyageRestoresBoostChargesAndCancelsAnActiveBoost()
        {
            CreateBoat();
            yield return null;
            Assert.IsTrue(boat.TryBoost());

            boat.ResetVoyage();

            Assert.AreEqual(3, boat.BoostsRemaining);
            Assert.IsFalse(boat.IsBoosting);
            Assert.IsTrue(boat.TryBoost());
        }

        private void CreateBoat()
        {
            boatObject = new GameObject("TestBoat", typeof(Rigidbody2D));
            boat = boatObject.AddComponent<DemoVoyager>();
        }

        private void SetBoostTimeRemaining(float value)
        {
            FieldInfo field = GetField("boostTimeRemaining");
            Assert.IsNotNull(field);
            field.SetValue(boat, value);
        }

        private float GetFloat(string fieldName)
        {
            FieldInfo field = GetField(fieldName);
            Assert.IsNotNull(field);
            return (float)field.GetValue(boat);
        }

        private static FieldInfo GetField(string fieldName)
        {
            return typeof(DemoVoyager).GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
        }
    }
}
