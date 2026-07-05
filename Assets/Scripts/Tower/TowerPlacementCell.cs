using System;
using UnityEngine;

/// <summary>
/// 배치 슬롯 1칸 — 클릭으로 설치/교체.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class TowerPlacementCell : MonoBehaviour
{
    public int SlotIndex { get; private set; }
    public Tower Occupant { get; private set; }

    SpriteRenderer _sprite;
    Color _normalColor = new(0.2f, 0.65f, 0.35f, 0.35f);
    Color _hoverColor = new(0.35f, 0.85f, 0.45f, 0.55f);
    Color _blockedColor = new(0.85f, 0.25f, 0.25f, 0.45f);

    public event Action<TowerPlacementCell> OnCellClicked;

    public void Initialize(int index, Vector2 worldPos, bool snapToLayout = true)
    {
        SlotIndex = index;
        if (snapToLayout)
            transform.position = worldPos;

        EnsureVisual();
    }

    public void SetupFromScene(int index)
    {
        SlotIndex = index;
        EnsureVisual();
    }

    public void SetOccupant(Tower tower)
    {
        Occupant = tower;
    }

    public void ClearOccupant()
    {
        Occupant = null;
    }

    void OnMouseEnter()
    {
        if (!CanInteract())
            return;

        EnsureVisual();
        _sprite.color = GetHoverAffordable() ? _hoverColor : _blockedColor;
    }

    void OnMouseExit()
    {
        EnsureVisual();
        _sprite.color = _normalColor;
    }

    void Awake() => EnsureVisual();

    void EnsureVisual()
    {
        if (_sprite != null)
            return;

        _sprite = GetComponent<SpriteRenderer>();
        if (_sprite == null)
        {
            _sprite = gameObject.AddComponent<SpriteRenderer>();
            _sprite.sprite = GreyboxSprites.Cell;
            _sprite.sortingOrder = 0;
            transform.localScale = Vector3.one * 0.85f;
        }

        _sprite.color = _normalColor;

        var col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            col.size = Vector2.one;
            col.isTrigger = false;
        }
    }

    void OnMouseDown()
    {
        if (!CanInteract())
            return;

        OnCellClicked?.Invoke(this);
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

    bool GetHoverAffordable()
    {
        var placer = TowerPlacer.Instance;
        if (placer == null)
            return true;

        int cost = placer.GetSelectedCost();
        if (cost <= 0)
            return false;

        int gold = ResourceManager.Instance != null ? ResourceManager.Instance.Gold : 0;
        bool canPlaceCount = placer.TowerCount < TowerPlacer.MaxTowers || Occupant != null;
        return gold >= cost && canPlaceCount;
    }
}
