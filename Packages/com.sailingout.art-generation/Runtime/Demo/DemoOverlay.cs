using UnityEngine;

namespace SailingOut
{
    /// <summary>Demo-only controls and seed diagnostics. Has no reward/damage/collection logic.</summary>
    [RequireComponent(typeof(SpawnManager))]
    public sealed class DemoOverlay : MonoBehaviour
    {
        private SpawnManager manager;
        private DemoVoyager boat;
        private Camera worldCamera;
        private bool diagnostics;
        private GUIStyle title;
        private GUIStyle label;
        private Texture2D boostIconTexture;

        private void Start()
        {
            manager = GetComponent<SpawnManager>();
            boat = FindAnyObjectByType<DemoVoyager>();
            worldCamera = Camera.main;
            Application.targetFrameRate = 60;
        }

        private void OnDestroy()
        {
            if (boostIconTexture != null)
                Destroy(boostIconTexture);
        }

        private void HandleKey(Event input)
        {
            if (input.type != EventType.KeyDown || boat == null)
                return;
            if (input.keyCode == KeyCode.F1)
                diagnostics = !diagnostics;
            if (input.keyCode == KeyCode.R || input.keyCode == KeyCode.N)
            {
                int seed = manager.CurrentSeed + (input.keyCode == KeyCode.N ? 1 : 0);
                boat.ResetVoyage();
                worldCamera.transform.position = new Vector3(0f, 3f, -10f);
                manager.Initialize(seed);
            }
        }

        private void OnGUI()
        {
            if (manager == null || boat == null)
                return;
            HandleKey(Event.current);
            GUI.depth = 0;
            title ??= new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.88f, 0.96f, 0.91f) } };
            label ??= new GUIStyle(GUI.skin.label) { fontSize = 13,
                normal = { textColor = new Color(0.67f, 0.86f, 0.86f) } };
            boostIconTexture ??= CreateBoostIconTexture();
            Matrix4x4 old = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(Vector3.one * (Screen.height / 540f));
            float width = Screen.width * 540f / Screen.height;
            GUI.color = new Color(0.005f, 0.025f, 0.04f, 0.72f);
            GUI.DrawTexture(new Rect(width - 153, 17, 140, 52), Texture2D.whiteTexture);
            GUI.color = new Color(0.025f, 0.08f, 0.12f, 0.92f);
            GUI.DrawTexture(new Rect(width - 156, 14, 140, 52), Texture2D.whiteTexture);
            GUI.color = new Color(0.20f, 0.48f, 0.49f, 0.8f);
            GUI.DrawTexture(new Rect(width - 154, 15, 136, 1), Texture2D.whiteTexture);
            GUI.color = new Color(0.008f, 0.035f, 0.05f, 0.95f);
            GUI.DrawTexture(new Rect(width - 154, 63, 136, 2), Texture2D.whiteTexture);
            for (int i = 0; i < 3; i++)
            {
                GUI.color = i < boat.BoostsRemaining
                    ? Color.white
                    : new Color(0.38f, 0.48f, 0.46f, 0.72f);
                GUI.DrawTexture(new Rect(width - 148 + i * 43, 19, 34, 40), boostIconTexture);
            }
            GUI.color = new Color(0.025f, 0.08f, 0.12f, 0.92f);
            GUI.DrawTexture(new Rect(16, 14, 240, diagnostics ? 140 : 88), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(30, 20, 240, 38), "SAILING OUT", title);
            float distance = Mathf.Max(0f, boat.transform.position.y);
            string sea = distance < 120f ? "OPEN WATER" : distance < 300f ? "REEF WATCH" :
                distance < 600f ? "CROSSCURRENT" : "ROCKY PASSAGE";
            GUI.Label(new Rect(32, 58, 215, 24), $"{sea}   /   {distance:0000} m", label);
            if (diagnostics)
            {
                GUI.Label(new Rect(32, 86, 230, 24), $"Seed {manager.CurrentSeed}", label);
                GUI.Label(new Rect(32, 108, 230, 24),
                    $"Active {manager.ActiveObjectCount}   Pool {manager.PooledObjectCount}", label);
            }
            GUI.color = new Color(0.025f, 0.08f, 0.12f, 0.9f);
            GUI.DrawTexture(new Rect(16, 496, width - 32, 30), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(30, 500, width - 50, 24),
                $"A/D  steer     SPACE  guide: {(boat.Guided ? "ON" : "OFF")}     W  boost     R  restart     N  new sea     F1  details", label);
            GUI.matrix = old;
        }

        private static Texture2D CreateBoostIconTexture()
        {
            const int textureWidth = 32;
            const int textureHeight = 40;
            Vector2[] outline =
            {
                new Vector2(15, 37), new Vector2(27, 37), new Vector2(20, 23),
                new Vector2(29, 23), new Vector2(8, 2), new Vector2(12, 17), new Vector2(4, 17)
            };
            Vector2[] face =
            {
                new Vector2(16, 33), new Vector2(23, 33), new Vector2(16, 22),
                new Vector2(24, 22), new Vector2(11, 7), new Vector2(14, 19), new Vector2(9, 19)
            };
            var pixels = new Color32[textureWidth * textureHeight];
            FillPolygon(pixels, textureWidth, textureHeight, Offset(outline, 2, -2), new Color32(8, 50, 34, 255));
            FillPolygon(pixels, textureWidth, textureHeight, outline, new Color32(16, 69, 43, 255));

            for (int y = 0; y < textureHeight; y++)
            {
                for (int x = 0; x < textureWidth; x++)
                {
                    if (!ContainsPoint(face, x + 0.5f, y + 0.5f))
                        continue;

                    Color32 color = x <= 13 && y >= 12
                        ? new Color32(171, 255, 169, 255)
                        : x >= 19 || y <= 11
                            ? new Color32(35, 151, 73, 255)
                            : new Color32(77, 235, 105, 255);
                    pixels[y * textureWidth + x] = color;
                }
            }

            var texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false)
            {
                name = "Boost Charge Pixel Icon",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Vector2[] Offset(Vector2[] polygon, float x, float y)
        {
            var offset = new Vector2[polygon.Length];
            for (int i = 0; i < polygon.Length; i++)
                offset[i] = polygon[i] + new Vector2(x, y);
            return offset;
        }

        private static void FillPolygon(Color32[] pixels, int width, int height, Vector2[] polygon, Color32 color)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (ContainsPoint(polygon, x + 0.5f, y + 0.5f))
                        pixels[y * width + x] = color;
                }
            }
        }

        private static bool ContainsPoint(Vector2[] polygon, float x, float y)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                Vector2 current = polygon[i];
                Vector2 previous = polygon[j];
                bool crosses = (current.y > y) != (previous.y > y);
                if (crosses && x < (previous.x - current.x) * (y - current.y) /
                    (previous.y - current.y) + current.x)
                    inside = !inside;
            }
            return inside;
        }
    }
}
