using System;
using UnityEngine;

public class RunStats : MonoBehaviour
{
    public static RunStats Instance { get; private set; }

    public int PerfectCount { get; private set; }
    public int GoodCount { get; private set; }
    public int MissCount { get; private set; }
    public int EighthNoteKills { get; private set; }
    public int DownbeatKills { get; private set; }

    public int EliteKills { get; private set; }

    public event Action OnStatsChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void RecordJudgment(JudgmentResult result)
    {
        switch (result)
        {
            case JudgmentResult.Perfect:
                PerfectCount++;
                break;
            case JudgmentResult.Good:
                GoodCount++;
                break;
            case JudgmentResult.Miss:
            case JudgmentResult.Cooldown:
            case JudgmentResult.NoTower:
                MissCount++;
                break;
        }

        OnStatsChanged?.Invoke();
    }

    public void RecordEnemyKill(EnemyKind kind, EliteTier eliteTier = EliteTier.None)
    {
        switch (kind)
        {
            case EnemyKind.Elite:
                EliteKills++;
                break;
            case EnemyKind.Downbeat:
                DownbeatKills++;
                break;
            default:
                EighthNoteKills++;
                break;
        }

        OnStatsChanged?.Invoke();
    }
}
