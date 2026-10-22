using SailingOut.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SailingOut.Sailing
{
    /// <summary>A self-contained local sailing loop. Does not implement or take over Player/Monsters.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class SailingVoyage : MonoBehaviour
    {
        [SerializeField, HideInInspector] SailingDynamics model = new();
        public SailingDynamics Model => model;
        public GameSession Session { get; private set; }
        Camera view;
        VoyagePixelArt art;
        Transform vessel, sail, crew, wake;
        SpriteRenderer crewRenderer, wakeRenderer;
        readonly Transform[] foam = new Transform[60];
        bool help, inputOverride;
        float overrideRudder, overrideSheet;
        GUIStyle titleStyle, bodyStyle, smallStyle, numberStyle, buttonStyle;
        Font hudFont;
        static readonly Color Ink = new Color32(13, 36, 46, 245);
        static readonly Color Cream = new Color32(246, 235, 198, 255);
        static readonly Color Gold = new Color32(255, 191, 76, 255);

        void OnEnable()
        {
            // Runtime objects survive an assembly reload; managed graphics and readonly caches do not.
            // Reuse the existing hierarchy and preserve the model/session instead of starting a new run.
            vessel = transform.Find("Sloop");
            if (vessel == null) return; // First entry is initialized by Start.
            Session = GameSession.Instance;
            view = Camera.main;
            art = new VoyagePixelArt();
            vessel.GetComponent<SpriteRenderer>().sprite = art.Hull;
            sail = vessel.Find("Trimmed mainsail");
            sail.GetComponent<SpriteRenderer>().sprite = art.Sail;
            crew = vessel.Find("Life-vest crew");
            crewRenderer = crew.GetComponent<SpriteRenderer>();
            vessel.Find("Crew seat").GetComponent<SpriteRenderer>().sprite = art.Seat;
            wake = vessel.Find("Wake");
            wakeRenderer = wake.GetComponent<SpriteRenderer>();
            wakeRenderer.sprite = art.Wake;
            for (int i = 0; i < foam.Length; i++)
            {
                foam[i] = transform.Find($"Foam {i}");
                foam[i].GetComponent<SpriteRenderer>().sprite = art.Foam;
            }
            titleStyle = bodyStyle = smallStyle = numberStyle = buttonStyle = null;
            UpdateVisuals();
        }

        void Start()
        {
            if (vessel != null) return;
            Session = GameSession.Instance;
            view = Camera.main;
            if (Session == null || view == null) { Debug.LogError("Pixel Voyage requires the FreeSailing scene's GameSession and Camera."); enabled = false; return; }
            view.orthographic = true; view.orthographicSize = 7.6f;
            art = new VoyagePixelArt();
            vessel = SpriteObject("Sloop", art.Hull, 10, transform).transform;
            sail = SpriteObject("Trimmed mainsail", art.Sail, 20, vessel).transform;
            sail.localPosition = new Vector3(0, 0.4f, 0);
            crewRenderer = SpriteObject("Life-vest crew", art.Crew[0], 35, vessel);
            crew = crewRenderer.transform; crew.localPosition = new Vector3(0, -1.1f, 0);
            Transform seat = SpriteObject("Crew seat", art.Seat, 28, vessel).transform;
            seat.localPosition = new Vector3(0, -1.1f, 0);
            wakeRenderer = SpriteObject("Wake", art.Wake, 2, vessel);
            wake = wakeRenderer.transform; wake.localPosition = new Vector3(0, -2.8f, 0);
            for (int i = 0; i < foam.Length; i++)
            {
                foam[i] = SpriteObject($"Foam {i}", art.Foam, -1, transform).transform;
                foam[i].position = new Vector3((i * 17.31f % 42) - 21, (i * 7.17f % 30) - 15, 0);
            }
            ResetVoyage();
        }

        static SpriteRenderer SpriteObject(string name, Sprite sprite, int order, Transform parent)
        {
            GameObject obj = new(name); obj.transform.SetParent(parent, false);
            SpriteRenderer r = obj.AddComponent<SpriteRenderer>(); r.sprite = sprite; r.sortingOrder = order; return r;
        }

        public void BeginVoyage()
        {
            if (Session.State == GameState.Playing) return;
            if (Session.State == GameState.Paused) { Session.ResumeRun(); return; }
            if (Session.State == GameState.GameOver) ResetVoyage();
            Session.StartRun();
        }
        public void ResetVoyage()
        {
            Model.WindHeading = 65f;
            Model.Reset();
            overrideRudder = overrideSheet = 0;
            Session.ResetRun();
            if (view != null) view.transform.position = new Vector3(0, 1.6f, -10);
            UpdateVisuals();
        }
        public void TogglePause()
        {
            if (Session.State == GameState.Playing) Session.PauseRun();
            else if (Session.State == GameState.Paused) Session.ResumeRun();
        }
        /// <summary>Uses the same control path for deterministic Play Mode validation and future alternative inputs.</summary>
        public void SetControlInput(bool enabledOverride, float rudder = 0, float sheetRate = 0)
        { inputOverride = enabledOverride; overrideRudder = rudder; overrideSheet = sheetRate; }

        void Update()
        {
            Keyboard k = Keyboard.current;
            if (k == null || Session == null) return;
            if (k.enterKey.wasPressedThisFrame) BeginVoyage();
            if (k.rKey.wasPressedThisFrame) ResetVoyage();
            if (k.spaceKey.wasPressedThisFrame) TogglePause();
            if (k.hKey.wasPressedThisFrame) help = !help;
        }
        void FixedUpdate()
        {
            if (Session == null || Session.State != GameState.Playing) return;
            Keyboard k = Keyboard.current;
            float rudder = overrideRudder, sheetRate = overrideSheet;
            if (!inputOverride)
            {
                rudder = k == null ? 0 : ((k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0));
                sheetRate = k == null ? 0 : ((k.sKey.isPressed || k.downArrowKey.isPressed ? 1 : 0) - (k.wKey.isPressed || k.upArrowKey.isPressed ? 1 : 0));
            }
            Model.Step(Time.fixedDeltaTime, rudder, sheetRate);
        }
        void LateUpdate()
        {
            if (vessel == null) return;
            UpdateVisuals();
            Vector3 desired = new Vector3(Model.Position.x, Model.Position.y + 1.6f, -10);
            view.transform.position = Vector3.Lerp(view.transform.position, desired, 1f - Mathf.Exp(-5 * Time.deltaTime));
            // Quantize the camera to the native art grid to limit shimmering while preserving smooth ship controls.
            Vector3 p = view.transform.position; p.x = Mathf.Round(p.x * 48) / 48; p.y = Mathf.Round(p.y * 48) / 48; view.transform.position = p;
            for (int i = 0; i < foam.Length; i++)
            {
                Vector3 f = foam[i].position;
                f.x = Model.Position.x + Mathf.Repeat(f.x - Model.Position.x + 21, 42) - 21;
                f.y = Model.Position.y + Mathf.Repeat(f.y - Model.Position.y + 15, 30) - 15;
                foam[i].position = f;
            }
        }
        void UpdateVisuals()
        {
            if (vessel == null) return;
            vessel.position = Model.Position; vessel.rotation = Quaternion.Euler(0, 0, -Model.Heading);
            int facing = Mathf.RoundToInt(Model.Heading / 90f) % 4;
            crewRenderer.sprite = art.Crew[facing]; crew.rotation = Quaternion.identity;
            float side = Vector3.Cross(Model.Forward, Model.Wind).z < 0 ? 1 : -1;
            sail.localScale = new Vector3(side, 1, 1);
            sail.localRotation = Quaternion.Euler(0, 0, side * (Model.Sheet * 35f - 12f));
            wakeRenderer.color = new Color(1, 1, 1, Mathf.Clamp01(Model.Speed / 2));
            wake.localScale = new Vector3(1, 0.7f + Model.Speed * 0.15f, 1);
        }

        void EnsureStyles()
        {
            if (titleStyle != null) return;
            hudFont = Resources.Load<Font>("Voyage/SailingOutUI");
            if (hudFont == null) throw new System.InvalidOperationException("Bundled Chinese HUD font is missing.");
            const string glyphs = "自由航行风速节向吹迎失请转舵顺横近帆索效率放幅度随调整空格暂停重置说明操作已扬出坐稳船内选择左右收时正会可用之字换关闭继续";
            hudFont.RequestCharactersInTexture(glyphs, 18, FontStyle.Normal);
            bool covered = true;
            foreach (char c in glyphs)
                if (!hudFont.HasCharacter(c)) { Debug.LogError("Chinese HUD font missing glyph: " + c); covered = false; }
            if (covered) Debug.Log("Chinese HUD glyph coverage verified: " + hudFont.fontNames[0]);
            titleStyle = Style(30, FontStyle.Bold); bodyStyle = Style(18); smallStyle = Style(13); numberStyle = Style(26, FontStyle.Bold);
            buttonStyle = new GUIStyle(GUI.skin.button) { font = hudFont, fontSize = 18, fontStyle = FontStyle.Bold };
            buttonStyle.normal.textColor = Cream;
        }
        GUIStyle Style(int size, FontStyle weight = FontStyle.Normal) => new GUIStyle { font = hudFont, fontSize = size, fontStyle = weight, normal = { textColor = Cream } };
        void Label(Rect rect, string text, GUIStyle style, Color? color = null)
        { Color old = GUI.color; GUI.color = color ?? Color.white; GUI.Label(rect, text, style); GUI.color = old; }
        static void Panel(Rect r, Color c) { Color old = GUI.color; GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = old; }
        void OnGUI()
        {
            if (Session == null || view == null) return;
            EnsureStyles();
            const float width = 1440, height = 900;
            float scale = Mathf.Min(Screen.width / width, Screen.height / height);
            Matrix4x4 previous = GUI.matrix;
            Vector2 offset = new((Screen.width - width * scale) / 2, (Screen.height - height * scale) / 2);
            GUI.matrix = Matrix4x4.TRS(offset, Quaternion.identity, Vector3.one * scale);
            Panel(new Rect(26, 26, 330, 111), Ink); Panel(new Rect(26, 26, 5, 111), Gold);
            Label(new Rect(47, 43, 300, 38), "SAILING OUT", titleStyle);
            Label(new Rect(49, 91, 280, 26), "自由航行", smallStyle, Gold);
            Panel(new Rect(1030, 26, 384, 180), Ink);
            Label(new Rect(1052, 44, 180, 24), "风速  /  12 节", bodyStyle, Gold);
            Label(new Rect(1052, 77, 290, 25), "风向：吹向 065°", smallStyle);
            Arrow(new Vector2(1363, 92), Model.WindHeading, Gold, 28);
            string point = Model.InIrons ? "迎风失速 · 请转舵" : Model.OffWind < 38 ? "顺风航行" : Model.OffWind < 112 ? "横风航行" : "近迎风航行";
            Label(new Rect(1052, 118, 340, 25), point, smallStyle, Model.InIrons ? new Color(1, 0.5f, 0.35f) : Cream);
            Label(new Rect(1052, 151, 330, 24), $"帆索效率   {Model.TrimEfficiency * 100:000}%", smallStyle);
            Panel(new Rect(26, 764, 1388, 110), Ink);
            Label(new Rect(49, 780, 170, 22), "航速", smallStyle);
            Label(new Rect(49, 809, 210, 40), $"{Model.Speed * 2.8f:0.0} 节", numberStyle);
            Label(new Rect(275, 780, 170, 22), "航向", smallStyle);
            Label(new Rect(275, 809, 180, 40), $"{Model.Heading:000}°", numberStyle);
            Label(new Rect(484, 780, 170, 22), "放帆幅度", smallStyle);
            Label(new Rect(484, 809, 190, 40), $"{Model.Sheet * 100:00}%", numberStyle, Gold);
            Panel(new Rect(681, 791, 180, 8), new Color(0.18f, 0.3f, 0.33f));
            Panel(new Rect(681, 791, 180 * Model.Sheet, 8), Gold);
            Label(new Rect(681, 820, 195, 24), "随风向调整帆索", smallStyle);
            Label(new Rect(931, 783, 465, 27), "A / D 转舵   W / S 收放帆", bodyStyle);
            Label(new Rect(931, 821, 465, 25), "空格 暂停   R 重置   H 说明", smallStyle);
            if (Session.State != GameState.Playing || help) DrawOverlay();
            GUI.matrix = previous;
        }
        void DrawOverlay()
        {
            Panel(new Rect(390, 228, 660, 396), Ink); Panel(new Rect(390, 228, 660, 4), Gold);
            string heading = help ? "操作说明" : Session.State == GameState.Paused ? "已暂停" : "扬帆出航";
            Label(new Rect(426, 259, 590, 44), heading, titleStyle);
            Label(new Rect(426, 321, 590, 28), "坐稳船内，自由选择航行方向。", bodyStyle);
            Label(new Rect(426, 367, 590, 26), "A / D 左右转舵　　W / S 收帆 / 放帆", smallStyle);
            Label(new Rect(426, 405, 590, 26), "顺风时放帆，近迎风时收帆。", smallStyle);
            Label(new Rect(426, 443, 590, 26), "正迎风会失速，可用之字换舷调整航向。", smallStyle);
            Rect button = new(426, 535, 360, 51);
            if (GUI.Button(button, help ? "关闭说明  [H]" : Session.State == GameState.Paused ? "继续航行  [Enter]" : "出航  [Enter]", buttonStyle))
            { if (help) help = false; else BeginVoyage(); }
            if (GUI.Button(new Rect(807, 535, 206, 51), "重置  [R]", buttonStyle)) ResetVoyage();
        }
        static void Arrow(Vector2 center, float heading, Color color, float size)
        {
            Vector2 d = new(Mathf.Sin(heading * Mathf.Deg2Rad), -Mathf.Cos(heading * Mathf.Deg2Rad));
            Vector2 side = new(-d.y, d.x), tip = center + d * size;
            PixelLine(center - d * size, tip, color);
            PixelLine(tip, tip - d * 12 + side * 8, color);
            PixelLine(tip, tip - d * 12 - side * 8, color);
        }
        static void PixelLine(Vector2 a, Vector2 b, Color color)
        {
            int steps = Mathf.CeilToInt(Vector2.Distance(a, b) / 2);
            for (int i = 0; i <= steps; i++)
            { Vector2 p = Vector2.Lerp(a, b, steps == 0 ? 0 : (float)i / steps); Panel(new Rect(p.x - 1.5f, p.y - 1.5f, 3, 3), color); }
        }
        void OnDisable()
        {
            // OnDisable runs before assemblies unload, while the managed owner still exists.
            art?.Dispose();
            art = null;
        }
    }
}
