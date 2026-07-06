using System;
using UnityEngine;

/// <summary>
/// 매치 타이머 — 승리 우선 · 패배는 Core HP = 0.
/// GameScene의 GameManager → Match Duration Seconds에서 길이 조정.
/// </summary>
[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    public const float DefaultMatchDurationSeconds = 120f;
    public const float SpawnGraceSeconds = 3f;

    public static GameManager Instance { get; private set; }

    [Tooltip("매치 길이(초). 클리어 테스트 시 10 등으로 줄이세요. 빌드 전 120 권장.")]
    [SerializeField] float matchDurationSeconds = DefaultMatchDurationSeconds;

    public static float MatchDurationSeconds =>
        Instance != null ? Instance.matchDurationSeconds : DefaultMatchDurationSeconds;

    public float ElapsedSeconds { get; private set; }
    public float RemainingSeconds => Mathf.Max(0f, MatchDurationSeconds - ElapsedSeconds);
    public bool IsRunning { get; private set; }
    public bool IsVictory { get; private set; }
    public bool IsDefeat { get; private set; }

    public event Action<float> OnTimerChanged;
    public event Action OnVictory;
    public event Action OnDefeat;

    BaseHealth _core;
    bool _ended;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        _core = BaseHealth.Instance ?? FindAnyObjectByType<BaseHealth>();
        if (_core != null)
            _core.OnDestroyed += HandleCoreDestroyed;

        ElapsedSeconds = 0f;
        OnTimerChanged?.Invoke(RemainingSeconds);

        if (FindAnyObjectByType<GameStartCountdownUI>() == null)
            BeginMatch();
    }

    public void BeginMatch()
    {
        if (_ended || IsRunning)
            return;

        IsRunning = true;
        ElapsedSeconds = 0f;
        OnTimerChanged?.Invoke(RemainingSeconds);
    }

    void OnDestroy()
    {
        if (_core != null)
            _core.OnDestroyed -= HandleCoreDestroyed;

        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (!IsRunning || _ended)
            return;

        ElapsedSeconds += Time.deltaTime;
        OnTimerChanged?.Invoke(RemainingSeconds);

        if (ElapsedSeconds >= MatchDurationSeconds && _core != null && _core.IsAlive)
            EndVictory();
    }

    void HandleCoreDestroyed()
    {
        if (_ended)
            return;

        if (BeatClock.IsRhythmTestInvincible)
            return;

        EndDefeat();
    }

    void EndVictory()
    {
        if (_ended)
            return;

        _ended = true;
        IsRunning = false;
        IsVictory = true;
        OnVictory?.Invoke();
        ResultScreenUI.Resolve()?.DisplayVictory();
        Debug.Log($"[GameManager] Victory — {MatchDurationSeconds:0}s survive!");
    }

    void EndDefeat()
    {
        if (_ended)
            return;

        _ended = true;
        IsRunning = false;
        IsDefeat = true;
        OnDefeat?.Invoke();
        ResultScreenUI.Resolve()?.DisplayDefeat();
        Debug.Log("[GameManager] Defeat — Core destroyed.");
    }
}
