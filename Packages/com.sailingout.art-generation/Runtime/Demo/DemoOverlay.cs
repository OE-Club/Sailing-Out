using UnityEngine;

namespace SailingOut
{
    /// <summary>Demo-only controls, health display, and seed diagnostics.</summary>
    [RequireComponent(typeof(SpawnManager))]
    public sealed class DemoOverlay : MonoBehaviour
    {
        private SpawnManager manager;
        private DemoVoyager boat;
        private DemoCameraFollow cameraFollow;
        private Camera worldCamera;
        private bool diagnostics;
        private bool gameStarted;
        private GUIStyle title;
        private GUIStyle label;
        private GUIStyle fullHeart;
        private GUIStyle emptyHeart;
        private GUIStyle startTitle;
        private GUIStyle startSubtitle;
        private GUIStyle startButton;
        private GUIStyle startControls;
        private GUIStyle deathTitle;
        private GUIStyle restartButton;
        private Texture2D boostIconTexture;

        private void Start()
        {
            manager = GetComponent<SpawnManager>();
            boat = FindAnyObjectByType<DemoVoyager>();
            worldCamera = Camera.main;
            cameraFollow = worldCamera != null ? worldCamera.GetComponent<DemoCameraFollow>() : null;
            if (boat != null)
                boat.SetRunning(false);
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
            if (gameStarted && (input.keyCode == KeyCode.R || input.keyCode == KeyCode.N))
                Restart(input.keyCode == KeyCode.N);
        }

        public void Restart(bool newSea)
        {
            int seed = manager.CurrentSeed + (newSea ? 1 : 0);
            boat.ResetVoyage();
            if (cameraFollow != null)
                cameraFollow.SnapToTarget();
            manager.Initialize(seed);
            gameStarted = true;
            boat.SetRunning(true);
        }

        public void BeginGame()
        {
            gameStarted = true;
            boat.SetRunning(true);
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
            if (!gameStarted)
            {
                DrawStartScreen(width);
                GUI.matrix = old;
                return;
            }

            if (boat.IsDead)
            {
                DrawDeathScreen(width);
                GUI.matrix = old;
                return;
            }

            GUI.color = new Color(0.025f, 0.08f, 0.12f, 0.92f);
            GUI.DrawTexture(new Rect(16, 14, 240, diagnostics ? 150 : 116), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(30, 20, 240, 38), "SAILING OUT", title);
            float distance = boat.DistanceTravelled;
            string sea = distance < 120f ? "OPEN WATER" : distance < 300f ? "REEF WATCH" :
                distance < 600f ? "CROSSCURRENT" : "ROCKY PASSAGE";
            GUI.Label(new Rect(32, 58, 215, 24), $"{sea}   /   {distance:0000} m", label);
            DrawHearts();
            DrawBoosts(width);
            if (diagnostics)
            {
                GUI.Label(new Rect(32, 112, 230, 24), $"Seed {manager.CurrentSeed}", label);
                GUI.Label(new Rect(32, 134, 230, 24),
                    $"Active {manager.ActiveObjectCount}   Pool {manager.PooledObjectCount}", label);
            }
            GUI.color = new Color(0.025f, 0.08f, 0.12f, 0.9f);
            GUI.DrawTexture(new Rect(16, 496, width - 32, 30), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(30, 500, width - 50, 24),
                $"A/D  steer     SPACE  guide: {(boat.Guided ? "ON" : "OFF")}     W  boost     R  restart     N  new sea     F1  details", label);
            GUI.matrix = old;
        }

        private void DrawStartScreen(float width)
        {
            GUI.color = new Color(0.008f, 0.045f, 0.065f, 0.82f);
            GUI.DrawTexture(new Rect(0f, 0f, width, 540f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            startTitle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 54,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.92f, 0.98f, 0.91f) }
            };
            startSubtitle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.57f, 0.86f, 0.86f) }
            };
            startButton ??= new GUIStyle(GUI.skin.button)
            {
                fontSize = 21,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.025f, 0.12f, 0.14f) },
                hover = { textColor = new Color(0.02f, 0.08f, 0.1f) },
                active = { textColor = Color.black }
            };
            startControls ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.82f, 0.92f, 0.88f) }
            };

            GUI.Label(new Rect(24f, 130f, width - 48f, 72f), "SAILING OUT", startTitle);
            GUI.Label(new Rect(24f, 202f, width - 48f, 32f), "SURVIVE THE OPEN WATER", startSubtitle);
            GUI.backgroundColor = new Color(0.35f, 0.94f, 0.62f);
            if (GUI.Button(new Rect((width - 250f) * 0.5f, 275f, 250f, 58f), "START SAILING", startButton))
                BeginGame();
            GUI.backgroundColor = Color.white;
            GUI.Label(new Rect(24f, 366f, width - 48f, 30f),
                "A / D  STEER     W  BOOST     SPACE  AUTO NAVIGATION", startControls);
        }

        private void DrawBoosts(float width)
        {
            GUI.color = new Color(0.005f, 0.025f, 0.04f, 0.72f);
            GUI.DrawTexture(new Rect(width - 153f, 17f, 140f, 52f), Texture2D.whiteTexture);
            GUI.color = new Color(0.025f, 0.08f, 0.12f, 0.92f);
            GUI.DrawTexture(new Rect(width - 156f, 14f, 140f, 52f), Texture2D.whiteTexture);
            GUI.color = new Color(0.20f, 0.48f, 0.49f, 0.8f);
            GUI.DrawTexture(new Rect(width - 154f, 15f, 136f, 1f), Texture2D.whiteTexture);
            GUI.color = new Color(0.008f, 0.035f, 0.05f, 0.95f);
            GUI.DrawTexture(new Rect(width - 154f, 63f, 136f, 2f), Texture2D.whiteTexture);

            for (int i = 0; i < 3; i++)
            {
                GUI.color = i < boat.BoostsRemaining
                    ? Color.white
                    : new Color(0.38f, 0.48f, 0.46f, 0.72f);
                GUI.DrawTexture(new Rect(width - 148f + i * 43f, 19f, 34f, 40f), boostIconTexture);
            }
            GUI.color = Color.white;
        }

        private void DrawDeathScreen(float width)
        {
            GUI.color = new Color(0.62f, 0.015f, 0.04f, 1f);
            GUI.DrawTexture(new Rect(0f, 0f, width, 540f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            deathTitle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 46,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            restartButton ??= new GUIStyle(GUI.skin.button)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            GUI.Label(new Rect(0f, 190f, width, 72f), "YOU ARE DIE", deathTitle);
            Rect button = new Rect((width - 220f) * 0.5f, 290f, 220f, 54f);
            if (GUI.Button(button, "RESTART", restartButton))
                Restart(false);
        }

        private void DrawHearts()
        {
            fullHeart ??= new GUIStyle(label)
            {
                fontSize = 23,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.95f, 0.25f, 0.3f) }
            };
            emptyHeart ??= new GUIStyle(fullHeart)
            {
                normal = { textColor = new Color(0.32f, 0.22f, 0.25f) }
            };

            for (int i = 0; i < 3; i++)
            {
                Rect slot = new Rect(32f + i * 32f, 80f, 30f, 30f);
                GUI.Label(slot, "♡", emptyHeart);
                float fill = Mathf.Clamp01(boat.Health - i);
                if (fill <= 0f)
                    continue;
                if (fill >= 1f)
                {
                    GUI.Label(slot, "♥", fullHeart);
                    continue;
                }

                GUI.BeginGroup(new Rect(slot.x, slot.y, slot.width * fill, slot.height));
                GUI.Label(new Rect(0f, 0f, slot.width, slot.height), "♥", fullHeart);
                GUI.EndGroup();
            }
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
            FillPolygon(pixels, textureWidth, textureHeight, Offset(outline, 2, -2),
                new Color32(8, 50, 34, 255));
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
