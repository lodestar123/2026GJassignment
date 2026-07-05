using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 타워 클릭 시 판매 버튼 — 50% 환급.
/// </summary>
public class TowerSellUI : MonoBehaviour
{
    public static TowerSellUI Instance { get; private set; }

    [SerializeField] GameObject panelRoot;
    [SerializeField] TextMeshProUGUI infoText;
    [SerializeField] Button sellButton;

    Tower _selected;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Hide();

        if (sellButton != null)
            sellButton.onClick.AddListener(OnSellClicked);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Show(Tower tower)
    {
        _selected = tower;
        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (infoText != null && tower != null)
        {
            int refund = TowerPlacer.GetCost(tower.towerType) / 2;
            infoText.text = $"{tower.towerType}\nSell +{refund}G";
        }
    }

    public void Hide()
    {
        _selected = null;
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    void OnSellClicked()
    {
        if (_selected == null)
            return;

        TowerPlacer.Instance?.SellTower(_selected);
    }
}
