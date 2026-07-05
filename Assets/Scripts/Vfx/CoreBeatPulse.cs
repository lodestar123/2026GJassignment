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
    const float PulseDecaySpeed = 6.5f;
    static readonly Color RingBaseColor = new(1f, 0.85f, 0.25f, 0.72f);
    static readonly Vector3 RingRestScale = Vector3.one * 0.85f;

    Transform _coreTransform;
    SpriteRenderer _coreRenderer;
    SpriteRenderer _ringRenderer;
    Vector3 _baseScale;
    float _pulseScale = 1f;
    Coroutine _ringRoutine;
    bool _beatSubscribed;

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

    void OnEnable() => TrySubscribeBeat();

    void Start() => TrySubscribeBeat();

    void OnDisable()
    {
        if (BeatClock.Instance != null)
            BeatClock.Instance.OnBeat -= OnBeat;
        _beatSubscribed = false;

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

    void OnBeat()
    {
        if (BeatClock.Instance == null)
            return;

        _pulseScale = BeatClock.Instance.IsDownbeat ? DownbeatPulse : BeatPulse;

        if (_ringRoutine != null)
        {
            StopCoroutine(_ringRoutine);
            _ringRoutine = null;
        }

        RestoreRingRest();
        _ringRoutine = StartCoroutine(RingPulseRoutine(BeatClock.Instance.IsDownbeat));
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
