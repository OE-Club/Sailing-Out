using UnityEngine;

namespace SailingOut
{
    [DisallowMultipleComponent]
    public sealed class DemoTiledBackground : MonoBehaviour
    {
        [SerializeField] private Sprite waterTile;
        [SerializeField] private Sprite foamTile;

        private Camera targetCamera;
        private SpriteRenderer waterRenderer;
        private SpriteRenderer foamRenderer;

        private void Awake()
        {
            targetCamera = GetComponentInParent<Camera>();
            waterRenderer = CreateLayer("Water", waterTile, -20);
            foamRenderer = CreateLayer("Foam", foamTile, -19);
            RefreshSize();
        }

        private void LateUpdate()
        {
            RefreshSize();
        }

        private void RefreshSize()
        {
            if (targetCamera == null)
                targetCamera = GetComponentInParent<Camera>();
            if (targetCamera == null)
                return;

            Vector2 viewSize = new Vector2(targetCamera.orthographicSize * 2f * targetCamera.aspect,
                targetCamera.orthographicSize * 2f);
            PlaceLayer(waterRenderer, waterTile, viewSize, 0f);
            PlaceLayer(foamRenderer, foamTile, viewSize, Mathf.Floor(Time.time * 2f) / 16f);
        }

        private void PlaceLayer(SpriteRenderer renderer, Sprite sprite, Vector2 viewSize, float drift)
        {
            if (renderer == null || sprite == null)
                return;
            Vector2 tile = sprite.bounds.size;
            renderer.size = new Vector2((Mathf.Ceil(viewSize.x / tile.x) + 2f) * tile.x,
                (Mathf.Ceil(viewSize.y / tile.y) + 2f) * tile.y);
            Vector3 cameraPosition = targetCamera.transform.position;
            renderer.transform.position = new Vector3(Mathf.Floor(cameraPosition.x / tile.x) * tile.x,
                Mathf.Floor((cameraPosition.y - drift) / tile.y) * tile.y + drift, 0f);
        }

        private SpriteRenderer CreateLayer(string layerName, Sprite sprite, int sortingOrder)
        {
            GameObject layer = new GameObject(layerName);
            layer.transform.SetParent(transform, false);
            SpriteRenderer renderer = layer.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }
    }
}
