using UnityEngine;

namespace SailingOut
{
    /// <summary>Isolated demo presentation. Render at 384x216 and display with integer Point scaling.</summary>
    [RequireComponent(typeof(Camera))]
    public sealed class DemoPixelView : MonoBehaviour
    {
        private Camera worldCamera;
        private RenderTexture pixels;
        private GameObject presentation;

        private void OnEnable()
        {
            worldCamera = GetComponent<Camera>();
            pixels = new RenderTexture(384, 216, 16) { filterMode = FilterMode.Point, name = "Sailing Out Pixels" };
            pixels.Create();
            worldCamera.targetTexture = pixels;
            // A camera must target the display as well, otherwise the standalone
            // player has no backbuffer frame on which to draw its IMGUI presentation.
            presentation = new GameObject("Pixel Presentation", typeof(Camera));
            presentation.transform.SetParent(transform, false);
            Camera display = presentation.GetComponent<Camera>();
            display.depth = worldCamera.depth + 1f;
            display.cullingMask = 0;
            display.clearFlags = CameraClearFlags.SolidColor;
            display.backgroundColor = Color.black;
            display.allowHDR = false;
            display.allowMSAA = false;
        }

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || pixels == null)
                return;
            GUI.depth = 100;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.blackTexture);
            float scale = Mathf.Max(1f, Mathf.Floor(Mathf.Min(Screen.width / 384f, Screen.height / 216f)));
            Rect area = new Rect((Screen.width - 384f * scale) * 0.5f,
                (Screen.height - 216f * scale) * 0.5f, 384f * scale, 216f * scale);
            GUI.DrawTexture(area, pixels, ScaleMode.StretchToFill, false);
        }

        private void OnDisable()
        {
            if (worldCamera != null)
                worldCamera.targetTexture = null;
            if (pixels != null)
            {
                pixels.Release();
                Destroy(pixels);
            }
            if (presentation != null)
                Destroy(presentation);
        }
    }
}
