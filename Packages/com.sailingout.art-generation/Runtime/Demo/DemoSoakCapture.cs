using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SailingOut
{
    /// <summary>Opt-in standalone acceptance run: -sailingoutSoak OUTPUT_DIRECTORY. Normal Play never enables this.</summary>
    public sealed class DemoSoakCapture : MonoBehaviour
    {
        private readonly List<float> frameTimes = new List<float>();
        private SpawnManager manager;
        private DemoVoyager boat;
        private string destination;
        private float started;
        private int peakActive, peakManaged, warmupAllocated, lastAllocated;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnableIfRequested()
        {
            string[] args = Environment.GetCommandLineArgs();
            int flag = Array.IndexOf(args, "-sailingoutSoak");
            if (flag < 0 || flag + 1 >= args.Length) return;
            var capture = new GameObject("Acceptance Capture").AddComponent<DemoSoakCapture>();
            capture.destination = args[flag+1];
        }

        private IEnumerator Start()
        {
            Directory.CreateDirectory(destination);
            Screen.SetResolution(1920,1080,FullScreenMode.Windowed);
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            Application.runInBackground = true;
            yield return null;
            manager = FindAnyObjectByType<SpawnManager>();
            boat = FindAnyObjectByType<DemoVoyager>();
            started = Time.realtimeSinceStartup;
            StartCoroutine(CaptureAt(45f,"gameplay-reef.png"));
            StartCoroutine(CaptureAt(135f,"gameplay-dense.png"));
            yield return new WaitForSecondsRealtime(605f);
            frameTimes.Sort();
            float sum = 0f;
            foreach (float dt in frameTimes) sum += dt;
            string report = $"Unity: {Application.unityVersion}\nGPU: {SystemInfo.graphicsDeviceName}\n" +
                $"Resolution: {Screen.width}x{Screen.height}\nElapsed seconds: {Time.realtimeSinceStartup-started:F2}\n" +
                $"Distance: {boat.transform.position.y:F2}\nMeasured frames (after 10s): {frameTimes.Count}\n" +
                $"Mean FPS: {frameTimes.Count/sum:F2}\nMedian FPS: {1f/frameTimes[frameTimes.Count/2]:F2}\n" +
                $"99th percentile frame ms: {frameTimes[(int)(frameTimes.Count*0.99f)]*1000f:F2}\n" +
                $"Peak active: {peakActive}\nPeak managed: {peakManaged}\n" +
                $"Allocated at 180s: {warmupAllocated}\nFinal allocated: {lastAllocated}\n";
            File.WriteAllText(Path.Combine(destination,"soak-report.txt"),report);
            Debug.Log(report);
            Application.Quit();
        }

        private void Update()
        {
            if (manager == null) return;
            float elapsed = Time.realtimeSinceStartup-started;
            peakActive = Mathf.Max(peakActive,manager.ActiveObjectCount);
            peakManaged = Mathf.Max(peakManaged,manager.ManagedObjectCount);
            lastAllocated = manager.transform.childCount;
            if (elapsed < 180f) warmupAllocated=lastAllocated;
            if (elapsed > 10f) frameTimes.Add(Time.unscaledDeltaTime);
        }

        private IEnumerator CaptureAt(float seconds,string name)
        {
            yield return new WaitForSecondsRealtime(seconds);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(destination,name));
        }
    }
}
