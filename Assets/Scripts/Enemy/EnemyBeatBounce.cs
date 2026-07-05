using UnityEngine;

/// <summary>
/// 120BPM 박자 연출 — 스프라이트 스케일 펄스 (전투 수치 무관).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class EnemyBeatBounce : MonoBehaviour
{
    [SerializeField] float pulseScale = 1.12f;
    [SerializeField] float downbeatExtra = 0.06f;
    [SerializeField] float decaySpeed = 10f;

    SpriteRenderer _sprite;
    Vector3 _baseScale;
    float _targetScale = 1f;
    float _currentScale = 1f;
    bool _subscribed;

    void Awake()
    {
        _sprite = GetComponent<SpriteRenderer>();
        _baseScale = transform.localScale;
    }

    void OnEnable() => TrySubscribe();
    void Start() => TrySubscribe();
    void OnDisable() => TryUnsubscribe();

    void Update()
    {
        if (!_subscribed)
            TrySubscribe();

        _currentScale = Mathf.Lerp(_currentScale, _targetScale, Time.deltaTime * decaySpeed);
        _targetScale = Mathf.Lerp(_targetScale, 1f, Time.deltaTime * decaySpeed);
        transform.localScale = _baseScale * _currentScale;
    }

    void TrySubscribe()
    {
        if (_subscribed || BeatClock.Instance == null)
            return;

        BeatClock.Instance.OnBeat -= OnBeat;
        BeatClock.Instance.OnBeat += OnBeat;
        _subscribed = true;
    }

    void TryUnsubscribe()
    {
        if (BeatClock.Instance != null)
            BeatClock.Instance.OnBeat -= OnBeat;

        _subscribed = false;
    }

    void OnBeat()
    {
        float bump = pulseScale + (BeatClock.Instance != null && BeatClock.Instance.IsDownbeat ? downbeatExtra : 0f);
        _targetScale = bump;
    }
}
