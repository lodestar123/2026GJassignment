using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 리듬 타임라인 — bar = felt 마디(0~duration).
/// 사이클은 MeasureStart + offset 에서 시작(지연). 가이드·playhead·탭 마커 모두 felt 축.
/// OnBeat(Core 등)는 BeatClock baseline felt 별도.
/// </summary>
[DefaultExecutionOrder(200)]
public class RhythmTimelineUI : MonoBehaviour
{
    sealed class TapMarkerVisual
    {
        public RectTransform Rect;
        public Image Image;
        public Color BaseColor;
        public Vector2 BaseSize;
        public float SpawnTime;
        public TapTimingQuality Quality;
    }

    static Sprite _runtimeMarkerSprite;

    [SerializeField] RectTransform playhead;
    [SerializeField] RectTransform markersRoot;
    [SerializeField] RectTransform guidesRoot;
    [SerializeField] Vector2 playheadSize = new(5f, 34f);
    [SerializeField] Vector2 markerSize = new(5f, 30f);
    [SerializeField] Vector2 guideSize = new(4f, 32f);
    [SerializeField, Range(0.15f, 0.95f)] float guideAlpha = 0.78f;

    [Header("Mark range (--|---|--)")]
    [SerializeField, Range(0.02f, 0.25f)]
    float markerSideExtend = 0.1f;

    [SerializeField, Range(0.05f, 0.5f)]
    float markerEarlyLateReference = 0.22f;

    [Header("Mark fade")]
    [SerializeField, Min(0f)]
    float markerHoldSeconds = 0.65f;

    [SerializeField, Min(0.05f)]
    float markerFadeSeconds = 0.55f;

    [Header("Tap feedback")]
    [SerializeField, Min(0.04f)]
    float markerPulseSeconds = 0.14f;

    [SerializeField, Range(1f, 2f)]
    float markerPerfectPulseScale = 1.55f;

    [SerializeField, Range(1f, 1.6f)]
    float markerGoodPulseScale = 1.28f;

    [SerializeField, Range(1f, 1.3f)]
    float markerMissPulseScale = 1.05f;

    [SerializeField]
    bool playTapSound = true;

    [SerializeField]
    [Tooltip("켜면 Awake에 스크립트 기본 크기·배치를 적용합니다. StartScene 등 씬 bake 레이아웃은 끄세요.")]
    bool applyScriptLayoutOnAwake = true;

    readonly List<TapMarkerVisual> _activeMarkers = new();
    readonly List<Image> _guideLines = new();
    int _markerSequence;
    bool _subscribed;
    bool _selectorSubscribed;
    CommandType _lastGuideType = CommandType.None;
    Image _playheadImage;
    Color _playheadBaseColor = new(0.35f, 0.88f, 1f, 1f);
    float _playheadFlashUntil;

    void Awake()
    {
        EnforceSafeFadeValues();
        if (applyScriptLayoutOnAwake)
            ApplyTimelineLayout();
        ResolveReferences();
    }

    void OnValidate() => EnforceSafeFadeValues();

    void EnforceSafeFadeValues()
    {
        markerHoldSeconds = Mathf.Max(0f, markerHoldSeconds);
        if (markerFadeSeconds < 0.1f)
            markerFadeSeconds = 0.55f;
        if (markerHoldSeconds <= 0f && markerFadeSeconds <= 0f)
            markerHoldSeconds = 0.65f;
    }

    void OnEnable()
    {
        TrySubscribe();
        TrySubscribeSelector();
        RefreshPatternGuides();
    }

    void Start()
    {
        TrySubscribe();
        TrySubscribeSelector();
        RefreshPatternGuides();
    }

    void OnDisable()
    {
        TryUnsubscribe();
        TryUnsubscribeSelector();
        ClearAllMarkersImmediate();
        ClearGuides();
    }

    void OnDestroy()
    {
        TryUnsubscribe();
        TryUnsubscribeSelector();
    }

    void Update()
    {
        if (!_subscribed)
            TrySubscribe();

        if (!_selectorSubscribed)
            TrySubscribeSelector();

        UpdatePlayhead();
        UpdateMarkerFade();
        UpdatePlayheadFlash();
    }

    void ResolveReferences()
    {
        if (playhead == null)
            playhead = transform.Find("TrackArea/Playhead") as RectTransform;

        if (_playheadImage == null && playhead != null)
            _playheadImage = playhead.GetComponent<Image>();

        if (markersRoot == null)
            markersRoot = transform.Find("TrackArea/Markers") as RectTransform;

        if (guidesRoot == null)
            guidesRoot = transform.Find("TrackArea/Guides") as RectTransform;

        EnsureDrawOrder();
    }

    void ApplyTimelineLayout()
    {
        playheadSize = new Vector2(5f, 34f);
        markerSize = new Vector2(5f, 30f);
        guideSize = new Vector2(4f, 32f);
        guideAlpha = 0.78f;

        var root = GetComponent<RectTransform>();
        if (root != null)
        {
            root.sizeDelta = new Vector2(Mathf.Max(root.sizeDelta.x, 560f), 44f);
            root.anchoredPosition = new Vector2(root.anchoredPosition.x, 16f);
        }

        var track = transform.Find("TrackArea") as RectTransform;
        if (track == null)
            return;

        StretchRect(track, 10f, 8f, -10f, -8f);
        ResizeBar(track, "CapLeft", new Vector2(4f, 36f));
        ResizeBar(track, "CapRight", new Vector2(4f, 36f));
        ResizeBar(track, "TrackLine", new Vector2(0f, 3f));
        ResizeBar(track, "BeatMid", new Vector2(3f, 22f));

        var playheadRt = track.Find("Playhead") as RectTransform;
        if (playheadRt != null)
        {
            playheadRt.sizeDelta = playheadSize;
            _playheadImage = playheadRt.GetComponent<Image>();
            if (_playheadImage != null)
            {
                _playheadBaseColor = new Color(0.35f, 0.88f, 1f, 1f);
                _playheadImage.color = _playheadBaseColor;
            }
        }

        _lastGuideType = CommandType.None;
    }

    static void StretchRect(RectTransform rt, float left, float bottom, float right, float top)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(right, top);
    }

    static void ResizeBar(Transform parent, string name, Vector2 sizeDelta)
    {
        var rt = parent.Find(name) as RectTransform;
        if (rt == null)
            return;

        rt.sizeDelta = sizeDelta;
    }

    void EnsureDrawOrder()
    {
        if (guidesRoot == null || playhead == null || markersRoot == null)
            return;

        guidesRoot.SetSiblingIndex(playhead.GetSiblingIndex());
        playhead.SetSiblingIndex(guidesRoot.GetSiblingIndex() + 1);
        markersRoot.SetSiblingIndex(playhead.GetSiblingIndex() + 1);
    }

    void TrySubscribe()
    {
        var detector = RhythmCommandDetector.Instance ?? FindAnyObjectByType<RhythmCommandDetector>();
        if (detector == null)
            return;

        detector.OnTapTimingFeedback -= OnTapTimingFeedback;
        detector.OnTapTimingFeedback += OnTapTimingFeedback;
        _subscribed = true;
    }

    void TryUnsubscribe()
    {
        var detector = RhythmCommandDetector.Instance ?? FindAnyObjectByType<RhythmCommandDetector>();
        if (detector != null)
            detector.OnTapTimingFeedback -= OnTapTimingFeedback;

        _subscribed = false;
    }

    void TrySubscribeSelector()
    {
        var selector = RhythmPatternSelector.Instance ?? FindAnyObjectByType<RhythmPatternSelector>();
        if (selector == null)
            return;

        selector.OnSelectionChanged -= OnPatternSelectionChanged;
        selector.OnSelectionChanged += OnPatternSelectionChanged;
        _selectorSubscribed = true;
        RefreshPatternGuides();
    }

    void TryUnsubscribeSelector()
    {
        var selector = RhythmPatternSelector.Instance ?? FindAnyObjectByType<RhythmPatternSelector>();
        if (selector != null)
            selector.OnSelectionChanged -= OnPatternSelectionChanged;

        _selectorSubscribed = false;
    }

    void OnPatternSelectionChanged(CommandType type) => RefreshPatternGuides();

    void RefreshPatternGuides(bool force = false)
    {
        ResolveReferences();
        EnsureGuidesRoot();

        var type = RhythmPatternSelector.Instance != null
            ? RhythmPatternSelector.Instance.Selected
            : CommandType.GoldPulse;

        if (type == CommandType.None)
            type = CommandType.GoldPulse;

        if (!force && _lastGuideType == type && _guideLines.Count > 0)
            return;

        _lastGuideType = type;
        ClearGuides();

        if (guidesRoot == null || !RhythmPatternLibrary.TryGetByType(type, out var pattern))
            return;

        var color = GetGuideColor(type);
        color.a = guideAlpha;

        foreach (float fraction in pattern.HitFractions)
            CreateGuideLine(fraction, color, fraction <= 0.001f);

        EnsureDrawOrder();
    }

    void EnsureGuidesRoot()
    {
        if (guidesRoot != null)
            return;

        var track = transform.Find("TrackArea") as RectTransform;
        if (track == null)
            return;

        var go = new GameObject("Guides", typeof(RectTransform));
        go.transform.SetParent(track, false);
        guidesRoot = go.GetComponent<RectTransform>();
        guidesRoot.anchorMin = Vector2.zero;
        guidesRoot.anchorMax = Vector2.one;
        guidesRoot.offsetMin = Vector2.zero;
        guidesRoot.offsetMax = Vector2.zero;
    }

    void CreateGuideLine(float fractionInMeasure, Color color, bool emphasize)
    {
        var go = new GameObject($"Guide_{fractionInMeasure:0.###}", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(guidesRoot, false);

        var img = go.GetComponent<Image>();
        img.sprite = GetMarkerSprite();
        img.color = color;
        img.raycastTarget = false;

        var size = emphasize
            ? new Vector2(guideSize.x + 1f, guideSize.y + 2f)
            : guideSize;
        var rt = go.GetComponent<RectTransform>();
        SetAnchorPosition(rt, fractionInMeasure, size);
        _guideLines.Add(img);
    }

    void ClearGuides()
    {
        for (int i = _guideLines.Count - 1; i >= 0; i--)
        {
            if (_guideLines[i] != null)
            {
                if (Application.isPlaying)
                    Destroy(_guideLines[i].gameObject);
                else
                    DestroyImmediate(_guideLines[i].gameObject);
            }
        }

        _guideLines.Clear();
    }

    static Color GetGuideColor(CommandType type) => type switch
    {
        CommandType.GoldPulse => new Color(1f, 0.84f, 0.31f, 1f),
        CommandType.RhythmShot => new Color(0.92f, 0.92f, 0.92f, 1f),
        CommandType.OverloadStrike => new Color(0.94f, 0.33f, 0.31f, 1f),
        CommandType.ChainZap => new Color(0.81f, 0.58f, 0.85f, 1f),
        CommandType.TempoUp => new Color(0.35f, 0.85f, 1f, 1f),
        CommandType.TempoDown => new Color(0.62f, 0.55f, 0.95f, 1f),
        _ => new Color(0.75f, 0.75f, 0.75f, 1f)
    };

    void OnTapTimingFeedback(float feltSecondsInMeasure, TapTimingQuality quality)
    {
        if (BeatClock.Instance == null || markersRoot == null)
            return;

        float duration = BeatClock.Instance.EffectiveMeasureDuration;
        if (duration <= 0f)
            return;

        if (playTapSound && SimpleAudio.Instance != null)
            SimpleAudio.Instance.PlayTapFeedback(quality);

        if (quality == TapTimingQuality.Perfect)
            _playheadFlashUntil = Time.time + 0.1f;

        AddMarker(FeltRelToAnchor(feltSecondsInMeasure, duration), quality);
    }

    void UpdatePlayheadFlash()
    {
        if (_playheadImage == null)
            return;

        if (Time.time >= _playheadFlashUntil)
        {
            _playheadImage.color = _playheadBaseColor;
            return;
        }

        float t = 1f - (_playheadFlashUntil - Time.time) / 0.1f;
        _playheadImage.color = Color.Lerp(_playheadBaseColor, Color.white, t);
    }

    void UpdatePlayhead()
    {
        if (playhead == null || BeatClock.Instance == null)
            return;

        float duration = BeatClock.Instance.EffectiveMeasureDuration;
        if (duration <= 0f)
            return;

        float feltNow = GetFeltRelativeNow();
        SetAnchorPosition(playhead, FeltRelToAnchor(feltNow, duration), playheadSize);
    }

    void UpdateMarkerFade()
    {
        if (_activeMarkers.Count == 0)
            return;

        float now = Time.time;
        for (int i = _activeMarkers.Count - 1; i >= 0; i--)
        {
            var marker = _activeMarkers[i];
            if (marker.Rect == null)
            {
                _activeMarkers.RemoveAt(i);
                continue;
            }

            float age = now - marker.SpawnTime;

            if (age < markerPulseSeconds)
            {
                float pulseT = age / markerPulseSeconds;
                float peak = GetPulsePeakScale(marker.Quality);
                float scale = Mathf.Lerp(peak, 1f, pulseT * pulseT);
                marker.Rect.sizeDelta = marker.BaseSize * scale;
            }
            else if (marker.Rect.sizeDelta != marker.BaseSize)
            {
                marker.Rect.sizeDelta = marker.BaseSize;
            }

            if (age <= markerHoldSeconds)
                continue;

            float fadeT = markerFadeSeconds > 0.0001f
                ? (age - markerHoldSeconds) / markerFadeSeconds
                : 1f;

            if (fadeT >= 1f)
            {
                Destroy(marker.Rect.gameObject);
                _activeMarkers.RemoveAt(i);
                continue;
            }

            var c = marker.BaseColor;
            c.a = marker.BaseColor.a * (1f - fadeT);
            marker.Image.color = c;
        }
    }

    float GetFeltRelativeNow()
    {
        if (BeatClock.Instance == null)
            return 0f;

        return RhythmInputSettings.GetFeltElapsedInMeasure(
            Time.time,
            BeatClock.Instance.MeasureStartTime);
    }

    float FeltRelToAnchor(float feltRel, float duration)
    {
        float earlyLateRef = markerEarlyLateReference * (duration / BeatClock.ReferenceMeasureDuration);

        if (feltRel < 0f)
            return -markerSideExtend * Mathf.Clamp01(-feltRel / earlyLateRef);

        if (feltRel > duration)
            return 1f + markerSideExtend * Mathf.Clamp01((feltRel - duration) / earlyLateRef);

        return feltRel / duration;
    }

    float GetPulsePeakScale(TapTimingQuality quality) => quality switch
    {
        TapTimingQuality.Perfect => markerPerfectPulseScale,
        TapTimingQuality.Good => markerGoodPulseScale,
        _ => markerMissPulseScale
    };

    static Color GetMarkerColor(TapTimingQuality quality) => quality switch
    {
        TapTimingQuality.Perfect => new Color(1f, 0.92f, 0.38f, 1f),
        TapTimingQuality.Good => new Color(0.55f, 0.95f, 0.62f, 0.95f),
        _ => new Color(1f, 0.42f, 0.38f, 0.88f)
    };

    void AddMarker(float anchorX, TapTimingQuality quality)
    {
        if (markersRoot == null)
            return;

        var baseColor = GetMarkerColor(quality);
        var size = quality == TapTimingQuality.Perfect
            ? new Vector2(markerSize.x + 2f, markerSize.y + 6f)
            : markerSize;

        _markerSequence++;

        var go = new GameObject($"TapMark_{_markerSequence}", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(markersRoot, false);

        var img = go.GetComponent<Image>();
        img.sprite = GetMarkerSprite();
        img.color = baseColor;
        img.raycastTarget = false;

        var rt = go.GetComponent<RectTransform>();
        SetAnchorPosition(rt, anchorX, size);
        rt.SetAsLastSibling();

        _activeMarkers.Add(new TapMarkerVisual
        {
            Rect = rt,
            Image = img,
            BaseColor = baseColor,
            BaseSize = size,
            SpawnTime = Time.time,
            Quality = quality
        });
    }

    void ClearAllMarkersImmediate()
    {
        for (int i = _activeMarkers.Count - 1; i >= 0; i--)
        {
            if (_activeMarkers[i].Rect != null)
            {
                if (Application.isPlaying)
                    Destroy(_activeMarkers[i].Rect.gameObject);
                else
                    DestroyImmediate(_activeMarkers[i].Rect.gameObject);
            }
        }

        _activeMarkers.Clear();
    }

    static void SetAnchorPosition(RectTransform rt, float anchorX, Vector2 size)
    {
        rt.anchorMin = new Vector2(anchorX, 0.5f);
        rt.anchorMax = new Vector2(anchorX, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
    }

    static Sprite GetMarkerSprite()
    {
        if (_runtimeMarkerSprite != null)
            return _runtimeMarkerSprite;

        var tex = new Texture2D(2, 2);
        tex.SetPixel(0, 0, Color.white);
        tex.SetPixel(1, 0, Color.white);
        tex.SetPixel(0, 1, Color.white);
        tex.SetPixel(1, 1, Color.white);
        tex.Apply();

        _runtimeMarkerSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
        return _runtimeMarkerSprite;
    }
}
