using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 타워 종류 선택 HUD — Beat 20G / Strike 30G / Boost 25G. 같은 버튼 재클릭 시 선택 해제.
/// </summary>
public class TowerTypeSelectUI : MonoBehaviour
{
    [Serializable]
    public struct TowerButton
    {
        public TowerType Type;
        public Image Background;
        public TextMeshProUGUI Label;
    }

    [SerializeField] TowerButton[] towerButtons;
    [SerializeField] Color selectedColor = new(0.25f, 0.55f, 0.85f, 0.95f);
    [SerializeField] Color normalColor = new(0.12f, 0.12f, 0.14f, 0.88f);
    [SerializeField] Color disabledColor = new(0.1f, 0.1f, 0.1f, 0.5f);

    ResourceManager _resources;

    void Awake()
    {
        _resources = FindAnyObjectByType<ResourceManager>();
        EnsureMouseOnlyControls();
        TowerSelection.OnChanged -= Refresh;
        TowerSelection.OnChanged += Refresh;
    }

    void OnDestroy()
    {
        TowerSelection.OnChanged -= Refresh;
    }

    void OnEnable()
    {
        if (_resources != null)
        {
            _resources.OnGoldChanged -= OnGoldChanged;
            _resources.OnGoldChanged += OnGoldChanged;
        }

        Refresh();
    }

    void OnDisable()
    {
        if (_resources != null)
            _resources.OnGoldChanged -= OnGoldChanged;
    }

    void EnsureMouseOnlyControls()
    {
        if (towerButtons == null)
            return;

        foreach (var entry in towerButtons)
        {
            if (entry.Background == null)
                continue;

            var go = entry.Background.gameObject;
            var legacyButton = go.GetComponent<Button>();
            if (legacyButton != null)
                Destroy(legacyButton);

            var control = go.GetComponent<TowerTypeButton>();
            if (control == null)
                control = go.AddComponent<TowerTypeButton>();
            control.Type = entry.Type;
        }
    }

    void OnGoldChanged(int _) => Refresh();

    void Refresh()
    {
        if (towerButtons == null)
            return;

        int gold = _resources != null ? _resources.Gold : 0;
        bool hasSelection = TowerSelection.HasSelection;

        foreach (var entry in towerButtons)
        {
            if (entry.Background == null)
                continue;

            var control = entry.Background.GetComponent<TowerTypeButton>();
            int cost = GetCost(entry.Type);
            bool canAfford = gold >= cost;
            bool isSelected = hasSelection && entry.Type == TowerSelection.Selected;
            bool interactable = canAfford || isSelected;

            if (entry.Label != null)
                entry.Label.text = $"{GetShortName(entry.Type)}\n{cost}G";

            entry.Background.color = isSelected ? selectedColor
                : canAfford ? normalColor : disabledColor;

            if (control != null)
            {
                control.Type = entry.Type;
                control.Interactable = interactable;
                control.SetRaycast(interactable);
            }
        }
    }

    static int GetCost(TowerType type) => type switch
    {
        TowerType.Beat => 20,
        TowerType.Strike => 30,
        TowerType.Boost => 25,
        _ => 0
    };

    static string GetShortName(TowerType type) => type switch
    {
        TowerType.Beat => "Beat",
        TowerType.Strike => "Strike",
        TowerType.Boost => "Boost",
        _ => type.ToString()
    };
}
