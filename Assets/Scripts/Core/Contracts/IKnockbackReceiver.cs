using UnityEngine;

namespace SailingOut.Core.Contracts
{
    /// <summary>
    /// Contract used by hazards and monsters to request movement feedback from a
    /// player without depending on the Player module implementation.
    /// </summary>
    public interface IKnockbackReceiver
    {
        void ApplyKnockback(Vector2 force);
    }
}

