using System;
using UnityEngine;

/// <summary>
/// 본진(Core) HP — BALANCE §7 · 승리 시 HP &gt; 0.
/// </summary>
public class BaseHealth : MonoBehaviour
{
    public static BaseHealth Instance { get; private set; }

    public const int DefaultMaxHp = 3;

    [SerializeField] int maxHp = DefaultMaxHp;

    public int MaxHp => maxHp;
    public int CurrentHp { get; private set; }
    public bool IsAlive => CurrentHp > 0;

    public event Action<int, int> OnHpChanged;
    public event Action OnDestroyed;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        CurrentHp = maxHp;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void TakeDamage(int amount)
    {
        if (!IsAlive || amount <= 0)
            return;

        if (GameManager.Instance != null
            && GameManager.Instance.ElapsedSeconds >= GameManager.MatchDurationSeconds)
            return;

        CurrentHp = Mathf.Max(0, CurrentHp - amount);
        OnHpChanged?.Invoke(CurrentHp, maxHp);
        CombatVfxService.Instance?.PlayCoreHit(transform.position);

        if (!IsAlive)
            OnDestroyed?.Invoke();
    }

    public void ResetHealth()
    {
        CurrentHp = maxHp;
        OnHpChanged?.Invoke(CurrentHp, maxHp);
    }
}
