using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// TutorialScene UI/월드 계층 — 에디터에서 씬에 bake.
/// </summary>
public static class TutorialVisualBuilder
{
    static readonly Color PanelBg = new(0.06f, 0.07f, 0.12f, 0.94f);
    static readonly Color TitleGold = new(1f, 0.88f, 0.35f, 1f);
    static readonly Color BodyText = new(0.9f, 0.91f, 0.94f, 1f);
    static readonly Color ProgressText = new(0.65f, 0.68f, 0.75f, 0.9f);
    static readonly Color BtnColor = new(0.2f, 0.45f, 0.65f, 1f);
    static readonly Color TowerBtnColor = new(0.12f, 0.12f, 0.14f, 0.88f);

    static Sprite _uiSprite;

    static Sprite UiSprite => _uiSprite ?? GreyboxSprites.Square;

    public static void Build(Transform uiCanvasRoot, Transform sceneRoot, Sprite preferredSprite = null)
    {
        _uiSprite = preferredSprite
            ?? Resources.Load<Sprite>("UI/UiSquare")
            ?? GreyboxSprites.Square;

        if (uiCanvasRoot != null && uiCanvasRoot.Find("TutorialUI") == null)
            BuildTutorialPanel(uiCanvasRoot);

        if (uiCanvasRoot != null && uiCanvasRoot.Find("TowerTypeSelect") == null)
            BuildTowerTypeSelect(uiCanvasRoot);

        if (sceneRoot != null && sceneRoot.Find("TutorialPlacement") == null)
            BuildPlacementWorld(sceneRoot);
    }

    public static void BuildTutorialPanel(Transform uiRoot)
    {
        var tutorialGo = new GameObject("TutorialUI", typeof(RectTransform), typeof(TutorialUI));
        tutorialGo.transform.SetParent(uiRoot, false);
        var rootRt = tutorialGo.GetComponent<RectTransform>();
        StretchFull(rootRt);

        var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        panelGo.transform.SetParent(tutorialGo.transform, false);
        var panel = panelGo.GetComponent<RectTransform>();
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(720f, 340f);
        panel.anchoredPosition = new Vector2(0f, 120f);
        var panelImg = panelGo.GetComponent<Image>();
        panelImg.sprite = UiSprite;
        panelImg.color = PanelBg;

        var progress = CreateText(panel, "Progress", 14f,
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(16f, -12f), new Vector2(-16f, -36f),
            TextAlignmentOptions.TopRight);
        progress.color = ProgressText;
        progress.text = "1 / 10";

        var title = CreateText(panel, "Title", 28f,
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(24f, 11f), new Vector2(-24f, -33f),
            TextAlignmentOptions.TopLeft);
        title.fontStyle = FontStyles.Bold;
        title.color = TitleGold;
        title.text = "Beat Defender";

        var body = CreateText(panel, "Body", 19f,
            new Vector2(0f, 0f), new Vector2(1f, 1f),
            new Vector2(24f, 72f), new Vector2(-24f, -96f),
            TextAlignmentOptions.TopLeft);
        body.color = BodyText;
        body.enableWordWrapping = true;
        body.text = "튜토리얼 안내";

        CreateButton(panel, "Btn_Next", "다음", new Vector2(1f, 0f), new Vector2(-24f, 24f));
        CreateButton(panel, "Btn_Skip", "건너뛰기", new Vector2(0f, 0f), new Vector2(24f, 24f));
    }

    public static void BuildTowerTypeSelect(Transform uiRoot)
    {
        var root = new GameObject("TowerTypeSelect", typeof(RectTransform));
        root.transform.SetParent(uiRoot, false);

        var rt = root.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-40f, 56f);
        rt.sizeDelta = new Vector2(360f, 88f);

        var layout = root.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleRight;
        layout.spacing = 8f;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        var btnGo = new GameObject("Btn_Beat", typeof(RectTransform), typeof(Image), typeof(TowerTypeButton));
        btnGo.transform.SetParent(root.transform, false);
        var btnImg = btnGo.GetComponent<Image>();
        btnImg.sprite = UiSprite;
        btnImg.color = TowerBtnColor;
        btnImg.raycastTarget = true;

        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(btnGo.transform, false);
        StretchFull(labelGo.GetComponent<RectTransform>());
        var label = labelGo.AddComponent<TextMeshProUGUI>();
        label.text = $"Tower\n{TowerPlacer.TowerCost}G";
        label.fontSize = 18f;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        BeatDefenderFonts.Apply(label);

        var selectUi = root.AddComponent<TowerTypeSelectUI>();
        selectUi.SetTowerButtons(new[]
        {
            new TowerTypeSelectUI.TowerButton { Background = btnImg, Label = label }
        });
    }

    public static GameObject BuildPlacementWorld(Transform sceneRoot)
    {
        var worldRoot = new GameObject("TutorialPlacement");
        worldRoot.transform.SetParent(sceneRoot, false);

        var gridGo = new GameObject("PlacementGrid");
        gridGo.transform.SetParent(worldRoot.transform, false);
        var grid = gridGo.AddComponent<PlacementGrid>();

        var registry = MapPrefabRegistry.Instance;
        GameObject slotGo;
        if (registry != null && registry.TowerPlacementCell != null)
        {
            slotGo = PrefabSpawnUtility.Instantiate(registry.TowerPlacementCell, gridGo.transform);
            slotGo.name = "Slot_0";
        }
        else
        {
            slotGo = new GameObject("Slot_0");
            slotGo.transform.SetParent(gridGo.transform, false);
            slotGo.AddComponent<TowerPlacementCell>();
        }

        slotGo.transform.position = new Vector3(0f, 0.5f, 0f);
        var cell = slotGo.GetComponent<TowerPlacementCell>();
        cell.Initialize(0, new Vector2(0f, 0.5f));
        cell.ConfigureUnlock(true, 0);

        var towersRoot = new GameObject("Towers");
        towersRoot.transform.SetParent(worldRoot.transform, false);

        return worldRoot;
    }

    static TextMeshProUGUI CreateText(
        RectTransform parent,
        string name,
        float fontSize,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax,
        TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        BeatDefenderFonts.Apply(tmp);
        return tmp;
    }

    static Button CreateButton(RectTransform parent, string name, string label, Vector2 anchor, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.sizeDelta = new Vector2(140f, 40f);
        rt.anchoredPosition = pos;
        var img = go.GetComponent<Image>();
        img.sprite = UiSprite;
        img.color = BtnColor;

        var textGo = new GameObject("Label", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        StretchFull(textGo.GetComponent<RectTransform>());
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 17f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        BeatDefenderFonts.Apply(tmp);

        return go.GetComponent<Button>();
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
