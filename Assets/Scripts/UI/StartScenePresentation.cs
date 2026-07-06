using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// StartScene 런타임 연출 — 씬에 bake된 오브젝트를 참조해 박자 애니메이션·등장 효과만 담당.
/// 박자 레일·탭 마커는 RhythmTimelineUI + RhythmCommandDetector가 담당.
/// </summary>
public class StartScenePresentation : MonoBehaviour
{
    static readonly Color Cyan = new(0.35f, 0.88f, 1f, 1f);

    const float BeatInterval = 0.5f;

    struct FloatNote
    {
        public RectTransform Rt;
        public Vector2 Start;
        public float Phase;
    }

    RectTransform _coreRing;
    RectTransform _coreBody;
    RectTransform _titleRt;
    Image _beatRailGlow;
    CanvasGroup[] _menuGroups;
    readonly List<FloatNote> _floatNotes = new();

    float _fallbackBeatPhase;
    bool _ready;
    float _tapPulseUntil;

    void Awake()
    {
        if (GetComponent<StartSceneRhythmBootstrap>() == null)
            gameObject.AddComponent<StartSceneRhythmBootstrap>();

        ResolveRefs();
        StartCoroutine(PlayEntrance());
        _ready = true;
    }

    void OnEnable()
    {
        var detector = GetComponent<RhythmCommandDetector>();
        if (detector != null)
        {
            detector.OnTapTimingFeedback -= HandleTapTimingFeedback;
            detector.OnTapTimingFeedback += HandleTapTimingFeedback;
        }
    }

    void OnDisable()
    {
        var detector = GetComponent<RhythmCommandDetector>();
        if (detector != null)
            detector.OnTapTimingFeedback -= HandleTapTimingFeedback;
    }

    void HandleTapTimingFeedback(float _, TapTimingQuality quality)
    {
        if (quality == TapTimingQuality.Perfect)
            _tapPulseUntil = Time.unscaledTime + 0.12f;
    }

    void Update()
    {
        if (!_ready)
            return;

        float phase = BeatClock.Instance != null
            ? BeatClock.Instance.SecondsSinceMeasureStart()
            : (_fallbackBeatPhase += Time.unscaledDeltaTime);

        float beatWave = 0.5f + 0.5f * Mathf.Sin(phase * Mathf.PI * 2f / BeatInterval);
        float downbeat = 0.5f + 0.5f * Mathf.Cos(phase * Mathf.PI * 2f / BeatInterval);

        if (_coreRing != null)
        {
            _coreRing.localScale = Vector3.one * (1f + 0.08f * beatWave);
            var ringImg = _coreRing.GetComponent<Image>();
            if (ringImg != null)
                ringImg.color = new Color(1f, 0.85f, 0.25f, 0.35f + 0.25f * beatWave);
        }

        if (_coreBody != null)
        {
            float bodyScale = 1f + 0.06f * downbeat;
            if (Time.unscaledTime < _tapPulseUntil)
                bodyScale += 0.08f;
            _coreBody.localScale = Vector3.one * bodyScale;
        }

        if (_titleRt != null)
            _titleRt.localScale = Vector3.one * (1f + 0.025f * downbeat);

        if (_beatRailGlow != null)
            _beatRailGlow.color = new Color(Cyan.r, Cyan.g, Cyan.b, 0.08f + 0.12f * beatWave);

        for (int i = 0; i < _floatNotes.Count; i++)
        {
            var note = _floatNotes[i];
            if (note.Rt == null)
                continue;

            float time = Time.unscaledTime + note.Phase;
            note.Rt.anchoredPosition = note.Start + new Vector2(
                Mathf.Sin(time * 0.7f) * 8f,
                Mathf.Sin(time * 0.45f) * 12f);
        }
    }

    /// <summary>에디터 bake — Background 없을 때만 생성.</summary>
    public void EnsureSceneHierarchy(Sprite preferredSprite = null)
    {
        StartSceneVisualBuilder.Build(transform as RectTransform, preferredSprite);
    }

    void ResolveRefs()
    {
        var root = transform;
        _coreRing = root.Find("Decor/CoreEmblem/Ring") as RectTransform;
        _coreBody = root.Find("Decor/CoreEmblem/Core") as RectTransform;
        _titleRt = root.Find("Title") as RectTransform;
        _beatRailGlow = root.Find("Decor/BeatRail/TrackArea/TrackGlow")?.GetComponent<Image>();

        _floatNotes.Clear();
        var notesRoot = root.Find("Decor/FloatingNotes");
        if (notesRoot != null)
        {
            for (int i = 0; i < notesRoot.childCount; i++)
            {
                var rt = notesRoot.GetChild(i) as RectTransform;
                if (rt == null)
                    continue;

                _floatNotes.Add(new FloatNote
                {
                    Rt = rt,
                    Start = rt.anchoredPosition,
                    Phase = 0.4f + i * 0.17f
                });
            }
        }

        _menuGroups = new[]
        {
            _titleRt?.GetComponent<CanvasGroup>(),
            root.Find("Subtitle")?.GetComponent<CanvasGroup>(),
            root.Find("Tagline")?.GetComponent<CanvasGroup>(),
            root.Find("Btn_Start")?.GetComponent<CanvasGroup>(),
            root.Find("Btn_Tutorial")?.GetComponent<CanvasGroup>()
            ?? root.Find("Btn_Practice")?.GetComponent<CanvasGroup>(),
            root.Find("Btn_Settings")?.GetComponent<CanvasGroup>(),
            root.Find("Btn_Quit")?.GetComponent<CanvasGroup>(),
        };
    }

    IEnumerator PlayEntrance()
    {
        if (_menuGroups == null)
            yield break;

        foreach (var group in _menuGroups)
        {
            if (group != null)
                group.alpha = 0f;
        }

        yield return new WaitForSecondsRealtime(0.08f);

        float delay = 0f;
        foreach (var group in _menuGroups)
        {
            if (group == null)
                continue;

            StartCoroutine(FadeCanvasGroup(group, 0f, 1f, 0.45f, delay));
            delay += 0.1f;
        }
    }

    static IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration, float wait)
    {
        if (wait > 0f)
            yield return new WaitForSecondsRealtime(wait);

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }

        group.alpha = to;
    }
}
