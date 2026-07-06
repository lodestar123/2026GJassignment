using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// TowerTypeSelect UI bake — Btn_Tower 1개. 에디터 리빌드·튜토리얼 bake 공용.
/// </summary>
public static class TowerTypeSelectVisualBuilder
{
    static readonly Color DefaultButtonColor = new(0.12f, 0.12f, 0.14f, 0.88f);

    const string ButtonName = "Btn_Tower";
    const string LabelName = "Label";

    static Sprite _preferredSprite;

    public static void SetPreferredSprite(Sprite sprite) => _preferredSprite = sprite;

    public static TowerTypeSelectUI BuildRoot(Transform uiRoot)
    {
        var existing = uiRoot.GetComponentInChildren<TowerTypeSelectUI>(true);
        if (existing != null)
        {
            RebuildButton(existing.transform);
            return existing;
        }

        var root = new GameObject("TowerTypeSelect", typeof(RectTransform));
        root.transform.SetParent(uiRoot, false);

        var rt = root.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-40f, 56f);
        rt.sizeDelta = new Vector2(120f, 88f);

        var selectUi = root.AddComponent<TowerTypeSelectUI>();
        RebuildButton(root.transform);
        return selectUi;
    }

    /// <summary>기존 TowerTypeSelect 아래 자식을 지우고 Btn_Tower 1개만 생성·연결.</summary>
    public static void RebuildButton(Transform selectRoot)
    {
        if (selectRoot == null)
            return;

        RemoveLegacyLayout(selectRoot);

        for (int i = selectRoot.childCount - 1; i >= 0; i--)
        {
            var child = selectRoot.GetChild(i);
            if (Application.isPlaying)
                Object.Destroy(child.gameObject);
            else
                Object.DestroyImmediate(child.gameObject);
        }

        CreateButton(selectRoot, out var background, out var label);

        var selectUi = selectRoot.GetComponent<TowerTypeSelectUI>();
        if (selectUi == null)
            selectUi = selectRoot.gameObject.AddComponent<TowerTypeSelectUI>();

        selectUi.SetTowerButton(background, label);
    }

    static void CreateButton(Transform parent, out Image background, out TextMeshProUGUI label)
    {
        var sprite = _preferredSprite ?? GreyboxSprites.Square;

        var btnGo = new GameObject(ButtonName, typeof(RectTransform), typeof(Image), typeof(TowerTypeButton));
        btnGo.transform.SetParent(parent, false);

        var btnRt = btnGo.GetComponent<RectTransform>();
        btnRt.anchorMin = Vector2.zero;
        btnRt.anchorMax = Vector2.one;
        btnRt.offsetMin = Vector2.zero;
        btnRt.offsetMax = Vector2.zero;

        background = btnGo.GetComponent<Image>();
        background.sprite = sprite;
        background.color = DefaultButtonColor;
        background.raycastTarget = true;

        var labelGo = new GameObject(LabelName, typeof(RectTransform));
        labelGo.transform.SetParent(btnGo.transform, false);
        StretchFull(labelGo.GetComponent<RectTransform>());

        label = labelGo.AddComponent<TextMeshProUGUI>();
        label.text = $"Tower\n{TowerPlacer.TowerCost}G";
        label.fontSize = 18f;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        BeatDefenderFonts.Apply(label);
    }

    static void RemoveLegacyLayout(Transform selectRoot)
    {
        var layout = selectRoot.GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
            return;

        if (Application.isPlaying)
            Object.Destroy(layout);
        else
            Object.DestroyImmediate(layout);
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
