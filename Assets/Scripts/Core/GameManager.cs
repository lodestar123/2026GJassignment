using System;
using UnityEngine;

/// <summary>
/// 2분(120s) 매치 — 승리 우선 · 패배는 Core HP = 0.
/// </summary>
[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public const float MatchDurationSeconds = 120f;
    public const float SpawnGraceSeconds = 3f;

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
        Debug.Log("[GameManager] Victory — 120s survive!");
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
