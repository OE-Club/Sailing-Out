namespace SailingOut
{
    public enum SpawnCategory
    {
        Buoy,
        Reef,
        Seabird,
        Supply,
        Decoration,
        Lightning
    }

    public enum SpawnColliderKind
    {
        None,
        Solid,
        Trigger
    }

    public interface IPooledSpawnable
    {
        void OnSpawned();
        void OnDespawned();
    }
}
