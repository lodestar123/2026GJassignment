using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 설정 패널 — 입력 감도(선택) · 효과음 · 배경음.
/// </summary>
public class SettingsPanelUI : MonoBehaviour
{
    public static SettingsPanelUI Instance { get; private set; }

    [SerializeField] GameObject panelRoot;
    [SerializeField] Slider inputOffsetSlider;
    [SerializeField] TextMeshProUGUI inputOffsetLabel;
    [SerializeField] Slider sfxSlider;
    [SerializeField] Slider bgmSlider;
    [SerializeField] Button closeButton;
    [SerializeField] bool showInputOffset = true;
    [SerializeField] bool closeReturnsToPauseMenu = true;

    bool _initialized;

    public GameObject PanelRoot => panelRoot;
    public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static SettingsPanelUI Resolve()
    {
        if (Instance != null)
            return Instance;

        Instance = FindAnyObjectByType<SettingsPanelUI>(FindObjectsInactive.Include);
        return Instance;
    }

    public void Configure(
        GameObject panel,
        Slider inputSlider,
        TextMeshProUGUI inputLabel,
        Slider sfx,
        Slider bgm,
        Button close,
        bool inputOffsetVisible,
        bool returnsToPauseMenu)
    {
        _initialized = false;
        panelRoot = panel;
        inputOffsetSlider = inputSlider;
        inputOffsetLabel = inputLabel;
        sfxSlider = sfx;
        bgmSlider = bgm;
        closeButton = close;
        showInputOffset = inputOffsetVisible;
        closeReturnsToPauseMenu = returnsToPauseMenu;

        if (inputOffsetLabel != null)
            inputOffsetLabel.gameObject.SetActive(inputOffsetVisible);
        if (inputOffsetSlider != null)
            inputOffsetSlider.gameObject.SetActive(inputOffsetVisible);

        EnsureInitialized();
    }

    public void EnsureInitialized()
    {
        if (_initialized)
            return;

        _initialized = true;

        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);

        if (showInputOffset && inputOffsetSlider != null)
        {
            inputOffsetSlider.minValue = RhythmInputSettings.MinInputOffsetAdjustment;
            inputOffsetSlider.maxValue = RhythmInputSettings.MaxInputOffsetAdjustment;
            inputOffsetSlider.onValueChanged.AddListener(OnInputOffsetChanged);
        }

        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(v => GameSettings.SfxVolume = v);

        if (bgmSlider != null)
            bgmSlider.onValueChanged.AddListener(v => GameSettings.BgmVolume = v);

        RefreshFromData();
    }

    public void Show()
    {
        EnsureInitialized();
        RefreshFromData();
        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    public void Hide()
    {
        HidePanelOnly();
        if (closeReturnsToPauseMenu)
            PauseMenuUI.Resolve()?.ShowButtonsOnly();
    }

    public void HidePanelOnly()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    void RefreshFromData()
    {
        if (showInputOffset)
        {
            var settings = RhythmInputSettings.Instance;
            if (settings != null && inputOffsetSlider != null)
            {
                inputOffsetSlider.SetValueWithoutNotify(settings.InputOffsetAdjustment);
                UpdateInputLabel(settings.InputOffsetAdjustment, settings.InputOffsetSeconds);
            }
        }

        if (sfxSlider != null)
            sfxSlider.SetValueWithoutNotify(GameSettings.SfxVolume);

        if (bgmSlider != null)
            bgmSlider.SetValueWithoutNotify(GameSettings.BgmVolume);
    }

    void OnInputOffsetChanged(float adjustment)
    {
        var settings = RhythmInputSettings.Instance;
        if (settings == null)
            return;

        settings.SetInputOffsetAdjustment(adjustment, persist: true);
        UpdateInputLabel(adjustment, settings.InputOffsetSeconds);
    }

    void UpdateInputLabel(float adjustment, float absoluteSeconds)
    {
        if (inputOffsetLabel == null)
            return;

        int ms = Mathf.RoundToInt(adjustment * 1000f);
        int absMs = Mathf.RoundToInt(absoluteSeconds * 1000f);
        inputOffsetLabel.text = $"입력 감도: {ms:+0;-0}ms (적용 {absMs}ms)";
    }
}
