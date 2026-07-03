#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
static class CorePushSceneAutoBuilder
{
    static CorePushSceneAutoBuilder()
    {
        EditorApplication.delayCall += TryAutoBuildIfNeeded;
    }

    static void TryAutoBuildIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        var scene = SceneManager.GetActiveScene();
        if (scene.path == null || !scene.path.Replace('\\', '/').EndsWith("Assets/Scenes/SampleScene.unity"))
            return;

        if (GameObject.Find("Managers") != null)
            return;

        CorePushSceneBuilder.BuildScene();
    }
}

public static class CorePushSceneBuilder
{
    const string ScenePath = "Assets/Scenes/SampleScene.unity";
    const string SpritesFolder = "Assets/Sprites";
    const string PrefabsFolder = "Assets/Prefabs";

    [MenuItem("Core Push/Build Scene Hierarchy")]
    public static void BuildScene()
    {
        EnsureFolders();
        var squareSprite = GetOrCreateSprite("Square", new Color(1f, 1f, 1f, 1f), true);
        var circleSprite = GetOrCreateSprite("Circle", new Color(1f, 1f, 1f, 1f), false);

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ClearGameplayObjects();

        SetupCamera();
        var managers = CreateManagers();
        var coreBase = CreateCoreBase(squareSprite);
        CreateMap(squareSprite);
        var spawnPoints = CreateSpawnPoints(squareSprite);
        var player = CreatePlayer(circleSprite, coreBase.transform);
        var spawner = CreateEnemySpawner(spawnPoints, coreBase.transform);
        CreateCanvas(coreBase.GetComponent<BaseHealth>(), player.GetComponent<PlayerAttack>());

        WireManagers(spawner);
        Selection.activeGameObject = managers;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Core Push: Scene hierarchy built and saved.");
    }

    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Sprites"))
            AssetDatabase.CreateFolder("Assets", "Sprites");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Editor"))
            AssetDatabase.CreateFolder("Assets", "Editor");
    }

    static Sprite GetOrCreateSprite(string name, Color color, bool square)
    {
        var path = $"{SpritesFolder}/{name}.png";
        Texture2D texture;

        if (File.Exists(path))
        {
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        else
        {
            const int size = 32;
            texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var pixel = Color.clear;
                    if (square)
                    {
                        pixel = color;
                    }
                    else
                    {
                        var center = new Vector2(size * 0.5f, size * 0.5f);
                        if (Vector2.Distance(new Vector2(x, y), center) <= size * 0.45f)
                            pixel = color;
                    }

                    texture.SetPixel(x, y, pixel);
                }
            }

            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
        }

        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static void ClearGameplayObjects()
    {
        var toDestroy = new[]
        {
            "GameBootstrap", "Managers", "Map", "CoreBase", "Player",
            "SpawnPoints", "EnemySpawner", "Canvas"
        };

        foreach (var name in toDestroy)
        {
            var obj = GameObject.Find(name);
            if (obj != null)
                Object.DestroyImmediate(obj);
        }
    }

    static void SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null)
            return;

        cam.orthographic = true;
        cam.orthographicSize = 7f;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.backgroundColor = new Color(0.08f, 0.1f, 0.15f);

        if (cam.GetComponent<ScreenShake>() == null)
            cam.gameObject.AddComponent<ScreenShake>();
    }

    static GameObject CreateManagers()
    {
        var go = new GameObject("Managers");
        go.AddComponent<GameManager>();
        go.AddComponent<ResourceManager>();
        go.AddComponent<WaveManager>();
        go.AddComponent<SimpleAudio>();
        go.AddComponent<GameStarter>();
        return go;
    }

    static GameObject CreateCoreBase(Sprite sprite)
    {
        var go = new GameObject("CoreBase");
        go.transform.position = Vector3.zero;
        go.transform.localScale = Vector3.one * 1.5f;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(0.2f, 0.85f, 1f);
        sr.sortingOrder = 2;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;
        go.AddComponent<BaseHealth>();
        return go;
    }

    static void CreateMap(Sprite sprite)
    {
        var map = new GameObject("Map");
        CreateWall(map.transform, sprite, new Vector2(0f, 5.5f), new Vector2(14f, 1f));
        CreateWall(map.transform, sprite, new Vector2(0f, -5.5f), new Vector2(14f, 1f));
        CreateWall(map.transform, sprite, new Vector2(-7f, 0f), new Vector2(1f, 10f));
        CreateWall(map.transform, sprite, new Vector2(7f, 0f), new Vector2(1f, 10f));
        CreateWall(map.transform, sprite, new Vector2(-3f, 2f), new Vector2(2f, 0.5f));
        CreateWall(map.transform, sprite, new Vector2(3f, -2f), new Vector2(2f, 0.5f));
    }

    static void CreateWall(Transform parent, Sprite sprite, Vector2 position, Vector2 size)
    {
        var go = new GameObject("Wall");
        go.transform.SetParent(parent);
        go.transform.position = position;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(0.25f, 0.28f, 0.35f);
        sr.sortingOrder = 0;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;
    }

    static Transform[] CreateSpawnPoints(Sprite sprite)
    {
        var positions = new[]
        {
            new Vector3(-6f, 4f, 0f),
            new Vector3(6f, 4f, 0f),
            new Vector3(-6f, -4f, 0f),
            new Vector3(6f, -4f, 0f),
        };

        var parent = new GameObject("SpawnPoints").transform;
        var points = new Transform[positions.Length];

        for (var i = 0; i < positions.Length; i++)
        {
            var go = new GameObject($"SpawnPoint_{i + 1}");
            go.transform.SetParent(parent);
            go.transform.position = positions[i];
            go.transform.localScale = Vector3.one * 0.5f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(1f, 0.3f, 0.3f, 0.4f);
            sr.sortingOrder = 1;

            points[i] = go.transform;
        }

        return points;
    }

    static GameObject CreatePlayer(Sprite sprite, Transform baseTransform)
    {
        var go = new GameObject("Player");
        go.tag = "Player";
        go.transform.position = new Vector3(0f, -2f, 0f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(0.3f, 1f, 0.5f);
        sr.sortingOrder = 10;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.35f;

        go.AddComponent<PlayerController>();
        var attack = go.AddComponent<PlayerAttack>();
        SetSerializedField(attack, "baseTransform", baseTransform);
        return go;
    }

    static EnemySpawner CreateEnemySpawner(Transform[] spawnPoints, Transform baseTransform)
    {
        var go = new GameObject("EnemySpawner");
        var spawner = go.AddComponent<EnemySpawner>();
        SetSerializedField(spawner, "spawnPoints", spawnPoints);
        SetSerializedField(spawner, "baseTransform", baseTransform);
        SetSerializedField(spawner, "scoutPrefab", GetOrCreateEnemyPrefab(EnemyType.Scout));
        SetSerializedField(spawner, "brutePrefab", GetOrCreateEnemyPrefab(EnemyType.Brute));
        return spawner;
    }

    static GameObject GetOrCreateEnemyPrefab(EnemyType type)
    {
        var path = $"{PrefabsFolder}/{(type == EnemyType.Scout ? "Scout" : "Brute")}.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
            return existing;

        var circleSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesFolder}/Circle.png");
        var go = new GameObject(type == EnemyType.Scout ? "Scout" : "Brute");

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = circleSprite;
        sr.sortingOrder = 5;
        sr.color = type == EnemyType.Scout
            ? new Color(1f, 0.45f, 0.2f)
            : new Color(0.75f, 0.15f, 0.15f);
        go.transform.localScale = Vector3.one * (type == EnemyType.Scout ? 0.7f : 1.1f);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var bodyCol = go.AddComponent<CircleCollider2D>();
        bodyCol.isTrigger = false;
        bodyCol.radius = 0.4f;

        var triggerCol = go.AddComponent<CircleCollider2D>();
        triggerCol.isTrigger = true;
        triggerCol.radius = 0.45f;

        var health = go.AddComponent<EnemyHealth>();
        health.Configure(type);
        go.AddComponent<EnemyAI>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    static void CreateCanvas(BaseHealth baseHealth, PlayerAttack playerAttack)
    {
        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        var hudGo = new GameObject("HUD");
        hudGo.transform.SetParent(canvasGo.transform, false);
        var hud = hudGo.AddComponent<GameHUD>();

        var waveText = CreateTmpText(hudGo.transform, "WaveText", new Vector2(10, -10), TextAlignmentOptions.TopLeft, 24);
        var goldText = CreateTmpText(hudGo.transform, "GoldText", new Vector2(10, -40), TextAlignmentOptions.TopLeft, 24);
        var upgradeText = CreateTmpText(hudGo.transform, "UpgradeText", new Vector2(10, -70), TextAlignmentOptions.TopLeft, 20);
        var prepText = CreateTmpText(hudGo.transform, "PrepText", new Vector2(10, -100), TextAlignmentOptions.TopLeft, 22);
        var messageText = CreateTmpText(hudGo.transform, "MessageText", new Vector2(0, -120), TextAlignmentOptions.Top, 26);
        SetRect(messageText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -120), new Vector2(800, 40));
        messageText.color = new Color(1f, 0.9f, 0.4f);

        var healthSlider = CreateHealthSlider(hudGo.transform);
        var healthLabel = CreateTmpText(hudGo.transform, "HealthLabel", new Vector2(-10, -10), TextAlignmentOptions.TopRight, 22);
        SetRect(healthLabel.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-10, -10), new Vector2(250, 30));

        var controls = CreateTmpText(hudGo.transform, "Controls", new Vector2(10, -140), TextAlignmentOptions.TopLeft, 16);
        controls.text = "WASD:이동 | 클릭:공격 | Space:밀치기 | E:수리 | U:업그레이드 | ESC:일시정지";

        SetSerializedField(hud, "baseHealth", baseHealth);
        SetSerializedField(hud, "waveText", waveText);
        SetSerializedField(hud, "goldText", goldText);
        SetSerializedField(hud, "prepText", prepText);
        SetSerializedField(hud, "messageText", messageText);
        SetSerializedField(hud, "upgradeText", upgradeText);
        SetSerializedField(hud, "baseHealthSlider", healthSlider);
        SetSerializedField(hud, "baseHealthLabel", healthLabel);

        var overlayGo = new GameObject("GameOverScreen");
        overlayGo.transform.SetParent(canvasGo.transform, false);
        var overlay = overlayGo.AddComponent<GameOverScreen>();

        var panel = CreatePanel(overlayGo.transform);
        var title = CreateTmpText(panel.transform, "Title", Vector2.zero, TextAlignmentOptions.Center, 48);
        SetRect(title.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        title.rectTransform.anchorMin = new Vector2(0, 0.55f);
        title.rectTransform.anchorMax = new Vector2(1, 0.85f);

        var hint = CreateTmpText(panel.transform, "Hint", Vector2.zero, TextAlignmentOptions.Center, 24);
        hint.rectTransform.anchorMin = new Vector2(0, 0.25f);
        hint.rectTransform.anchorMax = new Vector2(1, 0.55f);
        hint.color = new Color(0.85f, 0.85f, 0.85f);

        var restartBtn = CreateButton(panel.transform, "RestartButton", "재시작 (R)");
        SetSerializedField(overlay, "panel", panel);
        SetSerializedField(overlay, "titleText", title);
        SetSerializedField(overlay, "hintText", hint);
        SetSerializedField(overlay, "restartButton", restartBtn);
    }

    static void WireManagers(EnemySpawner spawner)
    {
        var waveManager = Object.FindFirstObjectByType<WaveManager>();
        if (waveManager != null)
            SetSerializedField(waveManager, "spawner", spawner);
    }

    static TextMeshProUGUI CreateTmpText(Transform parent, string name, Vector2 anchoredPos, TextAlignmentOptions alignment, int fontSize)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(600, 30);

        var text = go.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = alignment;
        return text;
    }

    static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;
    }

    static Slider CreateHealthSlider(Transform parent)
    {
        var go = new GameObject("HealthSlider");
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(1, 1);
        rect.anchoredPosition = new Vector2(-10, -45);
        rect.sizeDelta = new Vector2(250, 20);

        var bg = new GameObject("Background");
        bg.transform.SetParent(go.transform, false);
        var bgRect = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        bg.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);

        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(go.transform, false);
        var fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(2, 2);
        fillAreaRect.offsetMax = new Vector2(-2, -2);

        var fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        var fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        var fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.2f, 0.85f, 1f);

        var slider = go.AddComponent<Slider>();
        slider.fillRect = fillRect;
        slider.targetGraphic = fillImage;
        slider.interactable = false;
        slider.value = 1f;
        return slider;
    }

    static GameObject CreatePanel(Transform parent)
    {
        var go = new GameObject("OverlayPanel");
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        go.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);
        go.SetActive(false);
        return go;
    }

    static Button CreateButton(Transform parent, string name, string label)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.35f, 0.08f);
        rect.anchorMax = new Vector2(0.65f, 0.2f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = go.AddComponent<Image>();
        image.color = new Color(0.25f, 0.55f, 0.95f);
        var btn = go.AddComponent<Button>();

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var text = textGo.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            text.font = TMP_Settings.defaultFontAsset;
        text.text = label;
        text.fontSize = 22;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;

        return btn;
    }

    static void SetSerializedField(Object target, string fieldName, object value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop == null)
        {
            Debug.LogWarning($"Field not found: {target.GetType().Name}.{fieldName}");
            return;
        }

        switch (value)
        {
            case Transform transform:
                prop.objectReferenceValue = transform;
                break;
            case BaseHealth baseHealth:
                prop.objectReferenceValue = baseHealth;
                break;
            case EnemySpawner spawner:
                prop.objectReferenceValue = spawner;
                break;
            case GameObject gameObject:
                prop.objectReferenceValue = gameObject;
                break;
            case TextMeshProUGUI tmp:
                prop.objectReferenceValue = tmp;
                break;
            case Slider slider:
                prop.objectReferenceValue = slider;
                break;
            case Button button:
                prop.objectReferenceValue = button;
                break;
            case Transform[] transforms:
                prop.arraySize = transforms.Length;
                for (var i = 0; i < transforms.Length; i++)
                    prop.GetArrayElementAtIndex(i).objectReferenceValue = transforms[i];
                break;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
