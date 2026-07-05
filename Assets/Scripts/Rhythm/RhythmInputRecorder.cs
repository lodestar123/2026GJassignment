using System;
using UnityEngine;

/// <summary>
/// Space 등 리듬 키 입력 시각 기록 · 패턴 진행(tap count) 표시용.
/// </summary>
public class RhythmInputRecorder : MonoBehaviour
{
    public static RhythmInputRecorder Instance { get; private set; }

    public float LastSpaceTime { get; private set; } = -999f;
    public int CurrentTapCount => _detector != null ? _detector.CurrentTapCount : 0;

    RhythmCommandDetector _detector;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _detector = GetComponent<RhythmCommandDetector>();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void RecordTap(float time)
    {
        LastSpaceTime = time;
    }

    public bool HasRecentInput(float windowSeconds = 1.2f)
    {
        return Time.time - LastSpaceTime <= windowSeconds;
    }
}
