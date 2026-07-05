using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 2/4 (2박 = 한 사이클).
/// 마디 경계 = wall-clock · OnBeat = baseline felt(0.24s) — Core·적·메트로놈과 타임라인 adj=0 정렬.
/// 판정·playhead = baseline + 감도 조정 (RhythmInputSettings).
/// </summary>
[DefaultExecutionOrder(-50)]
public class BeatClock : MonoBehaviour
{
    public static BeatClock Instance { get; private set; }

    public const float ReferenceMeasureDuration = 1f;
    public const int BeatsPerMeasure = 2;

    public event Action OnBeat;
    public event Action OnMeasureStart;
    public event Action OnMeasureEnd;
    public event Action<float> OnTimingChanged;
    public event Action<bool> OnRhythmTestInvincibleChanged;

    [SerializeField]
    [Min(0.25f)]
    float measureDurationSeconds = 1f;

    [Header("Rhythm test")]
    [SerializeField] bool rhythmTestInvincible;
    [SerializeField] bool createInvincibleToggleUi = true;
    [SerializeField] KeyCode invincibleToggleKey = KeyCode.F8;

    Toggle _invincibleToggle;

    public float MeasureDurationSeconds
    {
        get => measureDurationSeconds;
        set
        {
            measureDurationSeconds = Mathf.Max(0.25f, value);
            NotifyTimingChanged();
        }
    }

    public float EffectiveMeasureDuration => measureDurationSeconds * TempoScale;
    public float TempoScale =>
        TempoController.Instance != null ? TempoController.Instance.CurrentScale : 1f;
    public float PatternTimeScale => EffectiveMeasureDuration / ReferenceMeasureDuration;
    public float BeatInterval => EffectiveMeasureDuration / BeatsPerMeasure;
    public float CurrentBpm => 60f / BeatInterval;
    public float MeasureStartTime { get; private set; }
    public int BeatIndexInMeasure { get; private set; }
    public bool IsDownbeat => BeatIndexInMeasure == 0;

    /// <summary>리듬 테스트 — Core HP 0이어도 패배하지 않음.</summary>
    public bool RhythmTestInvincible
    {
        get => rhythmTestInvincible;
        set => SetRhythmTestInvincible(value);
    }

    public static bool IsRhythmTestInvincible =>
        Instance != null && Instance.rhythmTestInvincible;

    float _lastEffectiveMeasureDuration;
    int _beatsSinceCycleStart;
    bool _cycleRunning;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _lastEffectiveMeasureDuration = EffectiveMeasureDuration;
    }

    void Start()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.name == SceneNames.Game && FindAnyObjectByType<GameStartCountdownUI>() != null)
            _cycleRunning = false;
        else
            BeginCycle();

        TryBuildInvincibleToggleUi();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (Input.GetKeyDown(invincibleToggleKey))
            SetRhythmTestInvincible(!rhythmTestInvincible);

        if (!_cycleRunning)
            return;

        if (!Mathf.Approximately(_lastEffectiveMeasureDuration, EffectiveMeasureDuration))
            NotifyTimingChanged();

        while (Time.time - MeasureStartTime >= EffectiveMeasureDuration)
            EndCycleAndBeginNext();

        float visualElapsed = GetVisualBeatElapsedInMeasure();
        if (visualElapsed < 0f)
            return;

        int beatIndex = Mathf.FloorToInt(visualElapsed / BeatInterval);
        while (_beatsSinceCycleStart <= beatIndex && _beatsSinceCycleStart < BeatsPerMeasure)
            FireBeat();
    }

    void BeginCycle()
    {
        _cycleRunning = true;
        MeasureStartTime = Time.time;
        _beatsSinceCycleStart = 0;
        OnMeasureStart?.Invoke();
    }

    /// <summary>BGM 선행 대기 후 마디·메트로놈 원점 재동기화.</summary>
    public void ResyncMeasureStart() => BeginCycle();

    void EndCycleAndBeginNext()
    {
        OnMeasureEnd?.Invoke();

        MeasureStartTime += EffectiveMeasureDuration;
        _beatsSinceCycleStart = 0;
        OnMeasureStart?.Invoke();
    }

    void FireBeat()
    {
        BeatIndexInMeasure = _beatsSinceCycleStart % BeatsPerMeasure;
        _beatsSinceCycleStart++;
        OnBeat?.Invoke();
    }

    void NotifyTimingChanged()
    {
        _lastEffectiveMeasureDuration = EffectiveMeasureDuration;
        OnTimingChanged?.Invoke(PatternTimeScale);
    }

    /// <summary>OnBeat·Core 펄스 축 — baseline offset만 (감도 슬라이더와 무관).</summary>
    public static float GetVisualBeatOffsetSeconds()
    {
        return RhythmInputSettings.BaselineInputOffsetSeconds;
    }

    public float GetVisualBeatElapsedInMeasure()
    {
        return (Time.time - GetVisualBeatOffsetSeconds()) - MeasureStartTime;
    }

    public float SecondsSinceMeasureStart() => Time.time - MeasureStartTime;

    public float SecondsSinceMeasureStartAt(float rawTime) => rawTime - MeasureStartTime;

    public void RefreshTempo()
    {
        if (!_cycleRunning)
            return;

        NotifyTimingChanged();
    }

    public void SetRhythmTestInvincible(bool enabled)
    {
        if (rhythmTestInvincible == enabled)
            return;

        rhythmTestInvincible = enabled;
        SyncInvincibleToggleUi();
        OnRhythmTestInvincibleChanged?.Invoke(rhythmTestInvincible);
        Debug.Log($"[BeatClock] Rhythm test invincible {(enabled ? "ON" : "OFF")}");
    }

    void SyncInvincibleToggleUi()
    {
        if (_invincibleToggle != null && _invincibleToggle.isOn != rhythmTestInvincible)
            _invincibleToggle.SetIsOnWithoutNotify(rhythmTestInvincible);
    }

    void TryBuildInvincibleToggleUi()
    {
        if (!createInvincibleToggleUi || _invincibleToggle != null)
            return;

        var scene = SceneManager.GetActiveScene();
        if (scene.name != SceneNames.Game && scene.name != SceneNames.Practice)
            return;

        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
            return;

        var root = new GameObject("RhythmTestInvincibleToggle", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        root.transform.SetAsLastSibling();

        var rt = root.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(12f, 12f);
        rt.sizeDelta = new Vector2(220f, 32f);

        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.06f, 0.1f, 0.82f);
        bg.raycastTarget = true;

        var toggleGo = new GameObject("Toggle", typeof(RectTransform));
        toggleGo.transform.SetParent(root.transform, false);
        var toggleRt = toggleGo.GetComponent<RectTransform>();
        toggleRt.anchorMin = new Vector2(0f, 0.5f);
        toggleRt.anchorMax = new Vector2(0f, 0.5f);
        toggleRt.pivot = new Vector2(0f, 0.5f);
        toggleRt.anchoredPosition = new Vector2(8f, 0f);
        toggleRt.sizeDelta = new Vector2(24f, 24f);

        var toggle = toggleGo.AddComponent<Toggle>();
        toggle.isOn = rhythmTestInvincible;

        var boxGo = new GameObject("Box", typeof(RectTransform), typeof(Image));
        boxGo.transform.SetParent(toggleGo.transform, false);
        StretchRect(boxGo.GetComponent<RectTransform>());
        var boxImg = boxGo.GetComponent<Image>();
        boxImg.sprite = GreyboxSprites.Square;
        boxImg.color = new Color(0.2f, 0.2f, 0.24f, 1f);

        var checkGo = new GameObject("Check", typeof(RectTransform), typeof(Image));
        checkGo.transform.SetParent(toggleGo.transform, false);
        StretchRect(checkGo.GetComponent<RectTransform>(), 4f);
        var checkImg = checkGo.GetComponent<Image>();
        checkImg.sprite = GreyboxSprites.Square;
        checkImg.color = new Color(0.45f, 0.85f, 1f, 1f);

        toggle.targetGraphic = boxImg;
        toggle.graphic = checkImg;

        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(root.transform, false);
        var labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = new Vector2(36f, 0f);
        labelRt.offsetMax = new Vector2(-8f, 0f);
        var label = labelGo.AddComponent<TextMeshProUGUI>();
        label.text = "Invincible (Rhythm Test)";
        label.fontSize = 15f;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.color = new Color(0.9f, 0.9f, 0.92f, 1f);
        label.raycastTarget = false;
        BeatDefenderFonts.Apply(label);

        toggle.onValueChanged.AddListener(SetRhythmTestInvincible);
        _invincibleToggle = toggle;
    }

    static void StretchRect(RectTransform rt, float inset = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
    }

    void OnValidate()
    {
        measureDurationSeconds = Mathf.Max(0.25f, measureDurationSeconds);
        if (Application.isPlaying && Instance == this)
            SyncInvincibleToggleUi();
    }
}
