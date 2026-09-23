namespace SailingOut.Core.Contracts
{
    /// <summary>
    /// Contract for anything that can receive gameplay damage.
    /// </summary>
    public interface IDamageable
    {
        bool IsAlive { get; }

        void TakeDamage(int amount);
    }
}

