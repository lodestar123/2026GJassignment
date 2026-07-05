using System;
using UnityEngine;

/// <summary>
/// MAP.md 배치 슬롯 14칸 생성.
/// </summary>
public class PlacementGrid : MonoBehaviour
{
    public event Action<TowerPlacementCell> OnCellClicked;

    TowerPlacementCell[] _cells;

    public TowerPlacementCell[] Cells => _cells;

    void Start() => EnsureCells();

    MapLayoutDefinition ResolveLayout()
    {
        var config = GetComponentInParent<MapSceneConfig>();
        return config != null ? MapLayout.Get(config.LayoutKind) : MapLayout.Active;
    }

    void EnsureCells()
    {
        if (_cells == null || _cells.Length == 0)
            _cells = GetComponentsInChildren<TowerPlacementCell>(true);

        var slots = ResolveLayout().PlacementSlots;
        for (int i = 0; i < _cells.Length; i++)
        {
            var cell = _cells[i];
            if (cell == null)
                continue;

            if (i < slots.Length)
                cell.SetupFromScene(i);

            cell.OnCellClicked -= HandleCellClicked;
            cell.OnCellClicked += HandleCellClicked;
        }
    }

    public void Build()
    {
        ClearChildrenRuntime();
        CreateCells();
    }

#if UNITY_EDITOR
    public void BuildForEditor()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        CreateCells();
    }
#endif

    void CreateCells()
    {
        var slots = ResolveLayout().PlacementSlots;
        _cells = new TowerPlacementCell[slots.Length];

        for (int i = 0; i < slots.Length; i++)
        {
            var go = new GameObject($"Slot_{i}");
            go.transform.SetParent(transform);
            var cell = go.AddComponent<TowerPlacementCell>();
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
