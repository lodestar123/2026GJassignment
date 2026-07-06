using System;
using UnityEngine;

/// <summary>
/// MAP.md 배치 슬롯 — Slot_N 인덱스와 slotUnlockRules 로 Rebuild 시 해금 지정.
/// 플레이 중에는 각 TowerPlacementCell 인스펙터 설정이 그대로 유지됩니다.
/// </summary>
public class PlacementGrid : MonoBehaviour
{
    [Header("Slot Unlock (Rebuild 시에만 적용)")]
    [Tooltip("Rebuild GameScene Placement Grid 메뉴 실행 시에만 반영. 플레이 중에는 각 Slot 오브젝트 인스펙터 설정이 우선합니다.")]
    [SerializeField] PlacementSlotUnlockRule[] slotUnlockRules;

    [SerializeField] int defaultLockedUnlockCost = 20;

    public event Action<TowerPlacementCell> OnCellClicked;

    TowerPlacementCell[] _cells;

    public TowerPlacementCell[] Cells => _cells;

    void Start()
    {
        EnsureCells();
    }

    MapLayoutDefinition ResolveLayout()
    {
        var config = GetComponentInParent<MapSceneConfig>();
        return config != null ? MapLayout.Get(config.LayoutKind) : MapLayout.Active;
    }

    void EnsureCells()
    {
        if (_cells == null || _cells.Length == 0)
            _cells = GetComponentsInChildren<TowerPlacementCell>(true);

        for (int i = 0; i < _cells.Length; i++)
        {
            var cell = _cells[i];
            if (cell == null)
                continue;

            var slots = ResolveLayout().PlacementSlots;
            if (i < slots.Length)
                cell.SetupFromScene(i);

            cell.OnCellClicked -= HandleCellClicked;
            cell.OnCellClicked += HandleCellClicked;
        }
    }

    public void ApplyUnlockRules()
    {
        if (_cells == null || _cells.Length == 0)
            _cells = GetComponentsInChildren<TowerPlacementCell>(true);

        if (_cells == null)
            return;

        bool hasRules = slotUnlockRules != null && slotUnlockRules.Length > 0;

        foreach (var cell in _cells)
        {
            if (cell == null)
                continue;

            if (!hasRules)
            {
                cell.ApplyGridUnlockRule(unlockedAtStart: true, goldCost: 0);
                continue;
            }

            if (TryGetRule(cell.SlotIndex, out var rule))
                cell.ApplyGridUnlockRule(rule.unlockedAtStart, rule.unlockGoldCost);
            else
                cell.ApplyGridUnlockRule(unlockedAtStart: false, goldCost: defaultLockedUnlockCost);
        }
    }

    bool TryGetRule(int slotIndex, out PlacementSlotUnlockRule rule)
    {
        if (slotUnlockRules != null)
        {
            foreach (var entry in slotUnlockRules)
            {
                if (entry.slotIndex != slotIndex)
                    continue;

                rule = entry;
                return true;
            }
        }

        rule = default;
        return false;
    }

    public void Build()
    {
        ClearChildrenRuntime();
        CreateCells();
        ApplyUnlockRules();
    }

#if UNITY_EDITOR
    public void BuildForEditor()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        CreateCells();
        ApplyUnlockRules();
    }
#endif

    void CreateCells()
    {
        var slots = ResolveLayout().PlacementSlots;
        _cells = new TowerPlacementCell[slots.Length];
        var cellPrefab = MapPrefabRegistry.Get()?.TowerPlacementCell;

        for (int i = 0; i < slots.Length; i++)
        {
            TowerPlacementCell cell;
            if (cellPrefab != null)
            {
                var go = PrefabSpawnUtility.Instantiate(cellPrefab, transform);
                go.name = $"Slot_{i}";
                cell = go.GetComponent<TowerPlacementCell>();
            }
            else
            {
                var go = new GameObject($"Slot_{i}");
                go.transform.SetParent(transform);
                cell = go.AddComponent<TowerPlacementCell>();
            }

            cell.Initialize(i, slots[i]);
            _cells[i] = cell;
        }
    }

    void OnDestroy()
    {
        if (_cells == null)
            return;

        foreach (var cell in _cells)
        {
            if (cell != null)
                cell.OnCellClicked -= HandleCellClicked;
        }
    }

    void HandleCellClicked(TowerPlacementCell cell) => OnCellClicked?.Invoke(cell);

    void ClearChildrenRuntime()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);
    }
}
