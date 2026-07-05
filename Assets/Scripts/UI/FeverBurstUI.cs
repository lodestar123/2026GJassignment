using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// FEVER TIME!! — 발동 순간 풀스크린 임팩트 배너.
/// </summary>
public class FeverBurstUI : MonoBehaviour, IRuntimeSceneUi
{
    public static FeverBurstUI Instance { get; private set; }

    Image _overlay;
    TextMeshProUGUI _titleText;
    TextMeshProUGUI _subText;
    RectTransform _titleRt;
    RectTransform _raysRoot;

    Coroutine _burstRoutine;
    bool _subscribed;

    static readonly Color OverlayColor = new(1f, 0.45f, 0.08f, 0.28f);
    static readonly Color RayColor = new(1f, 0.72f, 0.18f, 0.18f);

    public void EnsureSceneHierarchy()
    {
        ResolveRefs();
        if (_overlay == null)
            BuildUi();
        HideImmediate();
    }

    void ResolveRefs()
    {
        _overlay ??= transform.Find("Overlay")?.GetComponent<Image>();
        _raysRoot ??= transform.Find("Rays") as RectTransform;
        _titleRt ??= transform.Find("Title") as RectTransform;
        _titleText ??= transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
        _subText ??= transform.Find("Sub")?.GetComponent<TextMeshProUGUI>();
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

        var fever = FeverTimeController.Instance ?? FindAnyObjectByType<FeverTimeController>();
        if (fever == null)
            return;

        fever.OnFeverActivated -= OnFeverActivated;
        fever.OnFeverActivated += OnFeverActivated;
        _subscribed = true;
    }

    void Unsubscribe()
    {
        var fever = FeverTimeController.Instance ?? FindAnyObjectByType<FeverTimeController>();
        if (fever != null)
            fever.OnFeverActivated -= OnFeverActivated;
        _subscribed = false;
    }

    void BuildUi()
    {
        if (_overlay != null)
            return;

        var root = transform as RectTransform;
        if (root == null)
            root = gameObject.AddComponent<RectTransform>();
        Stretch(root);

        var overlayGo = new GameObject("Overlay");
        overlayGo.transform.SetParent(transform, false);
        var overlayRt = overlayGo.AddComponent<RectTransform>();
        Stretch(overlayRt);
        _overlay = overlayGo.AddComponent<Image>();
        _overlay.color = Color.clear;
        _overlay.raycastTarget = false;

        var raysGo = new GameObject("Rays");
        raysGo.transform.SetParent(transform, false);
        _raysRoot = raysGo.AddComponent<RectTransform>();
        Stretch(_raysRoot);

        for (int i = 0; i < 12; i++)
        {
            float angle = i * 30f;
            var rayGo = new GameObject($"Ray_{i}");
            rayGo.transform.SetParent(_raysRoot, false);
            var rayRt = rayGo.AddComponent<RectTransform>();
            rayRt.anchorMin = new Vector2(0.5f, 0.5f);
            rayRt.anchorMax = new Vector2(0.5f, 0.5f);
            rayRt.pivot = new Vector2(0.5f, 0f);
            rayRt.sizeDelta = new Vector2(18f, 420f);
            rayRt.anchoredPosition = Vector2.zero;
            rayRt.localRotation = Quaternion.Euler(0f, 0f, angle);
            var rayImg = rayGo.AddComponent<Image>();
            rayImg.sprite = GreyboxSprites.Square;
            rayImg.color = Color.clear;
            rayImg.raycastTarget = false;
        }

        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(transform, false);
        _titleRt = titleGo.AddComponent<RectTransform>();
        _titleRt.anchorMin = new Vector2(0.5f, 0.56f);
        _titleRt.anchorMax = new Vector2(0.5f, 0.56f);
        _titleRt.sizeDelta = new Vector2(900f, 140f);
        _titleText = titleGo.AddComponent<TextMeshProUGUI>();
        _titleText.text = "FEVER TIME!!";
        _titleText.fontSize = 88f;
        _titleText.fontStyle = FontStyles.Bold;
        _titleText.alignment = TextAlignmentOptions.Center;
        _titleText.color = new Color(1f, 0.92f, 0.35f, 1f);
        _titleText.raycastTarget = false;
        BeatDefenderFonts.Apply(_titleText);

        var subGo = new GameObject("Sub");
        subGo.transform.SetParent(transform, false);
        var subRt = subGo.AddComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0.5f, 0.44f);
        subRt.anchorMax = new Vector2(0.5f, 0.44f);
        subRt.sizeDelta = new Vector2(700f, 56f);
        _subText = subGo.AddComponent<TextMeshProUGUI>();
        _subText.fontSize = 32f;
        _subText.fontStyle = FontStyles.Bold;
        _subText.alignment = TextAlignmentOptions.Center;
        _subText.color = new Color(1f, 0.72f, 0.22f, 0.95f);
        _subText.raycastTarget = false;
        BeatDefenderFonts.Apply(_subText);
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    void OnFeverActivated()
    {
        if (_burstRoutine != null)
            StopCoroutine(_burstRoutine);
        _burstRoutine = StartCoroutine(BurstRoutine());
    }

    IEnumerator BurstRoutine()
    {
        gameObject.SetActive(true);
        _subText.text = $"DMG x{FeverTimeController.DamageMultiplier:0.#}  |  {FeverTimeController.FeverDurationSeconds:0}s";

        const float duration = 1.05f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            float overlayAlpha = t < 0.18f
                ? Mathf.Lerp(0f, OverlayColor.a, t / 0.18f)
                : OverlayColor.a * (1f - SmoothStep((t - 0.18f) / 0.82f));
            _overlay.color = new Color(OverlayColor.r, OverlayColor.g, OverlayColor.b, overlayAlpha);

            float titleIn = Mathf.Clamp01(t / 0.18f);
            float titleOut = t > 0.5f ? (t - 0.5f) / 0.5f : 0f;
            float titleScale = titleIn < 1f
                ? Mathf.Lerp(1.55f, 1f, 1f - (1f - titleIn) * (1f - titleIn))
                : 1f;
            titleScale *= 1f - titleOut * 0.3f;
            _titleRt.localScale = Vector3.one * titleScale;

            var titleColor = _titleText.color;
            titleColor.a = 1f - titleOut;
            _titleText.color = titleColor;

            var subColor = _subText.color;
            subColor.a = Mathf.Clamp01(1f - titleOut * 1.2f);
            _subText.color = subColor;

            if (_raysRoot != null)
            {
                _raysRoot.localRotation = Quaternion.Euler(0f, 0f, elapsed * 28f);
                float rayAlpha = RayColor.a * (1f - t) * 0.85f;
                for (int i = 0; i < _raysRoot.childCount; i++)
                {
                    var img = _raysRoot.GetChild(i).GetComponent<Image>();
                    if (img != null)
                        img.color = new Color(RayColor.r, RayColor.g, RayColor.b, rayAlpha);
                }
            }

            yield return null;
        }

        HideImmediate();
        _burstRoutine = null;
    }

    void HideImmediate()
    {
        if (_overlay != null)
            _overlay.color = Color.clear;
        if (_titleText != null)
            _titleText.color = new Color(1f, 0.92f, 0.35f, 0f);
        if (_subText != null)
            _subText.color = new Color(1f, 0.72f, 0.22f, 0f);
        if (_titleRt != null)
            _titleRt.localScale = Vector3.one;
        HideRays();
    }

    void HideRays()
    {
        if (_raysRoot == null)
            return;

        for (int i = 0; i < _raysRoot.childCount; i++)
        {
            var img = _raysRoot.GetChild(i).GetComponent<Image>();
            if (img != null)
                img.color = Color.clear;
        }
    }

    static float SmoothStep(float t) => t * t * (3f - 2f * t);
}
