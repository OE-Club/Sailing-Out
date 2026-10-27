using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SailingOut.Tests
{
    public sealed class DemoVoyagerLeanTests
    {
        private GameObject boatObject;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (boatObject != null)
                Object.Destroy(boatObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SteeringLeansTheHullIntoTheTurnAndReturnsToCenter()
        {
            DemoVoyager boat = CreateBoat(false);
            SetHorizontalInput(boat, 1f);
            yield return new WaitForSeconds(0.2f);

            Rigidbody2D body = boatObject.GetComponent<Rigidbody2D>();
            Assert.Less(body.rotation, -8f);

            SetHorizontalInput(boat, 0f);
            yield return new WaitForSeconds(0.2f);
            Assert.That(body.rotation, Is.EqualTo(0f).Within(0.5f));
        }

        [UnityTest]
        public IEnumerator DownwardFacingHullLeansIntoTheTurnAndRestartResetsRotation()
        {
            DemoVoyager boat = CreateBoat(true);
            SetHorizontalInput(boat, 1f);
            yield return new WaitForSeconds(0.2f);

            Rigidbody2D body = boatObject.GetComponent<Rigidbody2D>();
            Assert.Greater(body.rotation, 8f);

            boat.ResetVoyage();
            Assert.That(body.rotation, Is.EqualTo(0f).Within(0.001f));
        }

        private DemoVoyager CreateBoat(bool bowFacesDown)
        {
            boatObject = new GameObject("TestBoat", typeof(Rigidbody2D), typeof(SpriteRenderer));
            Rigidbody2D body = boatObject.GetComponent<Rigidbody2D>();
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            boatObject.GetComponent<SpriteRenderer>().flipY = bowFacesDown;
            return boatObject.AddComponent<DemoVoyager>();
        }

        private static void SetHorizontalInput(DemoVoyager boat, float value)
        {
            FieldInfo field = typeof(DemoVoyager).GetField("horizontalInput",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field);
            field.SetValue(boat, value);
        }
    }
}
