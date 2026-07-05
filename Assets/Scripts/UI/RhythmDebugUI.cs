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
        bool fever = FeverTimeController.Instance != null && FeverTimeController.Instance.IsFeverActive;
        float feverLeft = FeverTimeController.Instance != null
            ? FeverTimeController.Instance.FeverRemaining
            : 0f;
        float tempoScale = BeatClock.Instance != null ? BeatClock.Instance.TempoScale : 1f;
        int fastStacks = TempoController.Instance != null ? TempoController.Instance.FastStacks : 0;
        int slowStacks = TempoController.Instance != null ? TempoController.Instance.SlowStacks : 0;
        float scale = BeatClock.Instance != null ? BeatClock.Instance.PatternTimeScale : baseMeasure / BeatClock.ReferenceMeasureDuration;
        int gold = _resources != null ? _resources.Gold : 0;
        int taps = _detector != null ? _detector.CurrentTapCount : 0;
        int beatInMeasure = BeatClock.Instance != null ? BeatClock.Instance.BeatIndexInMeasure + 1 : 0;
        float strikeCd = _cooldowns != null ? _cooldowns.GetRemaining(CommandType.OverloadStrike) : 0f;
        float chainCd = _cooldowns != null ? _cooldowns.GetRemaining(CommandType.ChainZap) : 0f;
        int perfect = _stats != null ? _stats.PerfectCount : 0;
        int good = _stats != null ? _stats.GoodCount : 0;
        int miss = _stats != null ? _stats.MissCount : 0;

        float md = effectiveMeasure;
        float g = RhythmPatternLibrary.GetJudgmentGood(scale);

        string measureLine = tempoScale != 1f
            ? $"Measure {effectiveMeasure:0.##}s (base {baseMeasure:0.##}s x{tempoScale:0.##}) / Beat {beatSec:0.##}s ({bpm:0} BPM)"
            : $"Measure {baseMeasure:0.##}s / Beat {beatSec:0.##}s ({bpm:0} BPM)";
        string boostLine = fever
            ? $"<color=#FFB74D>FEVER DMG x{FeverTimeController.DamageMultiplier:0.#} {feverLeft:0.0}s</color>\n"
            : "";
        string tempoLine = fastStacks > 0 || slowStacks > 0
            ? $"Tempo fast x{fastStacks} / slow x{slowStacks}\n"
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
            tempoLine +
            $"{measureLine}  |  {beatInMeasure}/2  |  Gold: {gold}G  |  Taps {taps}\n" +
            $"Strike CD: {strikeCd:0.0}s  |  Chain CD: {chainCd:0.0}s  |  Window +/-{g:0.##}s  |  Input -{inputOffset * 1000f:0}ms (adj {inputAdj * 1000f:+0;-0}ms)\n" +
            $"Judge: P{perfect} G{good} M{miss}\n" +
            $"<b>Last: {_lastLine}</b>\n" +
            $"<size=75%><color=#AAAAAA>Judgment every {effectiveMeasure:0.##}s (2 beats). No judgment at beat 2 only.</color></size>\n\n" +
            "<size=80%>" +
            $"Gold - {RhythmPatternLibrary.FormatPatternHint(CommandType.GoldPulse, md)}\n" +
            $"RhythmShot - {RhythmPatternLibrary.FormatPatternHint(CommandType.RhythmShot, md)}\n" +
            $"Overload - {RhythmPatternLibrary.FormatPatternHint(CommandType.OverloadStrike, md)}\n" +
            $"ChainZap - {RhythmPatternLibrary.FormatPatternHint(CommandType.ChainZap, md)}\n" +
            $"Fast - {RhythmPatternLibrary.FormatPatternHint(CommandType.TempoUp, md)}\n" +
            $"Slow - {RhythmPatternLibrary.FormatPatternHint(CommandType.TempoDown, md)}" +
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
