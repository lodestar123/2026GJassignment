using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 통합 타워 배치 HUD — Tower 20G · 재클릭 시 배치 모드 해제.
/// </summary>
public class TowerTypeSelectUI : MonoBehaviour
{
    [SerializeField] Image towerButton;
    [SerializeField] TextMeshProUGUI towerButtonLabel;
    [SerializeField] Color selectedColor = new(0.25f, 0.55f, 0.85f, 0.95f);
    [SerializeField] Color normalColor = new(0.12f, 0.12f, 0.14f, 0.88f);
    [SerializeField] Color disabledColor = new(0.1f, 0.1f, 0.1f, 0.5f);

    ResourceManager _resources;

    public void SetTowerButton(Image background, TextMeshProUGUI label)
    {
        towerButton = background;
        towerButtonLabel = label;
        EnsureMouseOnlyControl();
        Refresh();
    }

    void Awake()
    {
        ResolveRefs();
        _resources = FindAnyObjectByType<ResourceManager>();
        EnsureMouseOnlyControl();
        TowerSelection.OnChanged -= Refresh;
        TowerSelection.OnChanged += Refresh;
    }

    void OnDestroy()
    {
        TowerSelection.OnChanged -= Refresh;
    }

    void OnEnable()
    {
        ResolveRefs();
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

    void ResolveRefs()
    {
        if (towerButton == null)
        {
            towerButton = transform.Find("Btn_Tower")?.GetComponent<Image>()
                ?? transform.Find("Btn_Beat")?.GetComponent<Image>();
        }

        if (towerButtonLabel == null && towerButton != null)
            towerButtonLabel = towerButton.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
    }

    void EnsureMouseOnlyControl()
    {
        if (towerButton == null)
            return;

        var go = towerButton.gameObject;
        var legacyButton = go.GetComponent<Button>();
        if (legacyButton != null)
            Destroy(legacyButton);

        if (go.GetComponent<TowerTypeButton>() == null)
            go.AddComponent<TowerTypeButton>();
    }

    void OnGoldChanged(int _) => Refresh();

    void Refresh()
    {
        if (towerButton == null)
            return;

        int gold = _resources != null ? _resources.Gold : 0;
        int cost = TowerPlacer.TowerCost;
        bool armed = TowerSelection.IsArmed;
        bool canAfford = gold >= cost;
        bool interactable = canAfford || armed;

        if (towerButtonLabel != null)
        {
            towerButtonLabel.text = armed
                ? $"Tower\n{cost}G  ON"
                : $"Tower\n{cost}G";
        }

        towerButton.color = armed ? selectedColor
            : canAfford ? normalColor : disabledColor;

        var control = towerButton.GetComponent<TowerTypeButton>();
        if (control != null)
        {
            control.Interactable = interactable;
            control.SetRaycast(interactable);
        }
    }
}
