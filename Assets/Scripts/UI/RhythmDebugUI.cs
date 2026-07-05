using TMPro;
using UnityEngine;

public class RhythmDebugUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI statusText;

    RhythmCommandDetector _detector;
    ResourceManager _resources;
    RunStats _stats;
    SkillCooldownController _cooldowns;

    string _lastLine = "-";

    void Awake()
    {
        BindReferences();
    }

    void OnEnable()
    {
        BindReferences();
        Subscribe();
    }

    void Start()
    {
        BindReferences();
        Subscribe();
        Refresh();
    }

    void OnDestroy()
    {
        Unsubscribe();
    }

    void BindReferences()
    {
        if (statusText == null)
            statusText = GetComponentInChildren<TextMeshProUGUI>();

        _detector ??= RhythmCommandDetector.Instance ?? FindAnyObjectByType<RhythmCommandDetector>();
        _resources ??= FindAnyObjectByType<ResourceManager>();
        _stats ??= FindAnyObjectByType<RunStats>();
        _cooldowns ??= FindAnyObjectByType<SkillCooldownController>();
    }

    void Subscribe()
    {
        if (_detector == null)
            return;

        _detector.OnCommandResolved -= OnCommandResolved;
        _detector.OnCommandResolved += OnCommandResolved;
    }

    void Unsubscribe()
    {
        if (_detector != null)
            _detector.OnCommandResolved -= OnCommandResolved;
    }

    void Update()
    {
        Refresh();
    }

    void OnCommandResolved(CommandType type, JudgmentResult judgment)
    {
        string label = type == CommandType.None ? "" : $" / {type}";
        _lastLine = $"{StripTags(FormatJudgment(judgment))}{label}";
        Refresh();
    }

    void Refresh()
    {
        if (statusText == null)
            return;

        float baseMeasure = BeatClock.Instance != null ? BeatClock.Instance.MeasureDurationSeconds : 1f;
        float effectiveMeasure = BeatClock.Instance != null ? BeatClock.Instance.EffectiveMeasureDuration : baseMeasure;
        float beatSec = BeatClock.Instance != null ? BeatClock.Instance.BeatInterval : baseMeasure * 0.5f;
        float bpm = BeatClock.Instance != null ? BeatClock.Instance.CurrentBpm : 120f;
        bool boosted = BeatClock.Instance != null && BeatClock.Instance.IsBoosted;
        float boostLeft = BeatClock.Instance != null ? BeatClock.Instance.BoostRemaining : 0f;
        float scale = BeatClock.Instance != null ? BeatClock.Instance.PatternTimeScale : baseMeasure / BeatClock.ReferenceMeasureDuration;
        int gold = _resources != null ? _resources.Gold : 0;
        int taps = _detector != null ? _detector.CurrentTapCount : 0;
        int beatInMeasure = BeatClock.Instance != null ? BeatClock.Instance.BeatIndexInMeasure + 1 : 0;
        float strikeCd = _cooldowns != null ? _cooldowns.GetRemaining(CommandType.OverloadStrike) : 0f;
        float boostCd = _cooldowns != null ? _cooldowns.GetRemaining(CommandType.BPMBoost) : 0f;
        int perfect = _stats != null ? _stats.PerfectCount : 0;
        int good = _stats != null ? _stats.GoodCount : 0;
        int miss = _stats != null ? _stats.MissCount : 0;

        float md = effectiveMeasure;
        float g = RhythmPatternLibrary.GetJudgmentGood(scale);

        string measureLine = boosted
            ? $"Measure {effectiveMeasure:0.##}s (base {baseMeasure:0.##}s x{BeatClock.BoostMeasureScale}) / Beat {beatSec:0.##}s ({bpm:0} BPM)"
            : $"Measure {baseMeasure:0.##}s / Beat {beatSec:0.##}s ({bpm:0} BPM)";
        string boostLine = boosted
            ? $"<color=#CE93D8>BPMBoost ACTIVE {boostLeft:0.0}s left</color>\n"
            : "";

        float inputOffset = RhythmInputSettings.Instance != null
            ? RhythmInputSettings.Instance.InputOffsetSeconds
            : RhythmInputSettings.DefaultInputOffsetSeconds;
        float inputAdj = RhythmInputSettings.Instance != null
            ? RhythmInputSettings.Instance.InputOffsetAdjustment
            : 0f;

        statusText.text =
            "<b>Beat Defender - Phase A</b>\n" +
            boostLine +
            $"{measureLine}  |  {beatInMeasure}/2  |  Gold: {gold}G  |  Taps {taps}\n" +
            $"Strike CD: {strikeCd:0.0}s  |  Boost CD: {boostCd:0.0}s  |  Window +/-{g:0.##}s  |  Input -{inputOffset * 1000f:0}ms (adj {inputAdj * 1000f:+0;-0}ms)\n" +
            $"Judge: P{perfect} G{good} M{miss}\n" +
            $"<b>Last: {_lastLine}</b>\n" +
            $"<size=75%><color=#AAAAAA>Judgment every {effectiveMeasure:0.##}s (2 beats). No judgment at beat 2 only.</color></size>\n\n" +
            "<size=80%>" +
            $"Gold - <b>2 taps</b>: 0s, {md * 0.5f:0.##}s (beat 1 and 2)\n" +
            $"RhythmShot - <b>3 taps</b>: 0, {md * 0.5f:0.##}, {md * 0.75f:0.##}s\n" +
            $"Overload - <b>5 taps</b>: 0, {md * 0.25f:0.##}, {md * 0.5f:0.##}, {md * 0.75f:0.##}, {md * 0.875f:0.##}s\n" +
            "BPMBoost - <b>1 tap</b>: 0s only (no extra input in cycle)" +
            "</size>";
    }

    static string FormatJudgment(JudgmentResult result)
    {
        return result switch
        {
            JudgmentResult.Perfect => "<color=#FFD54F>PERFECT</color>",
            JudgmentResult.Good => "<color=#A5D6A7>GOOD</color>",
            JudgmentResult.Cooldown => "<color=#EF5350>COOLDOWN</color>",
            JudgmentResult.NoTower => "<color=#EF5350>NO TOWER</color>",
            _ => "<color=#EF5350>MISS</color>"
        };
    }

    static string StripTags(string rich)
    {
        return rich.Replace("<color=#FFD54F>", "").Replace("<color=#A5D6A7>", "")
            .Replace("<color=#EF5350>", "").Replace("</color>", "");
    }
}
