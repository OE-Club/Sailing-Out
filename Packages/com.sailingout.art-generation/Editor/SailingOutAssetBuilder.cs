using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SailingOut.Editor
{
    public sealed class SailingOutAssetBuilder : EditorWindow
    {
        private const string DefaultSourceFolder = "Assets/SailingOut/Art/Sprites";
        private const string DefaultOutputFolder = "Assets/SailingOut/Generated";
        private static readonly Regex SpriteName = new Regex(
            "^(buoy|reef|seabird|supply|decoration)_([0-9]{2})_([0-9]{2})$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static readonly Regex TiledSpriteName = new Regex(
            "^(water_tile|foam_tile)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        [SerializeField] private DefaultAsset sourceFolder;
        [SerializeField] private DefaultAsset outputFolder;
        [SerializeField] private Sprite demoBoatSprite;
        [SerializeField] private Sprite waterTileSprite;
        [SerializeField] private Sprite foamTileSprite;
        [SerializeField] private int defaultSeed = 314159;

        [MenuItem("Sailing Out/Build Art Package and Demo")]
        private static void OpenWindow()
        {
            SailingOutAssetBuilder window = GetWindow<SailingOutAssetBuilder>(true, "Sailing Out Builder");
            window.minSize = new Vector2(500f, 310f);
            window.Show();
        }

        private void OnEnable()
        {
            sourceFolder ??= AssetDatabase.LoadAssetAtPath<DefaultAsset>(DefaultSourceFolder);
            outputFolder ??= AssetDatabase.LoadAssetAtPath<DefaultAsset>(DefaultOutputFolder);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Pixel Art Package Builder", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Use names such as reef_01_01.png. Categories: buoy, reef, seabird, supply, decoration. " +
                "Variant and frame numbers must use two digits. Add water_tile.png and foam_tile.png, " +
                "then select those and the project boat Sprite for the demo.",
                MessageType.Info);

            sourceFolder = (DefaultAsset)EditorGUILayout.ObjectField("Source sprite folder", sourceFolder,
                typeof(DefaultAsset), false);
            outputFolder = (DefaultAsset)EditorGUILayout.ObjectField("Generated output folder", outputFolder,
                typeof(DefaultAsset), false);
            demoBoatSprite = (Sprite)EditorGUILayout.ObjectField("Demo boat sprite", demoBoatSprite,
                typeof(Sprite), false);
            waterTileSprite = (Sprite)EditorGUILayout.ObjectField("Water tile sprite", waterTileSprite,
                typeof(Sprite), false);
            foamTileSprite = (Sprite)EditorGUILayout.ObjectField("Foam tile sprite", foamTileSprite,
                typeof(Sprite), false);
            defaultSeed = EditorGUILayout.IntField("Demo seed", defaultSeed);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Expected first delivery", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Water and foam: seamless tile textures");
            EditorGUILayout.LabelField("Buoy: 3 variants x 2 frames");
            EditorGUILayout.LabelField("Reef: 3 variants x 1 frame");
            EditorGUILayout.LabelField("Seabird: 3 variants x 4 frames");
            EditorGUILayout.LabelField("Supply: 3 variants x 2 frames");
            EditorGUILayout.LabelField("Decoration: 3 static variants");

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(sourceFolder == null || outputFolder == null || demoBoatSprite == null ||
                waterTileSprite == null || foamTileSprite == null))
            {
                if (GUILayout.Button("Validate and Build", GUILayout.Height(34f)))
                    Build();
            }
        }

        private void Build()
        {
            string sourcePath = AssetDatabase.GetAssetPath(sourceFolder);
            string outputPath = AssetDatabase.GetAssetPath(outputFolder);
            if (!AssetDatabase.IsValidFolder(sourcePath) || !AssetDatabase.IsValidFolder(outputPath))
            {
                EditorUtility.DisplayDialog("Sailing Out", "Choose valid folders under Assets.", "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            BuildAssets(sourcePath, outputPath, demoBoatSprite, waterTileSprite, foamTileSprite, defaultSeed);
        }

        // Entry point for batchmode, also used for rebuilding the checked-in demo.
        [MenuItem("Sailing Out/Rebuild Included Demo")]
        public static void BuildDefault()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            PrepareSourceTextures(DefaultSourceFolder);
            string art = "Assets/SailingOut/Art/";
            PrepareTexture(art + "Textures/water_tile.png", true);
            PrepareTexture(art + "Textures/foam_tile.png", true);
            PrepareTexture(art + "Demo/boat_demo.png", false);
            BuildAssets(DefaultSourceFolder, DefaultOutputFolder,
                AssetDatabase.LoadAssetAtPath<Sprite>(art + "Demo/boat_demo.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>(art + "Textures/water_tile.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>(art + "Textures/foam_tile.png"), 314159);
        }

        private static void BuildAssets(string sourcePath, string outputPath, Sprite boat, Sprite water, Sprite foam, int seed)
        {
            if (boat == null || water == null || foam == null)
                throw new InvalidOperationException("Boat, water and foam sprites are required.");
            try
            {
                EditorUtility.DisplayProgressBar("Sailing Out", "Applying pixel settings", 0.1f);
                PrepareSourceTextures(sourcePath);
                AssetDatabase.Refresh();
                IReadOnlyList<VariantFrames> variants = LoadAndValidate(sourcePath);
                if (variants == null)
                    return;

                ApplyPixelSettings(variants.SelectMany(item => item.Frames));
                AssetDatabase.Refresh();
                variants = LoadAndValidate(sourcePath);
                if (variants == null)
                    return;

                EnsureSubfolders(outputPath);
                EditorUtility.DisplayProgressBar("Sailing Out", "Creating prefabs and animations", 0.35f);
                Dictionary<SpawnCategory, SpawnableDefinition> definitions = BuildDefinitions(variants, outputPath);
                EditorUtility.DisplayProgressBar("Sailing Out", "Creating generation profile", 0.72f);
                GenerationProfile profile = BuildProfile(definitions.Values, outputPath, seed);
                EditorUtility.DisplayProgressBar("Sailing Out", "Creating demo scene", 0.9f);
                BuildDemoScene(profile, outputPath, boat, water, foam);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("Sailing Out: built all prefabs, animations, definitions and the demo scene.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static IReadOnlyList<VariantFrames> LoadAndValidate(string sourcePath)
        {
            Dictionary<(SpawnCategory, int), List<Sprite>> groups = new Dictionary<(SpawnCategory, int), List<Sprite>>();
            List<string> invalidNames = new List<string>();

            foreach (string guid in AssetDatabase.FindAssets("t:Sprite", new[] { sourcePath }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string assetName = Path.GetFileNameWithoutExtension(path);
                if (TiledSpriteName.IsMatch(assetName))
                    continue;
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null)
                    continue;

                Match match = SpriteName.Match(sprite.name);
                if (!match.Success || !TryCategory(match.Groups[1].Value, out SpawnCategory category))
                {
                    invalidNames.Add(sprite.name);
                    continue;
                }

                int variant = int.Parse(match.Groups[2].Value);
                var key = (category, variant);
                if (!groups.TryGetValue(key, out List<Sprite> frames))
                {
                    frames = new List<Sprite>();
                    groups.Add(key, frames);
                }

                frames.Add(sprite);
            }

            List<string> errors = new List<string>();
            foreach (SpawnCategory category in Enum.GetValues(typeof(SpawnCategory)))
            {
                int expectedFrames = ExpectedFrames(category);
                for (int variant = 1; variant <= 3; variant++)
                {
                    if (!groups.TryGetValue((category, variant), out List<Sprite> frames))
                    {
                        errors.Add($"Missing {category.ToString().ToLowerInvariant()}_{variant:00}_01.png");
                        continue;
                    }

                    frames.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
                    if (frames.Count != expectedFrames)
                    {
                        errors.Add($"{category} variant {variant:00}: expected {expectedFrames} frame(s), found {frames.Count}.");
                        continue;
                    }

                    Vector2 frameSize = frames[0].rect.size;
                    for (int frame = 1; frame <= expectedFrames; frame++)
                    {
                        string expectedName = $"{category.ToString().ToLowerInvariant()}_{variant:00}_{frame:00}";
                        Sprite sprite = frames[frame - 1];
                        if (!string.Equals(sprite.name, expectedName, StringComparison.OrdinalIgnoreCase))
                            errors.Add($"{category} variant {variant:00}: expected frame {expectedName}.png.");
                        if (sprite.rect.size != frameSize)
                            errors.Add($"{category} variant {variant:00}: every animation frame must use the same dimensions.");
                    }
                }
            }

            if (invalidNames.Count > 0)
                Debug.LogWarning("Sailing Out ignored sprites with unsupported names: " + string.Join(", ", invalidNames));

            if (errors.Count > 0)
            {
                throw new InvalidOperationException("Sprite validation failed:\n" + string.Join("\n", errors));
            }

            return groups.Select(pair => new VariantFrames(pair.Key.Item1, pair.Key.Item2, pair.Value))
                .OrderBy(item => item.Category).ThenBy(item => item.Variant).ToList();
        }

        private static void ApplyPixelSettings(IEnumerable<Sprite> sprites)
        {
            foreach (string path in sprites.Select(AssetDatabase.GetAssetPath).Distinct())
            {
                if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
                    continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 16f;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                TextureImporterSettings settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.spritePivot = new Vector2(0.5f, 0.5f);
                importer.SaveAndReimport();
            }
        }

        private static void PrepareSourceTextures(string sourcePath)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { sourcePath }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(path);
                if ((!SpriteName.IsMatch(fileName) && !TiledSpriteName.IsMatch(fileName)) ||
                    !(AssetImporter.GetAtPath(path) is TextureImporter importer))
                    continue;

                PrepareTexture(path, TiledSpriteName.IsMatch(fileName));
            }
        }

        internal static void PrepareTexture(string path, bool repeat)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
                throw new InvalidOperationException("Missing texture: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 16f;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.spritePivot = Vector2.one * 0.5f;
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private static Dictionary<SpawnCategory, SpawnableDefinition> BuildDefinitions(
            IReadOnlyList<VariantFrames> variants,
            string outputPath)
        {
            string prefabFolder = outputPath + "/Prefabs";
            string animationFolder = outputPath + "/Animations";
            string configFolder = outputPath + "/Configs";
            Dictionary<SpawnCategory, List<GameObject>> prefabs = new Dictionary<SpawnCategory, List<GameObject>>();

            foreach (VariantFrames variant in variants)
            {
                string baseName = $"{variant.Category}_{variant.Variant:00}";
                GameObject root = new GameObject(baseName);
                SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
                renderer.sprite = variant.Frames[0];
                renderer.sortingOrder = variant.Category == SpawnCategory.Seabird ? 5 : 1;
                // Built-in layers survive importing into a clean project without TagManager edits.
                root.layer = variant.Category == SpawnCategory.Decoration || variant.Category == SpawnCategory.Buoy ||
                    variant.Category == SpawnCategory.Seabird ? LayerMask.NameToLayer("Ignore Raycast") : 0;
                AddCollider(root, variant.Category, variant.Frames[0]);

                if (variant.Frames.Count > 1)
                {
                    AnimationClip clip = CreateAnimationClip(variant.Frames,
                        $"{animationFolder}/{baseName}.anim", variant.Category == SpawnCategory.Seabird ? 8f : 4f);
                    string controllerPath = $"{animationFolder}/{baseName}.controller";
                    AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
                    if (controller == null)
                        controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                    AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
                    AnimatorState state = stateMachine.states.Select(child => child.state)
                        .FirstOrDefault(candidate => candidate.name == baseName);
                    if (state == null)
                        state = stateMachine.AddState(baseName);
                    state.motion = clip;
                    stateMachine.defaultState = state;
                    EditorUtility.SetDirty(controller);
                    root.AddComponent<Animator>().runtimeAnimatorController = controller;
                    root.AddComponent<PooledAnimatorReset>();
                }

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{prefabFolder}/{baseName}.prefab");
                DestroyImmediate(root);
                if (!prefabs.TryGetValue(variant.Category, out List<GameObject> categoryPrefabs))
                {
                    categoryPrefabs = new List<GameObject>();
                    prefabs.Add(variant.Category, categoryPrefabs);
                }
                categoryPrefabs.Add(prefab);
            }

            Dictionary<SpawnCategory, SpawnableDefinition> definitions = new Dictionary<SpawnCategory, SpawnableDefinition>();
            foreach (KeyValuePair<SpawnCategory, List<GameObject>> pair in prefabs)
            {
                string assetPath = $"{configFolder}/{pair.Key}Definition.asset";
                SpawnableDefinition definition = AssetDatabase.LoadAssetAtPath<SpawnableDefinition>(assetPath);
                if (definition == null)
                {
                    definition = CreateInstance<SpawnableDefinition>();
                    AssetDatabase.CreateAsset(definition, assetPath);
                }

                SerializedObject serialized = new SerializedObject(definition);
                serialized.FindProperty("category").enumValueIndex = (int)pair.Key;
                serialized.FindProperty("prefab").objectReferenceValue = pair.Value[0];
                SerializedProperty variantProperty = serialized.FindProperty("variants");
                variantProperty.arraySize = pair.Value.Count;
                for (int i = 0; i < pair.Value.Count; i++)
                    variantProperty.GetArrayElementAtIndex(i).objectReferenceValue = pair.Value[i];
                serialized.FindProperty("weight").floatValue = 1f;
                serialized.FindProperty("minimumDistance").floatValue = pair.Key == SpawnCategory.Reef ? 0.5f : 0.25f;
                float radius = 0f;
                foreach (GameObject prefab in pair.Value)
                    radius = Mathf.Max(radius, prefab.GetComponent<SpriteRenderer>().sprite.bounds.extents.magnitude);
                serialized.FindProperty("clearanceRadius").floatValue = radius;
                serialized.FindProperty("collisionBoundaryPadding").floatValue = 0.125f;
                serialized.FindProperty("colliderKind").enumValueIndex = (int)ColliderKind(pair.Key);
                serialized.FindProperty("blocksPath").boolValue = pair.Key == SpawnCategory.Reef;
                serialized.FindProperty("collectible").boolValue = pair.Key == SpawnCategory.Supply;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(definition);

                for (int i = 0; i < pair.Value.Count; i++)
                    AttachIdentity(pair.Value[i], definition);
                definitions.Add(pair.Key, definition);
            }

            return definitions;
        }

        private static void AddCollider(GameObject root, SpawnCategory category, Sprite sprite)
        {
            if (category != SpawnCategory.Reef && category != SpawnCategory.Supply)
                return;

            CircleCollider2D collider = root.AddComponent<CircleCollider2D>();
            Vector2 size = sprite.bounds.size;
            collider.radius = Mathf.Max(0.1f, Mathf.Min(size.x, size.y) * 0.4f);
            collider.isTrigger = category == SpawnCategory.Supply;
        }

        private static AnimationClip CreateAnimationClip(IReadOnlyList<Sprite> frames, string path, float fps)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            bool isNew = clip == null;
            if (isNew)
                clip = new AnimationClip();
            clip.frameRate = fps;
            clip.name = Path.GetFileNameWithoutExtension(path);
            EditorCurveBinding binding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = string.Empty,
                propertyName = "m_Sprite"
            };
            ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[frames.Count + 1];
            for (int i = 0; i < frames.Count; i++)
                keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = frames[i] };
            keys[frames.Count] = new ObjectReferenceKeyframe { time = frames.Count / fps, value = frames[0] };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            if (isNew)
                AssetDatabase.CreateAsset(clip, path);
            else
                EditorUtility.SetDirty(clip);
            return clip;
        }

        private static void AttachIdentity(GameObject prefab, SpawnableDefinition definition)
        {
            string path = AssetDatabase.GetAssetPath(prefab);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            SpawnableIdentity identity = root.GetComponent<SpawnableIdentity>() ?? root.AddComponent<SpawnableIdentity>();
            SerializedObject serialized = new SerializedObject(identity);
            serialized.FindProperty("definition").objectReferenceValue = definition;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static GenerationProfile BuildProfile(IEnumerable<SpawnableDefinition> definitions, string outputPath,
            int seed)
        {
            string assetPath = outputPath + "/Configs/DefaultGenerationProfile.asset";
            GenerationProfile profile = AssetDatabase.LoadAssetAtPath<GenerationProfile>(assetPath);
            if (profile == null)
            {
                profile = CreateInstance<GenerationProfile>();
                AssetDatabase.CreateAsset(profile, assetPath);
            }

            SerializedObject serialized = new SerializedObject(profile);
            serialized.FindProperty("segmentLengthScreens").floatValue = 2f;
            serialized.FindProperty("generateAheadScreens").floatValue = 2.5f;
            serialized.FindProperty("recycleBehindScreens").floatValue = 1.5f;
            serialized.FindProperty("horizontalEdgeBuffer").floatValue = 0.75f;
            serialized.FindProperty("safeLaneMinimumWidth").floatValue = 2.5f;
            serialized.FindProperty("safeLaneSideBuffer").floatValue = 0.5f;
            serialized.FindProperty("maximumLaneOffset").floatValue = 4.5f;
            serialized.FindProperty("minimumObjectSeparation").floatValue = 0.5f;
            serialized.FindProperty("placementAttempts").intValue = 28;
            serialized.FindProperty("maxActiveObjects").intValue = 256;
            serialized.FindProperty("maxPooledObjects").intValue = 256;
            serialized.FindProperty("seed").intValue = seed;
            serialized.FindProperty("randomizeSeedOnStart").boolValue = false;

            SpawnableDefinition[] definitionArray = definitions.OrderBy(item => item.Category).ToArray();
            SerializedProperty definitionProperty = serialized.FindProperty("definitions");
            definitionProperty.arraySize = definitionArray.Length;
            for (int i = 0; i < definitionArray.Length; i++)
                definitionProperty.GetArrayElementAtIndex(i).objectReferenceValue = definitionArray[i];

            ConfigureStages(serialized.FindProperty("stages"));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void ConfigureStages(SerializedProperty stages)
        {
            float[] starts = { 0f, 120f, 300f, 600f };
            int[,] ranges =
            {
                { 2, 4, 0, 0, 1, 2, 0, 0, 0, 0 },
                { 2, 4, 1, 2, 1, 2, 1, 2, 1, 2 },
                { 3, 5, 2, 4, 2, 3, 1, 2, 1, 3 },
                { 4, 6, 4, 6, 2, 4, 1, 2, 1, 3 }
            };

            SpawnCategory[] categories =
            {
                SpawnCategory.Buoy, SpawnCategory.Reef, SpawnCategory.Seabird,
                SpawnCategory.Supply, SpawnCategory.Decoration
            };
            stages.arraySize = starts.Length;
            for (int stageIndex = 0; stageIndex < starts.Length; stageIndex++)
            {
                SerializedProperty stage = stages.GetArrayElementAtIndex(stageIndex);
                stage.FindPropertyRelative("startDistance").floatValue = starts[stageIndex];
                stage.FindPropertyRelative("obstacleDensityMultiplier").floatValue = 1f;
                SerializedProperty counts = stage.FindPropertyRelative("counts");
                counts.arraySize = categories.Length;
                for (int categoryIndex = 0; categoryIndex < categories.Length; categoryIndex++)
                {
                    SerializedProperty count = counts.GetArrayElementAtIndex(categoryIndex);
                    count.FindPropertyRelative("category").enumValueIndex = (int)categories[categoryIndex];
                    count.FindPropertyRelative("minimum").intValue = ranges[stageIndex, categoryIndex * 2];
                    count.FindPropertyRelative("maximum").intValue = ranges[stageIndex, categoryIndex * 2 + 1];
                }
            }
        }

        private static void BuildDemoScene(GenerationProfile profile, string outputPath, Sprite boatSprite,
            Sprite waterTile, Sprite foamTile)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(DemoCameraFollow));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            cameraObject.AddComponent<DemoPixelView>();
            camera.orthographic = true;
            // 384 x 216 logical pixels at 16 PPU, scaled 5x at 1920 x 1080.
            camera.orthographicSize = 6.75f;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.015f, 0.18f, 0.27f, 1f);
            cameraObject.transform.position = new Vector3(0f, 3f, -10f);
            GameObject background = new GameObject("Tiled Sea Background");
            background.transform.SetParent(cameraObject.transform, false);
            background.transform.localPosition = new Vector3(0f, 0f, 10f);
            DemoTiledBackground sea = background.AddComponent<DemoTiledBackground>();
            SerializedObject seaSettings = new SerializedObject(sea);
            seaSettings.FindProperty("waterTile").objectReferenceValue = waterTile;
            seaSettings.FindProperty("foamTile").objectReferenceValue = foamTile;
            seaSettings.ApplyModifiedPropertiesWithoutUndo();

            GameObject player = new GameObject("Demo Boat", typeof(BoxCollider2D), typeof(Rigidbody2D),
                typeof(DemoVoyager));
            if (boatSprite != null)
            {
                SpriteRenderer renderer = player.AddComponent<SpriteRenderer>();
                renderer.sprite = boatSprite;
                renderer.sortingOrder = 3;
            }
            BoxCollider2D playerCollider = player.GetComponent<BoxCollider2D>();
            playerCollider.size = new Vector2(0.65f, 1.35f);
            Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();
            playerBody.gravityScale = 0f;
            playerBody.constraints = RigidbodyConstraints2D.FreezeRotation;
            playerBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            playerBody.interpolation = RigidbodyInterpolation2D.Interpolate;
            cameraObject.GetComponent<DemoCameraFollow>().Target = player.transform;

            GameObject managerObject = new GameObject("Spawn Manager", typeof(SpawnManager));
            SerializedObject manager = new SerializedObject(managerObject.GetComponent<SpawnManager>());
            manager.FindProperty("profile").objectReferenceValue = profile;
            manager.FindProperty("worldCamera").objectReferenceValue = camera;
            manager.FindProperty("playerCollider").objectReferenceValue = playerCollider;
            manager.FindProperty("initializeOnStart").boolValue = true;
            manager.ApplyModifiedPropertiesWithoutUndo();
            managerObject.AddComponent<DemoOverlay>();

            string path = outputPath + "/Demo/SailingOutGenerationDemo.unity";
            EditorSceneManager.SaveScene(scene, path);
        }

        private static void EnsureSubfolders(string outputPath)
        {
            foreach (string name in new[] { "Prefabs", "Animations", "Configs", "Demo" })
            {
                string path = outputPath + "/" + name;
                if (!AssetDatabase.IsValidFolder(path))
                    AssetDatabase.CreateFolder(outputPath, name);
            }
        }

        private static bool TryCategory(string name, out SpawnCategory category)
        {
            return Enum.TryParse(name, true, out category);
        }

        private static int ExpectedFrames(SpawnCategory category)
        {
            switch (category)
            {
                case SpawnCategory.Buoy:
                case SpawnCategory.Supply:
                    return 2;
                case SpawnCategory.Seabird:
                    return 4;
                case SpawnCategory.Decoration:
                default:
                    return 1;
            }
        }

        private static SpawnColliderKind ColliderKind(SpawnCategory category)
        {
            if (category == SpawnCategory.Reef)
                return SpawnColliderKind.Solid;
            return category == SpawnCategory.Supply ? SpawnColliderKind.Trigger : SpawnColliderKind.None;
        }

        private readonly struct VariantFrames
        {
            public readonly SpawnCategory Category;
            public readonly int Variant;
            public readonly IReadOnlyList<Sprite> Frames;

            public VariantFrames(SpawnCategory category, int variant, IReadOnlyList<Sprite> frames)
            {
                Category = category;
                Variant = variant;
                Frames = frames;
            }
        }
    }
}
