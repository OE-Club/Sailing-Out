using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SailingOut.Editor
{
    public static class DeliveryBuilder
    {
        private const string Scene = "Assets/SailingOut/Generated/Demo/SailingOutGenerationDemo.unity";

        public static void BuildWindowsDemo()
        {
            string destination = Environment.GetEnvironmentVariable("SAILINGOUT_BUILD_PATH");
            if (string.IsNullOrEmpty(destination))
                destination = "Builds/SailingOutDemo/SailingOutDemo.exe";
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destination)));
            var target = UnityEditor.Build.NamedBuildTarget.Standalone;
            ScriptingImplementation previous = PlayerSettings.GetScriptingBackend(target);
            try
            {
                PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.Mono2x);
                BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { Scene },
                    locationPathName = destination,
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.Development
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new Exception("Demo build failed: " + report.summary.result);
                Debug.Log($"Sailing Out Windows demo built: {report.summary.totalSize} bytes.");
            }
            finally { PlayerSettings.SetScriptingBackend(target, previous); }
        }

        // Run in the isolated staging project where the module scripts live under Assets.
        public static void ExportStandaloneAssets()
        {
            string destination = Environment.GetEnvironmentVariable("SAILINGOUT_EXPORT_PATH");
            if (string.IsNullOrEmpty(destination))
                throw new Exception("Set SAILINGOUT_EXPORT_PATH for the delivery artifact.");
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
#pragma warning disable CS0618 // Stable Unity 6 API, including the originally requested 6000.6.1 editor.
            AssetDatabase.ExportPackage("Assets/SailingOut", destination, ExportPackageOptions.Recurse);
#pragma warning restore CS0618
            Debug.Log("Sailing Out standalone resource package exported to " + destination);
        }

        public static void ValidateImportedDemo()
        {
            EditorSceneManager.OpenScene(Scene);
            var manager = UnityEngine.Object.FindAnyObjectByType<SpawnManager>();
            if (manager == null || manager.Profile == null || manager.Profile.Definitions.Count != 6)
                throw new Exception("Imported demo is missing its generation configuration.");
            foreach (SpawnableDefinition definition in manager.Profile.Definitions)
                foreach (GameObject prefab in definition.Variants)
                    if (prefab == null || prefab.GetComponent<SpawnableIdentity>()?.Definition == null)
                        throw new Exception("Missing imported prefab or definition reference.");
            Debug.Log("Sailing Out clean import validation passed, including scene and definition references.");
        }
    }
}
