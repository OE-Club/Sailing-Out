using System.Collections.Generic;
using UnityEngine;

namespace SailingOut
{
    [CreateAssetMenu(menuName = "Sailing Out/Spawnable Definition", fileName = "Spawnable")]
    public sealed class SpawnableDefinition : ScriptableObject
    {
        [SerializeField] private SpawnCategory category;
        [SerializeField] private GameObject prefab;
        [SerializeField] private List<GameObject> variants = new List<GameObject>();
        [SerializeField, Min(0f)] private float weight = 1f;
        [SerializeField, Min(0f)] private float minimumDistance = 2f;
        [SerializeField, Min(0f)] private float clearanceRadius = 0.5f;
        [SerializeField, Min(0f)] private float collisionBoundaryPadding = 0.125f;
        [SerializeField] private SpawnColliderKind colliderKind;
        [SerializeField] private bool blocksPath;
        [SerializeField] private bool collectible;

        public SpawnCategory Category => category;
        public GameObject Prefab => prefab;
        public IReadOnlyList<GameObject> Variants => variants;
        public float Weight => weight;
        public float MinimumDistance => minimumDistance;
        // The authored radius encloses every visual/collider variant, including animation frames.
        public float ClearanceRadius => Mathf.Max(0f, clearanceRadius) + CollisionBoundaryPadding;
        public float CollisionBoundaryPadding => Mathf.Max(0f, collisionBoundaryPadding);
        public SpawnColliderKind ColliderKind => colliderKind;
        public bool BlocksPath => blocksPath;
        public bool Collectible => collectible;

        public GameObject ChoosePrefab(ref SeededRandom random)
        {
            int count = variants != null ? variants.Count : 0;
            if (count == 0)
                return prefab;

            int usableCount = 0;
            for (int i = 0; i < count; i++)
            {
                if (variants[i] != null)
                    usableCount++;
            }

            if (usableCount == 0)
                return prefab;

            int choice = random.Range(0, usableCount);
            for (int i = 0; i < count; i++)
            {
                if (variants[i] == null)
                    continue;
                if (choice-- == 0)
                    return variants[i];
            }

            return prefab;
        }

        public bool HasUsablePrefab
        {
            get
            {
                if (prefab != null)
                    return true;

                if (variants == null)
                    return false;

                for (int i = 0; i < variants.Count; i++)
                {
                    if (variants[i] != null)
                        return true;
                }

                return false;
            }
        }
    }
}
