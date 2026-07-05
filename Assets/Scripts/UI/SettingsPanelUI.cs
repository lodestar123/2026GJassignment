using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pause 설정 패널 — 입력 감도 · 메트로놈 · SFX 볼륨.
/// </summary>
public class SettingsPanelUI : MonoBehaviour
{
    public static SettingsPanelUI Instance { get; private set; }

    [SerializeField] GameObject panelRoot;
    [SerializeField] Slider inputOffsetSlider;
    [SerializeField] TextMeshProUGUI inputOffsetLabel;
    [SerializeField] Slider metronomeSlider;
    [SerializeField] Slider sfxSlider;
    [SerializeField] Button closeButton;

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

    public void EnsureInitialized()
    {
        if (_initialized)
            return;

        _initialized = true;

        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);

        if (inputOffsetSlider != null)
        {
            inputOffsetSlider.minValue = RhythmInputSettings.MinInputOffsetAdjustment;
            inputOffsetSlider.maxValue = RhythmInputSettings.MaxInputOffsetAdjustment;
            inputOffsetSlider.onValueChanged.AddListener(OnInputOffsetChanged);
        }

        if (metronomeSlider != null)
            metronomeSlider.onValueChanged.AddListener(v => GameSettings.MetronomeVolume = v);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(v => GameSettings.SfxVolume = v);

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
        PauseMenuUI.Resolve()?.ShowButtonsOnly();
    }

    public void HidePanelOnly()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    void RefreshFromData()
    {
        var settings = RhythmInputSettings.Instance;
        if (settings != null && inputOffsetSlider != null)
        {
            inputOffsetSlider.SetValueWithoutNotify(settings.InputOffsetAdjustment);
            UpdateInputLabel(settings.InputOffsetAdjustment, settings.InputOffsetSeconds);
        }

        if (metronomeSlider != null)
            metronomeSlider.SetValueWithoutNotify(GameSettings.MetronomeVolume);

        if (sfxSlider != null)
            sfxSlider.SetValueWithoutNotify(GameSettings.SfxVolume);
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
