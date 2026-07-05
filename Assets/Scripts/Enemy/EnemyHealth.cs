using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public float maxHp = 4f;
    public EnemyKind kind = EnemyKind.EighthNote;
    public EliteTier eliteTier = EliteTier.None;
    public int goldReward = 6;
    public int coreDamage = 1;

    public float CurrentHp { get; private set; }
    public bool IsAlive => CurrentHp > 0f;

    public event Action<EnemyHealth> OnDied;

    void Awake()
    {
        CurrentHp = maxHp;
    }

    public void Configure(EnemyKind enemyKind, EliteTier tier = EliteTier.None)
    {
        kind = enemyKind;
        eliteTier = tier;

        switch (enemyKind)
        {
            case EnemyKind.Elite:
                ApplyEliteStats(tier);
                break;
            case EnemyKind.Downbeat:
                maxHp = EnemyBalance.DownbeatHp;
                goldReward = EnemyBalance.DownbeatGold;
                coreDamage = EnemyBalance.DownbeatCoreDamage;
                break;
            default:
                maxHp = EnemyBalance.EighthNoteHp;
                goldReward = EnemyBalance.EighthNoteGold;
                coreDamage = EnemyBalance.EighthNoteCoreDamage;
                break;
        }

        CurrentHp = maxHp;
    }

    void ApplyEliteStats(EliteTier tier)
    {
        if (tier == EliteTier.Wave90)
        {
            maxHp = EnemyBalance.EliteWave90Hp;
            goldReward = EnemyBalance.EliteWave90Gold;
            coreDamage = EnemyBalance.EliteWave90CoreDamage;
            return;
        }

        maxHp = EnemyBalance.EliteWave60Hp;
        goldReward = EnemyBalance.EliteWave60Gold;
        coreDamage = EnemyBalance.EliteWave60CoreDamage;
    }

    public void Heal(float amount)
    {
        if (!IsAlive || amount <= 0f)
            return;

        CurrentHp = Mathf.Min(maxHp, CurrentHp + amount);
    }

    public void TakeDamage(float amount)
    {
        if (!IsAlive || amount <= 0f)
            return;

        var elite = GetComponent<EliteEnemyBehavior>();
        if (elite != null)
            amount = elite.ModifyIncomingDamage(amount);

        amount = FeverTimeController.ApplyDamageMultiplier(amount);

        CurrentHp = Mathf.Max(0f, CurrentHp - amount);
        CombatVfxService.Instance?.ReportDamage(this, amount);

        if (!IsAlive)
        {
            CombatVfxService.Instance?.ReportDeath(this);
            OnDied?.Invoke(this);
        }
    }
}
