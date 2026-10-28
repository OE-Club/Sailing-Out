using System;
using System.Collections.Generic;
using UnityEngine;

namespace SailingOut
{
    [Serializable]
    public sealed class CategorySpawnRange
    {
        [SerializeField] private SpawnCategory category;
        [SerializeField, Min(0)] private int minimum;
        [SerializeField, Min(0)] private int maximum;

        public SpawnCategory Category => category;
        public int Minimum => Mathf.Min(minimum, maximum);
        public int Maximum => Mathf.Max(minimum, maximum);
    }

    [Serializable]
    public sealed class GenerationStage
    {
        [SerializeField, Min(0f)] private float startDistance;
        [SerializeField] private List<CategorySpawnRange> counts = new List<CategorySpawnRange>();
        [SerializeField, Min(0f)] private float obstacleDensityMultiplier = 1f;

        public float StartDistance => startDistance;
        public float ObstacleDensityMultiplier => obstacleDensityMultiplier;

        public int CountRange(SpawnCategory category, out int maximum)
        {
            for (int i = 0; i < counts.Count; i++)
            {
                if (counts[i].Category != category)
                    continue;

                maximum = counts[i].Maximum;
                return counts[i].Minimum;
            }

            maximum = 0;
            return 0;
        }
    }

    [CreateAssetMenu(menuName = "Sailing Out/Generation Profile", fileName = "GenerationProfile")]
    public sealed class GenerationProfile : ScriptableObject
    {
        [Header("Endless field")]
        [SerializeField] private bool travelDownward;
        [SerializeField, Min(0.25f)] private float segmentLengthScreens = 2f;
        [SerializeField, Min(1f)] private float generateAheadScreens = 2.5f;
        [SerializeField, Min(0.25f)] private float recycleBehindScreens = 1.5f;
        [SerializeField, Min(0f)] private float horizontalEdgeBuffer = 1f;
        [SerializeField, Min(0f)] private float safeLaneMinimumWidth = 0f;
        [SerializeField, Min(0f)] private float safeLaneSideBuffer = 0.5f;
        [SerializeField, Min(0f)] private float maximumLaneOffset = 5f;
        [SerializeField, Min(0f)] private float minimumObjectSeparation = 0.5f;
        [SerializeField, Min(1)] private int placementAttempts = 28;
        [SerializeField, Min(1)] private int maxActiveObjects = 256;
        [SerializeField, Min(0)] private int maxPooledObjects = 256;

        [Header("Random seed")]
        [SerializeField] private int seed = 314159;
        [SerializeField] private bool randomizeSeedOnStart;

        [Header("Definitions and progression")]
        [SerializeField] private List<SpawnableDefinition> definitions = new List<SpawnableDefinition>();
        [SerializeField] private List<GenerationStage> stages = new List<GenerationStage>();
        [SerializeField] private AnimationCurve obstacleDensityByDistance = AnimationCurve.Linear(0f, 1f, 1200f, 1.25f);

        public bool TravelDownward => travelDownward;
        public float TravelDirectionSign => travelDownward ? -1f : 1f;
        public float SegmentLengthScreens => segmentLengthScreens;
        public float GenerateAheadScreens => generateAheadScreens;
        public float RecycleBehindScreens => recycleBehindScreens;
        public float HorizontalEdgeBuffer => horizontalEdgeBuffer;
        public float SafeLaneMinimumWidth => safeLaneMinimumWidth;
        public float SafeLaneSideBuffer => safeLaneSideBuffer;
        public float MaximumLaneOffset => maximumLaneOffset;
        public float MinimumObjectSeparation => minimumObjectSeparation;
        public int PlacementAttempts => placementAttempts;
        public int MaxActiveObjects => maxActiveObjects;
        public int MaxPooledObjects => maxPooledObjects;
        public int Seed => seed;
        public bool RandomizeSeedOnStart => randomizeSeedOnStart;
        public IReadOnlyList<SpawnableDefinition> Definitions => definitions;

        public float GetSegmentLength(Camera camera)
        {
            return Mathf.Max(1f, camera.orthographicSize * 2f * segmentLengthScreens);
        }

        public float GetViewHeight(Camera camera) => camera.orthographicSize * 2f;

        public float GetAheadDistance(Camera camera) => GetViewHeight(camera) * generateAheadScreens;

        public float GetRecycleDistance(Camera camera) => GetViewHeight(camera) * recycleBehindScreens;

        public GenerationStage GetStage(float distance)
        {
            GenerationStage selected = null;
            for (int i = 0; i < stages.Count; i++)
            {
                GenerationStage candidate = stages[i];
                if (candidate != null && candidate.StartDistance <= distance &&
                    (selected == null || candidate.StartDistance > selected.StartDistance))
                    selected = candidate;
            }

            return selected;
        }

        public int GetCount(GenerationStage stage, SpawnCategory category, ref SeededRandom random, float distance = 0f)
        {
            if (stage == null)
                return 0;

            int minimum = stage.CountRange(category, out int maximum);
            int count = random.Range(minimum, maximum + 1);
            if (category == SpawnCategory.Reef)
                count = Mathf.RoundToInt(count * stage.ObstacleDensityMultiplier *
                    Mathf.Clamp(obstacleDensityByDistance.Evaluate(distance), 0f, 8f));
            return count;
        }

        public SpawnableDefinition ChooseDefinition(SpawnCategory category, ref SeededRandom random)
        {
            float totalWeight = 0f;
            for (int i = 0; i < definitions.Count; i++)
            {
                SpawnableDefinition definition = definitions[i];
                if (definition != null && definition.Category == category && definition.HasUsablePrefab &&
                    definition.Weight > 0f)
                    totalWeight += definition.Weight;
            }

            if (totalWeight <= 0f)
                return null;

            float choice = random.Range(0f, totalWeight);
            for (int i = 0; i < definitions.Count; i++)
            {
                SpawnableDefinition definition = definitions[i];
                if (definition == null || definition.Category != category || !definition.HasUsablePrefab ||
                    definition.Weight <= 0f)
                    continue;

                choice -= definition.Weight;
                if (choice <= 0f)
                    return definition;
            }

            return null;
        }
    }
}
