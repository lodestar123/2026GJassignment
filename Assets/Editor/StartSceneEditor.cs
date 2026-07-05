#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// StartScene 비주얼을 Hierarchy에 bake — Beat Defender/Build Start Scene Visuals.
/// </summary>
[InitializeOnLoad]
public static class StartSceneEditor
{
    const string ScenePath = "Assets/Scenes/StartScene.unity";

    static StartSceneEditor()
    {
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.delayCall += TryAutoBake;
    }

    static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (scene.name != SceneNames.Start || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        EditorApplication.delayCall += TryAutoBake;
    }

    static void TryAutoBake()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        var menu = Object.FindFirstObjectByType<StartMenuUI>();
        if (menu == null)
            return;

        if (menu.transform.Find("Background") != null)
        {
            UpgradeBeatRailIfNeeded(menu.transform as RectTransform);
            EnsureRhythmStack(menu);
            var scene = menu.gameObject.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return;
        }

        BuildStartScene(silent: true);
    }

    [MenuItem("Beat Defender/Build Start Scene Visuals")]
    public static void BuildStartSceneMenu() => BuildStartScene(silent: false);

    public static void BuildStartScene() => BuildStartScene(silent: false);

    public static void BuildStartScene(bool silent)
    {
        if (!File.Exists(ScenePath))
        {
            Debug.LogError($"StartSceneEditor: scene not found at {ScenePath}");
            return;
        }

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != SceneNames.Start)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var menu = Object.FindFirstObjectByType<StartMenuUI>();
        if (menu == null)
        {
            Debug.LogError("StartSceneEditor: StartMenuUI not found in StartScene.");
            return;
        }

        var presentation = menu.GetComponent<StartScenePresentation>();
        if (presentation == null)
            presentation = Undo.AddComponent<StartScenePresentation>(menu.gameObject);

        UpgradeBeatRailIfNeeded(menu.transform as RectTransform);

        var bakeSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/UI/UiSquare.png")
            ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Square.png");
        presentation.EnsureSceneHierarchy(bakeSprite);

        EnsureRhythmStack(menu);

        var cam = Camera.main;
        if (cam != null)
            cam.backgroundColor = new Color(0.04f, 0.05f, 0.11f, 1f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (!silent)
            Debug.Log("Beat Defender: StartScene visuals baked and saved.");
    }

    static void UpgradeBeatRailIfNeeded(RectTransform root)
    {
        if (root == null)
            return;

        var decor = root.Find("Decor");
        if (decor == null)
            return;

        var rail = decor.Find("BeatRail");
        if (rail == null)
            return;

        if (rail.Find("TrackArea") != null)
            return;

        Undo.DestroyObjectImmediate(rail.gameObject);
        StartSceneVisualBuilder.BuildBeatRailOnly(decor);
    }

    static void EnsureRhythmStack(StartMenuUI menu)
    {
        var go = menu.gameObject;
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);

        if (go.GetComponent<StartSceneRhythmBootstrap>() == null)
            Undo.AddComponent<StartSceneRhythmBootstrap>(go);

        if (go.GetComponent<BeatClock>() == null)
            Undo.AddComponent<BeatClock>(go);

        var detector = go.GetComponent<RhythmCommandDetector>();
        if (detector == null)
            detector = Undo.AddComponent<RhythmCommandDetector>(go);

        var detectorSo = new SerializedObject(detector);
        detectorSo.FindProperty("commandsEnabled").boolValue = false;
        detectorSo.ApplyModifiedPropertiesWithoutUndo();

        var rail = menu.transform.Find("Decor/BeatRail");
        if (rail == null)
            return;

        var timeline = rail.GetComponent<RhythmTimelineUI>();
        if (timeline == null)
            timeline = Undo.AddComponent<RhythmTimelineUI>(rail.gameObject);

        var timelineSo = new SerializedObject(timeline);
        timelineSo.FindProperty("applyScriptLayoutOnAwake").boolValue = false;
        timelineSo.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
