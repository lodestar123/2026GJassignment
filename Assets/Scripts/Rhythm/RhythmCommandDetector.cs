using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 탭 입력 → 즉시 판정(패턴 완성·연장 불가 시) + 사이클 종료 flush.
/// 경계 백필: Overload 5타 등 <b>후반 슬롯</b>만, 완성 패턴에는 추가 안 함.
/// </summary>
[DefaultExecutionOrder(-80)]
public class RhythmCommandDetector : MonoBehaviour
{
    public static RhythmCommandDetector Instance { get; private set; }

    public event Action<CommandType, JudgmentResult> OnCommandResolved;
    /// <summary>offset 적용 마디 내 초 — 타임라인·판정 동일 축.</summary>
    public event Action<float> OnTapVisualized;

    public int CurrentTapCount => _cycleTaps.Count;

    readonly List<float> _cycleTaps = new();
    readonly List<float> _evalSnapshot = new();

    float? _pendingNextCycleTap;
    float _pendingTapAbsoluteTime;
    bool _applyPendingThisFrame;

    readonly List<float> _pendingEvalTaps = new();
    bool _hasPendingEval;
    bool _pendingAwaitingBackfill;
    float _pendingEvalDuration;
    float _pendingEvalScale;
    float _endedCycleStartTime;

    RhythmInputRecorder _recorder;
    RhythmInputSettings _inputSettings;
    SkillCooldownController _cooldowns;
    CommandEffectController _effects;
    RunStats _stats;
    TowerRegistry _towers;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (GetComponent<RhythmUiInputGuard>() == null)
            gameObject.AddComponent<RhythmUiInputGuard>();
        _inputSettings = GetComponent<RhythmInputSettings>();
        if (_inputSettings == null)
            _inputSettings = gameObject.AddComponent<RhythmInputSettings>();
        _recorder = GetComponent<RhythmInputRecorder>();
        _cooldowns = GetComponent<SkillCooldownController>();
        _effects = GetComponent<CommandEffectController>();
        _stats = GetComponent<RunStats>();
        _towers = GetComponent<TowerRegistry>();
        if (_towers == null)
            _towers = FindAnyObjectByType<TowerRegistry>();
    }

    void OnEnable() => TrySubscribe();
    void Start() => TrySubscribe();
    void OnDisable() => TryUnsubscribe();

    void OnDestroy()
    {
        TryUnsubscribe();
        if (Instance == this)
            Instance = null;
    }

    void TrySubscribe()
    {
        if (BeatClock.Instance == null)
            return;

        BeatClock.Instance.OnMeasureEnd -= OnCycleEnd;
        BeatClock.Instance.OnMeasureEnd += OnCycleEnd;
        BeatClock.Instance.OnMeasureStart -= OnCycleStart;
        BeatClock.Instance.OnMeasureStart += OnCycleStart;
    }

    void TryUnsubscribe()
    {
        if (BeatClock.Instance == null)
            return;

        BeatClock.Instance.OnMeasureEnd -= OnCycleEnd;
        BeatClock.Instance.OnMeasureStart -= OnCycleStart;
    }

    void Update()
    {
        if (Time.timeScale <= 0f)
            return;

        if (RhythmKeyFilter.TryGetRhythmKeysDown(_frameKeys) > 0)
        {
            float rawTime = Time.time;
            VisualizeTap(rawTime);
            RegisterTap(rawTime);
        }

        TryEvaluateWhenExtensionClosed(_cycleTaps);
    }

    readonly List<KeyCode> _frameKeys = new();

    void LateUpdate()
    {
        if (_applyPendingThisFrame)
        {
            _applyPendingThisFrame = false;
            ApplyPendingTap();
            TryEvaluateWhenExtensionClosed(_cycleTaps);
        }
    }

    float AdjustTapTime(float rawTime)
    {
        if (_inputSettings != null)
            return _inputSettings.AdjustTapTime(rawTime);

        if (RhythmInputSettings.Instance != null)
            return RhythmInputSettings.Instance.AdjustTapTime(rawTime);

        return rawTime - RhythmInputSettings.DefaultInputOffsetSeconds;
    }

    void OnCycleStart()
    {
        _cycleTaps.Clear();
        _applyPendingThisFrame = _pendingNextCycleTap.HasValue;
    }

    void OnCycleEnd()
    {
        if (_hasPendingEval)
            FlushPendingEval();

        float scale = BeatClock.Instance != null ? BeatClock.Instance.PatternTimeScale : 1f;
        float duration = BeatClock.Instance != null ? BeatClock.Instance.EffectiveMeasureDuration : 1f;

        _pendingEvalTaps.Clear();
        _pendingEvalTaps.AddRange(_cycleTaps);
        _pendingEvalDuration = duration;
        _pendingEvalScale = scale;
        _endedCycleStartTime = BeatClock.Instance != null ? BeatClock.Instance.MeasureStartTime : Time.time;
        _pendingAwaitingBackfill = RhythmPatternLibrary.NeedsBoundaryBackfillSlot(
            _pendingEvalTaps, duration, scale);
        _hasPendingEval = _pendingEvalTaps.Count > 0;

        if (_hasPendingEval && !_pendingAwaitingBackfill)
            FlushPendingEval();

        _cycleTaps.Clear();
    }

    void RegisterTap(float rawTime)
    {
        if (BeatClock.Instance == null)
            return;

        bool backfilled = TryBackfillEndedCycle(rawTime);

        if (_hasPendingEval && _pendingAwaitingBackfill && !backfilled)
        {
            FlushPendingEval();
            _pendingAwaitingBackfill = false;
        }

        float time = AdjustTapTime(rawTime);
        float cycleDuration = BeatClock.Instance.EffectiveMeasureDuration;
        float scale = BeatClock.Instance.PatternTimeScale;
        float good = RhythmPatternLibrary.GetJudgmentGood(scale);
        float perfect = RhythmPatternLibrary.GetJudgmentPerfect(scale);
        float rel = GetAdjustedRelativeTime(rawTime);

        if (rel > cycleDuration + good)
            return;

        if (TryQueueEarlyNextBeat(rel, cycleDuration, perfect, time))
            return;

        if (rel < -good)
            return;

        if (AddTapToCycle(rel, time))
            TryEvaluateWhenExtensionClosed(_cycleTaps, rel);
    }

    bool TryBackfillEndedCycle(float rawTime)
    {
        if (!_hasPendingEval || !_pendingAwaitingBackfill)
            return false;

        if (_pendingEvalTaps.Count == 0)
            return false;

        float relEnded = AdjustTapTime(rawTime) - _endedCycleStartTime;
        float newCycleRel = GetAdjustedRelativeTime(rawTime);

        if (!RhythmPatternLibrary.ShouldBackfillLateTap(
                _pendingEvalTaps, relEnded, newCycleRel, _pendingEvalDuration, _pendingEvalScale))
            return false;

        float minGap = RhythmPatternLibrary.MinTapGapReference * _pendingEvalScale;
        for (int i = 0; i < _pendingEvalTaps.Count; i++)
        {
            if (Mathf.Abs(relEnded - _pendingEvalTaps[i]) < minGap)
                return false;
        }

        _pendingEvalTaps.Add(relEnded);
        _pendingEvalTaps.Sort();

        if (TryEvaluateImmediate(_pendingEvalTaps, _pendingEvalDuration, _pendingEvalScale))
        {
            _hasPendingEval = false;
            _pendingAwaitingBackfill = false;
        }

        return true;
    }

    public float GetAdjustedRelativeTime(float rawTime)
    {
        return AdjustTapTime(rawTime) - BeatClock.Instance.MeasureStartTime;
    }

    void VisualizeTap(float rawTime)
    {
        if (BeatClock.Instance == null)
            return;

        OnTapVisualized?.Invoke(GetAdjustedRelativeTime(rawTime));
    }

    bool TryQueueEarlyNextBeat(float rel, float cycleDuration, float perfect, float absoluteTime)
    {
        if (_cycleTaps.Count > 0)
            return false;

        float nextRel = rel - cycleDuration;
        if (Mathf.Abs(nextRel) > perfect)
            return false;

        _pendingNextCycleTap = nextRel;
        _pendingTapAbsoluteTime = absoluteTime;
        return true;
    }

    void ApplyPendingTap()
    {
        if (!_pendingNextCycleTap.HasValue)
            return;

        if (AddTapToCycle(_pendingNextCycleTap.Value, _pendingTapAbsoluteTime))
        {
            float rel = _pendingNextCycleTap.Value;
            _pendingNextCycleTap = null;
            TryEvaluateWhenExtensionClosed(_cycleTaps, rel);
        }
    }

    bool AddTapToCycle(float rel, float absoluteTime)
    {
        float scale = BeatClock.Instance.PatternTimeScale;
        float minGap = RhythmPatternLibrary.MinTapGapReference * scale;

        for (int i = 0; i < _cycleTaps.Count; i++)
        {
            if (Mathf.Abs(rel - _cycleTaps[i]) < minGap)
                return false;
        }

        _cycleTaps.Add(rel);
        _cycleTaps.Sort();
        _recorder?.RecordTap(absoluteTime);
        return true;
    }

    void TryEvaluateWhenExtensionClosed(List<float> taps, float? nowRelOverride = null)
    {
        if (taps.Count == 0 || BeatClock.Instance == null)
            return;

        float duration = BeatClock.Instance.EffectiveMeasureDuration;
        float scale = BeatClock.Instance.PatternTimeScale;
        float nowRel = nowRelOverride ?? GetAdjustedRelativeTime(Time.time);

        if (RhythmPatternLibrary.CanExtendToLongerPattern(taps, nowRel, duration, scale))
            return;

        TryEvaluateImmediate(taps, duration, scale);
    }

    void TryEvaluateImmediate(List<float> taps)
    {
        if (BeatClock.Instance == null)
            return;

        TryEvaluateImmediate(
            taps,
            BeatClock.Instance.EffectiveMeasureDuration,
            BeatClock.Instance.PatternTimeScale);
    }

    bool TryEvaluateImmediate(List<float> taps, float duration, float scale)
    {
        if (taps.Count == 0)
            return false;

        if (!RhythmPatternLibrary.TryMatchCompletePattern(taps, duration, scale, out var pattern, out var judgment))
            return false;

        _evalSnapshot.Clear();
        _evalSnapshot.AddRange(taps);
        ResolveCommand(pattern.Type, judgment, taps.Count, duration);
        taps.Clear();
        return true;
    }

    void FlushPendingEval()
    {
        if (!_hasPendingEval)
            return;

        _hasPendingEval = false;
        _pendingAwaitingBackfill = false;

        if (_pendingEvalTaps.Count == 0)
            return;

        EvaluateTaps(_pendingEvalTaps, _pendingEvalDuration, _pendingEvalScale);
        _pendingEvalTaps.Clear();
    }

    void EvaluateTaps(List<float> taps, float cycleDuration, float scale)
    {
        if (TryEvaluateImmediate(taps, cycleDuration, scale))
            return;

        _evalSnapshot.Clear();
        _evalSnapshot.AddRange(taps);

        if (_evalSnapshot.Count == 0)
            return;

        ResolveCommand(CommandType.None, JudgmentResult.Miss, _evalSnapshot.Count, cycleDuration);
    }

    void ResolveCommand(CommandType type, JudgmentResult judgment, int tapCount, float cycleDuration)
    {
        if (type != CommandType.None && judgment != JudgmentResult.Miss)
        {
            if (_cooldowns != null && _cooldowns.RequiresCooldown(type) && !_cooldowns.CanUse(type))
            {
                judgment = JudgmentResult.Cooldown;
            }
            else if (!HasRequiredTower(type))
            {
                judgment = JudgmentResult.NoTower;
            }
            else if (judgment != JudgmentResult.Cooldown && judgment != JudgmentResult.NoTower)
            {
                if (_cooldowns != null && _cooldowns.RequiresCooldown(type))
                    _cooldowns.TryConsume(type);

                _effects?.Apply(type, judgment);
            }
        }

        _stats?.RecordJudgment(judgment);
        OnCommandResolved?.Invoke(type, judgment);

        if (type == CommandType.None && judgment == JudgmentResult.Miss)
            Debug.Log($"[Rhythm] MISS — {tapCount} taps [{FormatTaps(_evalSnapshot)}] (measure {cycleDuration:0.###}s)");
        else if (judgment == JudgmentResult.NoTower)
            Debug.Log($"[Rhythm] NO TOWER — {type} (해당 타워 미배치)");
        else
            Debug.Log($"[Rhythm] {judgment} — {type} ({tapCount} taps) [{FormatTaps(_evalSnapshot)}]");
    }

    bool HasRequiredTower(CommandType type) => type switch
    {
        CommandType.OverloadStrike => _towers != null && _towers.StrikeTowers.Count > 0,
        CommandType.BPMBoost => _towers != null && _towers.BoostTowers.Count > 0,
        _ => true
    };

    static string FormatTaps(List<float> taps)
    {
        if (taps == null || taps.Count == 0)
            return "";

        var parts = new string[taps.Count];
        for (int i = 0; i < taps.Count; i++)
            parts[i] = $"{taps[i]:0.###}s";
        return string.Join(", ", parts);
    }
}
