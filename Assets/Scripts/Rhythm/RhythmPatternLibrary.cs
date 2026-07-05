using System.Collections.Generic;

public readonly struct RhythmPattern
{
    public CommandType Type { get; }
    public int TapCount { get; }
    public float[] HitFractions { get; }

    public RhythmPattern(CommandType type, float[] hitFractions)
    {
        Type = type;
        HitFractions = hitFractions;
        TapCount = hitFractions.Length;
    }

    public float[] GetExpectedHitTimes(float measureDuration)
    {
        var times = new float[HitFractions.Length];
        for (int i = 0; i < HitFractions.Length; i++)
            times[i] = HitFractions[i] * measureDuration;
        return times;
    }
}

public static class RhythmPatternLibrary
{
    public const float JudgmentGoodSeconds = 0.22f;
    public const float JudgmentPerfectSeconds = 0.11f;
    /// <summary>
    /// 마커를 가이드에 스냅하는 표시용 창(기준 초). Perfect 판정 + 이 창 이내일 때만 스냅.
    /// JudgmentPerfectSeconds와 같으면 Perfect 전부 스냅, 더 좁히면 “거의 정확” 입력만 스냅.
    /// </summary>
    public const float VisualMarkerSnapSeconds = JudgmentPerfectSeconds;
    public const float MinTapGapReference = 0.02f;
    public const float MinVisualTapGapReference = 0.008f;

    public static readonly IReadOnlyList<RhythmPattern> All = new[]
    {
        new RhythmPattern(CommandType.GoldPulse, new[] { 0f, 0.5f }),
        new RhythmPattern(CommandType.RhythmShot, new[] { 0f, 0.5f, 0.75f }),
        new RhythmPattern(CommandType.OverloadStrike, new[] { 0f, 0.25f, 0.5f, 0.75f, 0.875f }),
        new RhythmPattern(CommandType.ChainZap, new[] { 0f, 0.125f, 0.25f, 0.5f, 0.625f, 0.75f }),
        new RhythmPattern(CommandType.TempoUp, new[] { 0f, 0.25f }),
        new RhythmPattern(CommandType.TempoDown, new[] { 0f, 0.75f })
    };

    /// <summary>Scroll UI · 휠 스왑 순서.</summary>
    public static readonly CommandType[] SelectableOrder =
    {
        CommandType.GoldPulse,
        CommandType.RhythmShot,
        CommandType.OverloadStrike,
        CommandType.ChainZap,
        CommandType.TempoUp,
        CommandType.TempoDown
    };

    public static readonly Dictionary<int, List<RhythmPattern>> ByTapCount = BuildLookup();

    public static float GetJudgmentGood(float timeScale) => JudgmentGoodSeconds * timeScale;
    public static float GetJudgmentPerfect(float timeScale) => JudgmentPerfectSeconds * timeScale;
    public static float GetVisualMarkerSnap(float timeScale) => VisualMarkerSnapSeconds * timeScale;
    public static float GetMinVisualTapGap(float timeScale) => MinVisualTapGapReference * timeScale;

    /// <summary>마디 내 felt 시각 vs 패턴 슬롯 — 탭 즉시 피드백.</summary>
    public static TapTimingQuality EvaluateTapInMeasure(
        float feltSecondsInMeasure,
        RhythmPattern pattern,
        float measureDuration,
        float timeScale,
        int slotIndex)
    {
        if (pattern.TapCount <= 0 || measureDuration <= 0f)
            return TapTimingQuality.Miss;

        slotIndex = UnityEngine.Mathf.Clamp(slotIndex, 0, pattern.TapCount - 1);
        float expected = pattern.GetExpectedHitTimes(measureDuration)[slotIndex];
        return EvaluateDelta(UnityEngine.Mathf.Abs(feltSecondsInMeasure - expected), timeScale);
    }

    /// <summary>가장 가까운 가이드 슬롯 — 시퀀스 상태와 무관한 즉시 피드백용.</summary>
    public static TapTimingQuality EvaluateTapNearestGuide(
        float feltSecondsInMeasure,
        RhythmPattern pattern,
        float measureDuration,
        float timeScale)
    {
        return TryEvaluateTapNearestGuide(
            feltSecondsInMeasure, pattern, measureDuration, timeScale, out var eval)
            ? eval.Quality
            : TapTimingQuality.Miss;
    }

    /// <summary>가장 가까운 가이드 + 판정 + 스냅용 메타. 게임플레이 타이밍은 변경하지 않음.</summary>
    public static bool TryEvaluateTapNearestGuide(
        float feltSecondsInMeasure,
        RhythmPattern pattern,
        float measureDuration,
        float timeScale,
        out TapNearestGuideEvaluation evaluation)
    {
        evaluation = TapNearestGuideEvaluation.Invalid;

        if (pattern.TapCount <= 0 || measureDuration <= 0f)
            return false;

        var expected = pattern.GetExpectedHitTimes(measureDuration);
        int bestIndex = 0;
        float bestDelta = float.MaxValue;
        for (int i = 0; i < expected.Length; i++)
        {
            float delta = UnityEngine.Mathf.Abs(feltSecondsInMeasure - expected[i]);
            if (delta < bestDelta)
            {
                bestDelta = delta;
                bestIndex = i;
            }
        }

        evaluation = new TapNearestGuideEvaluation(
            EvaluateDelta(bestDelta, timeScale),
            bestIndex,
            pattern.HitFractions[bestIndex],
            expected[bestIndex],
            bestDelta);
        return true;
    }

    /// <summary>타임라인 마커 표시용 felt — Perfect + visual snap 창 이내면 가이드 위치.</summary>
    public static float GetMarkerDisplayFelt(
        float feltSecondsInMeasure,
        in TapNearestGuideEvaluation evaluation,
        float timeScale)
    {
        if (!evaluation.IsValid || !ShouldSnapMarkerToGuide(in evaluation, timeScale))
            return feltSecondsInMeasure;

        return evaluation.GuideSecondsInMeasure;
    }

    public static bool ShouldSnapMarkerToGuide(in TapNearestGuideEvaluation evaluation, float timeScale)
    {
        if (!evaluation.IsValid || evaluation.Quality != TapTimingQuality.Perfect)
            return false;

        return evaluation.AbsDeltaSeconds <= GetVisualMarkerSnap(timeScale);
    }

    public static TapTimingQuality EvaluateDelta(float absDelta, float timeScale)
    {
        if (absDelta <= GetJudgmentPerfect(timeScale))
            return TapTimingQuality.Perfect;
        if (absDelta <= GetJudgmentGood(timeScale))
            return TapTimingQuality.Good;
        return TapTimingQuality.Miss;
    }

    public static string FormatPatternHint(CommandType type, float measureDuration)
    {
        if (!TryGetByType(type, out var pattern))
            return "";

        var parts = new string[pattern.TapCount];
        for (int i = 0; i < pattern.TapCount; i++)
            parts[i] = $"{pattern.HitFractions[i] * measureDuration:0.##}";

        return $"{pattern.TapCount} taps: {string.Join(", ", parts)}s";
    }

    public static bool TryGetByType(CommandType type, out RhythmPattern pattern)
    {
        foreach (var p in All)
        {
            if (p.Type != type)
                continue;

            pattern = p;
            return true;
        }

        pattern = default;
        return false;
    }

    public static bool TryMatchSinglePattern(
        IReadOnlyList<float> taps,
        float measureDuration,
        float scale,
        RhythmPattern pattern,
        out JudgmentResult judgment)
    {
        judgment = JudgmentResult.Miss;
        if (taps == null || taps.Count != pattern.TapCount)
            return false;

        judgment = JudgeHitTimes(taps, pattern.GetExpectedHitTimes(measureDuration), scale);
        return judgment != JudgmentResult.Miss;
    }

    /// <summary>선택된 패턴 기준 — 다음 슬롯을 아직 칠 수 있는지.</summary>
    public static bool CanExtendPattern(
        IReadOnlyList<float> taps,
        float nowRel,
        float measureDuration,
        float scale,
        RhythmPattern pattern)
    {
        if (taps == null || taps.Count == 0)
            return true;

        if (taps.Count >= pattern.TapCount)
            return false;

        float good = GetJudgmentGood(scale);
        var expected = pattern.GetExpectedHitTimes(measureDuration);
        if (!PrefixMatches(taps, expected, good))
            return false;

        return nowRel <= expected[taps.Count] + good;
    }

    public static bool TryGetByTapCount(int tapCount, out RhythmPattern pattern)
    {
        if (ByTapCount.TryGetValue(tapCount, out var list) && list.Count > 0)
        {
            pattern = list[0];
            return true;
        }

        pattern = default;
        return false;
    }

    public static JudgmentResult JudgeHitTimes(IReadOnlyList<float> actual, float[] expected, float timeScale)
    {
        if (actual.Count != expected.Length)
            return JudgmentResult.Miss;

        float goodWindow = GetJudgmentGood(timeScale);
        float perfectWindow = GetJudgmentPerfect(timeScale);

        bool allPerfect = true;
        for (int i = 0; i < expected.Length; i++)
        {
            float diff = UnityEngine.Mathf.Abs(actual[i] - expected[i]);
            if (diff > goodWindow)
                return JudgmentResult.Miss;

            if (diff > perfectWindow)
                allPerfect = false;
        }

        return allPerfect ? JudgmentResult.Perfect : JudgmentResult.Good;
    }

    /// <summary>현재 탭列이 어떤 패턴과도 GOOD 이상으로 완성됐는지.</summary>
    public static bool IsCompletePattern(IReadOnlyList<float> taps, float measureDuration, float scale)
    {
        return TryMatchCompletePattern(taps, measureDuration, scale, out _, out _);
    }

    public static bool TryMatchCompletePattern(
        IReadOnlyList<float> taps,
        float measureDuration,
        float scale,
        out RhythmPattern pattern,
        out JudgmentResult judgment)
    {
        pattern = default;
        judgment = JudgmentResult.Miss;

        if (taps == null || taps.Count == 0)
            return false;

        foreach (var p in All)
        {
            if (p.TapCount != taps.Count)
                continue;

            var j = JudgeHitTimes(taps, p.GetExpectedHitTimes(measureDuration), scale);
            if (j == JudgmentResult.Miss)
                continue;

            pattern = p;
            judgment = j;
            return true;
        }

        return false;
    }

    /// <summary>더 긴 패턴(Gold→RhythmShot 등)의 다음 슬롯을 아직 칠 수 있는지.</summary>
    public static bool CanExtendToLongerPattern(
        IReadOnlyList<float> taps,
        float nowRel,
        float measureDuration,
        float scale)
    {
        if (taps == null || taps.Count == 0)
            return true;

        float good = GetJudgmentGood(scale);
        int n = taps.Count;

        foreach (var pattern in All)
        {
            if (pattern.TapCount <= n)
                continue;

            var expected = pattern.GetExpectedHitTimes(measureDuration);
            if (!PrefixMatches(taps, expected, good))
                continue;

            float nextExpected = expected[n];
            if (nowRel <= nextExpected + good)
                return true;
        }

        return false;
    }

    /// <summary>경계 백필 1타만 기다리면 완성되는 미완 패턴(Overload 4타 등).</summary>
    public static bool NeedsBoundaryBackfillSlot(
        IReadOnlyList<float> taps,
        float measureDuration,
        float scale)
    {
        if (taps == null || taps.Count == 0 || IsCompletePattern(taps, measureDuration, scale))
            return false;

        float good = GetJudgmentGood(scale);
        int n = taps.Count;

        foreach (var pattern in All)
        {
            if (pattern.TapCount != n + 1)
                continue;

            var expected = pattern.GetExpectedHitTimes(measureDuration);
            if (!PrefixMatches(taps, expected, good))
                continue;

            if (IsLateBoundaryHit(expected[n], measureDuration, scale))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 경계 백필 — 이미 완성된 패턴·중간 슬롯(0.75)에는 새 사이클 1박을 넣지 않음.
    /// </summary>
    public static bool ShouldBackfillLateTap(
        IReadOnlyList<float> pendingTaps,
        float relEndedCycle,
        float newCycleRel,
        float measureDuration,
        float scale)
    {
        if (pendingTaps == null || pendingTaps.Count == 0)
            return false;

        if (IsCompletePattern(pendingTaps, measureDuration, scale))
            return false;

        float good = GetJudgmentGood(scale);
        int nextIndex = pendingTaps.Count;

        foreach (var pattern in All)
        {
            if (pattern.TapCount != nextIndex + 1)
                continue;

            var expected = pattern.GetExpectedHitTimes(measureDuration);
            if (!PrefixMatches(pendingTaps, expected, good))
                continue;

            float expectedNext = expected[nextIndex];
            if (!IsLateBoundaryHit(expectedNext, measureDuration, scale))
                continue;

            if (UnityEngine.Mathf.Abs(relEndedCycle - expectedNext) <= good)
                return true;

            if (newCycleRel >= -good && newCycleRel <= good)
                return true;
        }

        return false;
    }

    static bool PrefixMatches(IReadOnlyList<float> actual, float[] expected, float good)
    {
        for (int i = 0; i < actual.Count; i++)
        {
            if (UnityEngine.Mathf.Abs(actual[i] - expected[i]) > good)
                return false;
        }

        return true;
    }

    static bool IsLateBoundaryHit(float expectedTime, float measureDuration, float scale)
    {
        return expectedTime >= measureDuration - GetJudgmentGood(scale);
    }

    static Dictionary<int, List<RhythmPattern>> BuildLookup()
    {
        var map = new Dictionary<int, List<RhythmPattern>>();
        foreach (var pattern in All)
        {
            if (!map.TryGetValue(pattern.TapCount, out var list))
            {
                list = new List<RhythmPattern>();
                map[pattern.TapCount] = list;
            }

            list.Add(pattern);
        }

        return map;
    }
}
