using UnityEngine;

namespace SailingOut
{
    /// <summary>Demo-only controls and seed diagnostics. Has no reward/damage/collection logic.</summary>
    [RequireComponent(typeof(SpawnManager))]
    public sealed class DemoOverlay : MonoBehaviour
    {
        private SpawnManager manager;
        private DemoVoyager boat;
        private DemoCameraFollow cameraFollow;
        private Camera worldCamera;
        private bool diagnostics;
        private GUIStyle title;
        private GUIStyle label;

        private void Start()
        {
            manager = GetComponent<SpawnManager>();
            boat = FindAnyObjectByType<DemoVoyager>();
            worldCamera = Camera.main;
            cameraFollow = worldCamera != null ? worldCamera.GetComponent<DemoCameraFollow>() : null;
            Application.targetFrameRate = 60;
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
                if (cameraFollow != null)
                    cameraFollow.SnapToTarget();
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
            Matrix4x4 old = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(Vector3.one * (Screen.height / 540f));
            float width = Screen.width * 540f / Screen.height;
            GUI.color = new Color(0.025f, 0.08f, 0.12f, 0.92f);
            GUI.DrawTexture(new Rect(16, 14, 240, diagnostics ? 140 : 88), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(30, 20, 240, 38), "SAILING OUT", title);
            float distance = boat.DistanceTravelled;
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
                $"A/D  steer     SPACE  guide: {(boat.Guided ? "ON" : "OFF")}     R  restart     N  new sea     F1  details", label);
            GUI.matrix = old;
        }
    }
}
