using UnityEngine;

namespace SailingOut
{
    [RequireComponent(typeof(Animator))]
    public sealed class PooledAnimatorReset : MonoBehaviour, IPooledSpawnable
    {
        private Animator cachedAnimator;

        private void Awake()
        {
            cachedAnimator = GetComponent<Animator>();
        }

        public void OnSpawned()
        {
            if (cachedAnimator == null)
                cachedAnimator = GetComponent<Animator>();

            cachedAnimator.Rebind();
            cachedAnimator.Update(0f);
        }

        public void OnDespawned()
        {
            if (cachedAnimator != null)
                cachedAnimator.Rebind();
        }
    }
}
