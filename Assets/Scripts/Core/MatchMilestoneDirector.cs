using System;
using UnityEngine;

/// <summary>
/// 매치 마일스톤 — UI 알림 + Last Stand 구간 스폰 가속.
/// 시각은 120초 기준 설계를 MatchDurationSeconds에 비례해 스케일.
/// </summary>
public class MatchMilestoneDirector : MonoBehaviour
{
    const float ReferenceMatchSeconds = 120f;

    public static float PressureRisingSeconds =>
        GameManager.MatchDurationSeconds * (30f / ReferenceMatchSeconds);

    public const float EliteWarningLeadSeconds = 3f;

    public static float LastStandSeconds =>
        GameManager.MatchDurationSeconds * (100f / ReferenceMatchSeconds);

    public static float FinalPushSeconds =>
        GameManager.MatchDurationSeconds * (110f / ReferenceMatchSeconds);

    public const float LastStandSpawnScale = 0.65f;
    public const float FinalPushSpawnScale = 0.45f;

    public static event Action<MatchMilestoneKind> OnMilestoneReached;

    [SerializeField] ContinuousSpawner spawner;

    bool _fired30;
    bool _firedWarn60;
    bool _firedWarn90;
    bool _fired100;
    bool _fired110;

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

        TryReach(MatchMilestoneKind.PressureRising30, ref _fired30, elapsed >= PressureRisingSeconds);
        TryReach(
            MatchMilestoneKind.EliteWarning60,
            ref _firedWarn60,
            elapsed >= EliteSpawnDirector.Wave60Seconds - EliteWarningLeadSeconds);
        TryReach(
            MatchMilestoneKind.EliteWarning90,
            ref _firedWarn90,
            elapsed >= EliteSpawnDirector.Wave90Seconds - EliteWarningLeadSeconds);
        TryReach(
            MatchMilestoneKind.LastStand100,
            ref _fired100,
            elapsed >= LastStandSeconds,
            () => spawner?.ApplySpawnPressure(LastStandSpawnScale));
        TryReach(
            MatchMilestoneKind.FinalPush110,
            ref _fired110,
            elapsed >= FinalPushSeconds,
            () => spawner?.ApplySpawnPressure(FinalPushSpawnScale));
    }

    void TryReach(MatchMilestoneKind kind, ref bool fired, bool condition, Action effect = null)
    {
        if (fired || !condition)
            return;

        fired = true;
        effect?.Invoke();
        OnMilestoneReached?.Invoke(kind);
        Debug.Log($"[Milestone] {kind}");
    }
}
