using UnityEngine;

/// <summary>
/// Perfect / Good / Miss 판정별 보상·페널티 상수.
/// </summary>
public static class JudgmentRewards
{
    /// <summary>Perfect GoldPulse — 기본 대비 +100%.</summary>
    public const float PerfectGoldMultiplier = 2f;

    /// <summary>Perfect RhythmShot — ActiveDamage 배율 (미세 상향).</summary>
    public const float PerfectRhythmShotDamageMultiplier = 1.15f;

    /// <summary>Good — 피버 콤보 진행도.</summary>
    public const float GoodComboProgress = 0.5f;

    /// <summary>Miss — 입력 무효(스턴) 시간.</summary>
    public const float MissInputStunSeconds = 0.35f;

    public static int ScaleGoldPulseReward(JudgmentResult judgment)
    {
        int baseGold = ResourceManager.GoldPulseReward;
        return judgment == JudgmentResult.Perfect
            ? Mathf.RoundToInt(baseGold * PerfectGoldMultiplier)
            : baseGold;
    }

    public static float ScaleRhythmShotDamage(float activeDamage, JudgmentResult judgment)
    {
        return judgment == JudgmentResult.Perfect
            ? activeDamage * PerfectRhythmShotDamageMultiplier
            : activeDamage;
    }
}
