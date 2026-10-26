using UnityEngine;

namespace SailingOut
{
    public struct SeededRandom
    {
        private uint state;

        public SeededRandom(int seed)
        {
            state = unchecked((uint)seed) + 0x9E3779B9u;
            if (state == 0u)
                state = 0xA341316Cu;
            NextUInt();
        }

        public uint NextUInt()
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return state;
        }

        public int Range(int minimumInclusive, int maximumExclusive)
        {
            if (maximumExclusive <= minimumInclusive)
                return minimumInclusive;

            uint span = (uint)(maximumExclusive - minimumInclusive);
            return minimumInclusive + (int)(NextUInt() % span);
        }

        public float Range(float minimumInclusive, float maximumInclusive)
        {
            float normalized = (NextUInt() >> 8) * (1f / 16777215f);
            return Mathf.Lerp(minimumInclusive, maximumInclusive, normalized);
        }

        public static int SegmentSeed(int seed, long segmentIndex)
        {
            unchecked
            {
                ulong value = (ulong)segmentIndex;
                uint hash = (uint)seed ^ (uint)value ^ (uint)(value >> 32) * 0x85EBCA6Bu;
                hash ^= hash >> 16;
                hash *= 0x7FEB352Du;
                hash ^= hash >> 15;
                hash *= 0x846CA68Bu;
                hash ^= hash >> 16;
                return (int)hash;
            }
        }
    }
}
