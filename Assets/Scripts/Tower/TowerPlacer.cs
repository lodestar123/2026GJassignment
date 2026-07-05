using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 타워 설치/교체/판매 — max 8 · 50% 환급 · BALANCE §2.
/// </summary>
public class TowerPlacer : MonoBehaviour
{
    public static TowerPlacer Instance { get; private set; }

    public const int MaxTowers = 8;

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

        if (!TowerSelection.HasSelection)
            return;

        var selected = TowerSelection.Selected;
        int cost = GetCost(selected);

        if (cell.Occupant != null)
        {
            RefundSell(cell.Occupant);
            RemoveTower(cell.Occupant, cell);
        }
        else if (_towers.Count >= MaxTowers)
        {
            Debug.Log("[TowerPlacer] 최대 8기 — 빈 슬롯에만 교체 가능");
            return;
        }

        if (_resources == null || !_resources.TrySpendGold(cost))
        {
            Debug.Log($"[TowerPlacer] 골드 부족 ({cost}G 필요)");
            return;
        }

        PlaceTower(cell, selected);
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

    void PlaceTower(TowerPlacementCell cell, TowerType type)
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
        rangeCol.radius = Tower.DefaultRange;

        if (type == TowerType.Beat)
            go.AddComponent<BeatTower>();

        go.AddComponent<TowerClickTarget>();
        go.AddComponent<TowerFireRecoil>();

        cell.SetOccupant(tower);
        _towers.Add(tower);
        _registry?.RegisterTower(tower);

        CombatVfxService.Instance?.PlayTowerPlaced(cell.transform.position, type);
        Debug.Log($"[TowerPlacer] {type} 설치 ({GetCost(type)}G)");
    }

    void RemoveTower(Tower tower, TowerPlacementCell cell)
    {
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

        int refund = GetCost(tower.towerType) / 2;
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
/// 타워 직접 클릭 → TowerSellUI.
/// </summary>
[RequireComponent(typeof(Tower))]
public class TowerClickTarget : MonoBehaviour
{
    Tower _tower;

    void Awake() => _tower = GetComponent<Tower>();

    void OnMouseDown()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null
            && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;

        if (GameManager.Instance != null && !GameManager.Instance.IsRunning)
            return;

        TowerSellUI.Instance?.Show(_tower);
    }
}
