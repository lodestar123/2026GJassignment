using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 설정 패널 UI bake — 입력 감도(선택) · 효과음 · 배경음.
/// </summary>
public static class SettingsPanelVisualBuilder
{
    public struct BuildOptions
    {
        public bool ShowInputOffset;
        public bool CloseReturnsToPauseMenu;
    }

    static readonly Color PanelBg = new(0.08f, 0.08f, 0.1f, 0.98f);
    static readonly Color Accent = new(0.35f, 0.88f, 1f, 1f);
    static readonly Color LabelColor = new(0.82f, 0.85f, 0.92f, 1f);

    public static SettingsPanelUI Build(RectTransform uiRoot, BuildOptions options)
    {
        if (uiRoot == null)
            return null;

        DestroyExistingPanel(uiRoot);

        var panelRoot = CreatePanelRoot(uiRoot);
        CreateRowLabel(panelRoot.transform, "Title", "설정", -36f, 22f, FontStyles.Bold);

        Slider inputSlider = null;
        TextMeshProUGUI inputLabel = null;
        float nextY = -88f;

        if (options.ShowInputOffset)
        {
            inputLabel = CreateRowLabel(panelRoot.transform, "InputOffsetLabel", "입력 감도", -72f, 16f);
            inputSlider = CreateSliderRow(panelRoot.transform, "InputOffsetSlider", -108f);
            nextY = -168f;
        }

        CreateRowLabel(panelRoot.transform, "SfxLabel", "효과음", nextY, 16f);
        var sfxSlider = CreateSliderRow(panelRoot.transform, "SfxSlider", nextY - 36f);

        CreateRowLabel(panelRoot.transform, "BgmLabel", "배경음", nextY - 96f, 16f);
        var bgmSlider = CreateSliderRow(panelRoot.transform, "BgmSlider", nextY - 132f);

        var closeButton = CreateCloseButton(panelRoot.transform, nextY - 188f);

        float panelHeight = options.ShowInputOffset ? 360f : 300f;
        var panelRt = panelRoot.GetComponent<RectTransform>();
        panelRt.sizeDelta = new Vector2(420f, panelHeight);

        var settingsUi = uiRoot.GetComponent<SettingsPanelUI>();
        if (settingsUi == null)
            settingsUi = uiRoot.gameObject.AddComponent<SettingsPanelUI>();

        settingsUi.Configure(
            panelRoot,
            inputSlider,
            inputLabel,
            sfxSlider,
            bgmSlider,
            closeButton,
            options.ShowInputOffset,
            options.CloseReturnsToPauseMenu);

        panelRoot.SetActive(false);
        return settingsUi;
    }

    static void DestroyExistingPanel(RectTransform uiRoot)
    {
        var existingPanel = uiRoot.Find("SettingsPanel");
        if (existingPanel == null)
            return;

        if (Application.isPlaying)
            Object.Destroy(existingPanel.gameObject);
        else
            Object.DestroyImmediate(existingPanel.gameObject);
    }

    static GameObject CreatePanelRoot(RectTransform parent)
    {
        var panel = new GameObject("SettingsPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        panel.transform.SetAsLastSibling();

        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(420f, 360f);

        var img = panel.GetComponent<Image>();
        img.sprite = Resources.Load<Sprite>("UI/UiSquare") ?? GreyboxSprites.Square;
        img.color = PanelBg;
        img.raycastTarget = true;

        return panel;
    }

    static TextMeshProUGUI CreateRowLabel(Transform parent, string name, string text, float y, float fontSize,
        FontStyles style = FontStyles.Normal)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(340f, 28f);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = LabelColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        BeatDefenderFonts.Apply(tmp);
        return tmp;
    }

    static Slider CreateSliderRow(Transform parent, string name, float y)
    {
        var slider = CreateHorizontalSlider(parent, name);
        var rt = slider.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(340f, 36f);
        slider.minValue = 0f;
        slider.maxValue = 1f;
        return slider;
    }

    static Button CreateCloseButton(Transform parent, float y)
    {
        var go = new GameObject("Btn_Close", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(160f, 44f);

        var img = go.GetComponent<Image>();
        img.sprite = Resources.Load<Sprite>("UI/UiSquare") ?? GreyboxSprites.Square;
        img.color = new Color(0.16f, 0.18f, 0.24f, 1f);

        var label = CreateText(go.transform, "Label", "닫기", 18f);
        StretchFull(label.rectTransform);
        label.alignment = TextAlignmentOptions.Center;
        label.color = Accent;

        var button = go.GetComponent<Button>();
        button.targetGraphic = img;
        return button;
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
        StretchRect(fillArea.GetComponent<RectTransform>(), new Vector2(0.02f, 0.22f), new Vector2(0.98f, 0.78f));

        var fill = CreateImage(fillArea.transform, "Fill");
        StretchFull(fill.rectTransform);
        fill.color = new Color(Accent.r, Accent.g, Accent.b, 0.85f);

        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(go.transform, false);
        StretchFull(handleArea.GetComponent<RectTransform>());

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

    static Image CreateImage(Transform parent, string name, bool raycastTarget = false)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = Resources.Load<Sprite>("UI/UiSquare") ?? GreyboxSprites.Square;
        img.raycastTarget = raycastTarget;
        return img;
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
}
