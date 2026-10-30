using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SailingOut
{
    /// <summary>Opt-in standalone acceptance run: -sailingoutSoak OUTPUT_DIRECTORY.</summary>
    public sealed class DemoSoakCapture : MonoBehaviour
    {
        private readonly List<float> frameTimes = new List<float>();
        private readonly HashSet<SpawnCategory> seenCategories = new HashSet<SpawnCategory>();
        private SpawnManager manager;
        private DemoVoyager boat;
        private DemoOverlay overlay;
        private string destination;
        private float started, lastDistance, lastMovement, totalDistance, maxDistance, movingSeconds;
        private float nextSample, nextProgress;
        private int peakActive, peakManaged, warmupAllocated, lastAllocated, restarts;
        private bool ready, reefCaptured, denseCaptured, finished;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnableIfRequested()
        {
            string[] args = Environment.GetCommandLineArgs();
            int flag = Array.IndexOf(args, "-sailingoutSoak");
            if (flag < 0 || flag + 1 >= args.Length) return;
            var capture = new GameObject("Acceptance Capture").AddComponent<DemoSoakCapture>();
            capture.destination = args[flag + 1];
        }

        private IEnumerator Start()
        {
            Directory.CreateDirectory(destination);
            Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            Application.runInBackground = true;
            // Allow the demo's Start methods to finish before using its public controls.
            yield return null;
            manager = FindAnyObjectByType<SpawnManager>();
            boat = FindAnyObjectByType<DemoVoyager>();
            overlay = FindAnyObjectByType<DemoOverlay>();
            if (manager == null || manager.Profile == null || boat == null || overlay == null)
            {
                Finish("Missing demo components");
                yield break;
            }
            overlay.BeginGame();
            started = lastMovement = Time.realtimeSinceStartup;
            lastDistance = boat.DistanceTravelled;
            ready = true;
        }

        private void Update()
        {
            if (!ready || finished) return;
            float now = Time.realtimeSinceStartup;
            float elapsed = now - started;
            float distance = boat.DistanceTravelled;
            float delta = distance - lastDistance;
            if (delta > 0.0001f)
            {
                totalDistance += delta;
                lastMovement = now;
            }
            maxDistance = Mathf.Max(maxDistance, distance);
            lastDistance = distance;
            if (!boat.IsDead && now - lastMovement < 0.25f)
                movingSeconds += Time.unscaledDeltaTime;
            if (boat.IsDead)
            {
                // Exercise normal restart logic and vary the seed instead of disabling damage.
                overlay.Restart(true);
                restarts++;
                lastDistance = boat.DistanceTravelled;
            }
            if (now - lastMovement > 5f)
            {
                Finish("No measured voyage progress for five seconds");
                return;
            }

            peakActive = Mathf.Max(peakActive, manager.ActiveObjectCount);
            peakManaged = Mathf.Max(peakManaged, manager.ManagedObjectCount);
            lastAllocated = manager.transform.childCount;
            if (elapsed < 180f) warmupAllocated = lastAllocated;
            if (elapsed > 10f && Time.unscaledDeltaTime > 0f)
                frameTimes.Add(Time.unscaledDeltaTime);
            if (elapsed >= nextSample)
            {
                nextSample = elapsed + 1f;
                foreach (var identity in manager.GetComponentsInChildren<SpawnableIdentity>())
                    if (identity.Definition != null) seenCategories.Add(identity.Definition.Category);
            }
            if (!boat.IsDead && distance >= 120f && !reefCaptured)
            {
                reefCaptured = true;
                StartCoroutine(CaptureFrame("gameplay-reef.png"));
            }
            if (!boat.IsDead && distance >= 600f && !denseCaptured)
            {
                denseCaptured = true;
                StartCoroutine(CaptureFrame("gameplay-dense.png"));
            }
            if (elapsed >= nextProgress)
            {
                nextProgress = elapsed + 30f;
                File.WriteAllText(Path.Combine(destination, "soak-progress.txt"), Progress(elapsed));
            }
            if (elapsed < 605f) return;
            string failure = totalDistance < 2000f ? "Insufficient measured travel" :
                movingSeconds < 570f ? "Insufficient time in active motion" :
                maxDistance < 600f || !denseCaptured ? "Dense progression was not reached" :
                seenCategories.Count != 6 ? "Not all six generated categories were observed" :
                peakActive > manager.Profile.MaxActiveObjects ||
                peakManaged > manager.Profile.MaxActiveObjects + manager.Profile.MaxPooledObjects
                    ? "Configured object limits exceeded" :
                frameTimes.Count == 0 ? "No frame measurements" : null;
            Finish(failure);
        }

        private string Progress(float elapsed) =>
            $"Elapsed seconds: {elapsed:F2}\nMeasured total distance: {totalDistance:F2}\n" +
            $"Longest voyage: {maxDistance:F2}\nSeconds in active motion: {movingSeconds:F2}\n" +
            $"Death restarts: {restarts}\nObserved categories: {string.Join(", ", seenCategories)}\n";

        private void Finish(string failure)
        {
            if (finished) return;
            finished = true;
            frameTimes.Sort();
            float sum = 0f;
            foreach (float dt in frameTimes) sum += dt;
            string report = $"Result: {(failure == null ? "PASS" : "FAIL")}\nReason: {failure ?? "All activity/content gates passed"}\n" +
                $"Unity: {Application.unityVersion}\nGPU: {SystemInfo.graphicsDeviceName}\n" +
                $"Resolution: {Screen.width}x{Screen.height}\n" + Progress(Time.realtimeSinceStartup - started) +
                $"Measured frames (after 10s): {frameTimes.Count}\n";
            if (frameTimes.Count > 0)
                report += $"Mean FPS: {frameTimes.Count / sum:F2}\nMedian FPS: {1f / frameTimes[frameTimes.Count / 2]:F2}\n" +
                    $"99th percentile frame ms: {frameTimes[(int)(frameTimes.Count * 0.99f)] * 1000f:F2}\n";
            report += $"Peak active: {peakActive}\nPeak managed: {peakManaged}\n" +
                $"Allocated at 180s: {warmupAllocated}\nFinal allocated: {lastAllocated}\n";
            File.WriteAllText(Path.Combine(destination, "soak-report.txt"), report);
            Debug.Log(report);
            Application.Quit(failure == null ? 0 : 1);
        }

        private IEnumerator CaptureFrame(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(destination, name));
        }
    }
}
