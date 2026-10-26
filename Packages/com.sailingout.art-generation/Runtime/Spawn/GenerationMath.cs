using UnityEngine;

namespace SailingOut
{
    public static class GenerationMath
    {
        public static float LaneCenterAt(
            int seed,
            float y,
            float segmentLength,
            float worldCenterX,
            float maximumOffset)
        {
            if (segmentLength <= 0f || maximumOffset <= 0f)
                return worldCenterX;

            const int knotSegments = 3;
            float segmentPosition = y / segmentLength;
            int segment = Mathf.FloorToInt(segmentPosition);
            int knot = Mathf.FloorToInt((float)segment / knotSegments);
            float localKnotPosition = (segmentPosition - knot * knotSegments) / knotSegments;
            float t = Mathf.SmoothStep(0f, 1f, localKnotPosition);
            float first = KnotOffset(seed, knot) * maximumOffset;
            float second = KnotOffset(seed, knot + 1) * maximumOffset;
            return worldCenterX + Mathf.Lerp(first, second, t);
        }

        public static Vector2 LaneBoundsForCircle(
            int seed,
            float y,
            float segmentLength,
            float worldCenterX,
            float maximumOffset,
            float laneWidth,
            float circleRadius)
        {
            float lowY = y - circleRadius;
            float highY = y + circleRadius;
            float lowCenter = LaneCenterAt(seed, lowY, segmentLength, worldCenterX, maximumOffset);
            float highCenter = LaneCenterAt(seed, highY, segmentLength, worldCenterX, maximumOffset);
            float minimumCenter = Mathf.Min(lowCenter, highCenter);
            float maximumCenter = Mathf.Max(lowCenter, highCenter);
            float knotSpacing = segmentLength * 3f;
            if (knotSpacing > 0f)
            {
                int firstKnot = Mathf.CeilToInt(lowY / knotSpacing);
                int lastKnot = Mathf.FloorToInt(highY / knotSpacing);
                for (int knot = firstKnot; knot <= lastKnot; knot++)
                {
                    float laneCenter = LaneCenterAt(seed, knot * knotSpacing, segmentLength,
                        worldCenterX, maximumOffset);
                    minimumCenter = Mathf.Min(minimumCenter, laneCenter);
                    maximumCenter = Mathf.Max(maximumCenter, laneCenter);
                }
            }
            float halfClearance = laneWidth * 0.5f + circleRadius;
            return new Vector2(minimumCenter - halfClearance, maximumCenter + halfClearance);
        }

        public static bool IsOutsideSafeLane(
            int seed,
            float x,
            float y,
            float segmentLength,
            float worldCenterX,
            float maximumOffset,
            float laneWidth,
            float circleRadius)
        {
            Vector2 bounds = LaneBoundsForCircle(seed, y, segmentLength, worldCenterX,
                maximumOffset, laneWidth, circleRadius);
            return x <= bounds.x || x >= bounds.y;
        }

        private static float KnotOffset(int seed, int knot)
        {
            SeededRandom random = new SeededRandom(SeededRandom.SegmentSeed(seed, knot));
            return random.Range(-1f, 1f);
        }
    }
}
