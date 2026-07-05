using System;
using UnityEngine;

/// <summary>
/// Scroll TempoUp/TempoDown 스택 — 마디 길이 배율 (작을수록 빠른 BPM).
/// </summary>
public class TempoController : MonoBehaviour
{
    public static TempoController Instance { get; private set; }

    public const int MaxStacksPerDirection = 4;
    public const float StackDelta = 0.07f;
    public const float MinScale = 0.72f;
    public const float MaxScale = 1.36f;

    int _fastStacks;
    int _slowStacks;

    public int FastStacks => _fastStacks;
    public int SlowStacks => _slowStacks;
    public float CurrentScale { get; private set; } = 1f;

    public event Action OnTempoChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        Recalculate();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void AddFastStack()
    {
        _fastStacks = Mathf.Min(_fastStacks + 1, MaxStacksPerDirection);
        Recalculate();
    }

    public void AddSlowStack()
    {
        _slowStacks = Mathf.Min(_slowStacks + 1, MaxStacksPerDirection);
        Recalculate();
    }

    public void ResetTempo()
    {
        _fastStacks = 0;
        _slowStacks = 0;
        Recalculate();
    }

    void Recalculate()
    {
        float scale = 1f - _fastStacks * StackDelta + _slowStacks * StackDelta;
        CurrentScale = Mathf.Clamp(scale, MinScale, MaxScale);
        OnTempoChanged?.Invoke();
        BeatClock.Instance?.RefreshTempo();
    }
}
