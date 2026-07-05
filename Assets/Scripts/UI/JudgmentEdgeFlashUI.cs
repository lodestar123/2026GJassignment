using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 판정·피버 — 화면 가장자리 짧은 flash (BeatPulseRail 대체).
/// </summary>
public class JudgmentEdgeFlashUI : MonoBehaviour, IRuntimeSceneUi
{
    public static JudgmentEdgeFlashUI Instance { get; private set; }

    [SerializeField] float judgmentFlashSeconds = 0.62f;
    [SerializeField] float goodFlashSeconds = 0.4f;
    [SerializeField] float feverFlashSeconds = 0.55f;
    [SerializeField] float edgeThickness = 18f;
    [SerializeField] float perfectEdgeThickness = 26f;
    [SerializeField] float feverEdgeThickness = 20f;
    [SerializeField] float fadeInPortion = 0.18f;

    Image _top;
    Image _bottom;
    Image _left;
    Image _right;

    float _flashTimer;
    float _flashDuration;
    Color _flashColor = Color.clear;
    float _activeEdgeThickness;
    bool _subscribed;

    static readonly Color PerfectColor = new(1f, 0.9f, 0.35f, 0.78f);
    static readonly Color GoodColor = new(0.65f, 0.93f, 0.65f, 0.48f);
    static readonly Color MissColor = new(0.94f, 0.33f, 0.31f, 0.52f);
    static readonly Color FeverColor = new(1f, 0.62f, 0.12f, 0.4f);
    static readonly Color FeverPulseColor = new(1f, 0.72f, 0.18f, 0.32f);

    public void EnsureSceneHierarchy()
    {
        ResolveRefs();
        if (_top == null)
            BuildEdges();
        ApplyEdgeColor(Color.clear);
        ApplyEdgeThickness(edgeThickness);
    }

    void ResolveRefs()
    {
        _top ??= transform.Find("EdgeTop")?.GetComponent<Image>();
        _bottom ??= transform.Find("EdgeBottom")?.GetComponent<Image>();
        _left ??= transform.Find("EdgeLeft")?.GetComponent<Image>();
        _right ??= transform.Find("EdgeRight")?.GetComponent<Image>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureSceneHierarchy();
    }

    void OnDestroy()
    {
        Unsubscribe();
        if (Instance == this)
            Instance = null;
    }

    void OnEnable() => Subscribe();
    void Start() => Subscribe();
    void OnDisable() => Unsubscribe();

    void Subscribe()
    {
        if (_subscribed)
            return;

        var detector = RhythmCommandDetector.Instance ?? FindAnyObjectByType<RhythmCommandDetector>();
        if (detector != null)
        {
            detector.OnCommandResolved -= OnCommandResolved;
            detector.OnCommandResolved += OnCommandResolved;
        }

        var fever = FeverTimeController.Instance ?? FindAnyObjectByType<FeverTimeController>();
        if (fever != null)
        {
            fever.OnFeverActivated -= OnFeverActivated;
            fever.OnFeverActivated += OnFeverActivated;
        }

        _subscribed = detector != null;
    }

    void Unsubscribe()
    {
        var detector = RhythmCommandDetector.Instance ?? FindAnyObjectByType<RhythmCommandDetector>();
        if (detector != null)
            detector.OnCommandResolved -= OnCommandResolved;

        var fever = FeverTimeController.Instance ?? FindAnyObjectByType<FeverTimeController>();
        if (fever != null)
            fever.OnFeverActivated -= OnFeverActivated;

        _subscribed = false;
    }

    void BuildEdges()
    {
        if (_top != null)
            return;

        if (transform is not RectTransform)
            gameObject.AddComponent<RectTransform>();

        var rt = transform as RectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        _top = CreateEdgeStrip("EdgeTop", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(0f, edgeThickness));
        _bottom = CreateEdgeStrip("EdgeBottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, edgeThickness));
        _left = CreateEdgeStrip("EdgeLeft", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(edgeThickness, 0f));
        _right = CreateEdgeStrip("EdgeRight", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(edgeThickness, 0f));
    }

    Image CreateEdgeStrip(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var stripRt = go.AddComponent<RectTransform>();
        stripRt.anchorMin = anchorMin;
        stripRt.anchorMax = anchorMax;
        stripRt.pivot = anchorMin;
        stripRt.anchoredPosition = anchoredPos;
        stripRt.sizeDelta = sizeDelta;

        var img = go.AddComponent<Image>();
        img.color = Color.clear;
        img.raycastTarget = false;
        return img;
    }

    void OnCommandResolved(CommandType type, JudgmentResult judgment)
    {
        switch (judgment)
        {
            case JudgmentResult.Perfect:
                TriggerFlash(PerfectColor, judgmentFlashSeconds, perfectEdgeThickness);
                break;
            case JudgmentResult.Good:
                TriggerFlash(GoodColor, goodFlashSeconds);
                break;
            case JudgmentResult.Miss:
            case JudgmentResult.Cooldown:
            case JudgmentResult.NoTower:
                TriggerFlash(MissColor, judgmentFlashSeconds);
                break;
        }
    }

    void OnFeverActivated()
    {
        TriggerFlash(FeverColor, feverFlashSeconds, feverEdgeThickness);
    }

    void TriggerFlash(Color color, float duration, float thicknessOverride = -1f)
    {
        _flashColor = color;
        _flashDuration = Mathf.Max(0.1f, duration);
        _flashTimer = _flashDuration;
        _activeEdgeThickness = thicknessOverride > 0f ? thicknessOverride : edgeThickness;
        ApplyEdgeThickness(_activeEdgeThickness);
    }

    void ApplyEdgeThickness(float thickness)
    {
        if (_top != null) _top.rectTransform.sizeDelta = new Vector2(0f, thickness);
        if (_bottom != null) _bottom.rectTransform.sizeDelta = new Vector2(0f, thickness);
        if (_left != null) _left.rectTransform.sizeDelta = new Vector2(thickness, 0f);
        if (_right != null) _right.rectTransform.sizeDelta = new Vector2(thickness, 0f);
    }

    static float SmoothStep(float t) => t * t * (3f - 2f * t);

    float EvaluateFlashAlpha()
    {
        float progress = 1f - (_flashTimer / _flashDuration);
        float inPortion = Mathf.Clamp01(fadeInPortion);

        if (progress <= inPortion)
            return _flashColor.a * SmoothStep(progress / Mathf.Max(inPortion, 0.001f));

        float outT = (progress - inPortion) / Mathf.Max(1f - inPortion, 0.001f);
        return _flashColor.a * (1f - SmoothStep(outT));
    }

    void Update()
    {
        if (_flashTimer > 0f)
        {
            _flashTimer -= Time.unscaledDeltaTime;
            var c = _flashColor;
            c.a = EvaluateFlashAlpha();
            ApplyEdgeColor(c);
            return;
        }

        if (FeverTimeController.Instance != null && FeverTimeController.Instance.IsFeverActive)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3.2f);
            var c = FeverPulseColor;
            c.a = Mathf.Lerp(0.14f, 0.3f, pulse);
            ApplyEdgeColor(c);
            ApplyEdgeThickness(Mathf.Lerp(feverEdgeThickness * 0.8f, feverEdgeThickness, pulse));
            return;
        }

        ApplyEdgeColor(Color.clear);
        ApplyEdgeThickness(edgeThickness);
    }

    void ApplyEdgeColor(Color color)
    {
        if (_top != null) _top.color = color;
        if (_bottom != null) _bottom.color = color;
        if (_left != null) _left.color = color;
        if (_right != null) _right.color = color;
    }
}
