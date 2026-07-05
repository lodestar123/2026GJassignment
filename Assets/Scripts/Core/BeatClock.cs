using System;
using UnityEngine;

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

    [SerializeField]
    [Min(0.25f)]
    float measureDurationSeconds = 1f;

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

    void Start() => BeginCycle();

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
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

    void OnValidate()
    {
        measureDurationSeconds = Mathf.Max(0.25f, measureDurationSeconds);
    }
}
