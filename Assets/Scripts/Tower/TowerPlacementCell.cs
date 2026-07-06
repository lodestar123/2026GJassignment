using System;
using UnityEngine;

/// <summary>
/// 배치 슬롯 1칸 — 클릭으로 해금 / 설치 / 교체.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class TowerPlacementCell : MonoBehaviour
{
    [Header("Unlock (맵 수정 시 셀마다 직접 지정 가능)")]
    [SerializeField] bool useLocalUnlockSettings;
    [SerializeField] bool unlockedAtStart = true;
    [SerializeField] int unlockGoldCost = 20;

    public int SlotIndex { get; private set; }
    public Tower Occupant { get; private set; }
    public bool IsUnlocked { get; private set; }
    public int UnlockGoldCost { get; private set; }

    SpriteRenderer _sprite;
    bool _usesRegistrySprite;
    Color _normalColor = new(0.2f, 0.65f, 0.35f, 0.35f);
    Color _hoverColor = new(0.35f, 0.85f, 0.45f, 0.55f);
    Color _blockedColor = new(0.85f, 0.25f, 0.25f, 0.45f);
    Color _lockedColor = new(0.35f, 0.35f, 0.38f, 0.55f);
    Color _lockedHoverColor = new(0.55f, 0.5f, 0.2f, 0.65f);

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

    public void RefreshFromRegistry() => RefreshVisual();

    /// <summary>PlacementGrid 규칙 적용 — useLocalUnlockSettings 이면 무시.</summary>
    public void ApplyGridUnlockRule(bool unlockedAtStart, int goldCost)
    {
        if (useLocalUnlockSettings)
            return;

        ConfigureUnlock(unlockedAtStart, goldCost);
    }

    public void ConfigureUnlock(bool unlocked, int goldCost)
    {
        unlockedAtStart = unlocked;
        unlockGoldCost = Mathf.Max(0, goldCost);
        IsUnlocked = unlocked;
        UnlockGoldCost = unlockGoldCost;
        RefreshVisual();
    }

    public void SetUnlocked(bool unlocked)
    {
        IsUnlocked = unlocked;
        RefreshVisual();
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
        _sprite.color = GetHoverColor();

        if (!IsUnlocked)
            PlacementSlotTooltipUI.Instance?.Show(this);
        else if (Occupant != null && !TowerSelection.HasSelection)
            TowerSellUI.Resolve()?.Show(Occupant);
    }

    void OnMouseExit()
    {
        EnsureVisual();
        RefreshVisual();

        if (!IsUnlocked)
            PlacementSlotTooltipUI.Instance?.Hide();
    }

    void Awake()
    {
        EnsureVisual();
        if (useLocalUnlockSettings)
            ConfigureUnlock(unlockedAtStart, unlockGoldCost);
    }

    void EnsureVisual()
    {
        _sprite ??= GetComponent<SpriteRenderer>();
        if (_sprite == null)
        {
            _sprite = gameObject.AddComponent<SpriteRenderer>();
            _sprite.sortingOrder = 0;
            transform.localScale = Vector3.one * 0.85f;
        }

        var col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            col.size = Vector2.one;
            col.isTrigger = false;
        }

        RefreshVisual();
    }

    void RefreshVisual()
    {
        if (_sprite == null)
            return;

        ApplyRegistrySprite();
        _sprite.color = _usesRegistrySprite
            ? Color.white
            : IsUnlocked ? _normalColor : _lockedColor;
    }

    void ApplyRegistrySprite()
    {
        _usesRegistrySprite = false;
        var registry = MapPrefabRegistry.Get();
        if (registry == null)
            return;

        var sprite = IsUnlocked
            ? registry.PlacementTileAvailable ?? registry.PlacementTileEmpty
            : registry.PlacementTileEmpty ?? registry.PlacementTileAvailable;

        if (sprite == null)
            return;

        _sprite.sprite = sprite;
        _usesRegistrySprite = true;
    }

    Color GetHoverColor()
    {
        if (_usesRegistrySprite)
            return Color.white;

        if (!IsUnlocked)
        {
            int gold = ResourceManager.Instance != null ? ResourceManager.Instance.Gold : 0;
            return gold >= UnlockGoldCost ? _lockedHoverColor : _blockedColor;
        }

        return GetHoverAffordable() ? _hoverColor : _blockedColor;
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
        return gold >= cost;
    }
}
