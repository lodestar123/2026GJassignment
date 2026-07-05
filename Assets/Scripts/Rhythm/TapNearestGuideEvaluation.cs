/// <summary>
/// <see cref="RhythmPatternLibrary.TryEvaluateTapNearestGuide"/> 결과 — 탭 즉시 피드백·마커 스냅용.
/// </summary>
public readonly struct TapNearestGuideEvaluation
{
    public static TapNearestGuideEvaluation Invalid => default;

    public TapNearestGuideEvaluation(
        TapTimingQuality quality,
        int guideSlotIndex,
        float guideFraction,
        float guideSecondsInMeasure,
        float absDeltaSeconds)
    {
        Quality = quality;
        GuideSlotIndex = guideSlotIndex;
        GuideFraction = guideFraction;
        GuideSecondsInMeasure = guideSecondsInMeasure;
        AbsDeltaSeconds = absDeltaSeconds;
        IsValid = true;
    }

    public bool IsValid { get; }
    public TapTimingQuality Quality { get; }
    public int GuideSlotIndex { get; }
    /// <summary>패턴 HitFractions[slot] — 타임라인 anchor 0~1.</summary>
    public float GuideFraction { get; }
    public float GuideSecondsInMeasure { get; }
    public float AbsDeltaSeconds { get; }
}
