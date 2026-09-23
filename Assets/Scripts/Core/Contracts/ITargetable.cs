using UnityEngine;

namespace SailingOut.Core.Contracts
{
    /// <summary>
    /// Stable target surface for chasing systems.
    /// </summary>
    public interface ITargetable
    {
        Transform TargetTransform { get; }
    }
}

