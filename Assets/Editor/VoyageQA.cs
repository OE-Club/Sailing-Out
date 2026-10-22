using System;
using System.IO;
using SailingOut.Sailing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SailingOut.Editor
{
    [InitializeOnLoad]
    public static class VoyageQA
    {
        const string Key = "SailingOut.FreeSailingQA";
        static double started, phaseStarted;
        static int phase;
        static bool orientationApplied;
        static string pendingCapture;
        static SailingVoyage voyage;
        static Func<bool> confirmSceneSave = EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo;
        static Vector2 pausedPosition;
        static string folder => Path.GetFullPath("Logs/FreeSailingQA");
        static VoyageQA() { EditorApplication.playModeStateChanged += Changed; }
        [MenuItem("Sailing Out/Run seated free sailing QA")]
        public static void Start()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            { Debug.LogWarning("Stop Play Mode before starting voyage QA."); return; }
            if (!confirmSceneSave()) return;
            ResetRunState();
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "interactive.txt"), "Started seated free sailing Play Mode QA\n");
            SessionState.SetBool(Key, true);
            EditorSceneManager.OpenScene("Assets/Scenes/FreeSailing.unity");
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            if (EditorWindow.focusedWindow != null) EditorWindow.focusedWindow.maximized = true;
            EditorApplication.EnterPlaymode();
        }
        static void Changed(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key, false))
            {
                ResetRunState();
                EditorApplication.update -= Tick;
                EditorApplication.update += Tick;
            }
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                EditorApplication.update -= Tick;
                SessionState.SetBool(Key, false);
            }
        }
        static void ResetRunState()
        {
            voyage = null;
            pendingCapture = null;
            pausedPosition = default;
            orientationApplied = false;
            phase = 0;
            started = phaseStarted = EditorApplication.timeSinceStartup;
        }
        static void Log(string text) { File.AppendAllText(Path.Combine(folder, "interactive.txt"), text + "\n"); Debug.Log("Free Sailing QA: " + text); }
        static void Check(bool ok, string text) { if (!ok) throw new Exception(text); Log("PASS " + text); }
        static void Capture(string name)
        {
            pendingCapture = Path.Combine(folder, name + ".png");
            if (File.Exists(pendingCapture)) File.Delete(pendingCapture);
            ScreenCapture.CaptureScreenshot(pendingCapture);
        }
        static void Next() { phase++; orientationApplied = false; phaseStarted = EditorApplication.timeSinceStartup; }
        static void Tick()
        {
            try
            {
                if (!EditorApplication.isPlaying) return;
                if (EditorApplication.timeSinceStartup - started > 40) throw new Exception("QA timed out.");
                if (voyage == null) voyage = UnityEngine.Object.FindAnyObjectByType<SailingVoyage>();
                if (voyage == null) return;
                if (pendingCapture != null)
                { if (!File.Exists(pendingCapture)) return; pendingCapture = null; }
                double elapsed = EditorApplication.timeSinceStartup - phaseStarted;
                switch (phase)
                {
                    case 0:
                        if (elapsed < 1) return;
                        voyage.BeginVoyage(); voyage.SetControlInput(true); voyage.Model.Reset(Vector2.zero, 0); Next(); break;
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                        if (!orientationApplied)
                        { voyage.Model.Reset(Vector2.zero, (phase - 1) * 90); orientationApplied = true; phaseStarted = EditorApplication.timeSinceStartup; return; }
                        if (elapsed < 0.6) return;
                        Capture(new[] { "01-seated-north", "02-seated-east", "03-seated-south", "04-seated-west" }[phase - 1]);
                        Next(); break;
                    case 5:
                        if (!orientationApplied)
                        { voyage.ResetVoyage(); voyage.BeginVoyage(); voyage.SetControlInput(true); orientationApplied = true; phaseStarted = EditorApplication.timeSinceStartup; return; }
                        if (elapsed < 3) return;
                        Check(voyage.Model.Speed > 1.5f, "free sailing accelerates under wind"); Capture("05-free-sailing");
                        voyage.SetControlInput(true, 1, -1); Next(); break;
                    case 6:
                        if (elapsed < 1.8) return;
                        Check(voyage.Model.Heading > 30 && voyage.Model.Sheet < 0.1f, "rudder and sheet-in affect vessel");
                        voyage.TogglePause(); pausedPosition = voyage.Model.Position; Next(); break;
                    case 7:
                        if (elapsed < 0.6) return;
                        Check(voyage.Model.Position == pausedPosition, "pause holds vessel position"); Capture("06-paused");
                        Next(); break;
                    case 8:
                        if (!orientationApplied)
                        {
                        voyage.ResetVoyage(); voyage.ResetVoyage();
                        Check(voyage.Model.Speed == 0 && voyage.Model.Position == Vector2.zero, "repeated reset restores spawn and clears speed");
                        voyage.BeginVoyage(); voyage.SetControlInput(true); voyage.Model.Reset(Vector2.zero, 245);
                        orientationApplied = true; phaseStarted = EditorApplication.timeSinceStartup; return;
                        }
                        if (elapsed < 1) return;
                        Check(voyage.Model.InIrons && voyage.Model.Speed < 0.01f, "headwind stalls free sailing");
                        bool noRoute = true;
                        foreach (Transform t in voyage.GetComponentsInChildren<Transform>())
                            noRoute &= !t.name.Contains("buoy") && !t.name.Contains("Gate") && !t.name.Contains("Target");
                        Check(noRoute, "scene contains no route targets or gate objects");
                        Log("COMPLETE: actual FreeSailing Play Mode, four seated directions captured, no course or completion timer.");
                        voyage.ResetVoyage(); voyage.SetControlInput(false); Next(); break;
                    case 9:
                        if (elapsed < 0.6) return;
                        Capture("07-ready-chinese"); SessionState.SetBool(Key, false); EditorApplication.update -= Tick; break;
                }
            }
            catch (Exception e) { Log("FAIL " + e.Message); SessionState.SetBool(Key, false); EditorApplication.update -= Tick; }
        }
    }
}
