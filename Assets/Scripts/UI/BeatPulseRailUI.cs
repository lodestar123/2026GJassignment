using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 하단 Beat Pulse Rail — OnBeat 펄스 + 커맨드 성공 색 플래시 + BPMBoost 주황 테두리.
/// </summary>
[DefaultExecutionOrder(150)]
public class BeatPulseRailUI : MonoBehaviour
{
    [SerializeField] Image railBackground;
    [SerializeField] RectTransform pulseAnchor;
    [SerializeField] Image pulseRing;
    [SerializeField] Image flashOverlay;
    [SerializeField] Image boostBorderOverlay;

    [SerializeField] float pulseScaleDownbeat = 1.35f;
    [SerializeField] float pulseScaleRegular = 1.05f;
    [SerializeField] float pulseDecaySpeed = 8f;
    [SerializeField] float commandFlashSeconds = 0.18f;

    float _pulseScale = 1f;
    float _flashTimer;
    Color _flashColor = Color.clear;
    bool _subscribed;

    static readonly Dictionary<CommandType, Color> CommandFlashColors = new()
    {
        { CommandType.GoldPulse, new Color(1f, 0.84f, 0.31f, 0.55f) },
        { CommandType.RhythmShot, new Color(1f, 1f, 1f, 0.45f) },
        { CommandType.OverloadStrike, new Color(0.94f, 0.33f, 0.31f, 0.55f) },
        { CommandType.BPMBoost, new Color(1f, 0.55f, 0.12f, 0.55f) },
    };

    void OnEnable() => TrySubscribe();
    void Start() => TrySubscribe();
    void OnDisable() => TryUnsubscribe();

    void Update()
    {
        if (!_subscribed)
            TrySubscribe();

        AnimatePulse();
        AnimateFlash();
        UpdateBoostOverlay();
    }

    void TrySubscribe()
    {
        if (_subscribed)
            return;

        if (BeatClock.Instance != null)
        {
            BeatClock.Instance.OnBeat -= OnBeat;
            BeatClock.Instance.OnBeat += OnBeat;
        }

        var detector = RhythmCommandDetector.Instance ?? FindAnyObjectByType<RhythmCommandDetector>();
        if (detector != null)
        {
            detector.OnCommandResolved -= OnCommandResolved;
            detector.OnCommandResolved += OnCommandResolved;
        }

        _subscribed = BeatClock.Instance != null && detector != null;
    }

    void TryUnsubscribe()
    {
        if (BeatClock.Instance != null)
            BeatClock.Instance.OnBeat -= OnBeat;

        var detector = RhythmCommandDetector.Instance ?? FindAnyObjectByType<RhythmCommandDetector>();
        if (detector != null)
            detector.OnCommandResolved -= OnCommandResolved;

        _subscribed = false;
    }

    void OnBeat()
    {
        if (BeatClock.Instance == null)
            return;

        _pulseScale = BeatClock.Instance.IsDownbeat ? pulseScaleDownbeat : pulseScaleRegular;
    }

    void OnCommandResolved(CommandType type, JudgmentResult judgment)
    {
        if (judgment != JudgmentResult.Perfect && judgment != JudgmentResult.Good)
            return;

        if (!CommandFlashColors.TryGetValue(type, out var color))
            return;

        _flashColor = color;
        _flashTimer = commandFlashSeconds;
    }

    void AnimatePulse()
    {
        if (pulseRing == null)
            return;

        _pulseScale = Mathf.Lerp(_pulseScale, 1f, Time.deltaTime * pulseDecaySpeed);
        pulseRing.rectTransform.localScale = Vector3.one * _pulseScale;
    }

    void AnimateFlash()
    {
        if (flashOverlay == null)
            return;

        if (_flashTimer > 0f)
        {
            _flashTimer -= Time.deltaTime;
            var c = _flashColor;
            c.a = _flashColor.a * Mathf.Clamp01(_flashTimer / commandFlashSeconds);
            flashOverlay.color = c;
            flashOverlay.gameObject.SetActive(true);
        }
        else if (flashOverlay.gameObject.activeSelf)
        {
            flashOverlay.gameObject.SetActive(false);
        }
    }

    void UpdateBoostOverlay()
    {
        if (boostBorderOverlay == null || BeatClock.Instance == null)
            return;

        bool boosted = BeatClock.Instance.IsBoosted;
        boostBorderOverlay.gameObject.SetActive(boosted);
        if (!boosted)
            return;

        float pulse = 0.35f + 0.15f * Mathf.Sin(Time.unscaledTime * 6f);
        var c = boostBorderOverlay.color;
        c.a = pulse;
        boostBorderOverlay.color = c;
    }
}
