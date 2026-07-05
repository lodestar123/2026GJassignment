using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 잠긴 배치 슬롯 호버 — 해금 비용 표시 (클릭은 기존과 동일).
/// </summary>
public class PlacementSlotTooltipUI : MonoBehaviour
{
    public static PlacementSlotTooltipUI Instance { get; private set; }

    const float PanelWidth = 120f;
    const float PanelHeight = 56f;
    const float WorldOffsetY = 0.85f;

    [SerializeField] GameObject panelRoot;
    [SerializeField] TextMeshProUGUI labelText;

    Canvas _canvas;
    Camera _camera;
    TowerPlacementCell _trackedCell;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureInstance()
    {
        if (FindAnyObjectByType<PlacementSlotTooltipUI>(FindObjectsInactive.Include) != null)
            return;

        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
            return;

        var go = new GameObject("PlacementSlotTooltipUI");
        go.transform.SetParent(canvas.transform, false);
        go.AddComponent<PlacementSlotTooltipUI>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureStructure();
        Hide();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void LateUpdate()
    {
        if (!IsVisible || _trackedCell == null)
            return;

        if (_trackedCell.IsUnlocked || !IsPointerOverCell(_trackedCell))
            Hide();
    }

    void EnsureStructure()
    {
        _canvas = GetComponentInParent<Canvas>();
        _camera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? _canvas.worldCamera
            : Camera.main;

        if (panelRoot == null)
        {
            panelRoot = new GameObject("Panel");
            panelRoot.transform.SetParent(transform, false);

            var rt = panelRoot.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            var bg = panelRoot.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.1f, 0.92f);
            bg.raycastTarget = false;

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(panelRoot.transform, false);
            var textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(8f, 4f);
            textRt.offsetMax = new Vector2(-8f, -4f);

            labelText = textGo.AddComponent<TextMeshProUGUI>();
            labelText.fontSize = 18f;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.richText = true;
            labelText.raycastTarget = false;
        }

        panelRoot ??= transform.Find("Panel")?.gameObject;
        labelText ??= panelRoot?.GetComponentInChildren<TextMeshProUGUI>();
    }

    public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

    public void Show(TowerPlacementCell cell)
    {
        if (cell == null || cell.IsUnlocked)
        {
            Hide();
            return;
        }

        EnsureStructure();
        _trackedCell = cell;

        int cost = cell.UnlockGoldCost;
        int gold = ResourceManager.Instance != null ? ResourceManager.Instance.Gold : 0;
        bool canAfford = gold >= cost;

        if (labelText != null)
        {
            labelText.text = canAfford
                ? $"<b>해금</b>\n{cost}G"
                : $"<b>해금</b>\n{cost}G\n<size=80%><color=#FF5252>골드 부족</color></size>";
        }

        PositionAtWorld(cell.transform.position + Vector3.up * WorldOffsetY);
        panelRoot.SetActive(true);
    }

    public void Hide()
    {
        _trackedCell = null;
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    void PositionAtWorld(Vector3 worldPos)
    {
        if (panelRoot == null || _canvas == null)
            return;

        var rt = panelRoot.GetComponent<RectTransform>();
        if (rt == null)
            return;

        rt.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);

        var cam = _camera != null ? _camera : Camera.main;
        if (cam == null)
            return;

        var screen = RectTransformUtility.WorldToScreenPoint(cam, worldPos);

        if (_canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            rt.position = screen;
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.transform as RectTransform,
            screen,
            _canvas.worldCamera,
            out var local);
        rt.localPosition = local;
    }

    static bool IsPointerOverCell(TowerPlacementCell cell)
    {
        if (cell == null || Camera.main == null)
            return false;

        var world = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        world.z = 0f;
        var col = cell.GetComponent<Collider2D>();
        return col != null && col.OverlapPoint(world);
    }
}
