using UnityEngine;

namespace SailingOut.Player
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SailboatAppearance : MonoBehaviour
    {
        private static readonly Vector2[] HullOutline =
        {
            new(48, 4), new(64, 40), new(61, 91), new(48, 122), new(35, 91), new(32, 40)
        };

        private static readonly Vector2[] Deck =
        {
            new(48, 14), new(58, 43), new(56, 88), new(48, 108), new(40, 88), new(38, 43)
        };

        private static readonly Vector2[] MainSail =
        {
            new(50, 48), new(85, 91), new(52, 79)
        };

        private static readonly Vector2[] ForeSail =
        {
            new(46, 53), new(17, 87), new(43, 80)
        };

        private SpriteRenderer spriteRenderer;
        private Texture2D texture;
        private Sprite sprite;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            texture = new Texture2D(96, 128, TextureFormat.RGBA32, false)
            {
                name = "SailboatTexture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color[] pixels = new Color[96 * 128];
            FillPolygon(pixels, HullOutline, new Color(0.025f, 0.11f, 0.17f));
            FillPolygon(pixels, Deck, new Color(0.91f, 0.82f, 0.61f));
            FillPolygon(pixels, MainSail, new Color(0.96f, 0.97f, 0.9f));
            FillPolygon(pixels, ForeSail, new Color(0.94f, 0.54f, 0.26f));
            FillRectangle(pixels, 46, 42, 50, 98, new Color(0.28f, 0.2f, 0.14f));
            texture.SetPixels(pixels);
            texture.Apply(false, true);

            sprite = Sprite.Create(texture, new Rect(0, 0, 96, 128), new Vector2(0.5f, 0.5f), 64f);
            sprite.name = "SailboatSprite";
            spriteRenderer.sprite = sprite;
        }

        private void OnDestroy()
        {
            if (sprite != null)
            {
                Destroy(sprite);
            }

            if (texture != null)
            {
                Destroy(texture);
            }
        }

        private static void FillPolygon(Color[] pixels, Vector2[] points, Color color)
        {
            int minX = Mathf.FloorToInt(points[0].x);
            int maxX = Mathf.CeilToInt(points[0].x);
            int minY = Mathf.FloorToInt(points[0].y);
            int maxY = Mathf.CeilToInt(points[0].y);

            foreach (Vector2 point in points)
            {
                minX = Mathf.Min(minX, Mathf.FloorToInt(point.x));
                maxX = Mathf.Max(maxX, Mathf.CeilToInt(point.x));
                minY = Mathf.Min(minY, Mathf.FloorToInt(point.y));
                maxY = Mathf.Max(maxY, Mathf.CeilToInt(point.y));
            }

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (ContainsPoint(points, new Vector2(x + 0.5f, y + 0.5f)))
                    {
                        pixels[y * 96 + x] = color;
                    }
                }
            }
        }

        private static bool ContainsPoint(Vector2[] polygon, Vector2 point)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[j];
                bool crosses = (a.y > point.y) != (b.y > point.y)
                    && point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x;
                if (crosses)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        private static void FillRectangle(Color[] pixels, int minX, int minY, int maxX, int maxY, Color color)
        {
            for (int y = minY; y < maxY; y++)
            {
                for (int x = minX; x < maxX; x++)
                {
                    pixels[y * 96 + x] = color;
                }
            }
        }
    }
}
