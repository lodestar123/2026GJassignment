#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Play 시 자동 생성되던 UI를 씬에 미리 배치.
/// PracticeScene·TutorialScene은 GameScene 리듬 UI와 동기화.
/// </summary>
public static class BeatDefenderRuntimeUIEditor
{
    const string GameScenePath = "Assets/Scenes/GameScene.unity";
    const string PracticeScenePath = "Assets/Scenes/PracticeScene.unity";
    const string TutorialScenePath = "Assets/Scenes/TutorialScene.unity";

    static readonly string[] ScenePaths =
    {
        GameScenePath,
        PracticeScenePath,
        TutorialScenePath
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
            Debug.Log("Beat Defender: Practice UI를 GameScene과 동기화했습니다.");

        if (!string.IsNullOrEmpty(previous) && File.Exists(previous))
            EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
    }

    [MenuItem("Beat Defender/Sync Tutorial UI From Game")]
    public static void SyncTutorialUiFromGameMenu()
    {
        var previous = SceneManager.GetActiveScene().path;

        if (!File.Exists(GameScenePath) || !File.Exists(TutorialScenePath))
        {
            Debug.LogWarning("Beat Defender: GameScene 또는 TutorialScene을 찾을 수 없습니다.");
            return;
        }

        if (SyncTutorialUiFromGame())
            Debug.Log("Beat Defender: Tutorial UI를 GameScene 리듬 UI와 동기화했습니다.");

        if (!string.IsNullOrEmpty(previous) && File.Exists(previous))
            EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
    }

    public static void SyncPracticeUiFromGameBatch()
    {
        SyncPracticeUiFromGame();
        EditorApplication.Exit(0);
    }

    static bool EnsureScene(string scenePath)
    {
        if (scenePath == PracticeScenePath)
            return SyncPracticeUiFromGame();

        if (scenePath == TutorialScenePath)
            return SyncTutorialUiFromGame();

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var root = FindGameplayCanvasRoot(scene);
        if (root == null)
        {
            Debug.LogWarning($"Beat Defender: Canvas not found in {scenePath}");
            return false;
        }

        RemoveLegacyObjects();
        EnsureScreenSpaceCameraCanvas(root);
        PlaceUi<CoreCrisisOverlay>(root, "CoreCrisisOverlay", 0);
        PlaceUi<JudgmentEdgeFlashUI>(root, "JudgmentEdgeFlashUI", 1);
        PlaceUi<GameStartCountdownUI>(root, "GameStartCountdownUI", -1);
        PlaceUi<MatchMilestoneAlertUI>(root, "MatchMilestoneAlertUI", -1);
        PlaceUi<EliteSpawnAlertUI>(root, "EliteSpawnAlertUI", -1);
        PlaceUi<PlacementSlotTooltipUI>(root, "PlacementSlotTooltipUI", -1);
        PlaceUi<FeverComboUI>(root, "FeverComboUI", -1);
        PlaceUi<FeverBurstUI>(root, "FeverBurstUI", -1);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return true;
    }

    static bool SyncTutorialUiFromGame()
    {
        var tutorialScene = EditorSceneManager.OpenScene(TutorialScenePath, OpenSceneMode.Single);
        var tutorialRoot = FindGameplayCanvasRoot(tutorialScene);
        if (tutorialRoot == null)
        {
            Debug.LogWarning("Beat Defender: TutorialScene Canvas not found");
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
            TutorialSceneEditor.MigrateSceneContents(tutorialScene);

            CopyRhythmWidget<JudgmentFlashUI>(gameRoot, tutorialRoot);
            CopyRhythmWidget<RhythmScrollUI>(gameRoot, tutorialRoot);
            CopyRhythmWidget<RhythmTimelineUI>(gameRoot, tutorialRoot);

            DestroyComponentInChildren<PlacementSlotTooltipUI>(tutorialRoot);

            PlaceUi<JudgmentEdgeFlashUI>(tutorialRoot, "JudgmentEdgeFlashUI", 0);
            PlaceUi<TutorialHudUI>(tutorialRoot, "TutorialHud", 1);
            PlaceUi<FeverComboUI>(tutorialRoot, "FeverComboUI", -1);
            PlaceUi<FeverBurstUI>(tutorialRoot, "FeverBurstUI", -1);

            EditorSceneManager.MarkSceneDirty(tutorialScene);
            EditorSceneManager.SaveScene(tutorialScene);
        }
        finally
        {
            if (gameScene.isLoaded)
                EditorSceneManager.CloseScene(gameScene, false);
        }

        // TutorialUI / TowerTypeSelect / TutorialPlacement — GameScene 복사 대신 전용 bake
        TutorialSceneEditor.BakeTutorialSceneUi(silent: true);
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
            EnsureScreenSpaceCameraCanvas(practiceRoot);

            CopyRhythmWidget<JudgmentEdgeFlashUI>(gameRoot, practiceRoot);
            CopyRhythmWidget<JudgmentFlashUI>(gameRoot, practiceRoot);
            CopyRhythmWidget<RhythmScrollUI>(gameRoot, practiceRoot);
            CopyRhythmWidget<RhythmTimelineUI>(gameRoot, practiceRoot);
            CopyRhythmWidget<FeverComboUI>(gameRoot, practiceRoot);
            CopyRhythmWidget<FeverBurstUI>(gameRoot, practiceRoot);

            SyncPracticeHudFromGameHud(gameRoot, practiceRoot);
            AlignPracticeUiSiblingOrder(practiceRoot);

            EditorSceneManager.MarkSceneDirty(practiceScene);
            EditorSceneManager.SaveScene(practiceScene);
        }
        finally
        {
            if (gameScene.isLoaded)
                EditorSceneManager.CloseScene(gameScene, false);
        }

        return true;
    }

    static void SyncPracticeHudFromGameHud(Transform gameRoot, Transform practiceRoot)
    {
        var gameHud = gameRoot.GetComponentInChildren<GameHudUI>(true);
        var practiceHud = practiceRoot.GetComponentInChildren<PracticeHudUI>(true);
        if (gameHud == null)
        {
            Debug.LogWarning("Beat Defender: GameScene에 GameHudUI 없음");
            return;
        }

        if (practiceHud == null)
        {
            PlaceUi<PracticeHudUI>(practiceRoot, "PracticeHud", 1);
            practiceHud = practiceRoot.GetComponentInChildren<PracticeHudUI>(true);
            if (practiceHud == null)
                return;
        }

        practiceHud.EnsureSceneHierarchy();

        var gameHudRt = (RectTransform)gameHud.transform;
        var practiceRt = (RectTransform)practiceHud.transform;
        var canvasRt = (RectTransform)practiceRoot;
        var secondary = gameHudRt.Find("SecondaryLine") as RectTransform;

        if (secondary != null)
            CopyCanvasWorldRect(secondary, practiceRt, canvasRt);
        else
            CopyCanvasWorldRect(gameHudRt, practiceRt, canvasRt);

        var srcText = secondary != null ? secondary.GetComponent<TextMeshProUGUI>() : null;
        var dstText = practiceRt.Find("StatusLine")?.GetComponent<TextMeshProUGUI>();
        if (srcText != null && dstText != null)
        {
            CopyRectTransformLayout(secondary, (RectTransform)dstText.transform);
            CopyTmpVisuals(srcText, dstText);
        }

        EditorUtility.SetDirty(practiceHud.gameObject);
    }

    static void AlignPracticeUiSiblingOrder(Transform practiceRoot)
    {
        var orderedTypes = new[]
        {
            typeof(JudgmentEdgeFlashUI),
            typeof(PracticeHudUI),
            typeof(JudgmentFlashUI),
            typeof(RhythmScrollUI),
            typeof(RhythmTimelineUI),
            typeof(FeverComboUI),
            typeof(FeverBurstUI)
        };

        var index = 0;
        foreach (var type in orderedTypes)
        {
            var component = practiceRoot.GetComponentInChildren(type, true) as Component;
            if (component == null)
                continue;

            component.transform.SetSiblingIndex(index++);

            if (type != typeof(PracticeHudUI))
                continue;

            var exitBtn = practiceRoot.Find("Btn_ExitStart");
            if (exitBtn != null)
                exitBtn.SetSiblingIndex(index++);
        }
    }

    static void CopyCanvasWorldRect(RectTransform source, RectTransform dest, RectTransform canvasRoot)
    {
        var worldCorners = new Vector3[4];
        source.GetWorldCorners(worldCorners);

        var min = Vector2.positiveInfinity;
        var max = Vector2.negativeInfinity;
        for (var i = 0; i < 4; i++)
        {
            var local = canvasRoot.InverseTransformPoint(worldCorners[i]);
            min = Vector2.Min(min, local);
            max = Vector2.Max(max, local);
        }

        dest.SetParent(canvasRoot, false);
        dest.anchorMin = new Vector2(0.5f, 0.5f);
        dest.anchorMax = new Vector2(0.5f, 0.5f);
        dest.pivot = new Vector2(0.5f, 0.5f);
        dest.anchoredPosition = (min + max) * 0.5f;
        dest.sizeDelta = max - min;
    }

    static void CopyRectTransformLayout(RectTransform source, RectTransform dest)
    {
        dest.anchorMin = source.anchorMin;
        dest.anchorMax = source.anchorMax;
        dest.pivot = source.pivot;
        dest.anchoredPosition = source.anchoredPosition;
        dest.sizeDelta = source.sizeDelta;
        dest.offsetMin = source.offsetMin;
        dest.offsetMax = source.offsetMax;
        dest.localRotation = source.localRotation;
        dest.localScale = source.localScale;
    }

    static void CopyTmpVisuals(TextMeshProUGUI source, TextMeshProUGUI dest)
    {
        dest.font = source.font;
        dest.fontSharedMaterial = source.fontSharedMaterial;
        dest.fontSize = source.fontSize;
        dest.fontStyle = source.fontStyle;
        dest.color = source.color;
        dest.alignment = source.alignment;
        dest.characterSpacing = source.characterSpacing;
        dest.wordSpacing = source.wordSpacing;
        dest.lineSpacing = source.lineSpacing;
        dest.margin = source.margin;
        dest.raycastTarget = source.raycastTarget;
    }

    static void EnsureScreenSpaceCameraCanvas(Transform root)
    {
        var canvas = root.GetComponent<Canvas>();
        if (canvas == null)
            return;

        Camera cam = null;
        foreach (var c in UnityEngine.Object.FindObjectsByType<Camera>(
                     FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (!c.CompareTag("MainCamera"))
                continue;

            cam = c;
            break;
        }

        if (cam != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 100f;
        }

        canvas.vertexColorAlwaysGammaSpace = true;
        canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, GameplayCanvasSetup.DefaultSortingOrder);

        if (root.GetComponent<GameplayCanvasSetup>() == null)
            Undo.AddComponent<GameplayCanvasSetup>(root.gameObject);

        EditorUtility.SetDirty(canvas);
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
        Undo.RegisterCreatedObjectUndo(copy, "Sync Tutorial UI");
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
