using System;
using UnityEngine;

/// <summary>
/// 2/4 (2박 = 한 사이클). 매 사이클 끝에 OnMeasureEnd(판정) → OnMeasureStart(초기화).
/// 메트로놈 tick = 0s(1박), 절반(2박) — 판정은 사이클 끝에만.
/// </summary>
[DefaultExecutionOrder(-50)]
public class BeatClock : MonoBehaviour
{
    public static BeatClock Instance { get; private set; }

    public const float ReferenceMeasureDuration = 1f;
    public const int BeatsPerMeasure = 2;
    public const float BoostMeasureScale = 0.8f; // 120 -> 150 BPM (x0.8 duration, not x0.5)

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

    public float EffectiveMeasureDuration => measureDurationSeconds * (IsBoosted ? BoostMeasureScale : 1f);
    public float PatternTimeScale => EffectiveMeasureDuration / ReferenceMeasureDuration;
    public float BeatInterval => EffectiveMeasureDuration / BeatsPerMeasure;
    public float CurrentBpm => 60f / BeatInterval;
    public bool IsBoosted => _boostRemaining > 0f;
    public float BoostRemaining => _boostRemaining;
    public float MeasureStartTime { get; private set; }
    public int BeatIndexInMeasure { get; private set; }
    public bool IsDownbeat => BeatIndexInMeasure == 0;

    float _beatTimer;
    float _boostRemaining;
    float _lastEffectiveMeasureDuration;
    int _beatsSinceCycleStart;

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
        BeginCycle();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        float dt = Time.timeScale > 0f ? Time.deltaTime : Time.unscaledDeltaTime;

        if (_boostRemaining > 0f)
        {
            _boostRemaining -= dt;
            if (_boostRemaining <= 0f)
            {
                _boostRemaining = 0f;
                NotifyTimingChanged();
            }
        }

        if (!Mathf.Approximately(_lastEffectiveMeasureDuration, EffectiveMeasureDuration))
            ResyncBeatPhase();

        if (Time.time - MeasureStartTime >= EffectiveMeasureDuration)
            EndCycleAndBeginNext();

        _beatTimer += dt;
        while (_beatTimer >= BeatInterval)
        {
            _beatTimer -= BeatInterval;
            FireBeat();
        }
    }

    void BeginCycle()
    {
        MeasureStartTime = Time.time;
        _beatTimer = 0f;
        _beatsSinceCycleStart = 0;
        OnMeasureStart?.Invoke();
        FireBeat();
    }

    void EndCycleAndBeginNext()
    {
        OnMeasureEnd?.Invoke();

        MeasureStartTime += EffectiveMeasureDuration;
        _beatsSinceCycleStart = 0;
        _beatTimer = 0f;
        OnMeasureStart?.Invoke();
        FireBeat();
    }

    void ResyncBeatPhase()
    {
        NotifyTimingChanged();
        float elapsed = Mathf.Max(0f, Time.time - MeasureStartTime);
        _beatTimer = BeatInterval > 0f ? elapsed % BeatInterval : 0f;
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

    public float SecondsSinceMeasureStart()
    {
        return Time.time - MeasureStartTime;
    }

    /// <summary>특정 시각 기준 마디 내 경과(타임라인·playhead와 동일 축).</summary>
    public float SecondsSinceMeasureStartAt(float rawTime)
    {
        return rawTime - MeasureStartTime;
    }

    public void SetBoost(float durationSeconds)
    {
        _boostRemaining = Mathf.Max(_boostRemaining, durationSeconds);
        NotifyTimingChanged();
    }

    void OnValidate()
    {
        measureDurationSeconds = Mathf.Max(0.25f, measureDurationSeconds);
    }
}
