using System.Collections.Generic;
using UnityEngine;

namespace SailingOut.Sailing
{
    /// <summary>Small deterministic pixel drawings; owns and releases only the textures it creates.</summary>
    public sealed class VoyagePixelArt
    {
        readonly List<Object> owned = new();
        public Sprite Hull { get; }
        public Sprite Sail { get; }
        public Sprite Seat { get; }
        public Sprite Foam { get; }
        public Sprite Wake { get; }
        public Sprite[] Crew { get; }

        public VoyagePixelArt()
        {
            Hull = Draw(56, 112, 24, (x, y) =>
            {
                float width = y < 20 ? 19f : Mathf.Lerp(23f, 0f, Mathf.Pow((y - 20) / 92f, 2.4f));
                float edge = width - Mathf.Abs(x - 27.5f);
                if (edge < 0) return Color.clear;
                if (edge < 2) return C(12, 37, 43);
                if (edge < 4) return C(234, 211, 161);
                if (y < 7) return C(29, 66, 71);
                if (x % 9 == 0 || y % 19 == 0) return C(131, 84, 50);
                if (y > 38 && y < 65 && x > 18 && x < 37) return C(28, 76, 87);
                return (x / 9) % 2 == 0 ? C(190, 139, 83) : C(205, 155, 93);
            });
            Sail = Draw(56, 76, 24, (x, y) =>
            {
                if (x < 3 || y < 3 || x > (72 - y) * 0.7f || y > 71) return Color.clear;
                if (x < 5 || y < 5 || x > (72 - y) * 0.7f - 2) return C(72, 86, 79);
                if (y > 46 && y < 52) return C(218, 154, 89);
                return x < 12 ? C(229, 225, 192) : C(255, 247, 215);
            }, Vector2.zero);
            Seat = Draw(36, 10, 24, (x, y) =>
            {
                if (x < 2 || x > 33) return Color.clear;
                if (y < 2 || y > 8 || x < 4 || x > 31) return C(77, 57, 39);
                return y < 5 ? C(170, 112, 66) : C(230, 181, 114);
            });
            Foam = Draw(22, 6, 24, (x, y) => (y == 2 && x > 3 && x < 18) || (y == 3 && x > 11 && x < 21) ? C(112, 190, 183, 145) : Color.clear);
            Wake = Draw(64, 90, 24, (x, y) =>
            {
                float edge = 6f + (90 - y) * 0.28f;
                return Mathf.Abs(Mathf.Abs(x - 32) - edge) < 1.8f && y % 13 < 7 ? C(190, 229, 201, 150) : Color.clear;
            });
            Crew = new Sprite[4];
            Texture2D sheet = Resources.Load<Texture2D>("Voyage/CrewSeated");
            if (sheet == null) throw new System.InvalidOperationException("Required seated crew texture is missing: Resources/Voyage/CrewSeated");
            // Pivots locate each seated pelvis, read from the actual 1254x1254 draft; no pose deformation.
            Vector2[] hip = { new(0.50f, 0.345f), new(0.43f, 0.345f), new(0.50f, 0.407f), new(0.535f, 0.38f) };
            for (int i = 0; i < 4; i++)
            {
                int cw = sheet.width / 2, ch = sheet.height / 2;
                Crew[i] = Sprite.Create(sheet, new Rect(i % 2 * cw, (i < 2 ? 1 : 0) * ch, cw, ch), hip[i], ch / 1.5f);
                Crew[i].name = new[] { "Seated North", "Seated East", "Seated South", "Seated West" }[i];
                owned.Add(Crew[i]);
            }
        }

        Sprite Draw(int w, int h, float ppu, System.Func<int, int, Color> pixel, Vector2? pivot = null)
        {
            Texture2D t = new(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            Color[] colors = new Color[w * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) colors[y * w + x] = pixel(x, y);
            t.SetPixels(colors); t.Apply(); owned.Add(t);
            Sprite s = Sprite.Create(t, new Rect(0, 0, w, h), pivot ?? new Vector2(0.5f, 0.5f), ppu);
            owned.Add(s); return s;
        }
        public void Dispose() { foreach (Object o in owned) if (o != null) Object.Destroy(o); owned.Clear(); }
        public static Color C(byte r, byte g, byte b, byte a = 255) => new Color32(r, g, b, a);
    }
}
