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
    public const float MinTapGapReference = 0.02f;
    public const float MinVisualTapGapReference = 0.008f;

    public static readonly IReadOnlyList<RhythmPattern> All = new[]
    {
        new RhythmPattern(CommandType.GoldPulse, new[] { 0f, 0.5f }),
        new RhythmPattern(CommandType.RhythmShot, new[] { 0f, 0.5f, 0.75f }),
        new RhythmPattern(CommandType.OverloadStrike, new[] { 0f, 0.25f, 0.5f, 0.75f, 0.875f }),
        new RhythmPattern(CommandType.BPMBoost, new[] { 0f })
    };

    public static readonly Dictionary<int, List<RhythmPattern>> ByTapCount = BuildLookup();

    public static float GetJudgmentGood(float timeScale) => JudgmentGoodSeconds * timeScale;
    public static float GetJudgmentPerfect(float timeScale) => JudgmentPerfectSeconds * timeScale;
    public static float GetMinVisualTapGap(float timeScale) => MinVisualTapGapReference * timeScale;

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
