using NUnit.Framework;
using UnityEngine;

namespace SailingOut.Tests
{
    public sealed class SeededRandomTests
    {
        [Test]
        public void SameSeedProducesSameSequence()
        {
            SeededRandom first = new SeededRandom(1234567);
            SeededRandom second = new SeededRandom(1234567);

            for (int i = 0; i < 128; i++)
                Assert.AreEqual(first.NextUInt(), second.NextUInt());
        }

        [Test]
        public void SegmentSeedIsStableAndChangesBetweenSegments()
        {
            int first = SeededRandom.SegmentSeed(42, 15);
            int repeated = SeededRandom.SegmentSeed(42, 15);
            int next = SeededRandom.SegmentSeed(42, 16);

            Assert.AreEqual(first, repeated);
            Assert.AreNotEqual(first, next);
        }

        [Test]
        public void SameRunSeedProducesSameLaneAtEveryWorldPosition()
        {
            float[] positions = { -24f, -1f, 0f, 18f, 60f, 120f, 250f };
            foreach (float y in positions)
            {
                float first = GenerationMath.LaneCenterAt(314159, y, 21.6f, 3f, 4.5f);
                float repeated = GenerationMath.LaneCenterAt(314159, y, 21.6f, 3f, 4.5f);
                Assert.AreEqual(first, repeated, 0.00001f, $"Lane mismatch at Y={y}");
                Assert.That(first, Is.InRange(-1.50001f, 7.50001f));
            }
        }

        [Test]
        public void LanePathTransitionsSmoothlyAcrossSegmentBoundary()
        {
            const int seed = 77;
            const float segmentLength = 20f;
            float before = GenerationMath.LaneCenterAt(seed, segmentLength - 0.01f, segmentLength, 0f, 4f);
            float after = GenerationMath.LaneCenterAt(seed, segmentLength + 0.01f, segmentLength, 0f, 4f);

            Assert.That(Mathf.Abs(after - before), Is.LessThan(0.02f));
        }

        [Test]
        public void SafeLaneIsPreservedForTheFullReefColliderRadius()
        {
            const int seed = 882;
            const float segmentLength = 20f;
            const float laneWidth = 2.4f;
            const float reefRadius = 0.8f;

            for (float y = 0f; y < 500f; y += 0.25f)
            {
                Vector2 bounds = GenerationMath.LaneBoundsForCircle(seed, y, segmentLength, 0f,
                    4f, laneWidth, reefRadius);
                Assert.IsFalse(GenerationMath.IsOutsideSafeLane(seed, (bounds.x + bounds.y) * 0.5f,
                    y, segmentLength, 0f, 4f, laneWidth, reefRadius));
                Assert.IsTrue(GenerationMath.IsOutsideSafeLane(seed, bounds.x - 0.01f,
                    y, segmentLength, 0f, 4f, laneWidth, reefRadius));
                Assert.IsTrue(GenerationMath.IsOutsideSafeLane(seed, bounds.y + 0.01f,
                    y, segmentLength, 0f, 4f, laneWidth, reefRadius));
            }
        }
    }
}
