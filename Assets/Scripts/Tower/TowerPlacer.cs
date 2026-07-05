using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 타워 설치/교체/판매/강화 · 50% 환급 · BALANCE §2.
/// </summary>
public class TowerPlacer : MonoBehaviour
{
    public static TowerPlacer Instance { get; private set; }

    [SerializeField] Transform towerRoot;

    readonly List<Tower> _towers = new();
    PlacementGrid _grid;
    ResourceManager _resources;
    TowerRegistry _registry;

    public int TowerCount => _towers.Count;
    public IReadOnlyList<Tower> Towers => _towers;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _resources = FindAnyObjectByType<ResourceManager>();
        _registry = FindAnyObjectByType<TowerRegistry>();
    }

    void Start()
    {
        _grid = FindAnyObjectByType<PlacementGrid>();
        if (_grid != null)
            _grid.OnCellClicked += HandleCellClicked;
    }

    void OnDestroy()
    {
        if (_grid != null)
            _grid.OnCellClicked -= HandleCellClicked;

        if (Instance == this)
            Instance = null;
    }

    public int GetSelectedCost()
    {
        return TowerSelection.HasSelection ? GetCost(TowerSelection.Selected) : 0;
    }

    public static int GetCost(TowerType type) => type switch
    {
        TowerType.Beat => 20,
        TowerType.Strike => 30,
        TowerType.Boost => 25,
        _ => 0
    };

    public void HandleCellClicked(TowerPlacementCell cell)
    {
        if (cell == null)
            return;

        if (!cell.IsUnlocked)
        {
            TryUnlockSlot(cell);
            return;
        }

        if (!TowerSelection.HasSelection)
            return;

        var selected = TowerSelection.Selected;
        int cost = GetCost(selected);

        if (cell.Occupant != null)
        {
            RefundSell(cell.Occupant);
            RemoveTower(cell.Occupant, cell);
        }

        if (_resources == null || !_resources.TrySpendGold(cost))
        {
            Debug.Log($"[TowerPlacer] 골드 부족 ({cost}G 필요)");
            return;
        }

        PlaceTower(cell, selected, cost);
    }

    public bool TryUnlockSlot(TowerPlacementCell cell)
    {
        if (cell == null || cell.IsUnlocked)
            return true;

        if (cell.UnlockGoldCost <= 0)
        {
            cell.SetUnlocked(true);
            Debug.Log($"[TowerPlacer] Slot {cell.SlotIndex} 해금");
            return true;
        }

        if (_resources == null || !_resources.TrySpendGold(cell.UnlockGoldCost))
        {
            Debug.Log($"[TowerPlacer] 슬롯 해금 골드 부족 ({cell.UnlockGoldCost}G)");
            return false;
        }

        cell.SetUnlocked(true);
        CombatVfxService.Instance?.PlayGoldSpendPopup(cell.transform.position, cell.UnlockGoldCost);
        Debug.Log($"[TowerPlacer] Slot {cell.SlotIndex} 해금 (-{cell.UnlockGoldCost}G)");
        return true;
    }

    public bool TryUpgradeBeatTower(Tower tower)
    {
        if (tower == null || tower.towerType != TowerType.Beat)
            return false;

        var beat = tower.GetComponent<BeatTower>();
        if (beat == null || !beat.CanUpgrade)
            return false;

        int cost = beat.NextUpgradeCost;
        if (_resources == null || !_resources.TrySpendGold(cost))
        {
            Debug.Log($"[TowerPlacer] 강화 골드 부족 ({cost}G)");
            return false;
        }

        if (!beat.TryUpgrade())
        {
            _resources.AddGold(cost);
            return false;
        }

        CombatVfxService.Instance?.PlayGoldSpendPopup(tower.transform.position, cost);
        Debug.Log($"[TowerPlacer] BeatTower Lv{beat.Level} (-{cost}G)");
        TowerSellUI.Instance?.RefreshSelected();
        return true;
    }

    public void SellTower(Tower tower)
    {
        if (tower == null)
            return;

        var cell = FindCellForTower(tower);
        RefundSell(tower);
        RemoveTower(tower, cell);
        TowerSellUI.Instance?.Hide();
    }

    void PlaceTower(TowerPlacementCell cell, TowerType type, int spentGold)
    {
        var go = new GameObject($"{type}Tower");
        go.transform.SetParent(towerRoot != null ? towerRoot : transform);
        go.transform.position = cell.transform.position;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GreyboxSprites.Tower;
        sr.color = GetTowerColor(type);
        sr.sortingOrder = 10;
        go.transform.localScale = Vector3.one * 0.6f;

        var tower = go.AddComponent<Tower>();
        tower.towerType = type;

        var rangeCol = go.GetComponent<CircleCollider2D>();
        if (rangeCol == null)
            rangeCol = go.AddComponent<CircleCollider2D>();
        rangeCol.isTrigger = false;
        rangeCol.radius = Tower.HoverRadiusAtBaseScale;

        if (type == TowerType.Beat)
            go.AddComponent<BeatTower>();

        go.AddComponent<TowerClickTarget>();
        go.AddComponent<TowerFireRecoil>();

        cell.SetOccupant(tower);
        _towers.Add(tower);
        _registry?.RegisterTower(tower);

        CombatVfxService.Instance?.PlayGoldSpendPopup(cell.transform.position, spentGold);
        CombatVfxService.Instance?.PlayTowerPlaced(cell.transform.position, type);
        Debug.Log($"[TowerPlacer] {type} 설치 ({spentGold}G)");
    }

    void RemoveTower(Tower tower, TowerPlacementCell cell)
    {
        if (TowerSellUI.Instance != null && TowerSellUI.Instance.IsShowing(tower))
            TowerSellUI.Instance.Hide();

        _towers.Remove(tower);
        _registry?.UnregisterTower(tower);

        if (cell != null)
            cell.ClearOccupant();

        if (tower != null)
            Destroy(tower.gameObject);
    }

    void RefundSell(Tower tower)
    {
        if (_resources == null || tower == null)
            return;

        int invested = GetCost(tower.towerType);
        if (tower.towerType == TowerType.Beat)
        {
            var beat = tower.GetComponent<BeatTower>();
            if (beat != null)
                invested = beat.GetTotalInvestedGold();
        }

        int refund = invested / 2;
        _resources.AddGold(refund);
        Debug.Log($"[TowerPlacer] 판매 환급 +{refund}G");
    }

    TowerPlacementCell FindCellForTower(Tower tower)
    {
        if (_grid?.Cells == null)
            return null;

        foreach (var cell in _grid.Cells)
        {
            if (cell != null && cell.Occupant == tower)
                return cell;
        }

        return null;
    }

    static Color GetTowerColor(TowerType type) => type switch
    {
        TowerType.Beat => Color.white,
        TowerType.Strike => new Color(0.94f, 0.33f, 0.31f),
        TowerType.Boost => new Color(1f, 0.6f, 0f),
        _ => Color.gray
    };
}

/// <summary>
/// 타워 호버 → TowerSellUI.
/// </summary>
[RequireComponent(typeof(Tower))]
public class TowerClickTarget : MonoBehaviour
{
    Tower _tower;

    void Awake() => _tower = GetComponent<Tower>();

    void OnMouseEnter()
    {
        if (!CanInteract())
            return;

        TowerSellUI.Resolve()?.Show(_tower);
    }

    bool CanInteract()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null
            && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return false;

        if (GameManager.Instance != null && !GameManager.Instance.IsRunning)
            return false;

        return true;
    }
}
