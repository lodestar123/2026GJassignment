using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 통합 타워 배치 HUD — Tower 20G · 재클릭 시 배치 모드 해제.
/// </summary>
public class TowerTypeSelectUI : MonoBehaviour
{
    [Serializable]
    public struct TowerButton
    {
        public Image Background;
        public TextMeshProUGUI Label;
    }

    [SerializeField] TowerButton[] towerButtons;
    [SerializeField] Color selectedColor = new(0.25f, 0.55f, 0.85f, 0.95f);
    [SerializeField] Color normalColor = new(0.12f, 0.12f, 0.14f, 0.88f);
    [SerializeField] Color disabledColor = new(0.1f, 0.1f, 0.1f, 0.5f);

    ResourceManager _resources;

    public void SetTowerButtons(TowerButton[] buttons)
    {
        towerButtons = buttons;
        EnsureMouseOnlyControls();
        Refresh();
    }

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

            if (go.GetComponent<TowerTypeButton>() == null)
                go.AddComponent<TowerTypeButton>();
        }
    }

    void OnGoldChanged(int _) => Refresh();

    void Refresh()
    {
        if (towerButtons == null || towerButtons.Length == 0)
            return;

        int gold = _resources != null ? _resources.Gold : 0;
        int cost = TowerPlacer.TowerCost;
        bool armed = TowerSelection.IsArmed;
        bool canAfford = gold >= cost;
        bool interactable = canAfford || armed;

        for (int i = 0; i < towerButtons.Length; i++)
        {
            var entry = towerButtons[i];
            if (entry.Background == null)
                continue;

            bool isPrimary = i == 0;
            entry.Background.gameObject.SetActive(isPrimary);
            if (!isPrimary)
                continue;

            var control = entry.Background.GetComponent<TowerTypeButton>();
            if (entry.Label != null)
            {
                entry.Label.text = armed
                    ? $"Tower\n{cost}G  ON"
                    : $"Tower\n{cost}G";
            }

            entry.Background.color = armed ? selectedColor
                : canAfford ? normalColor : disabledColor;

            if (control != null)
            {
                control.Interactable = interactable;
                control.SetRaycast(interactable);
            }
        }
    }
}
