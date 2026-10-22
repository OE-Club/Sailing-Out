using UnityEngine;

namespace SailingOut.Sailing
{
    /// <summary>Local 2D vessel state. Heading is clockwise from north; wind points where air travels.</summary>
    public sealed class SailingDynamics
    {
        public const float MaximumSpeed = 3.8f;
        public Vector2 Position { get; private set; }
        public float Heading { get; private set; }
        public float Speed { get; private set; }
        public float Sheet { get; private set; } = 0.55f;
        public float WindHeading { get; set; } = 65f;
        public Vector2 Forward => Direction(Heading);
        public Vector2 Wind => Direction(WindHeading);
        public float OffWind => Vector2.Angle(Forward, Wind);
        public bool InIrons => OffWind >= 140f;
        public float IdealSheet => Mathf.Clamp01(1f - OffWind / 140f);
        public float TrimEfficiency => Mathf.Clamp01(1f - Mathf.Abs(Sheet - IdealSheet) * 1.4f);
        public float Drive => Polar(OffWind) * TrimEfficiency;

        public void Reset(Vector2 position = default, float heading = 0f, float sheet = 0.55f)
        {
            Position = position;
            Heading = Mathf.Repeat(heading, 360f);
            Speed = 0f;
            Sheet = Mathf.Clamp01(sheet);
        }

        public void Step(float deltaTime, float rudder, float sheetRate)
        {
            if (deltaTime <= 0f) return;
            Sheet = Mathf.Clamp01(Sheet + Mathf.Clamp(sheetRate, -1f, 1f) * deltaTime * 0.42f);
            // A little low-speed steerage keeps this small prototype recoverable from irons.
            float steerage = Mathf.Lerp(0.28f, 1f, Speed / MaximumSpeed);
            Heading = Mathf.Repeat(Heading + Mathf.Clamp(rudder, -1f, 1f) * 75f * steerage * deltaTime, 360f);
            float target = MaximumSpeed * Drive;
            Speed = Mathf.MoveTowards(Speed, target, deltaTime * (target > Speed ? 0.72f : 1.15f));
            Position += Forward * (Speed * deltaTime);
        }

        public static Vector2 Direction(float degrees) => new Vector2(Mathf.Sin(degrees * Mathf.Deg2Rad), Mathf.Cos(degrees * Mathf.Deg2Rad));

        public static float Polar(float offWind)
        {
            // Beam/broad reaches are strongest. A 40-degree cone either side of the wind source is unsailable.
            float reach = 0.78f + 0.22f * Mathf.Sin(offWind * Mathf.Deg2Rad);
            float noGo = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(130f, 145f, offWind));
            return reach * noGo;
        }
    }
}
