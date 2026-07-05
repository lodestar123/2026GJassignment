#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Play 시 자동 생성되던 UI를 씬에 미리 배치.
/// PracticeScene은 GameScene 리듬 UI와 동기화.
/// </summary>
public static class BeatDefenderRuntimeUIEditor
{
    const string GameScenePath = "Assets/Scenes/GameScene.unity";
    const string PracticeScenePath = "Assets/Scenes/PracticeScene.unity";

    static readonly string[] ScenePaths =
    {
        GameScenePath,
        PracticeScenePath
    };

    [MenuItem("Beat Defender/Ensure Runtime UI In Scenes")]
    public static void EnsureAllScenes()
    {
        var previous = SceneManager.GetActiveScene().path;
        var count = 0;

        foreach (var path in ScenePaths)
        {
            if (!File.Exists(path))
                continue;

            if (EnsureScene(path))
                count++;
        }

        if (!string.IsNullOrEmpty(previous) && File.Exists(previous))
            EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);

        Debug.Log($"Beat Defender: Runtime UI ensured in {count} scene(s). Hierarchy에서 위치·크기를 편집하세요.");
    }

    [MenuItem("Beat Defender/Sync Practice UI From Game")]
    public static void SyncPracticeUiFromGameMenu()
    {
        var previous = SceneManager.GetActiveScene().path;

        if (!File.Exists(GameScenePath) || !File.Exists(PracticeScenePath))
        {
            Debug.LogWarning("Beat Defender: GameScene 또는 PracticeScene을 찾을 수 없습니다.");
            return;
        }

        if (SyncPracticeUiFromGame())
            Debug.Log("Beat Defender: Practice UI를 GameScene 리듬 UI와 동기화했습니다.");

        if (!string.IsNullOrEmpty(previous) && File.Exists(previous))
            EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
    }

    static bool EnsureScene(string scenePath)
    {
        if (scenePath == PracticeScenePath)
            return SyncPracticeUiFromGame();

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var root = FindGameplayCanvasRoot(scene);
        if (root == null)
        {
            Debug.LogWarning($"Beat Defender: Canvas not found in {scenePath}");
            return false;
        }

        RemoveLegacyObjects();
        PlaceUi<CoreCrisisOverlay>(root, "CoreCrisisOverlay", 0);
        PlaceUi<JudgmentEdgeFlashUI>(root, "JudgmentEdgeFlashUI", 1);
        PlaceUi<MatchMilestoneAlertUI>(root, "MatchMilestoneAlertUI", -1);
        PlaceUi<EliteSpawnAlertUI>(root, "EliteSpawnAlertUI", -1);
        PlaceUi<PlacementSlotTooltipUI>(root, "PlacementSlotTooltipUI", -1);
        PlaceUi<FeverComboUI>(root, "FeverComboUI", -1);
        PlaceUi<FeverBurstUI>(root, "FeverBurstUI", -1);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return true;
    }

    static bool SyncPracticeUiFromGame()
    {
        var practiceScene = EditorSceneManager.OpenScene(PracticeScenePath, OpenSceneMode.Single);
        var practiceRoot = FindGameplayCanvasRoot(practiceScene);
        if (practiceRoot == null)
        {
            Debug.LogWarning("Beat Defender: PracticeScene Canvas not found");
            return false;
        }

        var gameScene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Additive);
        var gameRoot = FindGameplayCanvasRoot(gameScene);
        if (gameRoot == null)
        {
            Debug.LogWarning("Beat Defender: GameScene Canvas not found");
            EditorSceneManager.CloseScene(gameScene, false);
            return false;
        }

        try
        {
            RemoveLegacyObjects();

            CopyRhythmWidget<JudgmentFlashUI>(gameRoot, practiceRoot);
            CopyRhythmWidget<RhythmScrollUI>(gameRoot, practiceRoot);
            CopyRhythmWidget<RhythmTimelineUI>(gameRoot, practiceRoot);

            DestroyComponentInChildren<PlacementSlotTooltipUI>(practiceRoot);

            PlaceUi<JudgmentEdgeFlashUI>(practiceRoot, "JudgmentEdgeFlashUI", 0);
            PlaceUi<PracticeHudUI>(practiceRoot, "PracticeHud", 1);
            PlaceUi<FeverComboUI>(practiceRoot, "FeverComboUI", -1);
            PlaceUi<FeverBurstUI>(practiceRoot, "FeverBurstUI", -1);

            EditorSceneManager.MarkSceneDirty(practiceScene);
            EditorSceneManager.SaveScene(practiceScene);
            return true;
        }
        finally
        {
            if (gameScene.isLoaded)
                EditorSceneManager.CloseScene(gameScene, false);
        }
    }

    static Transform FindGameplayCanvasRoot(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == "--- UI ---")
                return root.transform;
        }

        foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (canvas.gameObject.scene == scene)
                return canvas.transform;
        }

        return null;
    }

    static void CopyRhythmWidget<T>(Transform sourceRoot, Transform destRoot)
        where T : Component
    {
        var source = sourceRoot.GetComponentInChildren<T>(true);
        if (source == null)
        {
            Debug.LogWarning($"Beat Defender: GameScene에 {typeof(T).Name} 없음");
            return;
        }

        var destScene = destRoot.gameObject.scene;
        var existing = destRoot.GetComponentInChildren<T>(true);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing.gameObject);

        var copy = UnityEngine.Object.Instantiate(source.gameObject);
        Undo.RegisterCreatedObjectUndo(copy, "Sync Practice UI");
        copy.name = source.gameObject.name;
        SceneManager.MoveGameObjectToScene(copy, destScene);
        copy.transform.SetParent(destRoot, false);
        EditorUtility.SetDirty(copy);
    }

    static void DestroyComponentInChildren<T>(Transform root)
        where T : Component
    {
        var existing = root.GetComponentInChildren<T>(true);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing.gameObject);
    }

    static void PlaceUi<T>(Transform canvasRoot, string objectName, int siblingIndex)
        where T : MonoBehaviour, IRuntimeSceneUi
    {
        var existing = canvasRoot.GetComponentInChildren<T>(true);
        T component;

        if (existing != null)
        {
            component = existing;
            if (component.gameObject.name != objectName)
                component.gameObject.name = objectName;
        }
        else
        {
            var go = new GameObject(objectName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Ensure Runtime UI");
            go.transform.SetParent(canvasRoot, false);
            component = Undo.AddComponent<T>(go);
        }

        component.EnsureSceneHierarchy();

        if (siblingIndex >= 0)
            component.transform.SetSiblingIndex(siblingIndex);
        else
            component.transform.SetAsLastSibling();

        EditorUtility.SetDirty(component.gameObject);
    }

    static void RemoveLegacyObjects()
    {
        DestroyIfExists("BeatPulseRail");
        DestroyIfExists("BoostBorderOverlay");
    }

    static void DestroyIfExists(string name)
    {
        var go = GameObject.Find(name);
        if (go != null)
            Undo.DestroyObjectImmediate(go);
    }
}
#endif
