using System;
using UnityEngine;

/// <summary>
/// PERFECT 연속 N회 → 피버(데미지 강화) 발동.
/// </summary>
public class FeverTimeController : MonoBehaviour
{
    public static FeverTimeController Instance { get; private set; }

    public const int RequiredPerfectStreak = 8;
    public const float FeverDurationSeconds = 6f;
    public const float DamageMultiplier = 1.5f;

    public float PerfectStreak { get; private set; }
    public float FeverRemaining { get; private set; }
    public bool IsFeverActive => FeverRemaining > 0f;

    public event Action<float, int> OnStreakChanged;
    public event Action OnFeverActivated;
    public event Action OnFeverEnded;

    RhythmCommandDetector _detector;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        _detector = GetComponent<RhythmCommandDetector>()
            ?? FindAnyObjectByType<RhythmCommandDetector>();

        if (_detector != null)
            _detector.OnCommandResolved += HandleCommandResolved;
    }

    void Update()
    {
        if (FeverRemaining <= 0f)
            return;

        FeverRemaining -= Time.deltaTime;
        if (FeverRemaining > 0f)
            return;

        FeverRemaining = 0f;
        OnFeverEnded?.Invoke();
    }

    void OnDestroy()
    {
        if (_detector != null)
            _detector.OnCommandResolved -= HandleCommandResolved;

        if (Instance == this)
            Instance = null;
    }

    public static float ApplyDamageMultiplier(float damage)
    {
        var fever = Instance;
        return fever != null && fever.IsFeverActive ? damage * DamageMultiplier : damage;
    }

    void HandleCommandResolved(CommandType type, JudgmentResult judgment)
    {
        switch (judgment)
        {
            case JudgmentResult.Perfect:
                AddComboProgress(1f);
                break;
            case JudgmentResult.Good:
                AddComboProgress(JudgmentRewards.GoodComboProgress);
                break;
            case JudgmentResult.Miss:
                ResetCombo();
                break;
        }
    }

    void AddComboProgress(float amount)
    {
        if (amount <= 0f)
            return;

        PerfectStreak += amount;
        OnStreakChanged?.Invoke(PerfectStreak, RequiredPerfectStreak);

        if (PerfectStreak >= RequiredPerfectStreak)
            ActivateFever();
    }

    void ResetCombo()
    {
        if (PerfectStreak <= 0f)
            return;

        PerfectStreak = 0f;
        OnStreakChanged?.Invoke(PerfectStreak, RequiredPerfectStreak);
    }

    void ActivateFever()
    {
        PerfectStreak = 0f;
        OnStreakChanged?.Invoke(PerfectStreak, RequiredPerfectStreak);

        FeverRemaining = FeverDurationSeconds;
        CombatVfxService.Instance?.PlayFeverActivated();
        OnFeverActivated?.Invoke();

        Debug.Log($"[Fever] FEVER TIME — {FeverDurationSeconds}s DMG x{DamageMultiplier:0.#}");
    }
}

