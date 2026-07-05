#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
/// <summary>
/// TutorialScene bake — Beat Defender/Bake Tutorial Scene UI.
/// </summary>
[InitializeOnLoad]
public static class TutorialSceneEditor
{
    const string PracticePath = "Assets/Scenes/PracticeScene.unity";
    const string TutorialPath = "Assets/Scenes/TutorialScene.unity";
    static TutorialSceneEditor()
    {
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.delayCall += TryAutoBake;
    }

    static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (scene.name != SceneNames.Tutorial || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        EditorApplication.delayCall += TryAutoBake;
    }

    static void TryAutoBake()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        var scene = SceneManager.GetActiveScene();
        if (scene.name != SceneNames.Tutorial)
            return;

        var uiRoot = GameObject.Find("--- UI ---")?.transform;
        if (uiRoot == null || uiRoot.Find("TutorialUI") != null)
            return;

        BakeTutorialSceneUi(silent: true);
    }

    [MenuItem("Beat Defender/Build Tutorial Scene")]
    public static void BuildTutorialSceneMenu() => BuildTutorialScene();

    [MenuItem("Beat Defender/Bake Tutorial Scene UI")]
    public static void BakeTutorialSceneUiMenu() => BakeTutorialSceneUi(silent: false);

    public static void BuildTutorialScene()
    {
        if (!File.Exists(PracticePath))
        {
            Debug.LogError("TutorialSceneEditor: PracticeScene not found — copy source missing.");
            return;
        }

        if (!File.Exists(TutorialPath))
            AssetDatabase.CopyAsset(PracticePath, TutorialPath);

        var scene = EditorSceneManager.OpenScene(TutorialPath, OpenSceneMode.Single);
        MigrateSceneContents(scene);
        BakeTutorialSceneUi(silent: true);
        UpdateBuildSettings();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("Beat Defender: TutorialScene built from PracticeScene template.");
    }

    public static void BakeTutorialSceneUi(bool silent)
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != SceneNames.Tutorial)
            scene = EditorSceneManager.OpenScene(TutorialPath, OpenSceneMode.Single);

        MigrateSceneContents(scene);

        var uiRoot = GameObject.Find("--- UI ---")?.transform;
        if (uiRoot == null)
        {
            Debug.LogError("TutorialSceneEditor: --- UI --- not found.");
            return;
        }

        var bakeSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/UI/UiSquare.png")
            ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Square.png");

        TutorialVisualBuilder.Build(uiRoot, null, bakeSprite);

        var flow = GameObject.Find("TutorialFlow")?.transform;
        if (flow != null && flow.Find("TutorialPlacement") == null)
            TutorialVisualBuilder.BuildPlacementWorld(flow);

        EnsureTowerPlacer(scene);
        WireTutorialFlow();
        ApplyFonts(uiRoot);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (!silent)
            Debug.Log("Beat Defender: TutorialScene UI baked. Hierarchy에서 위치·크기를 편집하세요.");
    }

    public static void MigrateSceneContents(Scene scene)
    {
        var flow = GameObject.Find("TutorialFlow") ?? GameObject.Find("PracticeFlow");
        if (flow != null)
        {
            flow.name = "TutorialFlow";
            StripLegacyFlowComponents(flow);

            if (flow.GetComponent<TutorialSceneController>() == null)
                Undo.AddComponent<TutorialSceneController>(flow);

            if (flow.GetComponent<TutorialController>() == null)
                Undo.AddComponent<TutorialController>(flow);

            if (flow.GetComponent<TutorialPlacementSetup>() == null)
                Undo.AddComponent<TutorialPlacementSetup>(flow);
        }

        foreach (var hud in Object.FindObjectsByType<PracticeHudUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (hud.gameObject.scene != scene)
                continue;

            hud.gameObject.name = "TutorialHud";
            if (hud.GetComponent<TutorialHudUI>() == null)
                Undo.AddComponent<TutorialHudUI>(hud.gameObject);
            Object.DestroyImmediate(hud, true);
        }

        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(
            GameObject.Find("TutorialFlow") ?? GameObject.Find("PracticeFlow"));

        var overlayRoot = GameObject.Find("OverlayPanel")?.transform.parent?.gameObject
            ?? GameObject.Find("OverlayRoot");
        if (overlayRoot != null)
            overlayRoot.SetActive(false);

        var title = GameObject.Find("Title")?.GetComponent<TMPro.TextMeshProUGUI>();
        if (title != null)
            title.text = "튜토리얼";

        var info = GameObject.Find("InfoText")?.GetComponent<TMPro.TextMeshProUGUI>();
        if (info != null)
            info.text = "단계별로 Beat Defender를 배워보세요";

        var exitLabel = GameObject.Find("Btn_ExitStart")?.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (exitLabel != null)
            exitLabel.text = "타이틀로";
    }

    static void WireTutorialFlow()
    {
        var flow = GameObject.Find("TutorialFlow");
        if (flow == null)
            return;

        var ui = Object.FindFirstObjectByType<TutorialUI>(FindObjectsInactive.Include);
        var controller = flow.GetComponent<TutorialController>();
        if (controller != null && ui != null)
        {
            var controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("ui").objectReferenceValue = ui;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();
        }

        var setup = flow.GetComponent<TutorialPlacementSetup>();
        if (setup == null)
            setup = Undo.AddComponent<TutorialPlacementSetup>(flow);

        var world = GameObject.Find("TutorialPlacement");
        var towerSelect = GameObject.Find("TowerTypeSelect");
        var towers = world != null ? world.transform.Find("Towers") : null;

        var setupSo = new SerializedObject(setup);
        setupSo.FindProperty("worldRoot").objectReferenceValue = world;
        setupSo.FindProperty("towerSelectRoot").objectReferenceValue = towerSelect;
        setupSo.FindProperty("towersRoot").objectReferenceValue = towers;
        setupSo.ApplyModifiedPropertiesWithoutUndo();

        ui?.ResolveRefs();
        setup.ResolveRefs();
    }

    static void EnsureTowerPlacer(Scene scene)
    {
        var systems = GameObject.Find("--- Systems ---");
        if (systems == null)
            return;

        var placer = systems.GetComponent<TowerPlacer>();
        if (placer == null)
            placer = Undo.AddComponent<TowerPlacer>(systems);

        var towers = GameObject.Find("TutorialPlacement")?.transform.Find("Towers");
        if (towers != null)
            placer.SetTowerRoot(towers);
    }

    static void ApplyFonts(Transform uiRoot)
    {
        foreach (var tmp in uiRoot.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
            BeatDefenderFonts.Apply(tmp);
    }

    static void StripLegacyFlowComponents(GameObject flow)
    {
        foreach (var mb in flow.GetComponents<MonoBehaviour>())
        {
            if (mb is TutorialSceneController or TutorialController or TutorialPlacementSetup)
                continue;

            Object.DestroyImmediate(mb, true);
        }
    }

    static void UpdateBuildSettings()
    {
        var scenes = EditorBuildSettings.scenes;
        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>();
        bool hasTutorial = false;

        foreach (var entry in scenes)
        {
            if (entry.path == PracticePath)
                continue;

            if (entry.path == TutorialPath)
                hasTutorial = true;

            list.Add(entry);
        }

        if (!hasTutorial)
            list.Add(new EditorBuildSettingsScene(TutorialPath, true));

        EditorBuildSettings.scenes = list.ToArray();
    }
}
#endif
