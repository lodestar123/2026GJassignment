using System;
using UnityEngine;

/// <summary>
/// 엘리트 웨이브 — 양쪽 동시 1마리씩. 시각은 120초 기준을 매치 길이에 비례 스케일.
/// </summary>
public class EliteSpawnDirector : MonoBehaviour
{
    const float ReferenceMatchSeconds = 120f;

    public static float Wave60Seconds =>
        GameManager.MatchDurationSeconds * (60f / ReferenceMatchSeconds);

    public static float Wave90Seconds =>
        GameManager.MatchDurationSeconds * (90f / ReferenceMatchSeconds);

    public static event Action<EliteTier> OnWaveStarted;

    [SerializeField] ContinuousSpawner spawner;

    bool _spawned60;
    bool _spawned90;

    void Awake()
    {
        if (spawner == null)
            spawner = GetComponent<ContinuousSpawner>();
    }

    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsRunning)
            return;

        float elapsed = GameManager.Instance.ElapsedSeconds;

        if (!_spawned60 && elapsed >= Wave60Seconds)
        {
            _spawned60 = true;
            TriggerWave(EliteTier.Wave60);
        }

        if (!_spawned90 && elapsed >= Wave90Seconds)
        {
            _spawned90 = true;
            TriggerWave(EliteTier.Wave90);
        }
    }

    void TriggerWave(EliteTier tier)
    {
        if (spawner == null)
            spawner = FindAnyObjectByType<ContinuousSpawner>();

        spawner?.SpawnEliteWave(tier);
        OnWaveStarted?.Invoke(tier);
        Debug.Log($"[Elite] {tier} wave — S1+S2 동시 스폰");
    }
}
