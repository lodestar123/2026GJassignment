using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// offset 적용 felt 시간 — playhead·mark 동일 축, duration 대비 선형(가속 없음).
/// InputOffset 조절 시 마커가 playhead 박자와 맞춰짐.
/// </summary>
[DefaultExecutionOrder(200)]
public class RhythmTimelineUI : MonoBehaviour
{
    sealed class TapMarkerVisual
    {
        public RectTransform Rect;
        public Image Image;
        public Color BaseColor;
        public float SpawnTime;
    }

    static Sprite _runtimeMarkerSprite;

    [SerializeField] RectTransform playhead;
    [SerializeField] RectTransform markersRoot;
    [SerializeField] Vector2 playheadSize = new(3f, 20f);
    [SerializeField] Vector2 markerSize = new(2f, 14f);

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

    readonly List<TapMarkerVisual> _activeMarkers = new();
    int _markerSequence;
    bool _subscribed;

    void Awake()
    {
        EnforceSafeFadeValues();
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

    void OnEnable() => TrySubscribe();
    void Start() => TrySubscribe();

    void OnDisable()
    {
        TryUnsubscribe();
        ClearAllMarkersImmediate();
    }

    void OnDestroy() => TryUnsubscribe();

    void Update()
    {
        if (!_subscribed)
            TrySubscribe();

        UpdatePlayhead();
        UpdateMarkerFade();
    }

    void ResolveReferences()
    {
        if (playhead == null)
            playhead = transform.Find("TrackArea/Playhead") as RectTransform;

        if (markersRoot == null)
            markersRoot = transform.Find("TrackArea/Markers") as RectTransform;

        EnsureMarkersDrawOnTop();
    }

    void EnsureMarkersDrawOnTop()
    {
        if (markersRoot == null || playhead == null)
            return;

        markersRoot.SetSiblingIndex(playhead.GetSiblingIndex() + 1);
    }

    void TrySubscribe()
    {
        var detector = RhythmCommandDetector.Instance ?? FindAnyObjectByType<RhythmCommandDetector>();
        if (detector == null)
            return;

        detector.OnTapVisualized -= OnTapVisualized;
        detector.OnTapVisualized += OnTapVisualized;
        _subscribed = true;
    }

    void TryUnsubscribe()
    {
        var detector = RhythmCommandDetector.Instance ?? FindAnyObjectByType<RhythmCommandDetector>();
        if (detector != null)
            detector.OnTapVisualized -= OnTapVisualized;

        _subscribed = false;
    }

    void OnTapVisualized(float adjustedSecondsInMeasure)
    {
        if (BeatClock.Instance == null || markersRoot == null)
            return;

        float duration = BeatClock.Instance.EffectiveMeasureDuration;
        if (duration <= 0f)
            return;

        AddMarker(FeltRelToAnchor(adjustedSecondsInMeasure, duration));
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

    float GetInputOffset()
    {
        if (RhythmInputSettings.Instance != null)
            return RhythmInputSettings.Instance.InputOffsetSeconds;

        return RhythmInputSettings.DefaultInputOffsetSeconds;
    }

    float GetFeltRelativeNow()
    {
        float offset = GetInputOffset();
        return (Time.time - offset) - BeatClock.Instance.MeasureStartTime;
    }

    /// <summary>felt rel 선형 매핑 — inner |---| = 0..duration, 가속/압축 없음.</summary>
    float FeltRelToAnchor(float feltRel, float duration)
    {
        float earlyLateRef = markerEarlyLateReference * (duration / BeatClock.ReferenceMeasureDuration);

        if (feltRel < 0f)
            return -markerSideExtend * Mathf.Clamp01(-feltRel / earlyLateRef);

        if (feltRel > duration)
            return 1f + markerSideExtend * Mathf.Clamp01((feltRel - duration) / earlyLateRef);

        return feltRel / duration;
    }

    void AddMarker(float anchorX)
    {
        if (markersRoot == null)
            return;

        var baseColor = new Color(1f, 0.72f, 0.3f, 0.95f);
        _markerSequence++;

        var go = new GameObject($"TapMark_{_markerSequence}", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(markersRoot, false);

        var img = go.GetComponent<Image>();
        img.sprite = GetMarkerSprite();
        img.color = baseColor;
        img.raycastTarget = false;

        var rt = go.GetComponent<RectTransform>();
        SetAnchorPosition(rt, anchorX, markerSize);
        rt.SetAsLastSibling();

        _activeMarkers.Add(new TapMarkerVisual
        {
            Rect = rt,
            Image = img,
            BaseColor = baseColor,
            SpawnTime = Time.time
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
