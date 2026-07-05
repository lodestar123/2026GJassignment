using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 타워 호버 — BeatTower 강화 · 판매 50% 환급 · 타워 오른쪽에 패널 표시.
/// </summary>
public class TowerSellUI : MonoBehaviour
{
    public static TowerSellUI Instance { get; private set; }

    const float PanelWidth = 220f;
    const float PanelHeight = 168f;
    const float SidePad = 12f;
    const float SellHeight = 38f;
    const float UpgradeHeight = 38f;
    const float SellBottom = 10f;
    const float UpgradeBottom = SellBottom + SellHeight + 6f;
    const float InfoBottom = UpgradeBottom + UpgradeHeight + 6f;
    const float WorldOffsetRight = 1.15f;
    const float HideGraceSeconds = 0.15f;
    const float ZoneOuterPaddingPx = 12f;

    [SerializeField] GameObject panelRoot;
    [SerializeField] TextMeshProUGUI infoText;
    [SerializeField] Button upgradeButton;
    [SerializeField] Button sellButton;

    Canvas _canvas;
    Camera _camera;
    Tower _selected;
    bool _ready;
    float _leaveZoneAt = -1f;
    Rect _interactionZone;
    readonly Vector3[] _panelCorners = new Vector3[4];

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    public bool IsShowing(Tower tower) => IsOpen && _selected == tower;

    public static TowerSellUI Resolve()
    {
        if (Instance != null)
            return Instance;

        var ui = FindAnyObjectByType<TowerSellUI>(FindObjectsInactive.Include);
        if (ui == null)
            return null;

        if (!ui.gameObject.activeInHierarchy)
            ui.gameObject.SetActive(true);

        return Instance;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        WireButtons();
    }

    void Start()
    {
        EnsureStructure();
        Hide();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (!IsOpen)
            return;

        if (!IsSelectedValid())
        {
            Hide();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            UpdateInteractionZone();
            if (!IsPointerInInteractionZone())
                Hide();
        }
    }

    void LateUpdate()
    {
        if (!IsOpen)
            return;

        if (!IsSelectedValid())
        {
            Hide();
            return;
        }

        UpdateInteractionZone();

        if (IsPointerInInteractionZone())
        {
            _leaveZoneAt = -1f;
            return;
        }

        if (_leaveZoneAt < 0f)
            _leaveZoneAt = Time.unscaledTime;

        if (Time.unscaledTime - _leaveZoneAt >= HideGraceSeconds)
            Hide();
    }

    bool IsSelectedValid() => _selected != null;

    void WireButtons()
    {
        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveListener(OnUpgradeClicked);
            upgradeButton.onClick.AddListener(OnUpgradeClicked);
        }

        if (sellButton != null)
        {
            sellButton.onClick.RemoveListener(OnSellClicked);
            sellButton.onClick.AddListener(OnSellClicked);
        }
    }

    void EnsureStructure()
    {
        if (_ready)
            return;

        _canvas = GetComponentInParent<Canvas>();
        _camera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? _canvas.worldCamera
            : Camera.main;

        ResolveReferences();
        ApplyPanelLayout();
        WireButtons();
        _ready = true;
    }

    void ResolveReferences()
    {
        if (panelRoot == null)
            panelRoot = transform.Find("Panel")?.gameObject ?? gameObject;

        var root = panelRoot.transform;
        infoText ??= root.Find("Info")?.GetComponent<TextMeshProUGUI>();
        upgradeButton ??= root.Find("UpgradeButton")?.GetComponent<Button>();
        sellButton ??= root.Find("SellButton")?.GetComponent<Button>();
    }

    void ApplyPanelLayout()
    {
        if (panelRoot == null)
            return;

        var panelRt = panelRoot.GetComponent<RectTransform>();
        if (panelRt != null)
            panelRt.sizeDelta = new Vector2(PanelWidth, PanelHeight);

        PlaceBottomBar(sellButton?.GetComponent<RectTransform>(), SellBottom, SellHeight);
        PlaceBottomBar(upgradeButton?.GetComponent<RectTransform>(), UpgradeBottom, UpgradeHeight);
        PlaceInfoArea(infoText?.rectTransform);

        if (infoText != null)
            infoText.transform.SetSiblingIndex(0);
        if (upgradeButton != null)
            upgradeButton.transform.SetSiblingIndex(1);
        if (sellButton != null)
            sellButton.transform.SetSiblingIndex(2);
    }

    void PlaceInfoArea(RectTransform rt)
    {
        if (rt == null)
            return;

        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(SidePad, InfoBottom);
        rt.offsetMax = new Vector2(-SidePad, -10f);
    }

    static void PlaceBottomBar(RectTransform rt, float bottom, float height)
    {
        if (rt == null)
            return;

        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.offsetMin = new Vector2(SidePad, bottom);
        rt.offsetMax = new Vector2(-SidePad, bottom + height);
    }

    public void Show(Tower tower)
    {
        if (tower == null)
            return;

        EnsureStructure();
        _selected = tower;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        ApplyPanelLayout();
        RefreshSelected();
        PositionBesideTower(tower);
        UpdateInteractionZone();
        _leaveZoneAt = -1f;
    }

    public void RefreshSelected()
    {
        if (_selected == null)
            return;

        if (infoText != null)
            infoText.text = BuildInfoText(_selected);

        RefreshUpgradeButton();
        ApplyPanelLayout();
        PositionBesideTower(_selected);
    }

    public void Hide()
    {
        _selected = null;
        _leaveZoneAt = -1f;
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    void UpdateInteractionZone()
    {
        if (panelRoot == null)
            return;

        var cam = _camera != null ? _camera : Camera.main;
        var panelRt = panelRoot.GetComponent<RectTransform>();
        if (cam == null || panelRt == null)
            return;

        panelRt.GetWorldCorners(_panelCorners);

        float minX = _panelCorners[0].x;
        float maxX = _panelCorners[2].x;
        float minY = _panelCorners[0].y;
        float maxY = _panelCorners[1].y;

        minX -= ZoneOuterPaddingPx;
        maxX += ZoneOuterPaddingPx;
        minY -= ZoneOuterPaddingPx;
        maxY += ZoneOuterPaddingPx;

        _interactionZone = Rect.MinMaxRect(minX, minY, maxX, maxY);
    }

    bool IsPointerInInteractionZone()
    {
        if (IsPointerOverPanel())
            return true;

        if (IsPointerOverSelectedTower())
            return true;

        return _interactionZone.Contains(Input.mousePosition);
    }

    bool IsPointerOverSelectedTower()
    {
        if (_selected == null)
            return false;

        var cam = _camera != null ? _camera : Camera.main;
        if (cam == null)
            return false;

        var world = cam.ScreenToWorldPoint(Input.mousePosition);
        var hit = Physics2D.OverlapPoint(world);
        if (hit == null)
            return false;

        return hit.GetComponentInParent<Tower>() == _selected;
    }

    void PositionBesideTower(Tower tower)
    {
        if (panelRoot == null || tower == null)
            return;

        EnsureStructure();

        var panelRt = panelRoot.GetComponent<RectTransform>();
        if (panelRt == null || _canvas == null)
            return;

        panelRt.pivot = new Vector2(0f, 0.5f);
        panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);

        var cam = _camera != null ? _camera : Camera.main;
        if (cam == null)
            return;

        var anchorWorld = tower.transform.position + Vector3.right * WorldOffsetRight;
        var screen = RectTransformUtility.WorldToScreenPoint(cam, anchorWorld);

        if (_canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            panelRt.position = screen;
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.transform as RectTransform,
            screen,
            _canvas.worldCamera,
            out var local);
        panelRt.localPosition = local;
    }

    bool IsPointerOverPanel()
    {
        if (EventSystem.current == null || panelRoot == null)
            return false;

        var data = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(data, hits);

        foreach (var hit in hits)
        {
            if (hit.gameObject.transform.IsChildOf(panelRoot.transform)
                || hit.gameObject.transform == panelRoot.transform)
                return true;
        }

        return false;
    }

    void RefreshUpgradeButton()
    {
        if (upgradeButton == null)
            return;

        var beat = _selected != null ? _selected.GetComponent<BeatTower>() : null;
        bool show = beat != null && beat.CanUpgrade;
        upgradeButton.gameObject.SetActive(show);

        if (!show)
            return;

        var label = upgradeButton.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
            label.text = $"Upgrade Lv{beat.Level + 1} ({beat.NextUpgradeCost}G)";
    }

    static string BuildInfoText(Tower tower)
    {
        int refund = TowerPlacer.GetCost(tower.towerType) / 2;
        var beat = tower.GetComponent<BeatTower>();
        if (beat != null)
            refund = beat.GetTotalInvestedGold() / 2;

        if (beat != null)
        {
            return $"Tower Lv{beat.Level}\n" +
                   $"DMG {beat.ActiveDamage:0.#} / fb {beat.FallbackDamage:0.#}\n" +
                   $"Sell +{refund}G";
        }

        return $"Tower\nSell +{refund}G";
    }

    void OnUpgradeClicked()
    {
        if (_selected == null)
            return;

        TowerPlacer.Instance?.TryUpgradeTower(_selected);
    }

    void OnSellClicked()
    {
        if (_selected == null)
            return;

        TowerPlacer.Instance?.SellTower(_selected);
    }
}
