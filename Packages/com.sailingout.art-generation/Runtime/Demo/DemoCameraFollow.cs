using UnityEngine;

namespace SailingOut
{
    [DisallowMultipleComponent]
    public sealed class DemoCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, -3f, -10f);
        private SpawnManager manager;

        private void Start()
        {
            manager = FindAnyObjectByType<SpawnManager>();
            SnapToTarget();
        }

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        private void LateUpdate()
        {
            SnapToTarget();
        }

        public void SnapToTarget()
        {
            if (target == null)
                return;

            Vector3 position = offset;
            if (manager != null)
                position.y = Mathf.Abs(offset.y) * manager.TravelDirectionSign;
            position.y += target.position.y;
            position.y = Mathf.Round(position.y * 16f) / 16f;
            transform.position = position;
        }
    }
}
