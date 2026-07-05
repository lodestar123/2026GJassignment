#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
static class BeatDefenderPlayModeBootstrap
{
    static BeatDefenderPlayModeBootstrap()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode)
            return;

        var scene = SceneManager.GetActiveScene();
        var path = scene.path?.Replace('\\', '/');
        if (path == null)
            return;

        if (!path.EndsWith("Assets/Scenes/GameScene.unity")
            && !path.EndsWith("Assets/Scenes/GameSceneSpiral.unity"))
            return;

        if (GameObject.Find("--- Systems ---") != null)
            return;

        if (path.EndsWith("GameSceneSpiral.unity"))
            BeatDefenderSceneBuilder.BuildSpiralIslandsScene();
        else
            BeatDefenderSceneBuilder.BuildPhaseCScene();
    }
}

public static class BeatDefenderSceneBuilder
{
    const string GameScenePath = "Assets/Scenes/GameScene.unity";
    const string GameSceneSpiralPath = "Assets/Scenes/GameSceneSpiral.unity";
    const string PretendardFontPath = "Assets/Resources/Font/PretendardVariable SDF.asset";

    static TMP_FontAsset GetPretendardFont() =>
        AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PretendardFontPath);

    static void StyleTmp(TextMeshProUGUI tmp)
    {
        var font = GetPretendardFont();
        if (font != null)
            tmp.font = font;
    }

    static TextMeshProUGUI CreateTmp(GameObject go)
    {
        var tmp = go.AddComponent<TextMeshProUGUI>();
        StyleTmp(tmp);
        return tmp;
    }

    [MenuItem("Beat Defender/Apply Pretendard Font To All Scenes")]
    public static void ApplyPretendardFontToAllScenes()
    {
        var font = GetPretendardFont();
        if (font == null)
        {
            Debug.LogError("Beat Defender: Pretendard font asset not found at " + PretendardFontPath);
            return;
        }

        var scenePaths = new[]
        {
            "Assets/Scenes/StartScene.unity",
            "Assets/Scenes/GameScene.unity",
            "Assets/Scenes/PracticeScene.unity"
        };

        var previousScene = SceneManager.GetActiveScene().path;
        var count = 0;

        foreach (var path in scenePaths)
        {
            if (!System.IO.File.Exists(path))
                continue;

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            foreach (var tmp in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                tmp.font = font;
                count++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        if (!string.IsNullOrEmpty(previousScene) && System.IO.File.Exists(previousScene))
            EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);

        Debug.Log($"Beat Defender: Applied Pretendard font to {count} TextMeshProUGUI components.");
    }

    [MenuItem("Beat Defender/Build Phase C Scene (GameScene — Classic Map)")]
    public static void BuildPhaseCScene() => BuildGameScene(GameScenePath, MapLayoutKind.Classic, includePhaseBUi: true, includePhaseCMap: true);

    [MenuItem("Beat Defender/Build Spiral Islands Scene (GameSceneSpiral — Map C)")]
    public static void BuildSpiralIslandsScene() => BuildGameScene(GameSceneSpiralPath, MapLayoutKind.SpiralIslands, includePhaseBUi: true, includePhaseCMap: true);

    static void BuildGameScene(string scenePath, MapLayoutKind mapLayout, bool includePhaseBUi, bool includePhaseCMap)
    {
        EnsureSceneAssetExists(scenePath);
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        ClearExisting();

        SetupCamera();
        var systems = CreateSystems(includePhaseCMap);
        if (includePhaseCMap)
            CreateMap(systems, mapLayout);
        else
            CreateTestField(systems);
        CreateCanvas(includePhaseBUi, includePhaseCMap);

        Selection.activeGameObject = systems;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EnsureSceneInBuildSettings(scenePath);
        Debug.Log(includePhaseCMap
            ? mapLayout == MapLayoutKind.SpiralIslands
                ? "Beat Defender: GameSceneSpiral (Map C) hierarchy built."
                : "Beat Defender: GameScene Phase C (Classic map) hierarchy built."
            : includePhaseBUi
                ? "Beat Defender: GameScene Phase B hierarchy built."
                : "Beat Defender: GameScene Phase A hierarchy built.");
    }

    [MenuItem("Beat Defender/Use Classic Map (Start Scene → GameScene)")]
    public static void SelectClassicMapLayout()
    {
        GameSettings.SelectedMapLayout = MapLayoutKind.Classic;
        Debug.Log("Beat Defender: Start menu will load GameScene (Classic Y-junction map).");
    }

    [MenuItem("Beat Defender/Use Spiral Islands Map (Start Scene → GameSceneSpiral)")]
    public static void SelectSpiralMapLayout()
    {
        GameSettings.SelectedMapLayout = MapLayoutKind.SpiralIslands;
        Debug.Log("Beat Defender: Start menu will load GameSceneSpiral (Map candidate C).");
    }
    [MenuItem("Beat Defender/Build Phase A Scene (GameScene)")]
    public static void BuildPhaseAScene() => BuildGameScene(GameScenePath, MapLayoutKind.Classic, includePhaseBUi: false, includePhaseCMap: false);

    [MenuItem("Beat Defender/Build Phase B Scene (GameScene)")]
    public static void BuildPhaseBScene() => BuildGameScene(GameScenePath, MapLayoutKind.Classic, includePhaseBUi: true, includePhaseCMap: false);

    [MenuItem("Beat Defender/Rebuild UI Hierarchy Only")]
    public static void RebuildUiHierarchyOnly()
    {
        RebuildUiHierarchyOnly(includePhaseBUi: true);
    }

    [MenuItem("Beat Defender/Rebuild UI Hierarchy (Phase A only)")]
    public static void RebuildUiHierarchyPhaseAOnly()
    {
        RebuildUiHierarchyOnly(includePhaseBUi: false);
    }

    static void RebuildUiHierarchyOnly(bool includePhaseBUi)
    {
        var ui = GameObject.Find("--- UI ---");
        if (ui != null)
            Object.DestroyImmediate(ui);

        var legacyCanvas = GameObject.Find("Canvas");
        if (legacyCanvas != null)
            Object.DestroyImmediate(legacyCanvas);

        bool includePhaseCMap = GameObject.Find("--- Map ---") != null;
        CreateCanvas(includePhaseBUi, includePhaseCMap);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log(includePhaseBUi
            ? includePhaseCMap
                ? "Beat Defender: Phase C UI hierarchy rebuilt."
                : "Beat Defender: Phase B UI hierarchy rebuilt."
            : "Beat Defender: Phase A UI hierarchy rebuilt.");
    }

    static void EnsureSceneAssetExists(string scenePath)
    {
        if (System.IO.File.Exists(scenePath))
            return;

        var dir = System.IO.Path.GetDirectoryName(scenePath);
        if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
            System.IO.Directory.CreateDirectory(dir);

        var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(newScene, scenePath);
        AssetDatabase.Refresh();
    }

    static void EnsureSceneInBuildSettings(string scenePath)
    {
        var normalized = scenePath.Replace('\\', '/');
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var entry in scenes)
        {
            if (entry.path.Replace('\\', '/') == normalized)
                return;
        }

        scenes.Add(new EditorBuildSettingsScene(normalized, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    static void ClearExisting()
    {
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == "Main Camera")
                continue;

            Object.DestroyImmediate(root);
        }
    }

    static void SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }

        cam.orthographic = true;
        cam.orthographicSize = 7.5f;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.backgroundColor = new Color(0.12f, 0.14f, 0.18f);
    }

    static GameObject CreateSystems(bool includePhaseCMap)
    {
        var go = new GameObject("--- Systems ---");
        var clock = go.AddComponent<BeatClock>();
        clock.MeasureDurationSeconds = 1f;
        go.AddComponent<ResourceManager>();
        go.AddComponent<RunStats>();
        go.AddComponent<RhythmInputRecorder>();
        go.AddComponent<RhythmInputSettings>();
        go.AddComponent<RhythmCommandDetector>();
        go.AddComponent<SkillCooldownController>();
        go.AddComponent<CommandEffectController>();
        go.AddComponent<RhythmUiInputGuard>();
        go.AddComponent<CombatVfxService>();
        go.AddComponent<TowerRegistry>();
        go.AddComponent<SimpleAudio>();
        if (includePhaseCMap)
        {
            go.AddComponent<GameManager>();
            go.AddComponent<PauseController>();
            go.AddComponent<PracticeSceneLoader>();
        }
        return go;
    }

    static void CreateTestField(GameObject systems)
    {
        var field = new GameObject("--- Test Field (Phase A) ---");

        CreateTower(field.transform, "BeatTower_Test", TowerType.Beat, new Vector3(-2f, -1f, 0f), Color.white);
        CreateTower(field.transform, "StrikeTower_Test", TowerType.Strike, new Vector3(0f, 0f, 0f), new Color(0.94f, 0.33f, 0.31f));
        CreateTower(field.transform, "BoostTower_Test", TowerType.Boost, new Vector3(2f, -1f, 0f), new Color(1f, 0.6f, 0f));

        CreateEnemy(field.transform, "TestEnemy_A", new Vector3(-0.5f, 0.5f, 0f), 4f, new Color(0.26f, 0.65f, 0.96f));
        CreateEnemy(field.transform, "TestEnemy_B", new Vector3(0.5f, 0.2f, 0f), 10f, new Color(0.67f, 0.28f, 0.74f));

        systems.GetComponent<TowerRegistry>().Refresh();
    }

    static void CreateMap(GameObject systems, MapLayoutKind layoutKind = MapLayoutKind.Classic)
    {
        var layout = MapLayout.Get(layoutKind);
        var map = new GameObject("--- Map ---");

        var config = map.AddComponent<MapSceneConfig>();
        var configSo = new SerializedObject(config);
        configSo.FindProperty("layoutKind").enumValueIndex = (int)layoutKind;
        configSo.ApplyModifiedPropertiesWithoutUndo();

        var coreGo = new GameObject("Core");
        coreGo.transform.SetParent(map.transform);
        coreGo.transform.position = layout.CorePosition;
        var coreSr = coreGo.AddComponent<SpriteRenderer>();
        coreSr.sprite = GetPlaceholderSprite();
        coreSr.color = new Color(1f, 0.85f, 0.2f);
        coreSr.sortingOrder = 2;
        coreGo.transform.localScale = Vector3.one * 0.85f;
        coreGo.AddComponent<BaseHealth>();
        var coreCol = coreGo.AddComponent<CircleCollider2D>();
        coreCol.isTrigger = true;
        coreCol.radius = 0.55f;

        CreatePathMarker(map.transform, "Path_S1", layout.PathFromS1, new Color(0.45f, 0.45f, 0.5f, 0.35f));
        CreatePathMarker(map.transform, "Path_S2", layout.PathFromS2, new Color(0.45f, 0.45f, 0.5f, 0.35f));

        if (layout.IslandDecorations is { Length: > 0 })
            CreateIslandDecorations(map.transform, layout.IslandDecorations);

        map.AddComponent<MapPathProvider>();

        var gridGo = new GameObject("PlacementGrid");
        gridGo.transform.SetParent(map.transform);
        var grid = gridGo.AddComponent<PlacementGrid>();
        grid.BuildForEditor();

        var towerRoot = new GameObject("Towers");
        towerRoot.transform.SetParent(map.transform);

        var placerGo = new GameObject("TowerPlacer");
        placerGo.transform.SetParent(map.transform);
        var placer = placerGo.AddComponent<TowerPlacer>();
        var placerSo = new SerializedObject(placer);
        placerSo.FindProperty("towerRoot").objectReferenceValue = towerRoot.transform;
        placerSo.ApplyModifiedPropertiesWithoutUndo();

        var enemyRoot = new GameObject("Enemies");
        enemyRoot.transform.SetParent(map.transform);

        var spawnerGo = new GameObject("ContinuousSpawner");
        spawnerGo.transform.SetParent(map.transform);
        var spawner = spawnerGo.AddComponent<ContinuousSpawner>();
        var spawnerSo = new SerializedObject(spawner);
        spawnerSo.FindProperty("enemyRoot").objectReferenceValue = enemyRoot.transform;
        spawnerSo.ApplyModifiedPropertiesWithoutUndo();
    }

    static void CreateIslandDecorations(Transform parent, Vector2[] positions)
    {
        var root = new GameObject("IslandDecorations");
        root.transform.SetParent(parent);

        var wallColor = new Color(0.36f, 0.25f, 0.22f, 0.92f);
        for (int i = 0; i < positions.Length; i++)
        {
            var block = new GameObject($"Island_{i}");
            block.transform.SetParent(root.transform);
            block.transform.position = positions[i];
            block.transform.localScale = Vector3.one * (i % 3 == 0 ? 1.15f : 0.85f);

            var sr = block.AddComponent<SpriteRenderer>();
            sr.sprite = GetPlaceholderSprite();
            sr.color = wallColor;
            sr.sortingOrder = 0;
        }
    }

    static void CreatePathMarker(Transform parent, string name, Vector2[] points, Color color)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent);
        for (int i = 0; i < points.Length; i++)
        {
            var dot = new GameObject($"P{i}");
            dot.transform.SetParent(root.transform);
            dot.transform.position = points[i];
            var sr = dot.AddComponent<SpriteRenderer>();
            sr.sprite = GetPlaceholderSprite();
            sr.color = color;
            sr.sortingOrder = 1;
            dot.transform.localScale = Vector3.one * 0.25f;
        }
    }

    static void CreateTower(Transform parent, string name, TowerType type, Vector3 pos, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = pos;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetPlaceholderSprite();
        sr.color = color;
        go.transform.localScale = Vector3.one * 0.6f;

        var tower = go.AddComponent<Tower>();
        tower.towerType = type;
        go.AddComponent<CircleCollider2D>().isTrigger = true;

        if (type == TowerType.Beat)
            go.AddComponent<BeatTower>();

        go.AddComponent<TowerFireRecoil>();
    }

    static void CreateEnemy(Transform parent, string name, Vector3 pos, float hp, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = pos;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetPlaceholderSprite();
        sr.color = color;
        go.transform.localScale = Vector3.one * 0.45f;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        var health = go.AddComponent<EnemyHealth>();
        health.maxHp = hp;
        go.AddComponent<EnemyPathProgress>();
    }

    static Sprite _placeholderSprite;

    static Sprite GetPlaceholderSprite()
    {
        if (_placeholderSprite != null)
            return _placeholderSprite;

        var tex = new Texture2D(4, 4);
        var fill = Color.white;
        for (int y = 0; y < 4; y++)
        for (int x = 0; x < 4; x++)
            tex.SetPixel(x, y, fill);
        tex.Apply();

        _placeholderSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        return _placeholderSprite;
    }

    static void CreateCanvas(bool includePhaseBUi, bool includePhaseCMap)
    {
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        var canvasGo = new GameObject("--- UI ---");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        if (includePhaseBUi)
        {
            CreateGameHud(canvasGo.transform);
            CreateRhythmScroll(canvasGo.transform);
            CreateBeatPulseRail(canvasGo.transform);
            CreateTowerTypeSelect(canvasGo.transform);
            CreateBoostBorderOverlay(canvasGo.transform);
            if (includePhaseCMap)
                CreateTowerSellPanel(canvasGo.transform);
        }

        var debugPanel = CreateRhythmDebugPanel(canvasGo.transform);
        if (includePhaseBUi && debugPanel != null)
            debugPanel.SetActive(false);

        CreateJudgmentFlash(canvasGo.transform);
        CreateRhythmTimeline(canvasGo.transform);

        if (includePhaseCMap)
        {
            CreatePauseMenu(canvasGo.transform);
            CreateResultScreen(canvasGo.transform);
        }
    }

    const string StartScenePath = "Assets/Scenes/StartScene.unity";
    const string PracticeScenePath = "Assets/Scenes/PracticeScene.unity";

    [MenuItem("Beat Defender/Build Start Scene")]
    public static void BuildStartScene()
    {
        var scene = EditorSceneManager.OpenScene(StartScenePath, OpenSceneMode.Single);
        ClearExisting();
        SetupCamera();

        EnsureEventSystem();
        var canvasGo = new GameObject("--- UI ---");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        CreateStartMenu(canvasGo.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Beat Defender: StartScene built.");
    }

    [MenuItem("Beat Defender/Build Practice Scene")]
    public static void BuildPracticeScene()
    {
        var scene = EditorSceneManager.OpenScene(PracticeScenePath, OpenSceneMode.Single);
        ClearExisting();
        SetupCamera();
        CreateSystems(includePhaseCMap: false);
        CreatePracticeCanvas();
        CreatePracticeFlow();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Beat Defender: PracticeScene built.");
    }

    static void CreatePracticeCanvas()
    {
        EnsureEventSystem();
        var canvasGo = new GameObject("--- UI ---");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        CreateRhythmScroll(canvasGo.transform);
        CreateBeatPulseRail(canvasGo.transform);
        CreateJudgmentFlash(canvasGo.transform);
        CreateRhythmTimeline(canvasGo.transform);

        var exitBtn = CreateMenuButton(canvasGo.transform, "Btn_ExitStart", "나가기 (Start)",
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(180f, 52f));
    }

    static void CreatePracticeFlow()
    {
        var flow = new GameObject("PracticeFlow");
        var overlay = CreateOverlayPracticePanel(flow.transform);
        overlay.SetActive(false);

        var controller = flow.AddComponent<PracticeSceneController>();
        var exitBtn = Object.FindAnyObjectByType<Canvas>()?.transform.Find("Btn_ExitStart")?.GetComponent<Button>();

        var so = new SerializedObject(controller);
        so.FindProperty("standaloneRoot").objectReferenceValue = GameObject.Find("--- UI ---");
        so.FindProperty("overlayRoot").objectReferenceValue = overlay;
        so.FindProperty("standaloneExitButton").objectReferenceValue = exitBtn;
        so.FindProperty("overlayCloseButton").objectReferenceValue =
            overlay.transform.Find("Btn_Close")?.GetComponent<Button>();
        so.FindProperty("infoText").objectReferenceValue =
            overlay.transform.Find("InfoText")?.GetComponent<TextMeshProUGUI>();
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static GameObject CreateOverlayPracticePanel(Transform parent)
    {
        var canvasGo = new GameObject("OverlayCanvas");
        canvasGo.transform.SetParent(parent, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        var root = new GameObject("OverlayPanel");
        root.transform.SetParent(canvasGo.transform, false);
        var rt = root.AddComponent<RectTransform>();
        StretchRect(rt, 0f, 0f, 0f, 0f);

        var bg = root.AddComponent<Image>();
        bg.sprite = GetUiWhiteSprite();
        bg.color = new Color(0f, 0f, 0f, 0.72f);

        var infoGo = new GameObject("InfoText");
        infoGo.transform.SetParent(root.transform, false);
        var infoRect = infoGo.AddComponent<RectTransform>();
        infoRect.anchorMin = new Vector2(0.5f, 0.55f);
        infoRect.anchorMax = new Vector2(0.5f, 0.55f);
        infoRect.sizeDelta = new Vector2(720f, 160f);
        var infoTmp = CreateTmp(infoGo);
        infoTmp.fontSize = 28f;
        infoTmp.alignment = TextAlignmentOptions.Center;
        infoTmp.raycastTarget = false;

        CreateMenuButton(root.transform, "Btn_Close", "닫기 (Pause 복귀)",
            new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.35f), Vector2.zero, new Vector2(260f, 56f));

        return root;
    }

    static void CreateStartMenu(Transform canvasRoot)
    {
        var root = new GameObject("StartMenu");
        root.transform.SetParent(canvasRoot, false);
        var rt = root.AddComponent<RectTransform>();
        StretchRect(rt, 0f, 0f, 0f, 0f);

        var bg = root.AddComponent<Image>();
        bg.sprite = GetUiWhiteSprite();
        bg.color = new Color(0.06f, 0.08f, 0.12f, 0.96f);
        bg.raycastTarget = false;

        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(root.transform, false);
        var titleRect = titleGo.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.72f);
        titleRect.anchorMax = new Vector2(0.5f, 0.72f);
        titleRect.sizeDelta = new Vector2(800f, 120f);
        var titleTmp = CreateTmp(titleGo);
        titleTmp.fontSize = 72f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.raycastTarget = false;

        var startBtn = CreateMenuButton(root.transform, "Btn_Start", "게임 시작",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(320f, 64f));
        var practiceBtn = CreateMenuButton(root.transform, "Btn_Practice", "박자 연습",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(320f, 64f));
        var quitBtn = CreateMenuButton(root.transform, "Btn_Quit", "종료",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -120f), new Vector2(320f, 64f));

        var menu = root.AddComponent<StartMenuUI>();
        var so = new SerializedObject(menu);
        so.FindProperty("titleText").objectReferenceValue = titleTmp;
        so.FindProperty("startGameButton").objectReferenceValue = startBtn;
        so.FindProperty("practiceButton").objectReferenceValue = practiceBtn;
        so.FindProperty("quitButton").objectReferenceValue = quitBtn;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void CreatePauseMenu(Transform canvasRoot)
    {
        var controllerGo = new GameObject("PauseMenuController");
        controllerGo.transform.SetParent(canvasRoot, false);
        var controllerRect = controllerGo.AddComponent<RectTransform>();
        StretchRect(controllerRect, 0f, 0f, 0f, 0f);

        var root = new GameObject("PauseMenu");
        root.transform.SetParent(controllerGo.transform, false);
        var rt = root.AddComponent<RectTransform>();
        StretchRect(rt, 0f, 0f, 0f, 0f);

        var bg = root.AddComponent<Image>();
        bg.sprite = GetUiWhiteSprite();
        bg.color = new Color(0f, 0f, 0f, 0.65f);

        var panel = new GameObject("Panel");
        panel.transform.SetParent(root.transform, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(360f, 420f);
        var panelBg = panel.AddComponent<Image>();
        panelBg.sprite = GetUiWhiteSprite();
        panelBg.color = new Color(0.1f, 0.1f, 0.12f, 0.95f);

        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(panel.transform, false);
        var titleRect = titleGo.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -28f);
        titleRect.sizeDelta = new Vector2(280f, 40f);
        var titleTmp = CreateTmp(titleGo);
        titleTmp.text = "일시정지";
        titleTmp.fontSize = 32f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;

        var continueBtn = CreateMenuButton(panel.transform, "Btn_Continue", "계속 (ESC)",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(280f, 48f));
        var restartBtn = CreateMenuButton(panel.transform, "Btn_Restart", "재시작",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -132f), new Vector2(280f, 48f));
        var practiceBtn = CreateMenuButton(panel.transform, "Btn_Practice", "박자 연습",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -192f), new Vector2(280f, 48f));
        var settingsBtn = CreateMenuButton(panel.transform, "Btn_Settings", "설정",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -252f), new Vector2(280f, 48f));
        var titleBtn = CreateMenuButton(panel.transform, "Btn_Title", "시작 화면",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -312f), new Vector2(280f, 48f));

        var settingsPanel = CreateSettingsPanel(root.transform, controllerGo);

        root.SetActive(false);

        var pauseUi = controllerGo.AddComponent<PauseMenuUI>();
        var so = new SerializedObject(pauseUi);
        so.FindProperty("panelRoot").objectReferenceValue = root;
        so.FindProperty("buttonsRoot").objectReferenceValue = panel;
        so.FindProperty("continueButton").objectReferenceValue = continueBtn;
        so.FindProperty("restartButton").objectReferenceValue = restartBtn;
        so.FindProperty("practiceButton").objectReferenceValue = practiceBtn;
        so.FindProperty("settingsButton").objectReferenceValue = settingsBtn;
        so.FindProperty("titleButton").objectReferenceValue = titleBtn;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static GameObject CreateSettingsPanel(Transform pauseRoot, GameObject controllerGo)
    {
        var panel = new GameObject("SettingsPanel");
        panel.transform.SetParent(pauseRoot, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(420f, 360f);
        var panelBg = panel.AddComponent<Image>();
        panelBg.sprite = GetUiWhiteSprite();
        panelBg.color = new Color(0.08f, 0.08f, 0.1f, 0.98f);

        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(panel.transform, false);
        var titleRect = titleGo.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -28f);
        titleRect.sizeDelta = new Vector2(360f, 36f);
        var titleTmp = CreateTmp(titleGo);
        titleTmp.text = "설정";
        titleTmp.fontSize = 28f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;

        var inputLabelGo = new GameObject("InputLabel");
        inputLabelGo.transform.SetParent(panel.transform, false);
        var inputLabelRect = inputLabelGo.AddComponent<RectTransform>();
        inputLabelRect.anchorMin = new Vector2(0.5f, 1f);
        inputLabelRect.anchorMax = new Vector2(0.5f, 1f);
        inputLabelRect.anchoredPosition = new Vector2(0f, -72f);
        inputLabelRect.sizeDelta = new Vector2(360f, 28f);
        var inputLabelTmp = CreateTmp(inputLabelGo);
        inputLabelTmp.fontSize = 18f;
        inputLabelTmp.alignment = TextAlignmentOptions.MidlineLeft;

        var inputSlider = CreateLabeledSlider(panel.transform, "InputSlider", "입력 감도",
            new Vector2(0.5f, 1f), new Vector2(0f, -108f), new Vector2(340f, 36f));

        var metroSlider = CreateLabeledSlider(panel.transform, "MetronomeSlider", "메트로놈",
            new Vector2(0.5f, 1f), new Vector2(0f, -168f), new Vector2(340f, 36f));

        var sfxSlider = CreateLabeledSlider(panel.transform, "SfxSlider", "효과음",
            new Vector2(0.5f, 1f), new Vector2(0f, -228f), new Vector2(340f, 36f));

        var closeBtn = CreateMenuButton(panel.transform, "Btn_Close", "닫기 (ESC)",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(220f, 44f));

        panel.SetActive(false);

        var settingsUi = controllerGo.AddComponent<SettingsPanelUI>();
        var so = new SerializedObject(settingsUi);
        so.FindProperty("panelRoot").objectReferenceValue = panel;
        so.FindProperty("inputOffsetSlider").objectReferenceValue = inputSlider;
        so.FindProperty("inputOffsetLabel").objectReferenceValue = inputLabelTmp;
        so.FindProperty("metronomeSlider").objectReferenceValue = metroSlider;
        so.FindProperty("sfxSlider").objectReferenceValue = sfxSlider;
        so.FindProperty("closeButton").objectReferenceValue = closeBtn;
        so.ApplyModifiedPropertiesWithoutUndo();
        return panel;
    }

    static Slider CreateLabeledSlider(Transform parent, string name, string label,
        Vector2 anchor, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform, false);
        var labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0.5f);
        labelRect.anchorMax = new Vector2(0f, 0.5f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.anchoredPosition = new Vector2(0f, 0f);
        labelRect.sizeDelta = new Vector2(90f, 28f);
        var labelTmp = CreateTmp(labelGo);
        labelTmp.text = label;
        labelTmp.fontSize = 18f;
        labelTmp.alignment = TextAlignmentOptions.MidlineLeft;

        var sliderGo = new GameObject("Slider");
        sliderGo.transform.SetParent(go.transform, false);
        var sliderRect = sliderGo.AddComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0f, 0f);
        sliderRect.anchorMax = new Vector2(1f, 1f);
        sliderRect.offsetMin = new Vector2(96f, 0f);
        sliderRect.offsetMax = Vector2.zero;

        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(sliderGo.transform, false);
        var bgRect = bgGo.AddComponent<RectTransform>();
        StretchRect(bgRect, 0f, 0f, 0f, 0f);
        var bgImg = bgGo.AddComponent<Image>();
        bgImg.sprite = GetUiWhiteSprite();
        bgImg.color = new Color(0.2f, 0.2f, 0.24f, 1f);

        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderGo.transform, false);
        var fillAreaRect = fillArea.AddComponent<RectTransform>();
        StretchRect(fillAreaRect, 8f, 8f, -8f, -8f);

        var fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        var fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        var fillImg = fill.AddComponent<Image>();
        fillImg.sprite = GetUiWhiteSprite();
        fillImg.color = new Color(0.25f, 0.55f, 0.85f, 1f);

        var handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderGo.transform, false);
        var handleAreaRect = handleArea.AddComponent<RectTransform>();
        StretchRect(handleAreaRect, 8f, 0f, -8f, 0f);

        var handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        var handleRect = handle.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(18f, 0f);
        var handleImg = handle.AddComponent<Image>();
        handleImg.sprite = GetUiWhiteSprite();
        handleImg.color = Color.white;

        var slider = sliderGo.AddComponent<Slider>();
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.5f;
        return slider;
    }

    [MenuItem("Beat Defender/Rebuild Pause And Settings UI")]
    public static void RebuildPauseUiOnly()
    {
        var pause = GameObject.Find("PauseMenu");
        var controller = GameObject.Find("PauseMenuController");
        if (pause != null)
            Object.DestroyImmediate(pause);
        if (controller != null)
            Object.DestroyImmediate(controller);

        var ui = GameObject.Find("--- UI ---");
        if (ui == null)
        {
            Debug.LogError("Beat Defender: --- UI --- not found.");
            return;
        }

        CreatePauseMenu(ui.transform);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("Beat Defender: Pause + Settings UI rebuilt.");
    }

    static void CreateResultScreen(Transform canvasRoot)
    {
        var host = new GameObject("ResultScreenHost");
        host.transform.SetParent(canvasRoot, false);
        var hostRect = host.AddComponent<RectTransform>();
        StretchRect(hostRect, 0f, 0f, 0f, 0f);

        var root = new GameObject("Panel");
        root.transform.SetParent(host.transform, false);
        var rt = root.AddComponent<RectTransform>();
        StretchRect(rt, 0f, 0f, 0f, 0f);

        var bg = root.AddComponent<Image>();
        bg.sprite = GetUiWhiteSprite();
        bg.color = new Color(0f, 0f, 0f, 0.78f);

        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(root.transform, false);
        var titleRect = titleGo.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.62f);
        titleRect.anchorMax = new Vector2(0.5f, 0.62f);
        titleRect.sizeDelta = new Vector2(900f, 80f);
        var titleTmp = CreateTmp(titleGo);
        titleTmp.fontSize = 48f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;

        var detailGo = new GameObject("Detail");
        detailGo.transform.SetParent(root.transform, false);
        var detailRect = detailGo.AddComponent<RectTransform>();
        detailRect.anchorMin = new Vector2(0.5f, 0.45f);
        detailRect.anchorMax = new Vector2(0.5f, 0.45f);
        detailRect.sizeDelta = new Vector2(900f, 180f);
        var detailTmp = CreateTmp(detailGo);
        detailTmp.fontSize = 24f;
        detailTmp.alignment = TextAlignmentOptions.Center;
        detailTmp.raycastTarget = false;

        var restartBtn = CreateMenuButton(root.transform, "Btn_Restart", "재시작",
            new Vector2(0.5f, 0.25f), new Vector2(0.5f, 0.25f), new Vector2(-90f, 0f), new Vector2(220f, 52f));
        var titleBtn = CreateMenuButton(root.transform, "Btn_Title", "시작 화면",
            new Vector2(0.5f, 0.25f), new Vector2(0.5f, 0.25f), new Vector2(90f, 0f), new Vector2(220f, 52f));

        root.SetActive(false);

        var resultUi = host.AddComponent<ResultScreenUI>();
        var so = new SerializedObject(resultUi);
        so.FindProperty("panelRoot").objectReferenceValue = root;
        so.FindProperty("titleText").objectReferenceValue = titleTmp;
        so.FindProperty("detailText").objectReferenceValue = detailTmp;
        so.FindProperty("restartButton").objectReferenceValue = restartBtn;
        so.FindProperty("titleButton").objectReferenceValue = titleBtn;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static Button CreateMenuButton(
        Transform parent,
        string name,
        string label,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPos,
        Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var bg = go.AddComponent<Image>();
        bg.sprite = GetUiWhiteSprite();
        bg.color = new Color(0.18f, 0.42f, 0.72f, 0.95f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = bg;

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform, false);
        var labelRect = labelGo.AddComponent<RectTransform>();
        StretchRect(labelRect, 0f, 0f, 0f, 0f);
        var tmp = CreateTmp(labelGo);
        tmp.text = label;
        tmp.fontSize = 24f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        return btn;
    }

    static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null)
            return;

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    static GameObject CreateRhythmDebugPanel(Transform canvasRoot)
    {
        var panel = new GameObject("RhythmDebugPanel");
        panel.transform.SetParent(canvasRoot, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(16f, -16f);
        panelRect.sizeDelta = new Vector2(520f, 320f);

        var bg = panel.AddComponent<Image>();
        bg.sprite = GetUiWhiteSprite();
        bg.color = new Color(0f, 0f, 0f, 0.55f);

        var textGo = new GameObject("StatusText");
        textGo.transform.SetParent(panel.transform, false);
        var textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12f, 12f);
        textRect.offsetMax = new Vector2(-12f, -12f);

        var tmp = CreateTmp(textGo);
        tmp.fontSize = 22f;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.richText = true;
        tmp.raycastTarget = false;

        var ui = panel.AddComponent<RhythmDebugUI>();
        var so = new SerializedObject(ui);
        so.FindProperty("statusText").objectReferenceValue = tmp;
        so.ApplyModifiedPropertiesWithoutUndo();
        return panel;
    }

    static void CreateJudgmentFlash(Transform canvasRoot)
    {
        var root = new GameObject("JudgmentFlashUI");
        root.transform.SetParent(canvasRoot, false);
        var rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = new Vector2(0f, 80f);
        rootRect.sizeDelta = new Vector2(900f, 220f);

        var textGo = new GameObject("FlashText");
        textGo.transform.SetParent(root.transform, false);
        var textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var flashTmp = CreateTmp(textGo);
        flashTmp.fontSize = 72f;
        flashTmp.fontStyle = FontStyles.Bold;
        flashTmp.alignment = TextAlignmentOptions.Center;
        flashTmp.richText = true;
        flashTmp.raycastTarget = false;

        var flashUi = root.AddComponent<JudgmentFlashUI>();
        var flashSo = new SerializedObject(flashUi);
        flashSo.FindProperty("flashText").objectReferenceValue = flashTmp;
        flashSo.ApplyModifiedPropertiesWithoutUndo();
    }

    static void CreateRhythmTimeline(Transform canvasRoot)
    {
        var root = new GameObject("RhythmTimeline");
        root.transform.SetParent(canvasRoot, false);
        var rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0f);
        rootRect.anchorMax = new Vector2(0.5f, 0f);
        rootRect.pivot = new Vector2(0.5f, 0f);
        rootRect.anchoredPosition = new Vector2(0f, 20f);
        rootRect.sizeDelta = new Vector2(480f, 28f);

        var bg = root.AddComponent<Image>();
        bg.sprite = GetUiWhiteSprite();
        bg.color = new Color(0f, 0f, 0f, 0.45f);
        bg.raycastTarget = false;

        var trackArea = CreateUiRect("TrackArea", rootRect);
        StretchRect(trackArea, 8f, 6f, -8f, -6f);

        CreateUiBar("CapLeft", trackArea,
            new Color(0.85f, 0.85f, 0.85f, 0.95f),
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(3f, 22f), Vector2.zero);

        CreateUiBar("CapRight", trackArea,
            new Color(0.85f, 0.85f, 0.85f, 0.95f),
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(3f, 22f), Vector2.zero);

        CreateUiBar("TrackLine", trackArea,
            new Color(0.55f, 0.55f, 0.55f, 0.9f),
            new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 2f), Vector2.zero);

        CreateUiBar("BeatMid", trackArea,
            new Color(0.75f, 0.75f, 0.75f, 0.35f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(2f, 12f), Vector2.zero);

        var playhead = CreateUiBar("Playhead", trackArea,
            new Color(0.31f, 0.76f, 0.97f, 1f),
            Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f),
            new Vector2(3f, 20f), Vector2.zero);

        var markers = CreateUiRect("Markers", trackArea);
        StretchRect(markers, 0f, 0f, 0f, 0f);

        var timeline = root.AddComponent<RhythmTimelineUI>();
        var so = new SerializedObject(timeline);
        so.FindProperty("playhead").objectReferenceValue = playhead;
        so.FindProperty("markersRoot").objectReferenceValue = markers;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void CreateGameHud(Transform canvasRoot)
    {
        var root = new GameObject("GameHud");
        root.transform.SetParent(canvasRoot, false);
        var rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 1f);
        rootRect.anchorMax = new Vector2(1f, 1f);
        rootRect.pivot = new Vector2(0.5f, 1f);
        rootRect.anchoredPosition = new Vector2(0f, -8f);
        rootRect.sizeDelta = new Vector2(-32f, 72f);

        var bg = root.AddComponent<Image>();
        bg.sprite = GetUiWhiteSprite();
        bg.color = new Color(0f, 0f, 0f, 0.45f);
        bg.raycastTarget = false;

        var statusGo = new GameObject("StatusLine");
        statusGo.transform.SetParent(root.transform, false);
        var statusRect = statusGo.AddComponent<RectTransform>();
        StretchRect(statusRect, 16f, 34f, -16f, -8f);
        var statusTmp = CreateTmp(statusGo);
        statusTmp.fontSize = 26f;
        statusTmp.alignment = TextAlignmentOptions.MidlineLeft;
        statusTmp.richText = true;
        statusTmp.raycastTarget = false;

        var detailGo = new GameObject("DetailLine");
        detailGo.transform.SetParent(root.transform, false);
        var detailRect = detailGo.AddComponent<RectTransform>();
        StretchRect(detailRect, 16f, 8f, -16f, -38f);
        var detailTmp = CreateTmp(detailGo);
        detailTmp.fontSize = 20f;
        detailTmp.color = new Color(0.85f, 0.85f, 0.85f);
        detailTmp.alignment = TextAlignmentOptions.MidlineLeft;
        detailTmp.richText = true;
        detailTmp.raycastTarget = false;

        var hud = root.AddComponent<GameHudUI>();
        var so = new SerializedObject(hud);
        so.FindProperty("statusLine").objectReferenceValue = statusTmp;
        so.FindProperty("detailLine").objectReferenceValue = detailTmp;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void CreateRhythmScroll(Transform canvasRoot)
    {
        var root = new GameObject("RhythmScroll");
        root.transform.SetParent(canvasRoot, false);
        var panelRect = root.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 0.5f);
        panelRect.anchoredPosition = new Vector2(8f, 0f);
        panelRect.sizeDelta = new Vector2(280f, -120f);

        var bg = root.AddComponent<Image>();
        bg.sprite = GetUiWhiteSprite();
        bg.color = new Color(0f, 0f, 0f, 0.5f);
        bg.raycastTarget = false;

        var scroll = root.AddComponent<RhythmScrollUI>();
        var cards = new RhythmScrollUI.PatternCard[4];
        cards[0] = CreateScrollCard(root.transform, CommandType.GoldPulse, 0, new Color(1f, 0.84f, 0.31f));
        cards[1] = CreateScrollCard(root.transform, CommandType.RhythmShot, 1, new Color(0.92f, 0.92f, 0.92f));
        cards[2] = CreateScrollCard(root.transform, CommandType.OverloadStrike, 2, new Color(0.94f, 0.33f, 0.31f));
        cards[3] = CreateScrollCard(root.transform, CommandType.BPMBoost, 3, new Color(0.81f, 0.58f, 0.85f));

        var so = new SerializedObject(scroll);
        so.FindProperty("panelRect").objectReferenceValue = panelRect;
        so.FindProperty("cards").arraySize = 4;
        for (int i = 0; i < 4; i++)
        {
            var elem = so.FindProperty("cards").GetArrayElementAtIndex(i);
            elem.FindPropertyRelative("Type").enumValueIndex = (int)cards[i].Type;
            elem.FindPropertyRelative("Root").objectReferenceValue = cards[i].Root;
            elem.FindPropertyRelative("TitleText").objectReferenceValue = cards[i].TitleText;
            elem.FindPropertyRelative("PatternText").objectReferenceValue = cards[i].PatternText;
            elem.FindPropertyRelative("CooldownText").objectReferenceValue = cards[i].CooldownText;
            elem.FindPropertyRelative("AccentBar").objectReferenceValue = cards[i].AccentBar;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static RhythmScrollUI.PatternCard CreateScrollCard(
        Transform parent, CommandType type, int index, Color accent)
    {
        var cardGo = new GameObject($"Card_{type}");
        cardGo.transform.SetParent(parent, false);
        var cardRect = cardGo.AddComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0f, 1f);
        cardRect.anchorMax = new Vector2(1f, 1f);
        cardRect.pivot = new Vector2(0.5f, 1f);
        cardRect.anchoredPosition = new Vector2(0f, -12f - index * 118f);
        cardRect.sizeDelta = new Vector2(-16f, 108f);

        var cardBg = cardGo.AddComponent<Image>();
        cardBg.sprite = GetUiWhiteSprite();
        cardBg.color = new Color(0.08f, 0.08f, 0.1f, 0.92f);
        cardBg.raycastTarget = false;

        var accentBar = CreateUiBar("Accent", cardRect,
            accent, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
            new Vector2(6f, 0f), Vector2.zero);
        accentBar.offsetMin = new Vector2(0f, 0f);
        accentBar.offsetMax = new Vector2(6f, 0f);

        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(cardGo.transform, false);
        var titleRect = titleGo.AddComponent<RectTransform>();
        StretchRect(titleRect, 14f, 62f, -8f, -8f);
        var titleTmp = CreateTmp(titleGo);
        titleTmp.fontSize = 24f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.raycastTarget = false;

        var patternGo = new GameObject("Pattern");
        patternGo.transform.SetParent(cardGo.transform, false);
        var patternRect = patternGo.AddComponent<RectTransform>();
        StretchRect(patternRect, 14f, 30f, -8f, -38f);
        var patternTmp = CreateTmp(patternGo);
        patternTmp.fontSize = 18f;
        patternTmp.color = new Color(0.82f, 0.82f, 0.82f);
        patternTmp.raycastTarget = false;

        var cdGo = new GameObject("Cooldown");
        cdGo.transform.SetParent(cardGo.transform, false);
        var cdRect = cdGo.AddComponent<RectTransform>();
        StretchRect(cdRect, 14f, 8f, -8f, -72f);
        var cdTmp = CreateTmp(cdGo);
        cdTmp.fontSize = 16f;
        cdTmp.color = new Color(1f, 0.55f, 0.45f);
        cdTmp.raycastTarget = false;

        return new RhythmScrollUI.PatternCard
        {
            Type = type,
            Root = cardGo,
            TitleText = titleTmp,
            PatternText = patternTmp,
            CooldownText = cdTmp,
            AccentBar = accentBar.GetComponent<Image>()
        };
    }

    static void CreateBeatPulseRail(Transform canvasRoot)
    {
        var root = new GameObject("BeatPulseRail");
        root.transform.SetParent(canvasRoot, false);
        var rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0f);
        rootRect.anchorMax = new Vector2(0.5f, 0f);
        rootRect.pivot = new Vector2(0.5f, 0f);
        rootRect.anchoredPosition = new Vector2(0f, 56f);
        rootRect.sizeDelta = new Vector2(640f, 40f);

        var bg = root.AddComponent<Image>();
        bg.sprite = GetUiWhiteSprite();
        bg.color = new Color(0f, 0f, 0f, 0.42f);
        bg.raycastTarget = false;

        var flashGo = new GameObject("FlashOverlay");
        flashGo.transform.SetParent(root.transform, false);
        var flashRect = flashGo.AddComponent<RectTransform>();
        StretchRect(flashRect, 0f, 0f, 0f, 0f);
        var flashImg = flashGo.AddComponent<Image>();
        flashImg.sprite = GetUiWhiteSprite();
        flashImg.color = Color.clear;
        flashImg.raycastTarget = false;
        flashGo.SetActive(false);

        var pulseGo = new GameObject("PulseRing");
        pulseGo.transform.SetParent(root.transform, false);
        var pulseRect = pulseGo.AddComponent<RectTransform>();
        pulseRect.anchorMin = new Vector2(0.5f, 0.5f);
        pulseRect.anchorMax = new Vector2(0.5f, 0.5f);
        pulseRect.pivot = new Vector2(0.5f, 0.5f);
        pulseRect.sizeDelta = new Vector2(28f, 28f);
        var pulseImg = pulseGo.AddComponent<Image>();
        pulseImg.sprite = GetUiWhiteSprite();
        pulseImg.color = new Color(0.31f, 0.76f, 0.97f, 0.95f);
        pulseImg.raycastTarget = false;

        var rail = root.AddComponent<BeatPulseRailUI>();
        var so = new SerializedObject(rail);
        so.FindProperty("railBackground").objectReferenceValue = bg;
        so.FindProperty("pulseAnchor").objectReferenceValue = pulseRect;
        so.FindProperty("pulseRing").objectReferenceValue = pulseImg;
        so.FindProperty("flashOverlay").objectReferenceValue = flashImg;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void CreateBoostBorderOverlay(Transform canvasRoot)
    {
        var go = new GameObject("BoostBorderOverlay");
        go.transform.SetParent(canvasRoot, false);
        var rt = go.AddComponent<RectTransform>();
        StretchRect(rt, 0f, 0f, 0f, 0f);
        var img = go.AddComponent<Image>();
        img.sprite = GetUiWhiteSprite();
        img.color = new Color(1f, 0.55f, 0.12f, 0.35f);
        img.raycastTarget = false;
        go.SetActive(false);

        var rail = Object.FindAnyObjectByType<BeatPulseRailUI>();
        if (rail != null)
        {
            var so = new SerializedObject(rail);
            so.FindProperty("boostBorderOverlay").objectReferenceValue = img;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    static void CreateTowerTypeSelect(Transform canvasRoot)
    {
        var root = new GameObject("TowerTypeSelect");
        root.transform.SetParent(canvasRoot, false);
        var rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(1f, 0f);
        rootRect.anchorMax = new Vector2(1f, 0f);
        rootRect.pivot = new Vector2(1f, 0f);
        rootRect.anchoredPosition = new Vector2(-16f, 56f);
        rootRect.sizeDelta = new Vector2(360f, 88f);

        var layout = root.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleRight;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        var ui = root.AddComponent<TowerTypeSelectUI>();
        var buttons = new TowerTypeSelectUI.TowerButton[3];
        buttons[0] = CreateTowerButton(root.transform, TowerType.Beat);
        buttons[1] = CreateTowerButton(root.transform, TowerType.Strike);
        buttons[2] = CreateTowerButton(root.transform, TowerType.Boost);

        var so = new SerializedObject(ui);
        so.FindProperty("towerButtons").arraySize = 3;
        for (int i = 0; i < 3; i++)
        {
            var elem = so.FindProperty("towerButtons").GetArrayElementAtIndex(i);
            elem.FindPropertyRelative("Type").enumValueIndex = (int)buttons[i].Type;
            elem.FindPropertyRelative("Background").objectReferenceValue = buttons[i].Background;
            elem.FindPropertyRelative("Label").objectReferenceValue = buttons[i].Label;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void CreateTowerSellPanel(Transform canvasRoot)
    {
        var root = new GameObject("TowerSellUI");
        root.transform.SetParent(canvasRoot, false);
        var rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = new Vector2(0f, -40f);
        rootRect.sizeDelta = new Vector2(220f, 120f);

        var bg = root.AddComponent<Image>();
        bg.sprite = GetUiWhiteSprite();
        bg.color = new Color(0.08f, 0.08f, 0.1f, 0.92f);

        var infoGo = new GameObject("Info");
        infoGo.transform.SetParent(root.transform, false);
        var infoRect = infoGo.AddComponent<RectTransform>();
        StretchRect(infoRect, 12f, 48f, -12f, -12f);
        var infoTmp = CreateTmp(infoGo);
        infoTmp.fontSize = 22f;
        infoTmp.alignment = TextAlignmentOptions.Center;
        infoTmp.raycastTarget = false;

        var btnGo = new GameObject("SellButton");
        btnGo.transform.SetParent(root.transform, false);
        var btnRect = btnGo.AddComponent<RectTransform>();
        StretchRect(btnRect, 24f, 12f, -24f, -72f);
        var btnBg = btnGo.AddComponent<Image>();
        btnBg.sprite = GetUiWhiteSprite();
        btnBg.color = new Color(0.75f, 0.22f, 0.22f, 0.95f);
        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = btnBg;

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(btnGo.transform, false);
        var labelRect = labelGo.AddComponent<RectTransform>();
        StretchRect(labelRect, 0f, 0f, 0f, 0f);
        var labelTmp = CreateTmp(labelGo);
        labelTmp.text = "Sell 50%";
        labelTmp.fontSize = 20f;
        labelTmp.alignment = TextAlignmentOptions.Center;
        labelTmp.raycastTarget = false;

        root.SetActive(false);

        var sellUi = root.AddComponent<TowerSellUI>();
        var so = new SerializedObject(sellUi);
        so.FindProperty("panelRoot").objectReferenceValue = root;
        so.FindProperty("infoText").objectReferenceValue = infoTmp;
        so.FindProperty("sellButton").objectReferenceValue = btn;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static TowerTypeSelectUI.TowerButton CreateTowerButton(Transform parent, TowerType type)
    {
        var go = new GameObject($"Btn_{type}");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(110f, 72f);

        var bg = go.AddComponent<Image>();
        bg.sprite = GetUiWhiteSprite();
        bg.color = new Color(0.12f, 0.12f, 0.14f, 0.88f);

        var control = go.AddComponent<TowerTypeButton>();
        control.Type = type;

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform, false);
        var labelRect = labelGo.AddComponent<RectTransform>();
        StretchRect(labelRect, 4f, 4f, -4f, -4f);
        var labelTmp = CreateTmp(labelGo);
        labelTmp.fontSize = 20f;
        labelTmp.alignment = TextAlignmentOptions.Center;
        labelTmp.raycastTarget = false;

        return new TowerTypeSelectUI.TowerButton
        {
            Type = type,
            Background = bg,
            Label = labelTmp
        };
    }

    static RectTransform CreateUiRect(string name, RectTransform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static RectTransform CreateUiBar(
        string name,
        RectTransform parent,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 size,
        Vector2 anchoredPos)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;

        var img = go.GetComponent<Image>();
        img.sprite = GetUiWhiteSprite();
        img.color = color;
        img.raycastTarget = false;
        return rt;
    }

    static void StretchRect(RectTransform rt, float left, float bottom, float right, float top)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(right, top);
    }

    static Sprite _uiWhiteSprite;

    static Sprite GetUiWhiteSprite()
    {
        if (_uiWhiteSprite != null)
            return _uiWhiteSprite;

        var tex = new Texture2D(2, 2);
        tex.SetPixel(0, 0, Color.white);
        tex.SetPixel(1, 0, Color.white);
        tex.SetPixel(0, 1, Color.white);
        tex.SetPixel(1, 1, Color.white);
        tex.Apply();

        _uiWhiteSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
        return _uiWhiteSprite;
    }
}
#endif
