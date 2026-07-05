using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// StartScene 하단 싱크(입력 offset) 조절 슬라이더.
/// </summary>
public class StartSceneSyncSliderUI : MonoBehaviour
{
    const string SliderPath = "Decor/SyncSlider/Slider";
    const string LabelPath = "Decor/SyncSlider/Label";

    [SerializeField] Slider slider;
    [SerializeField] TextMeshProUGUI label;

    bool _bound;

    void Awake()
    {
        var decor = transform.Find("Decor");
        if (decor != null && decor.Find("SyncSlider") == null)
            StartSceneVisualBuilder.BuildSyncSlider(decor);

        EnsureReferences();
    }

    void OnDestroy()
    {
        if (RhythmInputSettings.Instance != null)
            RhythmInputSettings.Instance.OnInputOffsetChanged -= OnSettingsOffsetChanged;
    }

    void Start()
    {
        EnsureReferences();
        BindSlider();
        RefreshFromSettings();

        if (RhythmInputSettings.Instance != null)
            RhythmInputSettings.Instance.OnInputOffsetChanged += OnSettingsOffsetChanged;
    }

    void EnsureReferences()
    {
        if (slider == null)
            slider = transform.Find(SliderPath)?.GetComponent<Slider>();

        if (label == null)
            label = transform.Find(LabelPath)?.GetComponent<TextMeshProUGUI>();
    }

    void BindSlider()
    {
        if (_bound || slider == null)
            return;

        _bound = true;
        slider.minValue = RhythmInputSettings.MinInputOffsetAdjustment;
        slider.maxValue = RhythmInputSettings.MaxInputOffsetAdjustment;
        slider.onValueChanged.AddListener(OnSliderChanged);
    }

    void OnSliderChanged(float adjustment)
    {
        var settings = RhythmInputSettings.Instance;
        if (settings == null)
            return;

        settings.SetInputOffsetAdjustment(adjustment, persist: true);
        UpdateLabel(adjustment, settings.InputOffsetSeconds);
    }

    void OnSettingsOffsetChanged(float absoluteSeconds)
    {
        var settings = RhythmInputSettings.Instance;
        if (settings == null || slider == null)
            return;

        slider.SetValueWithoutNotify(settings.InputOffsetAdjustment);
        UpdateLabel(settings.InputOffsetAdjustment, absoluteSeconds);
    }

    void RefreshFromSettings()
    {
        var settings = RhythmInputSettings.Instance;
        if (settings == null || slider == null)
            return;

        slider.SetValueWithoutNotify(settings.InputOffsetAdjustment);
        UpdateLabel(settings.InputOffsetAdjustment, settings.InputOffsetSeconds);
    }

    static void UpdateLabel(TextMeshProUGUI labelText, float adjustment, float absoluteSeconds)
    {
        if (labelText == null)
            return;

        int ms = Mathf.RoundToInt(adjustment * 1000f);
        int absMs = Mathf.RoundToInt(absoluteSeconds * 1000f);
        labelText.text = $"싱크 조절 {ms:+0;-0}ms";
    }

    void UpdateLabel(float adjustment, float absoluteSeconds)
    {
        UpdateLabel(label, adjustment, absoluteSeconds);
    }
}
