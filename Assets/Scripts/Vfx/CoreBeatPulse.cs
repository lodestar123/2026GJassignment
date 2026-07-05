using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Core 본체 + 링 — OnBeat 펄스 (BeatPulseRail 박자 역할 대체).
/// </summary>
public class CoreBeatPulse : MonoBehaviour
{
    const float DownbeatPulse = 1.32f;
    const float BeatPulse = 1.18f;
    const float RhythmPerfectPulse = 1.58f;
    const float FeverActivatePulse = 1.42f;
    const float FeverBeatPulse = 1.24f;
    const float FeverDownbeatPulse = 1.32f;
    const float PulseDecaySpeed = 6.5f;
    static readonly Color RingBaseColor = new(1f, 0.85f, 0.25f, 0.72f);
    static readonly Color FeverRingColor = new(1f, 0.55f, 0.12f, 0.52f);
    static readonly Vector3 RingRestScale = Vector3.one * 0.85f;

    Transform _coreTransform;
    SpriteRenderer _coreRenderer;
    SpriteRenderer _ringRenderer;
    Vector3 _baseScale;
    float _pulseScale = 1f;
    Coroutine _ringRoutine;
    bool _beatSubscribed;
    bool _feverSubscribed;
    bool _feverActive;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureOnGameScene()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryAttach(SceneManager.GetActiveScene());
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => TryAttach(scene);

    static void TryAttach(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded || scene.name != SceneNames.Game)
            return;

        var core = BaseHealth.Instance ?? Object.FindAnyObjectByType<BaseHealth>();
        if (core == null || core.GetComponent<CoreBeatPulse>() != null)
            return;

        core.gameObject.AddComponent<CoreBeatPulse>();
    }

    void Awake()
    {
        _coreTransform = transform;
        _coreRenderer = GetComponent<SpriteRenderer>();
        _baseScale = _coreTransform.localScale;
        EnsureRing();
    }

    void OnEnable()
    {
        TrySubscribeBeat();
        TrySubscribeFever();
    }

    void Start()
    {
        TrySubscribeBeat();
        TrySubscribeFever();
    }

    void OnDisable()
    {
        if (BeatClock.Instance != null)
            BeatClock.Instance.OnBeat -= OnBeat;
        _beatSubscribed = false;
        UnsubscribeFever();

        if (_ringRoutine != null)
        {
            StopCoroutine(_ringRoutine);
            _ringRoutine = null;
        }

        RestoreRingRest();
    }

    void TrySubscribeBeat()
    {
        if (_beatSubscribed || BeatClock.Instance == null)
            return;

        BeatClock.Instance.OnBeat += OnBeat;
        _beatSubscribed = true;
    }

    void Update()
    {
        if (!_beatSubscribed)
            TrySubscribeBeat();

        if (!_feverSubscribed)
            TrySubscribeFever();

        _pulseScale = Mathf.Lerp(_pulseScale, 1f, Time.deltaTime * PulseDecaySpeed);
        _coreTransform.localScale = Vector3.Scale(
            _baseScale,
            new Vector3(_pulseScale * 1.1f, _pulseScale * 1.1f, _pulseScale));
    }

    void EnsureRing()
    {
        if (_ringRenderer != null)
            return;

        var ringGo = new GameObject("BeatRing");
        ringGo.transform.SetParent(_coreTransform, false);
        ringGo.transform.localPosition = Vector3.zero;
        _ringRenderer = ringGo.AddComponent<SpriteRenderer>();
        _ringRenderer.sprite = GreyboxSprites.Ring;
        _ringRenderer.sortingOrder = _coreRenderer != null ? _coreRenderer.sortingOrder - 1 : 1;
        RestoreRingRest();
    }

    void TrySubscribeFever()
    {
        if (_feverSubscribed)
            return;

        var fever = FeverTimeController.Instance ?? Object.FindAnyObjectByType<FeverTimeController>();
        if (fever == null)
            return;

        fever.OnFeverActivated -= OnFeverActivated;
        fever.OnFeverActivated += OnFeverActivated;
        fever.OnFeverEnded -= OnFeverEnded;
        fever.OnFeverEnded += OnFeverEnded;
        _feverActive = fever.IsFeverActive;
        _feverSubscribed = true;
    }

    void UnsubscribeFever()
    {
        var fever = FeverTimeController.Instance ?? Object.FindAnyObjectByType<FeverTimeController>();
        if (fever != null)
        {
            fever.OnFeverActivated -= OnFeverActivated;
            fever.OnFeverEnded -= OnFeverEnded;
        }

        _feverSubscribed = false;
        _feverActive = false;
    }

    void OnFeverActivated() => _feverActive = true;

    void OnFeverEnded()
    {
        _feverActive = false;
        RestoreRingRest();
    }

    /// <summary>피버 발동 — Core 링·본체 폭발 펄스.</summary>
    public void PulseFeverActivate()
    {
        _feverActive = true;
        _pulseScale = FeverActivatePulse;

        if (_ringRoutine != null)
        {
            StopCoroutine(_ringRoutine);
            _ringRoutine = null;
        }

        RestoreRingRest();
        _ringRoutine = StartCoroutine(RhythmPerfectRingRoutine(FeverRingColor, 2.0f, 0.42f));
        StartCoroutine(CoreTintRoutine(FeverRingColor));
    }

    /// <summary>Perfect 마디 성공 — Core 본체·링 강한 펄스.</summary>
    public void PulseRhythmPerfect(CommandType commandType)
    {
        _pulseScale = RhythmPerfectPulse;

        if (_ringRoutine != null)
        {
            StopCoroutine(_ringRoutine);
            _ringRoutine = null;
        }

        RestoreRingRest();
        var ringColor = GetRhythmRingColor(commandType);
        _ringRoutine = StartCoroutine(RhythmPerfectRingRoutine(ringColor));
        StartCoroutine(CoreTintRoutine(ringColor));
    }

    static Color GetRhythmRingColor(CommandType type) => type switch
    {
        CommandType.GoldPulse => new Color(1f, 0.88f, 0.28f, 0.92f),
        CommandType.RhythmShot => new Color(0.95f, 0.95f, 1f, 0.88f),
        CommandType.OverloadStrike => new Color(1f, 0.38f, 0.32f, 0.9f),
        CommandType.ChainZap => new Color(1f, 0.78f, 0.22f, 0.9f),
        CommandType.TempoUp => new Color(0.4f, 0.88f, 1f, 0.88f),
        CommandType.TempoDown => new Color(0.65f, 0.58f, 1f, 0.88f),
        _ => new Color(1f, 0.85f, 0.25f, 0.9f)
    };

    IEnumerator CoreTintRoutine(Color ringColor)
    {
        if (_coreRenderer == null)
            yield break;

        Color original = _coreRenderer.color;
        Color peak = Color.Lerp(original, ringColor, 0.55f);
        float duration = 0.28f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float wave = Mathf.Sin(t * Mathf.PI);
            _coreRenderer.color = Color.Lerp(original, peak, wave);
            yield return null;
        }

        _coreRenderer.color = original;
    }

    IEnumerator RhythmPerfectRingRoutine(Color ringColor, float maxScale = 2.65f, float duration = 0.52f)
    {
        if (_ringRenderer == null)
            yield break;

        float elapsed = 0f;

        try
        {
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float scaleT = 1f - (1f - t) * (1f - t);
                float scale = Mathf.Lerp(0.85f, maxScale, scaleT);
                _ringRenderer.transform.localScale = RingRestScale * (scale / 0.85f);
                float alpha = ringColor.a * (1f - t * t * 0.85f);
                _ringRenderer.color = new Color(ringColor.r, ringColor.g, ringColor.b, alpha);
                yield return null;
            }
        }
        finally
        {
            RestoreRingRest();
            _ringRoutine = null;
        }
    }

    void OnBeat()
    {
        if (BeatClock.Instance == null)
            return;

        bool downbeat = BeatClock.Instance.IsDownbeat;
        if (_feverActive)
        {
            _pulseScale = downbeat ? FeverDownbeatPulse : FeverBeatPulse;
            if (_ringRoutine != null)
            {
                StopCoroutine(_ringRoutine);
                _ringRoutine = null;
            }

            _ringRoutine = StartCoroutine(FeverBeatRingRoutine(downbeat));
            return;
        }

        _pulseScale = downbeat ? DownbeatPulse : BeatPulse;

        if (_ringRoutine != null)
        {
            StopCoroutine(_ringRoutine);
            _ringRoutine = null;
        }

        RestoreRingRest();
        _ringRoutine = StartCoroutine(RingPulseRoutine(downbeat));
    }

    IEnumerator FeverBeatRingRoutine(bool downbeat)
    {
        if (_ringRenderer == null)
            yield break;

        float duration = downbeat ? 0.34f : 0.26f;
        float maxScale = downbeat ? 1.95f : 1.65f;
        float elapsed = 0f;

        try
        {
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float scaleT = 1f - (1f - t) * (1f - t);
                float scale = Mathf.Lerp(0.85f, maxScale, scaleT);
                _ringRenderer.transform.localScale = RingRestScale * (scale / 0.85f);
                float alpha = FeverRingColor.a * (1f - t * t * 0.7f);
                _ringRenderer.color = new Color(FeverRingColor.r, FeverRingColor.g, FeverRingColor.b, alpha);
                yield return null;
            }
        }
        finally
        {
            if (!_feverActive)
                RestoreRingRest();
            else
            {
                _ringRenderer.transform.localScale = RingRestScale;
                _ringRenderer.color = new Color(FeverRingColor.r, FeverRingColor.g, FeverRingColor.b, 0.22f);
            }

            _ringRoutine = null;
        }
    }

    void RestoreRingRest()
    {
        if (_ringRenderer == null)
            return;

        _ringRenderer.transform.localScale = RingRestScale;
        _ringRenderer.color = RingBaseColor;
    }

    IEnumerator RingPulseRoutine(bool downbeat)
    {
        if (_ringRenderer == null)
            yield break;

        float duration = downbeat ? 0.42f : 0.3f;
        float maxScale = downbeat ? 2.15f : 1.72f;
        float elapsed = 0f;

        try
        {
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float scaleT = 1f - (1f - t) * (1f - t);
                float scale = Mathf.Lerp(0.85f, maxScale, scaleT);
                _ringRenderer.transform.localScale = RingRestScale * (scale / 0.85f);
                float alpha = RingBaseColor.a * (1f - t * t);
                _ringRenderer.color = new Color(RingBaseColor.r, RingBaseColor.g, RingBaseColor.b, alpha);
                yield return null;
            }
        }
        finally
        {
            RestoreRingRest();
            _ringRoutine = null;
        }
    }
}
