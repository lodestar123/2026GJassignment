using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public float maxHp = 4f;
    public EnemyKind kind = EnemyKind.EighthNote;
    public int goldReward = 6;
    public int coreDamage = 1;

    public float CurrentHp { get; private set; }
    public bool IsAlive => CurrentHp > 0f;

    public event Action<EnemyHealth> OnDied;

    void Awake()
    {
        CurrentHp = maxHp;
    }

    public void Configure(EnemyKind enemyKind)
    {
        kind = enemyKind;
        switch (enemyKind)
        {
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

    public void TakeDamage(float amount)
    {
        if (!IsAlive || amount <= 0f)
            return;

        CurrentHp = Mathf.Max(0f, CurrentHp - amount);
        CombatVfxService.Instance?.ReportDamage(this, amount);

        if (!IsAlive)
        {
            CombatVfxService.Instance?.ReportDeath(this);
            OnDied?.Invoke(this);
        }
    }
}
