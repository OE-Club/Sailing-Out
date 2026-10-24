using UnityEngine;

namespace SailingOut
{
    [DisallowMultipleComponent]
    public sealed class SpawnableIdentity : MonoBehaviour
    {
        [SerializeField] private SpawnableDefinition definition;

        public SpawnableDefinition Definition => definition;
    }
}
