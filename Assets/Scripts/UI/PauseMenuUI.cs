using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ESC Pause 패널 — 계속 / 재시작 / 설정 / 타이틀.
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    public static PauseMenuUI Instance { get; private set; }

    [SerializeField] GameObject panelRoot;
    [SerializeField] GameObject buttonsRoot;
    [SerializeField] Button continueButton;
    [SerializeField] Button restartButton;
    [SerializeField] Button settingsButton;
    [SerializeField] Button titleButton;

    bool _initialized;

    public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureInitialized();
    }

    void Start()
    {
        if (PauseController.Instance == null || !PauseController.Instance.IsPaused)
            HideImmediate();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static PauseMenuUI Resolve()
    {
        if (Instance != null)
            return Instance;

        Instance = FindAnyObjectByType<PauseMenuUI>(FindObjectsInactive.Include);
        return Instance;
    }

    public void EnsureInitialized()
    {
        if (_initialized)
            return;

        _initialized = true;

        if (panelRoot == null)
            panelRoot = transform.Find("PauseMenu")?.gameObject ?? gameObject;

        if (buttonsRoot == null)
            buttonsRoot = panelRoot != null ? panelRoot.transform.Find("Panel")?.gameObject : null;

        WireButtons();
    }

    void WireButtons()
    {
        if (continueButton != null)
            continueButton.onClick.AddListener(() => PauseController.Instance?.Resume());

        if (restartButton != null)
            restartButton.onClick.AddListener(() => PauseController.Instance?.RestartGame());

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettings);

        if (titleButton != null)
            titleButton.onClick.AddListener(() => PauseController.Instance?.GoToTitle());
    }

    void OpenSettings()
    {
        HideButtons();
        SettingsPanelUI.Resolve()?.Show();
    }

    public void ShowButtonsOnly()
    {
        if (buttonsRoot != null)
            buttonsRoot.SetActive(true);
    }

    public void ShowButtons()
    {
        SettingsPanelUI.Resolve()?.HidePanelOnly();
        ShowButtonsOnly();
    }

    public void HideButtons()
    {
        if (buttonsRoot != null)
            buttonsRoot.SetActive(false);
    }

    public void Show()
    {
        EnsureInitialized();
        if (panelRoot != null)
            panelRoot.SetActive(true);
        ShowButtons();
    }

    public void Hide()
    {
        SettingsPanelUI.Resolve()?.HidePanelOnly();
        HideImmediate();
    }

    void HideImmediate()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }
}
