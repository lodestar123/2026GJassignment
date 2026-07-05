using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 탭 입력 → 판정.
/// Scroll에서 선택한 패턴 1종만 매칭한다(기본 GoldPulse).
/// </summary>
[DefaultExecutionOrder(-80)]
public class RhythmCommandDetector : MonoBehaviour
{
    public static RhythmCommandDetector Instance { get; private set; }

    public event Action<CommandType, JudgmentResult> OnCommandResolved;
    /// <summary>offset 적용 마디 내 초 — 타임라인 시각화 전용.</summary>
    public event Action<float> OnTapVisualized;

    public int CurrentTapCount => _seqOpen ? _seqTaps.Count : 0;

    readonly List<float> _seqTaps = new();
    float _seqAnchor;
    bool _seqOpen;

    readonly List<float> _evalSnapshot = new();
    readonly List<KeyCode> _frameKeys = new();

    RhythmInputRecorder _recorder;
    RhythmInputSettings _inputSettings;
    SkillCooldownController _cooldowns;
    CommandEffectController _effects;
    RunStats _stats;
    TowerRegistry _towers;
    RhythmPatternSelector _selector;

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
        _selector = GetComponent<RhythmPatternSelector>();
        if (_selector == null)
            _selector = gameObject.AddComponent<RhythmPatternSelector>();
    }

    void OnEnable()
    {
        if (_selector != null)
            _selector.OnSelectionChanged += HandleSelectionChanged;
    }

    void OnDisable()
    {
        if (_selector != null)
            _selector.OnSelectionChanged -= HandleSelectionChanged;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
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

        TryCloseIfExpired();
    }

    void HandleSelectionChanged(CommandType type)
    {
        AbortSequence();
    }

    CommandType GetSelectedType()
    {
        if (_selector != null && _selector.Selected != CommandType.None)
            return _selector.Selected;
        return CommandType.GoldPulse;
    }

    bool TryGetSelectedPattern(out RhythmPattern pattern)
    {
        return RhythmPatternLibrary.TryGetByType(GetSelectedType(), out pattern);
    }

    float AdjustTapTime(float rawTime)
    {
        if (_inputSettings != null)
            return _inputSettings.AdjustTapTime(rawTime);

        if (RhythmInputSettings.Instance != null)
            return RhythmInputSettings.Instance.AdjustTapTime(rawTime);

        return rawTime - RhythmInputSettings.DefaultInputOffsetSeconds;
    }

    void VisualizeTap(float rawTime)
    {
        if (BeatClock.Instance == null)
            return;

        float rel = RhythmInputSettings.GetFeltElapsedInMeasure(
            rawTime,
            BeatClock.Instance.MeasureStartTime);
        OnTapVisualized?.Invoke(rel);
    }

    void RegisterTap(float rawTime)
    {
        if (BeatClock.Instance == null)
            return;

        float time = AdjustTapTime(rawTime);

        if (!TryGetSelectedPattern(out var pattern))
            return;

        if (!_seqOpen)
        {
            StartSequence(time);
            return;
        }

        float duration = BeatClock.Instance.EffectiveMeasureDuration;
        float scale = BeatClock.Instance.PatternTimeScale;
        float good = RhythmPatternLibrary.GetJudgmentGood(scale);
        float rel = time - _seqAnchor;

        if (TryAcceptAsNextSlot(rel, pattern, duration, good))
        {
            _seqTaps.Add(rel);
            _recorder?.RecordTap(time);

            if (_seqTaps.Count >= pattern.TapCount)
                CloseSequence();
            else
                TryCloseIfExpired();

            return;
        }

        CloseSequence();
        StartSequence(time);
    }

    void StartSequence(float anchorTime)
    {
        _seqAnchor = anchorTime;
        _seqTaps.Clear();
        _seqTaps.Add(0f);
        _seqOpen = true;
        _recorder?.RecordTap(anchorTime);
    }

    bool TryAcceptAsNextSlot(float rel, RhythmPattern pattern, float duration, float good)
    {
        float minGap = RhythmPatternLibrary.MinTapGapReference * BeatClock.Instance.PatternTimeScale;
        for (int i = 0; i < _seqTaps.Count; i++)
        {
            if (Mathf.Abs(rel - _seqTaps[i]) < minGap)
                return false;
        }

        int n = _seqTaps.Count;
        if (n >= pattern.TapCount)
            return false;

        var expected = pattern.GetExpectedHitTimes(duration);
        if (!PrefixMatches(_seqTaps, expected, good))
            return false;

        return Mathf.Abs(rel - expected[n]) <= good;
    }

    static bool PrefixMatches(List<float> actual, float[] expected, float good)
    {
        for (int i = 0; i < actual.Count; i++)
        {
            if (Mathf.Abs(actual[i] - expected[i]) > good)
                return false;
        }

        return true;
    }

    void TryCloseIfExpired()
    {
        if (!_seqOpen || BeatClock.Instance == null || !TryGetSelectedPattern(out var pattern))
            return;

        float duration = BeatClock.Instance.EffectiveMeasureDuration;
        float scale = BeatClock.Instance.PatternTimeScale;
        float nowRel = AdjustTapTime(Time.time) - _seqAnchor;

        if (RhythmPatternLibrary.CanExtendPattern(_seqTaps, nowRel, duration, scale, pattern))
            return;

        CloseSequence();
    }

    void CloseSequence()
    {
        if (!_seqOpen)
            return;

        _seqOpen = false;

        if (BeatClock.Instance == null || !TryGetSelectedPattern(out var pattern))
        {
            _seqTaps.Clear();
            return;
        }

        float duration = BeatClock.Instance.EffectiveMeasureDuration;
        float scale = BeatClock.Instance.PatternTimeScale;

        _evalSnapshot.Clear();
        _evalSnapshot.AddRange(_seqTaps);

        if (RhythmPatternLibrary.TryMatchSinglePattern(_seqTaps, duration, scale, pattern, out var judgment))
            ResolveCommand(pattern.Type, judgment, _evalSnapshot.Count, duration);
        else
            ResolveCommand(CommandType.None, JudgmentResult.Miss, _evalSnapshot.Count, duration);

        _seqTaps.Clear();
    }

    void AbortSequence()
    {
        _seqOpen = false;
        _seqTaps.Clear();
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
            Debug.Log($"[Rhythm] MISS — {GetSelectedType()} / {tapCount} taps [{FormatTaps(_evalSnapshot)}]");
        else if (judgment == JudgmentResult.NoTower)
            Debug.Log($"[Rhythm] NO TOWER — {type} (해당 타워 미배치)");
        else
            Debug.Log($"[Rhythm] {judgment} — {type} ({tapCount} taps) [{FormatTaps(_evalSnapshot)}]");
    }

    bool HasRequiredTower(CommandType type) => type switch
    {
        CommandType.OverloadStrike => _towers != null && _towers.StrikeTowers.Count > 0,
        CommandType.ChainZap => _towers != null && _towers.BoostTowers.Count > 0,
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
