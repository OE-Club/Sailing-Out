using UnityEngine;

namespace SailingOut.Core
{
    /// <summary>
    /// Scene entry point. Keep scene-specific references here; gameplay modules
    /// should register themselves through their own components instead.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private GameSession session;

        private void Awake()
        {
            if (session == null)
            {
                session = GetComponent<GameSession>();
            }

            if (session == null)
            {
                session = gameObject.AddComponent<GameSession>();
            }
        }
    }
}

