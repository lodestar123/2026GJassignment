using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 통합 타워 설치/교체/판매/강화 · 50% 환급 · BALANCE §2.
/// </summary>
public class TowerPlacer : MonoBehaviour
{
    public static TowerPlacer Instance { get; private set; }

    public const int DefaultTowerCost = 50;

    [SerializeField] int towerCost = DefaultTowerCost;

    [SerializeField] Transform towerRoot;

    /// <summary>씬 TowerPlacer 인스펙터에서 조절. Instance 없으면 <see cref="DefaultTowerCost"/>.</summary>
    public static int TowerCost => Instance != null ? Instance.towerCost : DefaultTowerCost;

    public void SetTowerRoot(Transform root) => towerRoot = root;

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
        EnsureGridBound();
    }

    public void EnsureGridBound()
    {
        var grid = FindAnyObjectByType<PlacementGrid>(FindObjectsInactive.Include);
        if (grid == _grid)
            return;

        if (_grid != null)
            _grid.OnCellClicked -= HandleCellClicked;

        _grid = grid;
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

    public int GetSelectedCost() => TowerSelection.IsArmed ? TowerCost : 0;

    public static int GetCost(TowerType type = TowerType.Beat) => TowerCost;

    public void HandleCellClicked(TowerPlacementCell cell)
    {
        if (cell == null)
            return;

        if (!cell.IsUnlocked)
        {
            TryUnlockSlot(cell);
            return;
        }

        if (!TowerSelection.IsArmed)
            return;

        if (cell.Occupant != null)
            return;

        if (_resources == null || !_resources.TrySpendGold(TowerCost))
        {
            Debug.Log($"[TowerPlacer] 골드 부족 ({TowerCost}G 필요)");
            return;
        }

        PlaceTower(cell, TowerCost);
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

    public bool TryUpgradeTower(Tower tower)
    {
        if (tower == null)
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
        Debug.Log($"[TowerPlacer] Tower Lv{beat.Level} (-{cost}G)");
        TowerSellUI.Instance?.RefreshSelected();
        return true;
    }

    public bool TryUpgradeBeatTower(Tower tower) => TryUpgradeTower(tower);

    public void SellTower(Tower tower)
    {
        if (tower == null)
            return;

        var cell = FindCellForTower(tower);
        RefundSell(tower);
        RemoveTower(tower, cell);
        TowerSellUI.Instance?.Hide();
    }

    void PlaceTower(TowerPlacementCell cell, int spentGold)
    {
        var tower = SpawnTower(cell.transform.position);
        if (tower == null)
            return;

        cell.SetOccupant(tower);
        _towers.Add(tower);
        _registry?.RegisterTower(tower);

        CombatVfxService.Instance?.PlayGoldSpendPopup(cell.transform.position, spentGold);
        CombatVfxService.Instance?.PlayTowerPlaced(cell.transform.position);
        Debug.Log($"[TowerPlacer] Tower 설치 ({spentGold}G)");
    }

    Tower SpawnTower(Vector3 worldPosition)
    {
        var parent = towerRoot != null ? towerRoot : transform;
        var registry = MapPrefabRegistry.Get();
        var prefab = registry != null ? registry.GetTowerPrefab(TowerType.Beat) : null;

        GameObject go;
        if (prefab != null)
        {
            go = PrefabSpawnUtility.Instantiate(prefab, worldPosition, Quaternion.identity, parent);
            go.name = "Tower";
        }
        else
            go = CreateFallbackTower(worldPosition, parent);

        go.GetComponent<BeatTower>()?.RefreshFromRegistry();
        return go.GetComponent<Tower>();
    }

    static GameObject CreateFallbackTower(Vector3 worldPosition, Transform parent)
    {
        var go = new GameObject("Tower");
        go.transform.SetParent(parent);
        go.transform.position = worldPosition;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 10;
        sr.color = Color.white;

        var tower = go.AddComponent<Tower>();
        tower.towerType = TowerType.Beat;
        go.AddComponent<BeatTower>();

        var rangeCol = go.GetComponent<CircleCollider2D>();
        if (rangeCol == null)
            rangeCol = go.AddComponent<CircleCollider2D>();
        rangeCol.isTrigger = false;
        rangeCol.radius = Tower.HoverRadiusAtBaseScale;

        go.AddComponent<TowerClickTarget>();
        go.AddComponent<TowerFireRecoil>();
        return go;
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

        int invested = TowerCost;
        var beat = tower.GetComponent<BeatTower>();
        if (beat != null)
            invested = beat.GetTotalInvestedGold();

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
}
