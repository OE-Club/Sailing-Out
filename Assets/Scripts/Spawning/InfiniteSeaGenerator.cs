using UnityEngine;

namespace SailingOut.Spawning
{
    /// <summary>
    /// Keeps a finite grid of procedural water sprites centered around a target.
    /// Tiles are repositioned when the target crosses a cell boundary, so the
    /// visible sea can extend indefinitely without allocating new objects.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InfiniteSeaGenerator : MonoBehaviour
    {
        [Header("Follow")]
        [SerializeField] private Transform followTarget;

        [SerializeField] private float seaY;

        [Header("Tile Grid")]
        [Min(3)]
        [SerializeField] private int columns = 7;

        [Min(3)]
        [SerializeField] private int rows = 5;

        [Min(1f)]
        [SerializeField] private float segmentWidth = 16f;

        [Min(1f)]
        [SerializeField] private float segmentHeight = 10f;

        [SerializeField] private int sortingOrder = -10;

        [Header("Procedural Look")]
        [SerializeField] private Color deepColor = new(0.015f, 0.18f, 0.27f, 1f);

        [SerializeField] private Color surfaceColor = new(0.03f, 0.42f, 0.52f, 1f);

        [Min(32)]
        [SerializeField] private int textureResolution = 128;

        [Range(0.01f, 0.5f)]
        [SerializeField] private float waveAmplitude = 0.08f;

        [Min(1f)]
        [SerializeField] private float waveFrequency = 5f;

        private Transform[] tileTransforms;
        private int centerCellX;
        private int centerCellY;
        private Texture2D waterTexture;
        private Sprite waterSprite;
        private bool initialized;

        private void Awake()
        {
            ClampSettings();
            ResolveFollowTarget();
            CreateWaterSprite();
            CreateTiles();
            initialized = true;
            RecenterTiles(true);
        }

        private void LateUpdate()
        {
            if (!initialized)
            {
                return;
            }

            if (followTarget == null)
            {
                ResolveFollowTarget();
            }

            RecenterTiles(false);
        }

        private void OnDestroy()
        {
            if (waterSprite != null)
            {
                Destroy(waterSprite);
            }

            if (waterTexture != null)
            {
                Destroy(waterTexture);
            }
        }

        private void ResolveFollowTarget()
        {
            if (followTarget == null && Camera.main != null)
            {
                followTarget = Camera.main.transform;
            }
        }

        private void CreateWaterSprite()
        {
            waterTexture = new Texture2D(textureResolution, textureResolution, TextureFormat.RGBA32, false)
            {
                name = "ProceduralSeaTile",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color[] pixels = new Color[textureResolution * textureResolution];
            for (int y = 0; y < textureResolution; y++)
            {
                float verticalPosition = y / (textureResolution - 1f);
                for (int x = 0; x < textureResolution; x++)
                {
                    float horizontalPosition = x / (textureResolution - 1f);
                    float wave = Mathf.Sin(horizontalPosition * waveFrequency * Mathf.PI * 2f) * waveAmplitude;
                    float crestDistance = Mathf.Abs(verticalPosition - (0.86f + wave));
                    float crest = Mathf.Clamp01(1f - crestDistance * 24f);
                    float depth = Mathf.Clamp01(verticalPosition * 0.25f + crest * 0.45f);
                    pixels[y * textureResolution + x] = Color.Lerp(deepColor, surfaceColor, depth);
                }
            }

            waterTexture.SetPixels(pixels);
            waterTexture.Apply(false, false);
            waterSprite = Sprite.Create(
                waterTexture,
                new Rect(0f, 0f, textureResolution, textureResolution),
                new Vector2(0.5f, 0.5f),
                textureResolution);
            waterSprite.name = "ProceduralSeaTileSprite";
        }

        private void CreateTiles()
        {
            tileTransforms = new Transform[columns * rows];
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    GameObject tile = new GameObject($"SeaTile_{column}_{row}");
                    tile.transform.SetParent(transform, false);

                    SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
                    renderer.sprite = waterSprite;
                    renderer.sortingOrder = sortingOrder;
                    tile.transform.localScale = new Vector3(segmentWidth, segmentHeight, 1f);
                    tileTransforms[row * columns + column] = tile.transform;
                }
            }
        }

        private void RecenterTiles(bool force)
        {
            Vector3 targetPosition = followTarget != null ? followTarget.position : transform.position;
            int nextCenterCellX = Mathf.FloorToInt(targetPosition.x / segmentWidth + 0.5f);
            int nextCenterCellY = Mathf.FloorToInt((targetPosition.y - seaY) / segmentHeight + 0.5f);

            if (!force && nextCenterCellX == centerCellX && nextCenterCellY == centerCellY)
            {
                return;
            }

            centerCellX = nextCenterCellX;
            centerCellY = nextCenterCellY;
            int halfColumns = columns / 2;
            int halfRows = rows / 2;

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    int cellX = centerCellX + column - halfColumns;
                    int cellY = centerCellY + row - halfRows;
                    tileTransforms[row * columns + column].localPosition = new Vector3(
                        cellX * segmentWidth,
                        seaY + cellY * segmentHeight,
                        0f);
                }
            }
        }

        private void ClampSettings()
        {
            columns = MakeOddAtLeastThree(columns);
            rows = MakeOddAtLeastThree(rows);
            segmentWidth = Mathf.Max(1f, segmentWidth);
            segmentHeight = Mathf.Max(1f, segmentHeight);
            textureResolution = Mathf.Clamp(textureResolution, 32, 1024);
        }

        private static int MakeOddAtLeastThree(int value)
        {
            value = Mathf.Max(3, value);
            return value % 2 == 0 ? value + 1 : value;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ClampSettings();
        }
#endif
    }
}
