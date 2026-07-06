using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// StartScene 정적 UI 계층 생성 — 에디터에서 씬에 bake.
/// </summary>
public static class StartSceneVisualBuilder
{
    static readonly Color BgTop = new(0.04f, 0.05f, 0.11f, 1f);
    static readonly Color BgBottom = new(0.07f, 0.09f, 0.16f, 1f);
    static readonly Color Gold = new(1f, 0.84f, 0.31f, 1f);
    static readonly Color Cyan = new(0.35f, 0.88f, 1f, 1f);
    static readonly Color Purple = new(0.62f, 0.55f, 0.95f, 1f);

    static Sprite _uiSprite;

    const string ResourcesSpritePath = "UI/UiSquare";

    static Sprite UiSprite => _uiSprite ?? GreyboxSprites.Square;

    static Sprite ResolveUiSpriteFromRoot(RectTransform root)
    {
        if (root == null)
            return null;

        foreach (var name in new[] { "Btn_Start", "Btn_Tutorial", "Btn_Settings", "Btn_Practice", "Btn_Quit" })
        {
            var sprite = root.Find(name)?.GetComponent<Image>()?.sprite;
            if (sprite != null)
                return sprite;
        }

        return null;
    }

    static Sprite LoadResourcesSprite() => Resources.Load<Sprite>(ResourcesSpritePath);

    public static void Build(RectTransform root, Sprite preferredSprite = null)
    {
        if (root == null || root.Find("Background") != null)
            return;

        _uiSprite = preferredSprite
            ?? ResolveUiSpriteFromRoot(root)
            ?? LoadResourcesSprite()
            ?? GreyboxSprites.Square;

        var menuBg = root.GetComponent<Image>();
        if (menuBg != null)
            menuBg.color = Color.clear;

        BuildBackground(root);
        BuildDecor(root);
        StyleTitleAndCopy(root);
        StyleButtons(root);
    }

    static void BuildBackground(RectTransform root)
    {
        var bg = CreateStretchChild(root, "Background", 0);
        StretchFull(bg);

        AddColorLayer(bg, "GradientTop", BgTop, new Vector2(0f, 0.45f), Vector2.one);
        AddColorLayer(bg, "GradientBottom", BgBottom, Vector2.zero, new Vector2(1f, 0.55f));

        var glowGold = CreateImage(bg, "GlowGold");
        StretchRect(glowGold.rectTransform, new Vector2(0.2f, 0.52f), new Vector2(0.8f, 0.88f));
        glowGold.color = new Color(Gold.r, Gold.g, Gold.b, 0.07f);

        var glowCyan = CreateImage(bg, "GlowCyan");
        StretchRect(glowCyan.rectTransform, new Vector2(0.25f, 0.08f), new Vector2(0.75f, 0.42f));
        glowCyan.color = new Color(Cyan.r, Cyan.g, Cyan.b, 0.05f);

        var field = CreateStretchChild(bg, "Starfield", -1);
        StretchFull(field);

        var rng = new System.Random(42);
        for (int i = 0; i < 36; i++)
        {
            var star = CreateImage(field, $"Star_{i}");
            var rt = star.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2((float)rng.NextDouble(), (float)rng.NextDouble());
            rt.sizeDelta = new Vector2(2f + rng.Next(3), 2f + rng.Next(3));
            rt.anchoredPosition = Vector2.zero;
            float a = 0.08f + (float)rng.NextDouble() * 0.22f;
            star.color = rng.NextDouble() > 0.65
                ? new Color(Cyan.r, Cyan.g, Cyan.b, a)
                : new Color(1f, 1f, 1f, a * 0.7f);
        }
    }

    static void BuildDecor(RectTransform root)
    {
        var decor = CreateStretchChild(root, "Decor", 1);
        StretchFull(decor);

        var emblem = new GameObject("CoreEmblem", typeof(RectTransform));
        emblem.transform.SetParent(decor, false);
        var emblemRt = emblem.GetComponent<RectTransform>();
        emblemRt.anchorMin = emblemRt.anchorMax = new Vector2(0.5f, 0.72f);
        emblemRt.sizeDelta = new Vector2(180f, 180f);
        emblemRt.anchoredPosition = new Vector2(0f, 10f);

        var ring = CreateImage(emblem.transform, "Ring");
        StretchFull(ring.rectTransform);
        ring.color = new Color(1f, 0.85f, 0.25f, 0.45f);

        var core = CreateImage(emblem.transform, "Core");
        StretchRect(core.rectTransform, new Vector2(0.28f, 0.28f), new Vector2(0.72f, 0.72f));
        core.color = new Color(0.95f, 0.78f, 0.22f, 0.95f);

        BuildBeatRail(decor);
        BuildSyncSlider(decor);
        BuildFloatingNotes(decor);
    }

    /// <summary>Decor 하단 BeatRail 아래 싱크 조절 슬라이더.</summary>
    public static void BuildSyncSlider(Transform decor)
    {
        if (decor == null || decor.Find("SyncSlider") != null)
            return;

        var root = new GameObject("SyncSlider", typeof(RectTransform));
        root.transform.SetParent(decor, false);
        var rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = rootRt.anchorMax = new Vector2(0.5f, 0f);
        rootRt.pivot = new Vector2(0.5f, 0f);
        rootRt.anchoredPosition = new Vector2(0f, 10f);
        rootRt.sizeDelta = new Vector2(440f, 30f);

        var panel = CreateImage(root.transform, "Panel");
        StretchFull(panel.rectTransform);
        panel.color = new Color(0.08f, 0.1f, 0.14f, 0.72f);
        panel.raycastTarget = true;

        var label = CreateText(root.transform, "Label", "싱크 0ms", 12f);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        var labelRt = label.rectTransform;
        labelRt.anchorMin = new Vector2(0f, 0f);
        labelRt.anchorMax = new Vector2(0f, 1f);
        labelRt.pivot = new Vector2(0f, 0.5f);
        labelRt.anchoredPosition = new Vector2(10f, 0f);
        labelRt.sizeDelta = new Vector2(150f, 0f);
        label.color = new Color(0.72f, 0.78f, 0.88f, 0.9f);

        var slider = CreateHorizontalSlider(root.transform, "Slider");
        var sliderRt = slider.GetComponent<RectTransform>();
        sliderRt.anchorMin = new Vector2(0f, 0.5f);
        sliderRt.anchorMax = new Vector2(1f, 0.5f);
        sliderRt.pivot = new Vector2(0.5f, 0.5f);
        sliderRt.anchoredPosition = Vector2.zero;
        sliderRt.offsetMin = new Vector2(162f, -8f);
        sliderRt.offsetMax = new Vector2(-10f, 8f);
    }

    /// <summary>기존 Decor 아래 BeatRail만 교체(TrackArea 마이그레이션).</summary>
    public static void BuildBeatRailOnly(Transform decor)
    {
        if (decor == null)
            return;

        BuildBeatRail(decor);
    }

    static void BuildBeatRail(Transform decor)
    {
        var railRoot = new GameObject("BeatRail", typeof(RectTransform));
        railRoot.transform.SetParent(decor, false);
        var railRt = railRoot.GetComponent<RectTransform>();
        railRt.anchorMin = new Vector2(0.5f, 0f);
        railRt.anchorMax = new Vector2(0.5f, 0f);
        railRt.pivot = new Vector2(0.5f, 0f);
        railRt.anchoredPosition = new Vector2(0f, 48f);
        railRt.sizeDelta = new Vector2(640f, 36f);

        var trackArea = new GameObject("TrackArea", typeof(RectTransform));
        trackArea.transform.SetParent(railRoot.transform, false);
        StretchFull(trackArea.GetComponent<RectTransform>());

        var track = CreateImage(trackArea.transform, "Track");
        StretchFull(track.rectTransform);
        track.color = new Color(0.1f, 0.12f, 0.18f, 0.85f);

        var glow = CreateImage(trackArea.transform, "TrackGlow");
        StretchFull(glow.rectTransform);
        glow.color = new Color(Cyan.r, Cyan.g, Cyan.b, 0.12f);

        CreateBeatTick(trackArea.transform, 0f, Gold, 28f);
        CreateBeatTick(trackArea.transform, 0.5f, Cyan, 22f);
        CreateBeatTick(trackArea.transform, 1f, Gold, 28f);

        CreateCap(trackArea.transform, "CapLeft", 0f, Cyan);
        CreateCap(trackArea.transform, "CapRight", 1f, Gold);

        var guides = new GameObject("Guides", typeof(RectTransform));
        guides.transform.SetParent(trackArea.transform, false);
        StretchFull(guides.GetComponent<RectTransform>());

        var playhead = CreateImage(trackArea.transform, "Playhead");
        playhead.rectTransform.sizeDelta = new Vector2(5f, 34f);
        playhead.color = Cyan;

        var markers = new GameObject("Markers", typeof(RectTransform));
        markers.transform.SetParent(trackArea.transform, false);
        StretchFull(markers.GetComponent<RectTransform>());

        var bpm = CreateText(railRoot.transform, "BpmLabel", "120 BPM  2/4", 16f);
        var bpmRt = bpm.rectTransform;
        bpmRt.anchorMin = bpmRt.anchorMax = new Vector2(0.5f, 1f);
        bpmRt.anchoredPosition = new Vector2(0f, 14f);
        bpmRt.sizeDelta = new Vector2(200f, 24f);
        bpm.color = new Color(0.75f, 0.78f, 0.85f, 0.75f);
        bpm.alignment = TextAlignmentOptions.Center;
    }

    static void CreateCap(Transform parent, string name, float anchorX, Color color)
    {
        var cap = CreateImage(parent, name);
        var rt = cap.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(anchorX, 0.5f);
        rt.sizeDelta = new Vector2(4f, 30f);
        rt.anchoredPosition = Vector2.zero;
        cap.color = new Color(color.r, color.g, color.b, 0.55f);
    }

    static void CreateBeatTick(Transform parent, float anchorX, Color color, float height)
    {
        var tick = CreateImage(parent, $"Tick_{anchorX:0.##}");
        var rt = tick.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(anchorX, 0.5f);
        rt.sizeDelta = new Vector2(3f, height);
        rt.anchoredPosition = Vector2.zero;
        tick.color = new Color(color.r, color.g, color.b, 0.65f);
    }

    static void BuildFloatingNotes(Transform decor)
    {
        var notesRoot = new GameObject("FloatingNotes", typeof(RectTransform));
        notesRoot.transform.SetParent(decor, false);
        StretchFull(notesRoot.GetComponent<RectTransform>());

        var positions = new[]
        {
            new Vector2(0.12f, 0.38f),
            new Vector2(0.88f, 0.44f),
            new Vector2(0.18f, 0.22f),
            new Vector2(0.82f, 0.28f),
            new Vector2(0.08f, 0.58f),
            new Vector2(0.92f, 0.62f),
        };

        var colors = new[] { Gold, Cyan, Purple, Gold, Cyan, Purple };

        for (int i = 0; i < positions.Length; i++)
        {
            var note = CreateImage(notesRoot.transform, $"Note_{i}");
            var rt = note.rectTransform;
            rt.anchorMin = rt.anchorMax = positions[i];
            rt.sizeDelta = new Vector2(10f, 22f);
            rt.anchoredPosition = Vector2.zero;
            note.color = new Color(colors[i].r, colors[i].g, colors[i].b, 0.35f);
        }
    }

    static void StyleTitleAndCopy(RectTransform root)
    {
        var title = root.Find("Title")?.GetComponent<TextMeshProUGUI>();
        if (title != null)
        {
            var titleRt = title.rectTransform;
            titleRt.anchoredPosition = new Vector2(0f, 28f);
            title.text = "<color=#FFD84F>BEAT</color> <color=#59E0FF>DEFENDER</color>";
            title.fontSize = 78f;
            title.fontStyle = FontStyles.Bold;
            title.enableWordWrapping = false;
            title.alignment = TextAlignmentOptions.Center;
            BeatDefenderFonts.Apply(title);
            EnsureCanvasGroup(titleRt);
        }

        EnsureText(root, "Subtitle", "리듬 타워 디펜스", 26f, new Vector2(0f, -48f), new Vector2(640f, 40f),
            new Color(0.82f, 0.84f, 0.9f, 0.92f));
        EnsureText(root, "Tagline", "Space로 박자 체험, 마우스로 타워, 1분 30초간 코어를 지켜라", 18f, new Vector2(0f, -82f), new Vector2(720f, 32f),
            new Color(0.55f, 0.6f, 0.72f, 0.85f));
    }

    static void EnsureText(RectTransform root, string name, string text, float size, Vector2 pos, Vector2 sizeDelta, Color color)
    {
        var existing = root.Find(name)?.GetComponent<TextMeshProUGUI>();
        var tmp = existing ?? CreateText(root, name, text, size);
        var rt = tmp.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.72f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeDelta;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        BeatDefenderFonts.Apply(tmp);
        EnsureCanvasGroup(rt);
    }

    static void StyleButtons(RectTransform root)
    {
        EnsureMenuButton(root, "Btn_Start");
        EnsureMenuButton(root, "Btn_Tutorial");
        EnsureMenuButton(root, "Btn_Settings");
        EnsureMenuButton(root, "Btn_Quit");

        StyleButton(root, "Btn_Start", Gold, "게임 시작", 20f, applyLayout: true);
        StyleButton(root, "Btn_Tutorial", Cyan, "튜토리얼", -58f, applyLayout: true);
        StyleButton(root, "Btn_Practice", Cyan, "튜토리얼", -58f, applyLayout: true);
        StyleButton(root, "Btn_Settings", Purple, "설정", -97f, applyLayout: true);
        StyleButton(root, "Btn_Quit", new Color(0.45f, 0.48f, 0.55f), "종료", -136f, applyLayout: true);
    }

    public static void EnsureSettingsButton(RectTransform root)
    {
        if (root == null)
            return;

        bool created = EnsureMenuButton(root, "Btn_Settings");
        StyleButton(root, "Btn_Settings", Purple, "설정", -97f, applyLayout: created);
    }

    static bool EnsureMenuButton(RectTransform root, string name)
    {
        if (root.Find(name) != null)
            return false;

        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup));
        go.transform.SetParent(root, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(340f, 58f);

        var img = go.GetComponent<Image>();
        img.sprite = UiSprite;
        img.color = new Color(0.09f, 0.11f, 0.16f, 0.94f);
        img.raycastTarget = true;

        var accentGo = CreateImage(go.transform, "Accent");
        ApplyAccentBar(accentGo.rectTransform, Color.white);

        var label = CreateText(go.transform, "Label", name, 24f);
        StretchFull(label.rectTransform);
        label.alignment = TextAlignmentOptions.Center;
        label.fontStyle = FontStyles.Bold;

        var button = go.GetComponent<Button>();
        button.targetGraphic = img;
        return true;
    }

    static void StyleButton(
        RectTransform root,
        string name,
        Color accent,
        string label,
        float defaultY,
        bool applyLayout)
    {
        var buttonT = root.Find(name);
        if (buttonT == null)
            return;

        var rt = buttonT as RectTransform;
        if (applyLayout)
        {
            rt.sizeDelta = new Vector2(340f, 58f);
            rt.anchoredPosition = new Vector2(0f, defaultY);
        }

        var img = buttonT.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = UiSprite;
            img.color = new Color(0.09f, 0.11f, 0.16f, 0.94f);
        }

        var accentT = buttonT.Find("Accent");
        if (accentT == null)
        {
            var accentGo = CreateImage(buttonT, "Accent");
            accentT = accentGo.rectTransform;
        }

        ApplyAccentBar(accentT as RectTransform, accent);

        var labelText = buttonT.GetComponentInChildren<TextMeshProUGUI>();
        if (labelText != null)
        {
            labelText.text = label;
            labelText.fontSize = 24f;
            labelText.fontStyle = FontStyles.Bold;
            labelText.alignment = TextAlignmentOptions.Center;
            BeatDefenderFonts.Apply(labelText);
        }

        var button = buttonT.GetComponent<Button>();
        if (button != null)
        {
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            button.colors = colors;
        }

        EnsureCanvasGroup(rt);
    }

    static void ApplyAccentBar(RectTransform accentRt, Color accent)
    {
        if (accentRt == null)
            return;

        accentRt.SetAsFirstSibling();
        accentRt.anchorMin = new Vector2(0f, 0f);
        accentRt.anchorMax = new Vector2(0f, 1f);
        accentRt.pivot = new Vector2(0f, 0.5f);
        accentRt.sizeDelta = new Vector2(6f, 0f);
        accentRt.anchoredPosition = Vector2.zero;

        var accentImg = accentRt.GetComponent<Image>();
        if (accentImg != null)
        {
            accentImg.raycastTarget = false;
            accentImg.color = accent;
        }
    }

    static CanvasGroup EnsureCanvasGroup(RectTransform rt)
    {
        var group = rt.GetComponent<CanvasGroup>();
        if (group == null)
            group = rt.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 1f;
        return group;
    }

    static RectTransform CreateStretchChild(Transform parent, string name, int siblingIndex)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        if (siblingIndex >= 0)
            go.transform.SetSiblingIndex(siblingIndex);
        return go.GetComponent<RectTransform>();
    }

    static Image CreateImage(Transform parent, string name, bool raycastTarget = false)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = UiSprite;
        img.raycastTarget = raycastTarget;
        return img;
    }

    static Slider CreateHorizontalSlider(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Slider));
        go.transform.SetParent(parent, false);

        var background = CreateImage(go.transform, "Background", raycastTarget: true);
        StretchFull(background.rectTransform);
        background.color = new Color(0.14f, 0.16f, 0.22f, 1f);

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(go.transform, false);
        var fillAreaRt = fillArea.GetComponent<RectTransform>();
        StretchRect(fillAreaRt, new Vector2(0.02f, 0.22f), new Vector2(0.98f, 0.78f));

        var fill = CreateImage(fillArea.transform, "Fill");
        StretchFull(fill.rectTransform);
        fill.color = new Color(Cyan.r, Cyan.g, Cyan.b, 0.85f);

        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(go.transform, false);
        var handleAreaRt = handleArea.GetComponent<RectTransform>();
        StretchFull(handleAreaRt);

        var handle = CreateImage(handleArea.transform, "Handle", raycastTarget: true);
        var handleRt = handle.rectTransform;
        handleRt.sizeDelta = new Vector2(12f, 0f);
        handleRt.anchorMin = new Vector2(0f, 0f);
        handleRt.anchorMax = new Vector2(0f, 1f);
        handleRt.pivot = new Vector2(0.5f, 0.5f);
        handleRt.anchoredPosition = Vector2.zero;
        handle.color = new Color(0.92f, 0.95f, 1f, 1f);

        var slider = go.GetComponent<Slider>();
        slider.targetGraphic = handle;
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handleRt;
        slider.direction = Slider.Direction.LeftToRight;
        return slider;
    }

    static TextMeshProUGUI CreateText(Transform parent, string name, string text, float size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.raycastTarget = false;
        BeatDefenderFonts.Apply(tmp);
        EnsureCanvasGroup(tmp.rectTransform);
        return tmp;
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void StretchRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void AddColorLayer(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax)
    {
        var img = CreateImage(parent, name);
        StretchRect(img.rectTransform, anchorMin, anchorMax);
        img.color = color;
    }
}
